namespace CampusIssueReporting.Models;
public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = "";   // e.g. "Issue"
    public string EntityId { get; set; } = "";     // string to be flexible
    public string Action { get; set; } = "";       // e.g. "Create", "UpdateStatus"
    public string? UserId { get; set; }            // nullable for system actions
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }           // optional JSON or text
}
