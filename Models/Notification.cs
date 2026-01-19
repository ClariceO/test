using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace H4G_Project.Models
{
    [FirestoreData]
    public class Notification
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty]
        [Required]
        public string UserId { get; set; } = string.Empty; // User email who should receive the notification

        [FirestoreProperty]
        [Required]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty]
        [Required]
        public string Message { get; set; } = string.Empty;

        [FirestoreProperty]
        public string EventId { get; set; } = string.Empty; // Related event ID

        [FirestoreProperty]
        public string EventName { get; set; } = string.Empty; // Event name for easy display

        [FirestoreProperty]
        public string Type { get; set; } = "comment"; // notification type: comment, event_update, etc.

        [FirestoreProperty]
        public bool IsRead { get; set; } = false; // Whether user has read the notification

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty]
        public string CreatedBy { get; set; } = string.Empty; // Staff member who created the notification
    }
}