using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace SchoolManagement.Service;

// A short-lived, signed link proves the recipient can open the mailbox.
public static class EmailVerificationLinks
{
    private sealed record Claim(string Kind, int Id, int SchoolId, string Email, long ExpiresAt);

    private static byte[] SigningKey(IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey)) throw new InvalidOperationException("Verification signing key is not configured.");
        return SHA256.HashData(Encoding.UTF8.GetBytes("school-email-verification-v1:" + jwtKey));
    }

    public static string Create(IConfiguration configuration, HttpRequest request, string kind, int id, int schoolId, string email)
    {
        var payload = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new Claim(kind, id, schoolId, email.Trim().ToLowerInvariant(), DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds())));
        using var hmac = new HMACSHA256(SigningKey(configuration));
        var signature = WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        var configuredBase = configuration["EmailSettings:VerificationBaseUrl"]?.TrimEnd('/');
        var origin = !string.IsNullOrWhiteSpace(configuredBase)
            ? configuredBase
            : $"{request.Scheme}://{request.Host}{request.PathBase}";
        return $"{origin}/api/email-verification/verify?token={Uri.EscapeDataString(payload + "." + signature)}";
    }

    public static string Clickable(string body, string link)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(link);
        return body.Replace(encoded, $"<a href=\"{encoded}\">{encoded}</a>");
    }
    public static bool TryRead(IConfiguration configuration, string? token, out (string Kind, int Id, int SchoolId, string Email) result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return false;
        var parts = token.Split('.');
        if (parts.Length != 2) return false;
        try
        {
            using var hmac = new HMACSHA256(SigningKey(configuration));
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0]));
            var actual = WebEncoders.Base64UrlDecode(parts[1]);
            if (actual.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(actual, expected)) return false;
            var claim = JsonSerializer.Deserialize<Claim>(WebEncoders.Base64UrlDecode(parts[0]));
            if (claim == null || claim.Id <= 0 || claim.SchoolId <= 0 ||
                claim.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
                !new[] { "Staff", "Student", "Parent" }.Contains(claim.Kind) ||
                string.IsNullOrWhiteSpace(claim.Email)) return false;
            result = (claim.Kind, claim.Id, claim.SchoolId, claim.Email);
            return true;
        }
        catch { return false; }
    }
}
