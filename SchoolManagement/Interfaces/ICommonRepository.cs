namespace SchoolManagement.Interfaces
{
    public interface ICommonRepository
    {
        string GeneratePassword(string name, DateTime dob);
    }
}
