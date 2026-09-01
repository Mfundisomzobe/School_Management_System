namespace School_Management_System.Services.Interface
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetLink, string userName);
    }
}
