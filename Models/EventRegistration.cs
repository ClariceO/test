using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace H4G_Project.Models
{
    [FirestoreData]
    public class EventRegistration
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("eventId")]
        [Required]
        public string EventId { get; set; } = string.Empty;

        [FirestoreProperty("eventName")]
        public string EventName { get; set; } = string.Empty;

        [FirestoreProperty("role")]
        [Required]
        public string Role { get; set; } = string.Empty; // "Volunteer" or "Participant"

        [FirestoreProperty("fullName")]
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("phoneNumber")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        // Volunteer-specific fields
        [FirestoreProperty("nricLast4")]
        public string NricLast4 { get; set; } = string.Empty;

        [FirestoreProperty("gender")]
        public string Gender { get; set; } = string.Empty;

        [FirestoreProperty("dateOfBirth")]
        public string DateOfBirth { get; set; } = string.Empty;

        [FirestoreProperty("citizenshipType")]
        public string CitizenshipType { get; set; } = string.Empty;

        // Legacy volunteer fields (keeping for backward compatibility)
        [FirestoreProperty("preferredRole")]
        public string PreferredRole { get; set; } = string.Empty;

        [FirestoreProperty("skills")]
        public string Skills { get; set; } = string.Empty;

        // Participant-specific fields
        [FirestoreProperty("dietaryRequirements")]
        public string DietaryRequirements { get; set; } = string.Empty;

        [FirestoreProperty("emergencyContact")]
        public string EmergencyContact { get; set; } = string.Empty;

        [FirestoreProperty("emergencyContactName")]
        public string EmergencyContactName { get; set; } = string.Empty;

        [FirestoreProperty("paymentStatus")]
        public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Completed"

        [FirestoreProperty("paymentAmount")]
        public double PaymentAmount { get; set; }

        [FirestoreProperty("qrCode")]
        public string QrCode { get; set; } = string.Empty;

        [FirestoreProperty("registrationDate")]
        public Timestamp RegistrationDate { get; set; }

        [FirestoreProperty("paymentDate")]
        public Timestamp? PaymentDate { get; set; }

        [FirestoreProperty("waitlistStatus")]
        public string WaitlistStatus { get; set; } = string.Empty;



        [FirestoreProperty("attendance")]
        public bool Attendance { get; set; } = false;

        // Status field for volunteer registration approval workflow
        [FirestoreProperty("status")]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Declined"

    }
}
