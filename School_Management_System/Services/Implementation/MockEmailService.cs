using Microsoft.Extensions.Logging;
using School_Management_System.Services.Interface;

namespace School_Management_System.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendPasswordResetEmailAsync(string email, string resetLink, string userName)
        {
            // Log the email details - this is perfect for portfolio demonstration
            _logger.LogInformation("==========================================");
            _logger.LogInformation("📧 PASSWORD RESET EMAIL (DEMO MODE)");
            _logger.LogInformation("==========================================");
            _logger.LogInformation($"To: {email}");
            _logger.LogInformation($"User: {userName}");
            _logger.LogInformation($"Reset Link: {resetLink}");
            _logger.LogInformation("==========================================");
            _logger.LogInformation("⚠️ In production, this would send a real email.");
            _logger.LogInformation("==========================================");

            // For portfolio, you could also save to a file or database
            // This shows the recruiter you understand the flow

            return Task.CompletedTask;
        }
    }
}