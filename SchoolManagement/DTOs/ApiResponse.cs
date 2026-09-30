// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the api response request and response data.
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
