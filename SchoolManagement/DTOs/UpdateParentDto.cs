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
    [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? AddressLine2 { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? Landmark { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? City { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? District { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? State { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? Country { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(6)] public string? PinCode { get; set; }
}
