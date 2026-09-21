using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SchoolManagement.DTOs
{
    public sealed class StaffAdditionalDetailsAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value == null) return ValidationResult.Success;
            try
            {
                var groups = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>((string)value);
                if (groups == null) return new ValidationResult("Invalid staff details.");
                foreach (var group in groups)
                {
                    if (group.Key != "Educational Details" && group.Key != "Experience Details" && group.Key != "Certification Details")
                        return new ValidationResult("Invalid staff detail section.");
                    if (group.Value == null) return new ValidationResult("Invalid staff records.");
                    for (var i = 0; i < group.Value.Count; i++)
                    {
                        var row = group.Value[i];
                        if (row == null) return new ValidationResult("Invalid staff record.");
                        string Get(string key) => row.TryGetValue(key, out var text) ? text ?? "" : "";
                        string prefix = group.Key + " " + (i + 2) + ": ";
                        foreach (var field in row)
                            if ((field.Value?.Length ?? 0) > (field.Key == "experienceDetails" ? 2000 : 200))
                                return new ValidationResult(prefix + "Text exceeds the allowed length.");
                        var year = Get("passingYear");
                        if (year != "" && (!int.TryParse(year, out var y) || y < 1900 || y > DateTime.Today.Year))
                            return new ValidationResult(prefix + "Enter a valid passing year.");
                        var years = Get("experienceYears");
                        if (years != "" && (!decimal.TryParse(years, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var n) || n < 0 || n > 80))
                            return new ValidationResult(prefix + "Experience must be between 0 and 80 years.");
                        foreach (var pair in new[] { new[] { "experienceFrom", "experienceTo" }, new[] { "certificationDate", "certificationExpiry" } })
                        {
                            var from = Get(pair[0]); var to = Get(pair[1]);
                            DateTime start = default, end = default;
                            if ((from != "" && !DateTime.TryParseExact(from, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out start)) ||
                                (to != "" && !DateTime.TryParseExact(to, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out end)))
                                return new ValidationResult(prefix + "Enter valid dates.");
                            if (to != "" && (from == "" || end < start)) return new ValidationResult(prefix + "End date must be on or after the start date.");
                        }
                    }
                }
                return ValidationResult.Success;
            }
            catch (JsonException) { return new ValidationResult("Invalid staff details."); }
        }
    }
}
