using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using DateTime = System.DateTime;

namespace H4G_Project.Models
{
    [FirestoreData]
    public class Staff
    {
        [FirestoreProperty]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Required(ErrorMessage = "Please input a name!")]
        public string? Username { get; set; } = string.Empty;

        [FirestoreProperty]
        [Display(Name = "Email")]
        [RegularExpression(@"^.+@.+\..+$", ErrorMessage = "Invalid Email")]
        [StringLength(50, ErrorMessage = "Email address cannot exceed 50 characters")]
        [Required(ErrorMessage = "Please input an email address!")]
        [EmailAddress]
        public string? Email { get; set; } = string.Empty;

        [FirestoreProperty]
        [Display(Name = "LastDayOfService")]
        public string? LastDayOfService { get; set; } = null;

    }
}
