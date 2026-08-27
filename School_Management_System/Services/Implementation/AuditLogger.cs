using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services.Interface;

namespace School_Management_System.Services.Implementation
{
    // FIX: Add : IAuditLogger
    public class AuditLogger : IAuditLogger
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

        public async Task LogWithUserAsync(string action, string userId, string fullName, string details)
        {
            var log = new AuditLog
            {
                Action = action,
                UserId = userId,
                FullName = fullName,
                Details = details,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogWithUserObjectAsync(string action, ApplicationUser user, string details)
        {
            var log = new AuditLog
            {
                Action = action,
                UserId = user?.Id,
                FullName = user?.FullName,
                Details = details,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogWithDetailsAsync(string action, string userId, string fullName, string details, string? ipAddress = null, string? userRole = null)
        {
            var log = new AuditLog
            {
                Action = action,
                UserId = userId,
                FullName = fullName,
                Details = details,
                IpAddress = ipAddress,
                UserRole = userRole,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}