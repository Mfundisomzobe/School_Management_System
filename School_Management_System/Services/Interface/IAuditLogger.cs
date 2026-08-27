using School_Management_System.Models;

namespace School_Management_System.Services.Interface
{
    public interface IAuditLogger
    {
        Task LogAsync(string action, string user, string details);
        Task LogWithUserAsync(string action, string userId, string fullName, string details);
        Task LogWithUserObjectAsync(string action, ApplicationUser user, string details);
        Task LogWithDetailsAsync(string action, string userId, string fullName, string details, string? ipAddress = null, string? userRole = null);

    }
}
