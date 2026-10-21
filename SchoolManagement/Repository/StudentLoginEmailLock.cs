using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Repository;

// Global email keys require locks shared across schools and API processes.
public static class StudentLoginEmailLock
{
    public static async Task AcquireAsync(AppDbContext context, params string[] emails)
    {
        foreach (var resource in emails
            .Select(email => "StudentLogin:" + Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant()))))
            .Distinct().OrderBy(value => value, StringComparer.Ordinal))
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={resource},
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
                IF @result < 0 THROW 51001, 'Another import is using this login email. Please retry this row.', 1;
                """);
        }
    }
}