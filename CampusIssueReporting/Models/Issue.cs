using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusIssueReporting.Models
{
    public class Issue
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        [Required]
        public string ReporterId { get; set; } = null!;
        public ApplicationUser Reporter { get; set; } = null!;

        public string? AssignedAdminId { get; set; }
        [ForeignKey(nameof(AssignedAdminId))]
        public ApplicationUser? AssignedAdmin { get; set; }

        public int? CategoryId { get; set; }
        public IssueCategory? Category { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
