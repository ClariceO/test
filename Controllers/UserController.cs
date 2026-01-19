using Microsoft.AspNetCore.Mvc;
using H4G_Project.DAL;
using H4G_Project.Models;
using FirebaseAdmin.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace H4G_Project.Controllers
{
    public class UserController : Controller
    {
        private readonly UserDAL _userContext = new UserDAL();
        private readonly EventsDAL _eventsDAL = new EventsDAL();

        private readonly NotificationDAL _notificationDAL = new NotificationDAL();
        private readonly StaffDAL _staffContext = new StaffDAL();

        public async Task<ActionResult> ViewUsers()
        {
            var users = await _userContext.GetAllUsers();
            var staff = await _staffContext.GetAllStaff();

            ViewBag.Users = users;
            ViewBag.Staff = staff;

            return View();
        }

        // Attendance system
        public IActionResult ScanQR()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> MarkAttendance([FromBody] QRAttendanceRequest request)
        {
            if (string.IsNullOrEmpty(request.QrCode))
                return Json(new { success = false, message = "Invalid QR code" });

            string scannedQr = request.QrCode.Trim();


            // ✅ Find the event matching the scanned QR
            var events = await _eventsDAL.GetAllEvents();



            var ev = events.FirstOrDefault(e => e.QrCode == scannedQr);

            if (ev == null)
            {
                return Json(new { success = false, message = "QR code not recognized" });
            }



            string userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, message = "User not logged in" });

            var registrations = await _eventsDAL.GetRegistrationsByEventId(ev.Id);
            var registration = registrations.FirstOrDefault(r => r.Email == userEmail);

            if (registration == null)
                return Json(new { success = false, message = "You are not registered for this event" });

            // ✅ Mark attendance
            registration.Attendance = true;
            await _eventsDAL.UpdateRegistration(registration);

            return Json(new { success = true, message = "Attendance marked!" });
        }




        public class QRAttendanceRequest
        {
            public string QrCode { get; set; } = string.Empty;
        }



        // ===============================
        // DASHBOARD
        // ===============================
        public async Task<IActionResult> Index()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Home");

            User? user = await _userContext.GetUserByEmail(email);
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "Participant";

            // Get engagement usage information for participants
            if (user != null && user.Role == "Participant")
            {
                var (canRegister, currentCount, limit, message) = await _eventsDAL.CheckUserEngagementLimit(email, user.EngagementType ?? "Ad hoc engagement", null);
                ViewBag.EngagementUsage = new
                {
                    CurrentCount = currentCount,
                    Limit = limit,
                    CanRegister = canRegister,
                    Message = message
                };
            }

            return View(user);
        }

        // ===============================
        // FIREBASE TOKEN LOGIN
        // ===============================
        public class FirebaseTokenRequest
        {
            public string Token { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseTokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.Token))
                return BadRequest("Missing token");

            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.Token);
                string email = decoded.Claims["email"]?.ToString() ?? string.Empty;

                User? user = await _userContext.GetUserByEmail(email);
                if (user == null) return Unauthorized();

                // Check if user is deactivated (has LastDayOfService set)
                if (!string.IsNullOrEmpty(user.LastDayOfService))
                {
                    Console.WriteLine($"BLOCKING LOGIN - User access denied - deactivated on {user.LastDayOfService}");
                    return Unauthorized("Access denied. Your account has been deactivated.");
                }

                HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
                HttpContext.Session.SetString("UserRole", user.Role ?? "Participant");

                return Ok();
            }
            catch
            {
                return Unauthorized();
            }
        }

        // ===============================
        // FALLBACK LOGIN (for existing users not in Firebase Auth)
        // TEMPORARY: For testing purposes only
        // ===============================
        [HttpPost]
        public async Task<ActionResult> LogInUser(IFormCollection form)
        {
            // Retrieve the user from the database by the email
            User? user = await _userContext.GetUserByEmail(form["Email"].ToString() ?? string.Empty);

            if (user != null)
            {
                // Check if user is deactivated (has LastDayOfService set)
                if (!string.IsNullOrEmpty(user.LastDayOfService))
                {
                    Console.WriteLine($"BLOCKING LOGIN - User access denied - deactivated on {user.LastDayOfService}");
                    ModelState.AddModelError(string.Empty, "Access denied. Your account has been deactivated.");
                    return RedirectToAction("Index", "Home");
                }

                // TEMPORARY: Skip password check for testing
                // TODO: Remove this once users are migrated to Firebase Auth
                HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
                HttpContext.Session.SetString("UserRole", user.Role ?? "Participant");
                TempData["Username"] = user.Username ?? string.Empty;
                TempData.Keep("Username");
                return RedirectToAction("Index", "User");
            }

            ModelState.AddModelError(string.Empty, "User not found. Please check your email.");
            return RedirectToAction("Index", "Home");
        }

        // ===============================
        // PASSWORD RESET
        // ===============================
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(IFormCollection form)
        {
            string email = form["Email"];
            string currentPassword = form["CurrentPassword"];
            string newPassword = form["NewPassword"];
            string confirmPassword = form["ConfirmPassword"];

            // Validate inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(currentPassword) ||
                string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "Please fill in all fields.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return View();
            }

            if (newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "New password must be at least 8 characters long.";
                return View();
            }

            try
            {
                // 1. Verify current password by trying to sign in
                var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;

                // Get user by email
                var userRecord = await auth.GetUserByEmailAsync(email);

                // Note: Firebase Admin SDK doesn't have a direct way to verify password
                // In a real implementation, you might want to use Firebase Client SDK
                // For now, we'll update the password directly

                // 2. Update password in Firebase
                var updateRequest = new UserRecordArgs()
                {
                    Uid = userRecord.Uid,
                    Password = newPassword
                };

                await auth.UpdateUserAsync(updateRequest);

                TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                return View();
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = $"Error resetting password: {ex.Message}";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "User not found or invalid email.";
                return View();
            }
        }

        // ===============================
        // LOGOUT
        // ===============================
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ===============================
        // VIEW ALL EVENTS
        // ===============================
        [HttpGet]
        public async Task<IActionResult> ViewAllEvents()
        {
            string userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Index", "Home");

            // 1️⃣ Get all events for the user
            var events = await _eventsDAL.GetEventsByUserEmail(userEmail);

            // 2️⃣ Ensure each event has a proper Firebase Storage URL
            foreach (var ev in events)
            {
                if (!string.IsNullOrEmpty(ev.eventPhoto))
                {
                    // If your DAL only stores the path like "eventPhotos/abc.png",
                    // construct the full downloadable URL with token
                    if (!ev.eventPhoto.StartsWith("https://"))
                    {
                        ev.eventPhoto = $"https://firebasestorage.googleapis.com/v0/b/squad-60b0b.appspot.com/o/{Uri.EscapeDataString(ev.eventPhoto)}?alt=media";
                    }
                }
            }

            // 3️⃣ Load comments
            var eventComments = new Dictionary<string, List<CommentVM>>();
            foreach (var ev in events)
            {
                var comments = await _eventsDAL.GetCommentTree(ev.Id);
                eventComments[ev.Id] = comments;
            }

            ViewBag.EventComments = eventComments;
            return View(events);
        }


        // Add comment or reply
        [HttpPost]
        public async Task<IActionResult> AddComment(string eventId, string comment, string? parentCommentId = null)
        {
            if (string.IsNullOrEmpty(comment) || string.IsNullOrEmpty(eventId))
            {
                TempData["Message"] = "Invalid comment submission.";
                return RedirectToAction("ViewAllEvents");
            }

            string username = HttpContext.Session.GetString("Username") ?? "Anonymous";
            string role = HttpContext.Session.GetString("UserRole") ?? "Participant";
            string email = HttpContext.Session.GetString("UserEmail") ?? "";

            await _eventsDAL.AddComment(eventId, username, email, comment, role, parentCommentId);

            return RedirectToAction("ViewAllEvents");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                Console.WriteLine($"GetNotifications called for email: {email}");

                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("No email in session - user not logged in");
                    return Unauthorized();
                }

                var notifications = await _notificationDAL.GetUserNotifications(email);
                Console.WriteLine($"Found {notifications.Count} notifications for {email}");

                // Convert to a format that JavaScript can handle properly
                var notificationResponse = notifications.Select(n => new
                {
                    id = n.Id,
                    userId = n.UserId,
                    title = n.Title,
                    message = n.Message,
                    eventId = n.EventId,
                    eventName = n.EventName,
                    type = n.Type,
                    isRead = n.IsRead,
                    createdBy = n.CreatedBy,
                    createdAt = n.CreatedAt.ToDateTime().ToString("o"), // ISO 8601 format
                    createdAtMs = n.CreatedAt.ToDateTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds
                }).ToList();

                return Json(notificationResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return StatusCode(500, "Error getting notifications");
            }
        }

        // Get unread notification count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                if (string.IsNullOrEmpty(email))
                {
                    return Json(0);
                }

                var count = await _notificationDAL.GetUnreadCount(email);
                return Json(count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return Json(0);
            }
        }

        // Mark notification as read
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(string notificationId)
        {
            try
            {
                bool success = await _notificationDAL.MarkAsRead(notificationId);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
                return Json(new { success = false });
            }
        }

        // Mark all notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            try
            {
                string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized();
                }

                bool success = await _notificationDAL.MarkAllAsRead(email);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all notifications as read: {ex.Message}");
                return Json(new { success = false });
            }
        }

        // Show notifications page
        public IActionResult Notifications()
        {
            return View();
        }

        // Test method to create a notification
        [HttpPost]
        public async Task<IActionResult> CreateTestNotification()
        {
            try
            {
                string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                var notification = new Notification
                {
                    UserId = email,
                    Title = "Test Notification",
                    Message = "This is a test notification created manually",
                    EventId = "test-event",
                    EventName = "Test Event",
                    Type = "test",
                    CreatedBy = "System",
                    IsRead = false
                };

                bool success = await _notificationDAL.AddNotification(notification);
                return Json(new { success = success, message = success ? "Test notification created" : "Failed to create notification" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating test notification: {ex.Message}");
                return Json(new { success = false, message = "Error creating notification" });
            }
        }

        // Deactivate user
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }

                // Set LastDayOfService to today's date to deactivate the user
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                bool success = await _userContext.UpdateUserLastDayOfService(email, today);

                return Json(new
                {
                    success = success,
                    message = success ? "User deactivated successfully" : "Failed to deactivate user"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deactivating user: {ex.Message}");
                return Json(new { success = false, message = "Error deactivating user" });
            }
        }

        // Reactivate user
        [HttpPost]
        public async Task<IActionResult> ReactivateUser(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }

                // Clear LastDayOfService to reactivate the user
                bool success = await _userContext.UpdateUserLastDayOfService(email, null!);

                return Json(new
                {
                    success = success,
                    message = success ? "User reactivated successfully" : "Failed to reactivate user"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reactivating user: {ex.Message}");
                return Json(new { success = false, message = "Error reactivating user" });
            }
        }

        // Update staff LastDayOfService
        [HttpPost]
        public async Task<IActionResult> UpdateStaffLastDayOfService(string email, string lastDayOfService)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }

                // Validate date if provided
                if (!string.IsNullOrEmpty(lastDayOfService))
                {
                    if (!DateTime.TryParse(lastDayOfService, out DateTime parsedDate))
                    {
                        return Json(new { success = false, message = "Invalid date format" });
                    }

                    if (parsedDate.Date < DateTime.Today)
                    {
                        return Json(new { success = false, message = "Last day of service must be today or a future date" });
                    }
                }

                bool success = await _staffContext.UpdateStaffLastDayOfService(email, lastDayOfService);

                return Json(new
                {
                    success = success,
                    message = success ? "Staff last day of service updated successfully" : "Failed to update staff"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating staff: {ex.Message}");
                return Json(new { success = false, message = "Error updating staff" });
            }
        }

    }
}
