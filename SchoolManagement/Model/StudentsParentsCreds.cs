using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model
{
    public class Students_Parents_Creds
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password_Hash { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        [Required]
        public int School_Id { get; set; }

        public DateTime? Last_Login { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
