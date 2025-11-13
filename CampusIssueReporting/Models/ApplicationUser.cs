using Microsoft.AspNetCore.Identity;
namespace CampusIssueReporting.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Issue> Issues { get; set; } = new List<Issue>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<FileRecord>? UploadedFiles { get; set; } = new List<FileRecord>();

    }
}