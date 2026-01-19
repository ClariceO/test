using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using H4G_Project.DAL;
using H4G_Project.Models;
using H4G_Project.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace H4G_Project.Controllers
{
    public class StaffController : Controller
    {
        private readonly StaffDAL _staffContext = new StaffDAL();
        private readonly EventsDAL _eventsDAL = new EventsDAL();
        private readonly UserDAL _userContext = new UserDAL();
        private readonly ApplicationDAL _applicationContext = new ApplicationDAL();
        private readonly NotificationService _notificationService;
        private readonly NotificationDAL _notificationDAL = new NotificationDAL();
        private readonly EmailService _emailService;

        public StaffController(NotificationService notificationService, EmailService emailService)
        {
            _notificationService = notificationService;
            _emailService = emailService;
        }


        // ===============================
        // DASHBOARD
        // ===============================
        public async Task<IActionResult> Index()
        {
            string? email = HttpContext.Session.GetString("StaffEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            Staff? staff = await _staffContext.GetStaffByEmail(email);
            if (staff == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            return View(staff);
        }
        // ===============================
        // Attendance QR
        // ===============================


        [HttpGet]
        public async Task<IActionResult> DownloadEventQRCode(string eventId)
        {
            var events = await _eventsDAL.GetAllEvents();
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return NotFound("Event not found");

            // Ensure a stable QR code exists
            if (string.IsNullOrEmpty(ev.QrCode))
            {
                ev.QrCode = $"EVENT-{ev.Id}";
                await _eventsDAL.UpdateEventQrCode(ev.Id, ev.QrCode); // ✅ Use new method
            }

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(ev.QrCode, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.QRCode(qrCodeData);
            using var bitmap = qrCode.GetGraphic(20);

            var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;

            return File(stream, "image/png", $"{ev.Name}-QRCode.png");
        }


        // ===============================
        // REPORTS (FILTERABLE)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Reports(string? roleFilter, string? eventFilter)
        {
            var registrations = await _eventsDAL.GetAllRegistrations();

            // Get unique options for dropdowns
            ViewData["AllRoles"] = registrations
                .Where(r => !string.IsNullOrEmpty(r.Role))
                .Select(r => r.Role)
                .Distinct()
                .ToList();

            ViewData["AllEvents"] = registrations
                .Where(r => !string.IsNullOrEmpty(r.EventName))
                .Select(r => r.EventName)
                .Distinct()
                .ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(roleFilter))
                registrations = registrations
                    .Where(r => r.Role?.Equals(roleFilter, System.StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToList();

            if (!string.IsNullOrEmpty(eventFilter))
                registrations = registrations
                    .Where(r => r.EventName?.Equals(eventFilter, System.StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToList();

            ViewData["RoleFilter"] = roleFilter;
            ViewData["EventFilter"] = eventFilter;

            return View(registrations);
        }

        // ===============================
        // REGISTER NEW STAFF
        // ===============================
        [HttpGet]
        public IActionResult AddNewStaff()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewStaff(IFormCollection form)
        {
            string? email = form["Email"];
            string? password = form["Password"];
            string? username = form["Username"];
            string? lastDayOfService = form["LastDayOfService"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("", "Please fill in all required fields.");
                return View("AddNewStaff");
            }

            // Validate LastDayOfService if provided
            if (!string.IsNullOrEmpty(lastDayOfService))
            {
                if (DateTime.TryParse(lastDayOfService, out DateTime parsedDate))
                {
                    if (parsedDate.Date < DateTime.Today)
                    {
                        ModelState.AddModelError("", "Last Day of Service must be today or a future date.");
                        return View("AddNewStaff");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid date format for Last Day of Service.");
                    return View("AddNewStaff");
                }
            }

            try
            {
                await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = email,
                    Password = password
                });

                await _staffContext.AddStaff(new Staff
                {
                    Username = username,
                    Email = email,
                    LastDayOfService = string.IsNullOrEmpty(lastDayOfService) ? null : lastDayOfService
                });

                // Send welcome email with credentials
                bool emailSent = await _emailService.SendStaffAccountCreationEmailAsync(email, username, password);

                if (emailSent)
                {
                    TempData["SuccessMessage"] = $"Staff account created successfully for {username}. Welcome email sent to {email}.";
                }
                else
                {
                    TempData["WarningMessage"] = $"Staff account created successfully for {username}, but failed to send welcome email to {email}.";
                }

                return RedirectToAction("AddNewStaff");
            }
            catch (FirebaseAuthException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("AddNewStaff");
            }
        }

        // ===============================
        // FIREBASE TOKEN LOGIN
        // ===============================
        public class FirebaseTokenRequest
        {
            public string? Token { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseTokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.Token))
                return BadRequest("Missing token");

            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.Token);
                string email = decoded.Claims["email"]?.ToString() ?? "";

                Staff? staff = await _staffContext.GetStaffByEmail(email);
                if (staff == null)
                    return Unauthorized();

                Console.WriteLine($"Staff found: {staff.Email}, Username: {staff.Username}");
                Console.WriteLine($"Staff LastDayOfService value: '{staff.LastDayOfService}'");

                // Check if staff's last day of service has passed
                if (!string.IsNullOrEmpty(staff.LastDayOfService))
                {
                    Console.WriteLine("LastDayOfService is not null or empty, checking date...");
                    var today = DateTime.Today;
                    Console.WriteLine($"Today's date: {today}");

                    // Try to parse the LastDayOfService string to DateTime
                    if (DateTime.TryParse(staff.LastDayOfService, out DateTime lastDayOfService))
                    {
                        Console.WriteLine($"Successfully parsed LastDayOfService: {lastDayOfService}");
                        Console.WriteLine($"Staff last day of service: {lastDayOfService.Date}, Today: {today}");
                        Console.WriteLine($"Comparison: {lastDayOfService.Date} < {today} = {lastDayOfService.Date < today}");

                        if (lastDayOfService.Date < today)
                        {
                            Console.WriteLine($"BLOCKING LOGIN - Staff access denied - last day of service was {lastDayOfService.Date}");
                            return Unauthorized("Access denied. Your employment period has ended.");
                        }
                        else
                        {
                            Console.WriteLine($"LOGIN ALLOWED - Last day of service ({lastDayOfService.Date}) is today or in the future");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"FAILED TO PARSE - Invalid LastDayOfService format: '{staff.LastDayOfService}'");
                        // If the date format is invalid, allow login but log the issue
                    }
                }
                else
                {
                    Console.WriteLine("LastDayOfService is null or empty - no restriction applied");
                }

                HttpContext.Session.SetString("StaffUsername", staff.Username ?? "");
                HttpContext.Session.SetString("StaffEmail", staff.Email ?? "");

                Console.WriteLine($"Staff logged in: {staff.Email} (Staff)");
                return Ok();
            }
            catch
            {
                return Unauthorized();
            }
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
                // 1. Verify staff exists in database
                Staff? staff = await _staffContext.GetStaffByEmail(email);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Staff member not found.";
                    return View();
                }

                // 2. Check if staff's last day of service has passed
                if (!string.IsNullOrEmpty(staff.LastDayOfService))
                {
                    if (DateTime.TryParse(staff.LastDayOfService, out DateTime lastDayOfService))
                    {
                        if (lastDayOfService.Date < DateTime.Today)
                        {
                            TempData["ErrorMessage"] = "Access denied. Your employment period has ended.";
                            return View();
                        }
                    }
                }

                // 3. Get user from Firebase and update password
                var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                var userRecord = await auth.GetUserByEmailAsync(email);

                // Update password in Firebase
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
                TempData["ErrorMessage"] = "Staff member not found or invalid email.";
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
        // ADD USER
        // ===============================
        [HttpGet]
        public IActionResult AddUser(string applicantName = "", string applicantEmail = "", string applicantPhone = "", string applicantDateOfBirth = "", string applicationId = "")
        {
            // Pass application data to view for pre-population
            ViewBag.ApplicantName = applicantName;
            ViewBag.ApplicantEmail = applicantEmail;
            ViewBag.ApplicantPhone = applicantPhone;
            ViewBag.ApplicantDateOfBirth = applicantDateOfBirth;
            ViewBag.ApplicationId = applicationId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(IFormCollection form)
        {
            string username = form["Username"];
            string email = form["Email"];
            string phoneNumber = form["PhoneNumber"]; // Add phone number
            string dateOfBirth = form["DateOfBirth"]; // Add date of birth
            string role = form["Role"];
            string password = form["Password"]; // Get password from form
            string applicationId = form["ApplicationId"]; // Hidden field from the form

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError("", "Please fill in all required fields.");
                return View();
            }

            // Determine if this is from application approval or manual creation
            bool isFromApplication = !string.IsNullOrEmpty(applicationId);

            // Use provided password or generate random one if empty
            string finalPassword = string.IsNullOrEmpty(password) ? GenerateRandomPassword() : password;

            try
            {
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = email,
                    Password = finalPassword
                });

                await _userContext.AddUser(new User
                {
                    Username = username,
                    Email = email,
                    PhoneNumber = phoneNumber, // Add phone number
                    DateOfBirth = dateOfBirth, // Add date of birth
                    Role = role,
                    LastDayOfService = null // Explicitly set to null for new users
                });

                // Update application status to "Approved" if applicationId is provided (only after successful user creation)
                if (!string.IsNullOrEmpty(applicationId))
                {
                    await _applicationContext.UpdateApplicationStatus(applicationId, "Approved");
                }

                // Show confirmation page only if password was auto-generated (from application or manual with blank password)
                if (string.IsNullOrEmpty(form["Password"]))
                {
                    // Pass the generated password to the view for display
                    TempData["SuccessMessage"] = $"User account created successfully for {username}.";
                    TempData["GeneratedPassword"] = finalPassword;
                    TempData["UserEmail"] = email;
                    return View("UserCreated");
                }
                else
                {
                    // Manual password was provided - just show success message and redirect
                    TempData["SuccessMessage"] = $"User account created successfully for {username} with custom password.";
                    return RedirectToAction("AddUser");
                }
            }
            catch (FirebaseAuthException ex)
            {
                ModelState.AddModelError("", $"Error creating user: {ex.Message}");
                return View();
            }
        }

        // Show create event page
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new Event());
        }

        // Handle create event
        // ===============================
        // HANDLE CREATE EVENT WITH PHOTO
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CreateEvent(
            string Name,
            DateTime Start,
            DateTime? End,
            string Details,
            DateTime RegistrationDueDate,
            int MaxParticipants,
            IFormFile EventPhoto) // Added file parameter
        {
            if (string.IsNullOrEmpty(Name))
            {
                ModelState.AddModelError("Name", "Event name is required");
                return View();
            }

            Event ev = new Event
            {
                Name = Name,
                Start = Timestamp.FromDateTime(Start.ToUniversalTime()),
                End = End.HasValue ? Timestamp.FromDateTime(End.Value.ToUniversalTime()) : null,
                RegistrationDueDate = Timestamp.FromDateTime(RegistrationDueDate.ToUniversalTime()),
                MaxParticipants = MaxParticipants,
                Details = Details
            };

            // 2️⃣ Save event → get Firestore ID
            string eventId = await _eventsDAL.AddEventAndReturnId(ev);

            // 3️⃣ Upload image to Firebase Storage (if exists)
            if (EventPhoto != null && EventPhoto.Length > 0)
            {
                string imageUrl = await UploadEventPhotoToFirebase(eventId, EventPhoto);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    await _eventsDAL.UpdateEventPhoto(eventId, imageUrl);
                }
            }

            TempData["SuccessMessage"] = "Event created successfully!";
            return RedirectToAction(nameof(CreateEvent));
        }

        // ===============================
        // UPLOAD EVENT PHOTO TO FIREBASE STORAGE
        // ===============================
        private async Task<string> UploadEventPhotoToFirebase(string eventId, IFormFile file)
        {
            string bucketName = "squad-60b0b.firebasestorage.app"; // Firebase bucket
            string serviceAccountPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "DAL", "config",
                "squad-60b0b-firebase-adminsdk-fbsvc-cff3f594d5.json"
            );

            var credential = GoogleCredential.FromFile(serviceAccountPath);
            var storageClient = await StorageClient.CreateAsync(credential);

            string fileName = $"eventPhotos/{eventId}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();

            // Generate a GUID token
            string downloadToken = Guid.NewGuid().ToString();

            // Upload object with metadata for Firebase-style download token
            var obj = await storageClient.UploadObjectAsync(new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = fileName,
                ContentType = file.ContentType,
                Metadata = new System.Collections.Generic.Dictionary<string, string>
        {
            { "firebaseStorageDownloadTokens", downloadToken }
        }
            }, stream);

            // Construct the Firebase Storage download URL
            string url = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{Uri.EscapeDataString(fileName)}?alt=media&token={downloadToken}";

            return url;
        }



        // =============================== // VIEW ALL EVENTS // =============================== //
        [HttpGet]
        public async Task<IActionResult> ViewAllEvents()
        {
            // 1️⃣ Retrieve all events
            var events = await _eventsDAL.GetAllEvents();

            // 2️⃣ Sort by start date
            var sortedEvents = events
                .OrderBy(e => e.Start.ToDateTime())
                .ToList();

            // 3️⃣ Retrieve comments for each event
            var eventComments = new Dictionary<string, List<CommentVM>>();
            foreach (var ev in sortedEvents)
            {
                var comments = await _eventsDAL.GetCommentTree(ev.Id); // get threaded comments
                eventComments[ev.Id] = comments;
            }

            ViewBag.EventComments = eventComments;

            // 4️⃣ Ensure every event has a photo URL (optional safety check)
            foreach (var ev in sortedEvents)
            {
                if (string.IsNullOrEmpty(ev.eventPhoto))
                {
                    // Set a default image if none exists
                    ev.eventPhoto = "/images/default-event.png";
                }
            }

            return View(sortedEvents);
        }



        // Add comment or reply
        [HttpPost]
        public async Task<IActionResult> AddComment(string eventId, string comment, string parentCommentId = null)
        {
            if (string.IsNullOrEmpty(comment) || string.IsNullOrEmpty(eventId))
            {
                TempData["Message"] = "Invalid comment submission.";
                return RedirectToAction("ViewAllEvents");
            }

            string username = HttpContext.Session.GetString("StaffUsername") ?? "Anonymous";
            string role = HttpContext.Session.GetString("UserRole") ?? "staff";
            string email = HttpContext.Session.GetString("StaffEmail") ?? "";

            // Add the comment to the database
            bool commentAdded = await _eventsDAL.AddComment(eventId, username, email, comment, role, parentCommentId);

            if (commentAdded)
            {
                // Create notifications for users registered for this event
                await CreateCommentNotifications(eventId, username, comment);
            }

            return RedirectToAction("ViewAllEvents");
        }

        private async Task CreateCommentNotifications(string eventId, string staffUsername, string comment)
        {
            try
            {
                // Get event details for notification
                var eventDetails = await _eventsDAL.GetEventById(eventId);
                if (eventDetails == null) return;

                // Create notifications using NotificationDAL
                bool success = await _notificationDAL.CreateCommentNotifications(
                    eventId,
                    eventDetails.Name,
                    staffUsername,
                    comment
                );

                if (success)
                {
                    Console.WriteLine($"Successfully created notifications for comment on event {eventDetails.Name}");
                }
                else
                {
                    Console.WriteLine($"Failed to create notifications for event {eventId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating comment notifications: {ex.Message}");
                // Don't throw - notification failure shouldn't break comment posting
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // ===============================
        // MANAGE PARTICIPANT ENGAGEMENT
        // ===============================
        [HttpGet]
        public async Task<IActionResult> ManageEngagement()
        {
            var allUsers = await _userContext.GetAllUsers();
            // Filter to only show participants
            var participants = allUsers.Where(u => u.Role?.ToLower() == "participant").ToList();
            return View(participants);
        }

        [HttpGet]
        public IActionResult UpdateEngagement()
        {
            // Redirect to ManageEngagement page if accessed directly
            TempData["InfoMessage"] = "Please use the 'Update Engagement' button next to each participant to modify their engagement type.";
            return RedirectToAction("ManageEngagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEngagement(string email, string engagementType)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(engagementType))
            {
                TempData["ErrorMessage"] = "Email and engagement type are required.";
                return RedirectToAction("ManageEngagement");
            }

            try
            {
                bool success = await _userContext.UpdateEngagementType(email, engagementType);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Engagement type updated successfully for {email}.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update engagement type. User not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating engagement type: {ex.Message}";
            }

            return RedirectToAction("ManageEngagement");
        }
    }
}
