using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;
using DateTime = System.DateTime;

namespace H4G_Project.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("Username")]
        [Display(Name = "Username")]
        [Required]
        public string? Username { get; set; } = string.Empty;

        [FirestoreProperty("Email")]
        [Display(Name = "Email")]
        [Required]
        [EmailAddress]
        public string? Email { get; set; } = string.Empty;

        [FirestoreProperty("PhoneNumber")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; } = string.Empty;

        [FirestoreProperty("DateOfBirth")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public string? DateOfBirth { get; set; } = string.Empty;

        [FirestoreProperty("Role")]
        [Display(Name = "Role")]
        [Required]
        public string? Role { get; set; } = string.Empty;

        [FirestoreProperty("EngagementType")]
        [Display(Name = "Engagement Type")]
        public string? EngagementType { get; set; } = "Ad hoc engagement"; // Default engagement type

        [FirestoreProperty("LastDayOfService")]
        [Display(Name = "Last Day of Service")]
        public string? LastDayOfService { get; set; } = null;
    }
}