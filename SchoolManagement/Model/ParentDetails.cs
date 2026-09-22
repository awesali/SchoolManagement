namespace SchoolManagement.Model
{
    public class ParentDetails
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? AddressLine2 { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? Landmark { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? City { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? District { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? State { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(100)] public string? Country { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(6)] public string? PinCode { get; set; }
        public string Email { get; set; }
        public string Relationship { get; set; } // Father, Mother, Guardian

        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime? Modified_Date { get; set; }

        public int Created_By { get; set; }
        public int? Updated_By { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
