    // ViewModel for edit form
    namespace CampusIssueReporting.ViewModel;
    public class StudentViewModel
    {
        public string Id { get; set; } = default!;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        [System.ComponentModel.DataAnnotations.Display(Name = "User name")]
        public string? UserName { get; set; }

        [System.ComponentModel.DataAnnotations.EmailAddress]
        [System.ComponentModel.DataAnnotations.Display(Name = "Email")]
        public string? Email { get; set; }

        [System.ComponentModel.DataAnnotations.Phone]
        [System.ComponentModel.DataAnnotations.Display(Name = "Phone")]
        public string? PhoneNumber { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(200)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Full name")]
        public string? FullName { get; set; }
    }