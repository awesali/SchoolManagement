using System.ComponentModel.DataAnnotations;
namespace SchoolManagement.DTOs;
public class UpdateParentDto
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Range(1, int.MaxValue)] public int SchoolId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, StringLength(20)] public string PhoneNumber { get; set; } = "";
    [Required] public string Relationship { get; set; } = "";
    public string Address { get; set; } = "";
}
