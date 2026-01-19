using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace H4G_Project.Models
{
    public class CommentVM
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string ParentCommentId { get; set; } = string.Empty;
        public Timestamp Timestamp { get; set; }

        public List<CommentVM> Replies { get; set; } = new();
    }
}
