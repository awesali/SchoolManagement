namespace SchoolManagement.Model
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsActive { get; set; }
    }
}
