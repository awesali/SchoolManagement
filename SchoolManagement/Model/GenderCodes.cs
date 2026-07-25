namespace SchoolManagement.Model
{
    public static class GenderCodes
    {
        public const string Male = "M";
        public const string Female = "F";
        public const string Other = "O";
        public const string PreferNotToSay = "N";

        public static bool IsValid(string? value) =>
            value is Male or Female or Other or PreferNotToSay;
    }
}
