using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CampusIssueReporting.Models;

public class FileRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedById { get; set; } = "";
    
    public ApplicationUser? UploadedBy { get; set; }
     public int? IssueId { get; set; }
    [ForeignKey(nameof(IssueId))]
    
    public Issue? Issue { get; set; }
}