using School_Management_System.Models;

namespace School_Management_System.Services.Interface
{
    public interface IAuditLogger
    {
        Task LogAsync(string action, string fullName, string details);
        
    }
}
