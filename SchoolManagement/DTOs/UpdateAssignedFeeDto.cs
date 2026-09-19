using System.ComponentModel.DataAnnotations;
namespace SchoolManagement.DTOs;
public class UpdateAssignedFeeDto {
 [Range(1,int.MaxValue)] public int StudentFeeId { get; set; }
 [Range(1,int.MaxValue)] public int SchoolId { get; set; }
 [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Amount { get; set; }
}