using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using H4G_Project.Models;

namespace H4G_Project.DAL
{
    public class EventsDAL
    {
        private readonly FirestoreDb db;

        public EventsDAL()
        {
            string jsonPath = "./DAL/config/squad-60b0b-firebase-adminsdk-fbsvc-cff3f594d5.json";
            string projectId = "squad-60b0b";

            using StreamReader r = new StreamReader(jsonPath);
            string json = r.ReadToEnd();

            db = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                JsonCredentials = json
            }.Build();
        }

        // 🔹 Get all event registrations (FOR REPORTS)
        public async Task<List<EventRegistration>> GetAllRegistrations()
        {
            CollectionReference regRef = db.Collection("eventRegistrations");
            QuerySnapshot snapshot = await regRef.GetSnapshotAsync();

            List<EventRegistration> registrations = new();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    EventRegistration reg = doc.ConvertTo<EventRegistration>();
                    reg.Id = doc.Id;
                    registrations.Add(reg);
                }
            }

            return registrations;
        }


        // 🔹 Get all events (for FullCalendar)
        public async Task<List<Event>> GetAllEvents()
        {
            CollectionReference eventsRef = db.Collection("events");
            QuerySnapshot snapshot = await eventsRef.GetSnapshotAsync();

            List<Event> events = new List<Event>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    Event ev = doc.ConvertTo<Event>();
                    ev.Id = doc.Id;
                    events.Add(ev);
                }
            }

            return events;
        }

        /*public async Task<List<Event>> GetEventsByUserEmail()
        {
            CollectionReference eventsRef = db.Collection("events");
            QuerySnapshot snapshot = await eventsRef.GetSnapshotAsync();

            List<Event> events = new List<Event>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    Event ev = doc.ConvertTo<Event>();
                    ev.Id = doc.Id;
                    events.Add(ev);
                }
            }

            return events;
        }*/

        public async Task<List<Event>> GetEventsByUserEmail(string userEmail)
        {
            // 1. Get all registrations for this user
            CollectionReference regRef = db.Collection("eventRegistrations");
            QuerySnapshot regSnapshot = await regRef.WhereEqualTo("email", userEmail).GetSnapshotAsync();

            List<string> registeredEventNames = new();
            foreach (var doc in regSnapshot.Documents)
            {
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();
                    if (data.ContainsKey("eventName"))
                        registeredEventNames.Add(data["eventName"].ToString());
                }
            }

            // 2. Get all events
            CollectionReference eventsRef = db.Collection("events");
            QuerySnapshot eventsSnapshot = await eventsRef.GetSnapshotAsync();

            List<Event> events = new();
            foreach (var doc in eventsSnapshot.Documents)
            {
                if (doc.Exists)
                {
                    Event ev = doc.ConvertTo<Event>();
                    ev.Id = doc.Id;

                    // Only include events the user registered for
                    if (registeredEventNames.Contains(ev.Name))
                        events.Add(ev);
                }
            }

            // Sort by Start date ascending
            events.Sort((a, b) => a.Start.ToDateTime().CompareTo(b.Start.ToDateTime()));

            return events;
        }


        /*
        public async Task<Event> ExtractEventByID(string eid)
        {
            CollectionReference eventRef = db.Collection("events");
            QuerySnapshot snapshot = await eventRef.GetSnapshotAsync(); //Once connected to the database, this calls out specifally for the documents inside the Events collection
            Event @event = new Event();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (document.Id == eid)
                {
                    int eventId;
                    if (int.TryParse(document.Id, out eventId))
                    {
                        @event.eventID = eventId;

                    }

                    Dictionary<string, dynamic> documentDictionary = document.ToDictionary();

                    @event.name = documentDictionary["name"].ToString();
                    @event.Details = documentDictionary["details"].ToString();
                    @event.eventPhoto = documentDictionary["eventPhoto"].ToString();

                }
            }
        }*/

        // 🔹 Add event
        public async Task<bool> AddEvent(Event ev)
        {
            try
            {
                await db.Collection("events").AddAsync(ev);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding event: {ex.Message}");
                return false;
            }
        }


        // 🔹 Update event
        public async Task<bool> UpdateEvent(Event ev)
        {
            try
            {
                DocumentReference docRef = db.Collection("events").Document(ev.Id);

                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "name", ev.Name },
                    { "start", ev.Start },
                    { "end", ev.End }
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                return false;
            }
        }

        // 🔹 Delete event
        public async Task<bool> DeleteEvent(string eventId)
        {
            try
            {
                DocumentReference docRef = db.Collection("events").Document(eventId);
                await docRef.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting event: {ex.Message}");
                return false;
            }
        }

        // 🔹 Add event registration
        public async Task<string> AddRegistration(EventRegistration registration)
        {
            try
            {
                DocumentReference docRef = await db.Collection("eventRegistrations").AddAsync(registration);
                return docRef.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding registration: {ex.Message}");
                return null;
            }
        }

        // For Attendance
        public async Task<List<EventRegistration>> GetRegistrationsByEventId(string eventId)
        {
            var snapshot = await db.Collection("eventRegistrations")
                                   .WhereEqualTo("eventId", eventId)
                                   .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<EventRegistration>()).ToList();
        }

        // Get registered users for notifications
        public async Task<List<EventRegistration>> GetRegisteredUsers(string eventId)
        {
            return await GetRegistrationsByEventId(eventId);
        }

        public async Task<Event?> GetEventById(string id)
        {
            var doc = await db.Collection("events").Document(id).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<Event>() : null;
        }

        public async Task<bool> UpdateRegistration(EventRegistration registration)
        {
            try
            {
                var docRef = db.Collection("eventRegistrations").Document(registration.Id);
                var updates = new Dictionary<string, object>
                {
                    { "attendance", registration.Attendance }
                };
                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating registration: {ex.Message}");
                return false;
            }
        }

        // Update attendance for a specific registration
        public async Task<bool> UpdateAttendance(string registrationId, bool attendance)
        {
            try
            {
                var docRef = db.Collection("eventRegistrations").Document(registrationId);
                var updates = new Dictionary<string, object>
                {
                    { "attendance", attendance }
                };
                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating attendance: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateEventQrCode(string eventId, string qrCode)
        {
            try
            {
                DocumentReference docRef = db.Collection("events").Document(eventId);

                // Use SetAsync with merge option instead of UpdateAsync
                await docRef.SetAsync(new { qrCode = qrCode }, SetOptions.MergeAll);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // 🔹 Get registration by ID
        public async Task<EventRegistration> GetRegistrationById(string registrationId)
        {
            try
            {
                DocumentReference docRef = db.Collection("eventRegistrations").Document(registrationId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    EventRegistration registration = snapshot.ConvertTo<EventRegistration>();
                    registration.Id = snapshot.Id;
                    return registration;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting registration: {ex.Message}");
                return null;
            }
        }

        // 🔹 Get user's confirmed registrations for current week (based on event dates, not registration dates)
        public async Task<int> GetUserWeeklyRegistrationCount(string userEmail)
        {
            try
            {
                // Get start and end of current week (Monday to Sunday)
                DateTime today = DateTime.Now;
                int daysFromMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
                DateTime weekStart = today.AddDays(-daysFromMonday).Date;
                DateTime weekEnd = weekStart.AddDays(7).Date;

                // Get all user's confirmed registrations
                QuerySnapshot snapshot = await db.Collection("eventRegistrations")
                    .WhereEqualTo("email", userEmail)
                    .WhereEqualTo("role", "Participant")
                    .WhereIn("waitlistStatus", new[] { "Confirmed" })
                    .GetSnapshotAsync();

                // Get all events to check their dates
                var allEvents = await GetAllEvents();
                var eventDict = allEvents.ToDictionary(e => e.Id, e => e);

                int weeklyEventCount = 0;

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var registration = doc.ConvertTo<EventRegistration>();
                        
                        // Check if the event for this registration occurs in the current week
                        if (eventDict.ContainsKey(registration.EventId))
                        {
                            var eventObj = eventDict[registration.EventId];
                            DateTime eventDate = eventObj.Start.ToDateTime().Date;
                            
                            // Check if event date falls within current week
                            if (eventDate >= weekStart && eventDate < weekEnd)
                            {
                                weeklyEventCount++;
                            }
                        }
                    }
                }

                return weeklyEventCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting weekly registration count: {ex.Message}");
                return 0;
            }
        }

        // 🔹 Get engagement limit based on engagement type
        public static int GetEngagementWeeklyLimit(string engagementType)
        {
            return engagementType switch
            {
                "Ad hoc engagement" => 1,
                "Once a week engagement" => 1,
                "Twice a week engagement" => 2,
                "3 or more times a week engagement" => int.MaxValue, // No limit
                _ => 1 // Default to 1 for unknown types
            };
        }

        // 🔹 Check if user can register for more events this week (including the event they're trying to register for)
        public async Task<(bool canRegister, int currentCount, int limit, string message)> CheckUserEngagementLimit(string userEmail, string engagementType, string eventIdToRegister = null)
        {
            try
            {
                int currentCount = await GetUserWeeklyRegistrationCount(userEmail);
                
                // If we're checking for a specific event registration, we need to see if this would exceed the limit
                int projectedCount = currentCount;
                if (!string.IsNullOrEmpty(eventIdToRegister))
                {
                    // Check if the event they're trying to register for is in the current week
                    var allEvents = await GetAllEvents();
                    var eventToRegister = allEvents.FirstOrDefault(e => e.Id == eventIdToRegister);
                    
                    if (eventToRegister != null)
                    {
                        DateTime today = DateTime.Now;
                        int daysFromMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
                        DateTime weekStart = today.AddDays(-daysFromMonday).Date;
                        DateTime weekEnd = weekStart.AddDays(7).Date;
                        
                        DateTime eventDate = eventToRegister.Start.ToDateTime().Date;
                        
                        // If the event is in the current week, add 1 to the projected count
                        if (eventDate >= weekStart && eventDate < weekEnd)
                        {
                            projectedCount = currentCount + 1;
                        }
                    }
                }

                int limit = GetEngagementWeeklyLimit(engagementType);
                bool canRegister = projectedCount <= limit;
                
                string message;
                if (limit == int.MaxValue)
                {
                    // Unlimited engagement
                    message = $"You have unlimited event access. Current registrations this week: {currentCount}.";
                    canRegister = true; // Always allow registration for unlimited users
                }
                else
                {
                    // Limited engagement
                    if (!string.IsNullOrEmpty(eventIdToRegister))
                    {
                        // Checking for a specific registration
                        message = canRegister 
                            ? $"You have used {currentCount} of {limit} events this week. This registration would bring you to {projectedCount}."
                            : $"You have reached your weekly limit of {limit} events. Current registrations: {currentCount}. This registration would exceed your limit.";
                    }
                    else
                    {
                        // General check
                        message = canRegister 
                            ? $"You have used {currentCount} of {limit} events this week."
                            : $"You have reached your weekly limit of {limit} events. Current registrations: {currentCount}.";
                    }
                }

                return (canRegister, currentCount, limit, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking engagement limit: {ex.Message}");
                return (false, 0, 1, "Error checking engagement limit.");
            }
        }

        // 🔹 Count confirmed registrations for an event
        public async Task<int> CountConfirmedRegistrations(string eventId)
        {
            try
            {
                QuerySnapshot snapshot = await db.Collection("eventRegistrations")
                                                 .WhereEqualTo("eventId", eventId)
                                                 .WhereEqualTo("waitlistStatus", "Confirmed")
                                                 .GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting confirmed registrations: {ex.Message}");
                return 0;
            }
        }


        // 🔹 Update payment status
        public async Task<bool> UpdatePaymentStatus(string registrationId, string paymentStatus, string qrCode = null)
        {
            try
            {
                DocumentReference docRef = db.Collection("eventRegistrations").Document(registrationId);

                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "paymentStatus", paymentStatus },
                    { "paymentDate", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                if (!string.IsNullOrEmpty(qrCode))
                {
                    updates["qrCode"] = qrCode;
                }

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating payment status: {ex.Message}");
                return false;
            }
        }


        // Add a comment OR reply to an event
        public async Task<bool> AddComment(string eventId, string username, string email, string comment, string role, string parentId)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "username", username },
                    { "email", email },
                    { "comment", comment },
                    { "role", role },
                    { "parentId", string.IsNullOrEmpty(parentId) ? null : parentId },
                    { "timestamp", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                await db.Collection("events")
                        .Document(eventId)
                        .Collection("comments")
                        .AddAsync(data);

                return true;
            }
            catch
            {
                return false;
            }
        }




        // Get comments for an event (role is stored in the comment document)
        public async Task<List<(string username, string role, string comment)>> GetComments(string eventId)
        {
            var comments = new List<(string username, string role, string comment)>();

            try
            {
                var snapshot = await db.Collection("events")
                                       .Document(eventId)
                                       .Collection("comments")
                                       .OrderBy("timestamp")
                                       .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var data = doc.ToDictionary();
                        string username = data.ContainsKey("username") ? data["username"].ToString() : "Anonymous";
                        string comment = data.ContainsKey("comment") ? data["comment"].ToString() : "";
                        string role = data.ContainsKey("role") ? data["role"].ToString() : "";

                        comments.Add((username, role, comment));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting comments: {ex.Message}");
            }

            return comments;
        }


        public async Task<List<CommentVM>> GetThreadedComments(string eventId)
        {
            var list = new List<CommentVM>();

            var snapshot = await db.Collection("events")
                                   .Document(eventId)
                                   .Collection("comments")
                                   .OrderBy("timestamp")
                                   .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var d = doc.ToDictionary();

                list.Add(new CommentVM
                {
                    Id = doc.Id,
                    Username = d["username"].ToString(),
                    Role = d["role"].ToString(),
                    Comment = d["comment"].ToString(),
                    ParentCommentId = d.ContainsKey("parentId")
                        ? d["parentId"]?.ToString()
                        : ""
                });
            }

            return list;
        }


        // Get threaded comment tree
        public async Task<List<CommentVM>> GetCommentTree(string eventId)
        {
            var snapshot = await db.Collection("events")
                                   .Document(eventId)
                                   .Collection("comments")
                                   .OrderBy("timestamp")
                                   .GetSnapshotAsync();

            var all = new Dictionary<string, CommentVM>();

            foreach (var doc in snapshot.Documents)
            {
                var d = doc.ToDictionary();

                // Safe extraction of fields
                string username = d.ContainsKey("username") ? d["username"]?.ToString() ?? "Unknown" : "Unknown";
                string role = d.ContainsKey("role") ? d["role"]?.ToString() ?? "Unknown" : "Unknown";
                string commentText = d.ContainsKey("comment") ? d["comment"]?.ToString() ?? "" : "";
                string parentId = d.ContainsKey("parentId") ? d["parentId"]?.ToString() : null;
                Timestamp timestamp = d.ContainsKey("timestamp") && d["timestamp"] is Timestamp ts
                                      ? ts
                                      : Timestamp.FromDateTime(DateTime.UtcNow);

                all[doc.Id] = new CommentVM
                {
                    Id = doc.Id,
                    Username = username,
                    Role = role,
                    Comment = commentText,
                    ParentCommentId = parentId,
                    Timestamp = timestamp
                };
            }

            // Build threaded comment tree
            var roots = new List<CommentVM>();

            foreach (var c in all.Values)
            {
                if (!string.IsNullOrEmpty(c.ParentCommentId) && all.ContainsKey(c.ParentCommentId))
                {
                    all[c.ParentCommentId].Replies.Add(c);
                }
                else
                {
                    roots.Add(c);
                }
            }

            return roots;
        }

        // 🔹 Get volunteer registrations only (for staff approval)
        public async Task<List<EventRegistration>> GetVolunteerRegistrations()
        {
            CollectionReference regRef = db.Collection("eventRegistrations");
            QuerySnapshot snapshot = await regRef.WhereEqualTo("role", "Volunteer").GetSnapshotAsync();

            List<EventRegistration> volunteerRegistrations = new();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    EventRegistration reg = doc.ConvertTo<EventRegistration>();
                    reg.Id = doc.Id;
                    volunteerRegistrations.Add(reg);
                }
            }

            return volunteerRegistrations;
        }

        // 🔹 Promote waitlisted participants when spots become available
        public async Task<bool> PromoteFromWaitlist(string eventId)
        {
            try
            {
                // Get event details to check max capacity
                var events = await GetAllEvents();
                var eventDetails = events.FirstOrDefault(e => e.Id == eventId);
                if (eventDetails == null) return false;

                // Get all registrations for this event
                var allRegistrations = await GetAllRegistrations();
                var eventRegistrations = allRegistrations.Where(r => r.EventId == eventId).ToList();

                // Count current confirmed participants (excluding volunteers)
                int confirmedParticipants = eventRegistrations.Count(r => 
                    r.WaitlistStatus == "Confirmed" && r.Role == "Participant");

                // Get waitlisted participants ordered by registration date (first come, first served)
                var waitlistedParticipants = eventRegistrations
                    .Where(r => r.WaitlistStatus == "Waitlisted" && r.Role == "Participant")
                    .OrderBy(r => r.RegistrationDate.ToDateTime())
                    .ToList();

                // Promote participants from waitlist if there are available spots
                int availableSpots = eventDetails.MaxParticipants - confirmedParticipants;
                int promoted = 0;

                foreach (var waitlistedParticipant in waitlistedParticipants.Take(availableSpots))
                {
                    // Update status to confirmed
                    await UpdateWaitlistStatus(waitlistedParticipant.Id, "Confirmed");
                    
                    // Update payment status and amount
                    await UpdateRegistrationPaymentInfo(waitlistedParticipant.Id, "Pending", 50.0);
                    
                    promoted++;
                }

                return promoted > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error promoting from waitlist: {ex.Message}");
                return false;
            }
        }

        // 🔹 Update waitlist status
        public async Task<bool> UpdateWaitlistStatus(string registrationId, string status)
        {
            try
            {
                DocumentReference docRef = db.Collection("eventRegistrations").Document(registrationId);
                await docRef.UpdateAsync("waitlistStatus", status);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating waitlist status: {ex.Message}");
                return false;
            }
        }

        // 🔹 Update registration payment information
        public async Task<bool> UpdateRegistrationPaymentInfo(string registrationId, string paymentStatus, double paymentAmount)
        {
            try
            {
                DocumentReference docRef = db.Collection("eventRegistrations").Document(registrationId);
                
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "paymentStatus", paymentStatus },
                    { "paymentAmount", paymentAmount }
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating payment info: {ex.Message}");
                return false;
            }
        }

        // 🔹 Update volunteer registration status (legacy method for backward compatibility)
        public async Task<bool> UpdateVolunteerRegistrationStatus(string registrationId, string status)
        {
            try
            {
                DocumentReference docRef = db.Collection("eventRegistrations").Document(registrationId);

                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "status", status }
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating volunteer registration status: {ex.Message}");
                return false;
            }
        }

        // 🔹 Cancel registration and promote from waitlist
        public async Task<bool> CancelRegistration(string registrationId)
        {
            try
            {
                // Get the registration to cancel
                var registration = await GetRegistrationById(registrationId);
                if (registration == null) return false;

                // Update status to cancelled
                await UpdateWaitlistStatus(registrationId, "Cancelled");

                // If this was a confirmed participant, promote someone from waitlist
                if (registration.WaitlistStatus == "Confirmed" && registration.Role == "Participant")
                {
                    await PromoteFromWaitlist(registration.EventId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling registration: {ex.Message}");
                return false;
            }
        }


        public async Task<string> AddEventAndReturnId(Event ev)
        {
            DocumentReference docRef = await db.Collection("events").AddAsync(ev);
            return docRef.Id;
        }

        public async Task UpdateEventPhoto(string eventId, string imageUrl)
        {
            await db.Collection("events")
                    .Document(eventId)
                    .UpdateAsync("eventPhoto", imageUrl);
        }

    }
}
