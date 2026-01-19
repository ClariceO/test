using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace H4G_Project.Models
{
    [FirestoreData]
    public class Event
    {
        // Firestore document ID (not stored in Firestore)
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("eventID")]
        public int eventID { get; set; }

        [FirestoreProperty("name")]
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("details")]
        public string Details { get; set; } = string.Empty;

        [FirestoreProperty("eventPhoto")]
        public string eventPhoto { get; set; } = string.Empty;

        [FirestoreProperty("start")]
        [Required]
        public Timestamp Start { get; set; }

        [FirestoreProperty("end")]
        public Timestamp? End { get; set; }

        [FirestoreProperty("registrationDueDate")]
        [Required(ErrorMessage = "Registration due date is required")]
        public Timestamp RegistrationDueDate { get; set; }

        [FirestoreProperty("maxParticipants")]
        [Required(ErrorMessage = "Max participants is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Max participants must be at least 1")]
        public int MaxParticipants { get; set; }

        // unique for each event ;>
        [FirestoreProperty("qrCode")]
        public string QrCode { get; set; } = string.Empty;
    }
}
