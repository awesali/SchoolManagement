using Microsoft.AspNetCore.Http;

namespace SchoolManagement.DTOs
{
    public class StudentUpdateDto
    {
        public int Id { get; set; }
        public string? StudentName { get; set; }
        public DateTime? DOB { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? SessionId { get; set; }

        public ParentUpdateDto? Parent { get; set; }
        public List<int?>? DocumentIds { get; set; }
        public List<string>? DocumentNames { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    public class ParentUpdateDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Relationship { get; set; }
    }
}
