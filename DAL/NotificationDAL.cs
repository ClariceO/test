using Google.Cloud.Firestore;
using H4G_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace H4G_Project.DAL
{
    public class NotificationDAL
    {
        private readonly FirestoreDb db;

        public NotificationDAL()
        {
            db = new FirestoreDbBuilder
            {
                ProjectId = FirebaseHelper.GetProjectId(),
                JsonCredentials = FirebaseHelper.GetCredentialsJson()
            }.Build();
        }

        public async Task<bool> AddNotification(Notification notification)
        {
            try
            {
                notification.CreatedAt = Timestamp.GetCurrentTimestamp();
                await db.Collection("notifications").AddAsync(notification);
                Console.WriteLine($"Notification created for user: {notification.UserId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding notification: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Notification>> GetUserNotifications(string userEmail)
        {
            try
            {
                Console.WriteLine($"NotificationDAL: Getting notifications for '{userEmail}'");

                var allNotificationsQuery = db.Collection("notifications");
                var allSnapshot = await allNotificationsQuery.GetSnapshotAsync();

                Console.WriteLine($"NotificationDAL: Total notifications in database: {allSnapshot.Documents.Count}");

                foreach (var doc in allSnapshot.Documents.Take(3))
                {
                    var data = doc.ToDictionary();
                    Console.WriteLine($"NotificationDAL: Sample notification - ID: {doc.Id}");
                    foreach (var field in data)
                    {
                        Console.WriteLine($"  {field.Key}: {field.Value}");
                    }
                }

                var query = db.Collection("notifications")
                             .WhereEqualTo("UserId", userEmail)
                             .Limit(50);

                var snapshot = await query.GetSnapshotAsync();
                var notifications = new List<Notification>();

                Console.WriteLine($"NotificationDAL: Found {snapshot.Documents.Count} notification documents");

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var notification = doc.ConvertTo<Notification>();
                        notification.Id = doc.Id;

                        if (notification.CreatedAt != null)
                        {
                            var dateTime = notification.CreatedAt.ToDateTime();
                            var data = doc.ToDictionary();
                            data["createdAtMs"] = dateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                        }

                        notifications.Add(notification);
                        Console.WriteLine($"NotificationDAL: Added notification - {notification.Title}");
                    }
                }

                Console.WriteLine($"NotificationDAL: Returning {notifications.Count} notifications");
                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return new List<Notification>();
            }
        }

        public async Task<int> GetUnreadCount(string userEmail)
        {
            try
            {
                var query = db.Collection("notifications")
                             .WhereEqualTo("UserId", userEmail)
                             .WhereEqualTo("IsRead", false);

                var snapshot = await query.GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> MarkAsRead(string notificationId)
        {
            try
            {
                var docRef = db.Collection("notifications").Document(notificationId);
                await docRef.UpdateAsync("IsRead", true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAllAsRead(string userEmail)
        {
            try
            {
                var query = db.Collection("notifications")
                             .WhereEqualTo("UserId", userEmail)
                             .WhereEqualTo("IsRead", false);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    await doc.Reference.UpdateAsync("IsRead", true);
                }

                Console.WriteLine($"Marked {snapshot.Count} notifications as read for {userEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all notifications as read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateCommentNotifications(string eventId, string eventName, string staffUsername, string comment)
        {
            try
            {
                var eventsDAL = new EventsDAL();
                var registeredUsers = await eventsDAL.GetRegisteredUsers(eventId);

                if (!registeredUsers.Any())
                {
                    Console.WriteLine($"No registered users found for event {eventId}");
                    return true;
                }

                foreach (var user in registeredUsers)
                {
                    var notification = new Notification
                    {
                        UserId = user.Email,
                        Title = $"New comment on {eventName}",
                        Message = $"{staffUsername}: {(comment.Length > 100 ? comment.Substring(0, 100) + "..." : comment)}",
                        EventId = eventId,
                        EventName = eventName,
                        Type = "comment",
                        CreatedBy = staffUsername,
                        IsRead = false
                    };

                    await AddNotification(notification);
                }

                Console.WriteLine($"Created notifications for {registeredUsers.Count} users for event {eventName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating comment notifications: {ex.Message}");
                return false;
            }
        }
    }
}