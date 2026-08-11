using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace School_Management_System.Services.Implementations
{
    public class ParentCodeService : IParentCodeService
    {
        private readonly ApplicationDbContext _context;

        public ParentCodeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public (string RawCode, string HashedCode, string Salt) GenerateAndHashCode()
        {
            // Generate a 6-character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var code = new StringBuilder(6);
            var random = new Random();

            for (int i = 0; i < 6; i++)
            {
                code.Append(chars[random.Next(chars.Length)]);
            }

            var rawCode = code.ToString();
            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var hashedCode = HashCode(rawCode, salt);

            return (rawCode, hashedCode, salt);
        }

        public async Task<Student?> ValidateCodeAgainstStudentAsync(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode))
                return null;

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => !s.IsParentLinked)
                .ToListAsync();

            foreach (var student in students)
            {
                if (VerifyCode(rawCode, student.ParentCodeHash, student.ParentCodeSalt))
                {
                    return student;
                }
            }

            return null;
        }

        private bool VerifyCode(string rawCode, string hashedCode, string salt)
        {
            var computedHash = HashCode(rawCode, salt);
            return computedHash == hashedCode;
        }

        private string HashCode(string code, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var codeBytes = Encoding.UTF8.GetBytes(code);

            using var pbkdf2 = new Rfc2898DeriveBytes(codeBytes, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(hashBytes);
        }
    }
}
