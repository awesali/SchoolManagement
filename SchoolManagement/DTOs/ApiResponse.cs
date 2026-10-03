// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the api response request and response data.
    public class OneTimeCredentials { public string Username { get; set; } = ""; public string Password { get; set; } = ""; public string? ParentUsername { get; set; } public string? ParentPassword { get; set; } }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public OneTimeCredentials? OneTimeCredentials { get; set; }
    }
}
