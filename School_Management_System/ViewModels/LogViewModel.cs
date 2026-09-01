using School_Management_System.Models;

namespace School_Management_System.ViewModels
{
    public class LogViewModel
    {
        public List<AuditLog> Logs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
