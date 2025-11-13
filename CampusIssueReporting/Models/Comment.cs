namespace CampusIssueReporting.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int IssueId { get; set; }
        public Issue Issue { get; set; } = null!;

        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
