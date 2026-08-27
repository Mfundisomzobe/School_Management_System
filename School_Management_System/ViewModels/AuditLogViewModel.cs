using School_Management_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.ViewModels
{
    public class AuditLogViewModel
    {

        public List<AuditLog> Logs { get; set; } = new List<AuditLog>();

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }

        public string? FilterAction { get; set; }

        public DateTime? FilterDate { get; set; }

        public string? FilterUser { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

