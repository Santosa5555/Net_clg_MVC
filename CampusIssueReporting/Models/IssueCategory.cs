using System.ComponentModel.DataAnnotations;

namespace CampusIssueReporting.Models
{
    public class IssueCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Issue>? Issues { get; set; }
    }
}
