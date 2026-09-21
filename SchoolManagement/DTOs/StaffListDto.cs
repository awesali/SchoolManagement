namespace SchoolManagement.DTOs
{
    public class StaffListDto
    {
        public int Id { get; set; }
        public int EmployeeNumber { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DOB { get; set; }
        public string? GenderCode { get; set; }
        public DateTime DOJ { get; set; }

        public int RoleId { get; set; }
        public string RoleName { get; set; }

        public string SchoolName { get; set; }
        public string? Address { get; set; }

        public string? AdditionalDetails { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? AddressLine2 { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Landmark { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? City { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? District { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? State { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Country { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(6)]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^[1-9]\d{5}$", ErrorMessage = "Enter a valid 6-digit PIN code.")]
        public string? PinCode { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string? Qualification { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string? Specialization { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Institute { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? University { get; set; }
        public int? PassingYear { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Grade { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? PreviousEmployer { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string? PreviousDesignation { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, 80)]
        public decimal? ExperienceYears { get; set; }
        public DateTime? ExperienceFrom { get; set; }
        public DateTime? ExperienceTo { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(2000)]
        public string? ExperienceDetails { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? CertificationName { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? CertificationIssuer { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? CertificationNumber { get; set; }
        public DateTime? CertificationDate { get; set; }
        public DateTime? CertificationExpiry { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public List<StaffDocumentDto> Documents { get; set; }
    }

    public class StaffDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentURL { get; set; }
    }
}
