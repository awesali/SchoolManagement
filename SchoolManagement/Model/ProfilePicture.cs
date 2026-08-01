namespace SchoolManagement.Model
{
    public class ProfilePicture
    {
        public int Id { get; set; }
        public string PersonType { get; set; }
        public int PersonId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
