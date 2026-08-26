using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services.Implementation
{
    public class AuditLogger
    {
        private readonly ApplicationDbContext _context;

        public AuditLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string fullName, string details)
        {
            var log = new AuditLog
            {
                Action = action,
                FullName = fullName,
                Details = details,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        
    }
}
