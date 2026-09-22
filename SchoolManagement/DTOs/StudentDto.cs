namespace SchoolManagement.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int? EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public DateTime DOB { get; set; }
        public string? GenderCode { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? Address { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? AddressLine2 { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? Landmark { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? City { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? District { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? State { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? Country { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(6)] public string? PinCode { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)] public string? AdmissionType { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? PreviousSchoolName { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? PreviousSchoolAddress { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(50)] public string? PreviousClass { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? PreviousBoard { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? TransferCertificateNumber { get; set; }
        public DateTime? TransferCertificateDate { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? ReasonForLeaving { get; set; }
        public int ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? ParentRelationship { get; set; }
        public int SchoolId { get; set; }
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? SessionId { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public string RollNumber  { get; set; }
        public DateTime? AcademicSession { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public List<StudentDocumentDto> Documents { get; set; }
    }

    public class StudentDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentURL { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
