using School_Management_System.Models;

namespace School_Management_System.Services.Interfaces
{
    public interface IParentCodeService
    {
        (string RawCode, string HashedCode, string Salt) GenerateAndHashCode();
        Task<Student?> ValidateCodeAgainstStudentAsync(string rawCode);
    }
}
