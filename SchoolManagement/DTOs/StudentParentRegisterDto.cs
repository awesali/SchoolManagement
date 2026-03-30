using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.DTOs
{
    public class StudentParentRegisterDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        [Required]
        public int School_Id { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }
    }

    public class StudentParentLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
