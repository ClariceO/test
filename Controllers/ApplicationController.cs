using Microsoft.AspNetCore.Mvc;
using H4G_Project.DAL;
using H4G_Project.Models;
using H4G_Project.Services;
using System.Net;
using System.Net.Mail;

namespace H4G_Project.Controllers
{
    public class ApplicationController : Controller
    {
        ApplicationDAL applicationContext = new ApplicationDAL();
        EventsDAL eventsContext = new EventsDAL();
        private readonly IConfiguration _config;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;

        // Combine both into one constructor
        public ApplicationController(IConfiguration config, NotificationService notificationService, EmailService emailService)
        {
            _config = config;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // Example usage
        public async Task<IActionResult> NotifyUser(string token)
        {
            await _notificationService.SendNotificationAsync(token, "Update", "Your application status changed.");
            return Ok("Notification sent");
        }

        // Show list of applications with tabs for client applications and volunteer registrations
        public async Task<ActionResult> Index()
        {
            try
            {
                Console.WriteLine("=== APPLICATION CONTROLLER INDEX DEBUG ===");
                var applications = await applicationContext.GetAllApplications();
                Console.WriteLine($"Retrieved {applications.Count} applications from DAL");

                // Sort applications: Pending first, then Declined, then Approved at bottom
                var sortedApplications = applications
                    .OrderBy(a => a.Status == "Pending" ? 0 : (a.Status == "Declined" ? 1 : 2))
                    .ToList();

                Console.WriteLine($"Sorted applications count: {sortedApplications.Count}");

                // Log each application for debugging
                for (int i = 0; i < sortedApplications.Count; i++)
                {
                    var app = sortedApplications[i];
                    Console.WriteLine($"Application {i + 1}: ID={app.Id}, Caregiver={app.CaregiverName}, FamilyMember={app.FamilyMemberName}, Status={app.Status}");
                }

                Console.WriteLine("=== END APPLICATION CONTROLLER INDEX DEBUG ===");
                return View(sortedApplications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ApplicationController.Index: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View(new List<Application>());
            }
        }

        public async Task<ActionResult> VolunteerApplications()
        {
            var applications = await applicationContext.GetAllVolunteerApplications();

            // Sort applications: Pending first, then Declined, then Approved at bottom
            var sortedApplications = applications
                .OrderBy(a => a.Status == "Pending" ? 0 : (a.Status == "Declined" ? 1 : 2))
                .ToList();

            return View(sortedApplications);
        }

        // Show volunteer registrations page
        public async Task<ActionResult> VolunteerRegistrations()
        {
            var volunteerRegistrations = await eventsContext.GetVolunteerRegistrations();
            var allEvents = await eventsContext.GetAllEvents();

            // Sort volunteer registrations: Pending first, then Declined, then Approved at bottom
            var sortedVolunteerRegistrations = volunteerRegistrations
                .OrderBy(v => v.Status == "Pending" ? 0 : (v.Status == "Declined" ? 1 : 2))
                .ToList();

            ViewBag.AllEvents = allEvents;
            return View(sortedVolunteerRegistrations);
        }

        // Show the form (GET)
        [HttpGet]
        public IActionResult NewApplication()
        {
            return View("~/Views/Home/Create.cshtml");
        }

        // Handle form submission (POST)
        [HttpPost]
        public async Task<IActionResult> NewApplication(Application application, IFormFile medicalReport, IFormFile idDocument)
        {
            try
            {
                // Add some debugging
                Console.WriteLine($"Received application for: {application?.CaregiverName}");
                Console.WriteLine($"Medical report file: {medicalReport?.FileName} ({medicalReport?.Length} bytes)");
                Console.WriteLine($"ID document file: {idDocument?.FileName} ({idDocument?.Length} bytes)");

                bool success = await applicationContext.AddApplication(application, medicalReport, idDocument);

                if (success)
                {
                    Console.WriteLine("Application saved successfully");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    Console.WriteLine("Failed to save application");
                    ViewBag.ErrorMessage = "Failed to save application. Please try again.";
                    return View("~/Views/Home/Create.cshtml", application);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NewApplication: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View("~/Views/Home/Create.cshtml", application);
            }
        }

        [HttpGet]
        public IActionResult NewVolunteerApplication()
        {
            return View("~/Views/Home/CreateVolunteer.cshtml");
        }

        // Handle form submission from volunteer form (POST)
        [HttpPost]
        public async Task<IActionResult> NewVolunteerApplication(VolunteerApplication application, IFormFile resume)
        {
            try
            {
                // Validate DateOfBirth if provided
                if (!string.IsNullOrEmpty(application.DateOfBirth))
                {
                    if (DateTime.TryParse(application.DateOfBirth, out DateTime parsedDate))
                    {
                        if (parsedDate.Date > DateTime.Today)
                        {
                            ViewBag.ErrorMessage = "Date of Birth cannot be in the future.";
                            return View("~/Views/Home/CreateVolunteer.cshtml", application);
                        }

                        // Optional: Check for reasonable age limits (e.g., not older than 120 years)
                        var minDate = DateTime.Today.AddYears(-120);
                        if (parsedDate.Date < minDate)
                        {
                            ViewBag.ErrorMessage = "Please enter a valid date of birth.";
                            return View("~/Views/Home/CreateVolunteer.cshtml", application);
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Invalid date format for Date of Birth.";
                        return View("~/Views/Home/CreateVolunteer.cshtml", application);
                    }
                }

                bool success = await applicationContext.AddVolunteerApplication(application, resume);

                if (success)
                {
                    Console.WriteLine("Application saved successfully");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    Console.WriteLine("Failed to save application");
                    ViewBag.ErrorMessage = "Failed to save application. Please try again.";
                    return View("~/Views/Home/CreateVolunteer.cshtml", application);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NewApplication: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View("~/Views/Home/CreateVolunteer.cshtml", application);
            }
        }

        // Approve application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(string applicationId, string applicantName, string applicantEmail, string familyMemberName, string dateOfBirth)
        {
            try
            {
                // Get the full application to retrieve phone number
                var application = await applicationContext.GetApplicationByEmail(applicantEmail);
                string applicantPhone = application?.ContactNumber ?? "";

                // Debug: Log the received parameters
                Console.WriteLine($"=== CLIENT APPLICATION APPROVAL DEBUG ===");
                Console.WriteLine($"Application ID: {applicationId}");
                Console.WriteLine($"Applicant Name: {applicantName}");
                Console.WriteLine($"Applicant Email: {applicantEmail}");
                Console.WriteLine($"Applicant Phone: {applicantPhone}");
                Console.WriteLine($"Family Member Name: {familyMemberName}");
                Console.WriteLine($"Date of Birth: {dateOfBirth}");
                Console.WriteLine($"==========================================");

                // Generate password: family member name + date of birth
                string password = GenerateClientPassword(familyMemberName, dateOfBirth);
                Console.WriteLine($"Generated Password: {password}");

                // Create user account in Firebase Auth and Firestore
                var userDAL = new UserDAL();
                var newUser = new User
                {
                    Username = applicantName, // Caregiver name as username
                    Email = applicantEmail,
                    PhoneNumber = applicantPhone, // Add phone number from application
                    DateOfBirth = application?.DateOfBirth ?? "", // Add date of birth from application
                    Role = "participant", // Role for clients/participants (lowercase)
                    EngagementType = "Ad hoc engagement", // Engagement type for clients
                    LastDayOfService = string.Empty // Active user
                };

                bool userCreated = await userDAL.AddUser(newUser);

                if (userCreated)
                {
                    // Update application status to approved
                    bool statusUpdated = await applicationContext.UpdateApplicationStatus(applicationId, "Approved");

                    if (statusUpdated)
                    {
                        // Send approval email with credentials
                        bool emailSent = await _emailService.SendClientApplicationApprovalEmailAsync(applicantEmail, applicantName, familyMemberName, password);

                        if (emailSent)
                        {
                            TempData["SuccessMessage"] = $"Application for {applicantName} approved successfully. User account created and approval email sent.";
                        }
                        else
                        {
                            TempData["WarningMessage"] = $"Application approved and user account created, but failed to send email to {applicantEmail}.";
                        }
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"User account created for {applicantName}, but failed to update application status.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to create user account for {applicantName}.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error approving application: {ex.Message}");
                TempData["ErrorMessage"] = $"Error processing application: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Helper method to generate password for clients
        private static string GenerateClientPassword(string familyMemberName, string dateOfBirth)
        {
            try
            {
                Console.WriteLine($"--- PASSWORD GENERATION DEBUG ---");
                Console.WriteLine($"Input Family Member Name: '{familyMemberName}'");
                Console.WriteLine($"Input Date of Birth: '{dateOfBirth}'");

                // Remove spaces and special characters from family member name
                string cleanName = new string(familyMemberName.Where(c => char.IsLetterOrDigit(c)).ToArray());
                Console.WriteLine($"Cleaned Name: '{cleanName}'");

                // Parse date of birth and format as DDMMYYYY
                if (DateTime.TryParse(dateOfBirth, out DateTime dob))
                {
                    string dobString = dob.ToString("ddMMyyyy");
                    Console.WriteLine($"Parsed Date: {dob}");
                    Console.WriteLine($"Formatted Date: '{dobString}'");
                    string finalPassword = $"{cleanName}{dobString}";
                    Console.WriteLine($"Final Password: '{finalPassword}'");
                    Console.WriteLine($"--- END PASSWORD GENERATION ---");
                    return finalPassword;
                }
                else
                {
                    Console.WriteLine($"ERROR: Failed to parse date '{dateOfBirth}'");
                    // Fallback: use the date string as provided, removing non-digits
                    string cleanDob = new string(dateOfBirth.Where(c => char.IsDigit(c)).ToArray());
                    Console.WriteLine($"Fallback - Clean DOB digits: '{cleanDob}'");
                    string fallbackPassword = $"{cleanName}{cleanDob}";
                    Console.WriteLine($"Fallback Password: '{fallbackPassword}'");
                    Console.WriteLine($"--- END PASSWORD GENERATION (FALLBACK) ---");
                    return fallbackPassword;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in password generation: {ex.Message}");
                // Fallback password
                string errorFallback = $"{familyMemberName.Replace(" ", "")}123";
                Console.WriteLine($"Error Fallback Password: '{errorFallback}'");
                Console.WriteLine($"--- END PASSWORD GENERATION (ERROR) ---");
                return errorFallback;
            }
        }

        // Reject application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(string applicationId, string applicantName, string applicantEmail, string familyMemberName)
        {
            try
            {
                bool success = await applicationContext.UpdateApplicationStatus(applicationId, "Declined");

                if (success)
                {
                    // Send rejection email to the applicant
                    bool emailSent = await _emailService.SendClientApplicationRejectionEmailAsync(applicantEmail, applicantName, familyMemberName);

                    if (emailSent)
                    {
                        TempData["SuccessMessage"] = $"Application for {applicantName} declined successfully and notification email sent.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Application for {applicantName} declined successfully, but failed to send notification email.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to decline application.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error declining application: {ex.Message}");
                TempData["ErrorMessage"] = $"Error declining application: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Approve volunteer application and create user account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVolunteerApplication(string applicationId, string volunteerName, string volunteerEmail, string dateOfBirth)
        {
            try
            {
                // Get the full volunteer application to retrieve phone number and date of birth
                var volunteerApplication = await applicationContext.GetVolunteerApplicationById(applicationId);
                if (volunteerApplication == null)
                {
                    TempData["ErrorMessage"] = "Volunteer application not found.";
                    return RedirectToAction("VolunteerApplications");
                }

                // Generate password: name + date of birth
                string password = GenerateVolunteerPassword(volunteerName, dateOfBirth);

                // Create user account in Firebase Auth and Firestore
                var userDAL = new UserDAL();
                var newUser = new User
                {
                    Username = volunteerName,
                    Email = volunteerEmail,
                    PhoneNumber = volunteerApplication.ContactNumber, // Add phone number from application
                    DateOfBirth = volunteerApplication.DateOfBirth, // Add date of birth from application
                    Role = "Volunteer",
                    EngagementType = "Ad hoc engagement", // Default for volunteers
                    LastDayOfService = string.Empty // Active user
                };

                bool userCreated = await userDAL.AddUser(newUser);

                if (userCreated)
                {
                    // Update application status to approved
                    bool statusUpdated = await applicationContext.UpdateVolunteerApplicationStatus(applicationId, "Approved");

                    if (statusUpdated)
                    {
                        // Send approval email with credentials
                        bool emailSent = await _emailService.SendVolunteerApprovalEmailAsync(volunteerEmail, volunteerName, password);

                        if (emailSent)
                        {
                            TempData["SuccessMessage"] = $"Volunteer application for {volunteerName} approved successfully. User account created and approval email sent.";
                        }
                        else
                        {
                            TempData["WarningMessage"] = $"Volunteer application approved and user account created, but failed to send email to {volunteerEmail}.";
                        }
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"User account created for {volunteerName}, but failed to update application status.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to create user account for {volunteerName}.";
                }

                return RedirectToAction("VolunteerApplications");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error approving volunteer application: {ex.Message}");
                TempData["ErrorMessage"] = $"Error approving volunteer application: {ex.Message}";
                return RedirectToAction("VolunteerApplications");
            }
        }

        // Helper method to generate password
        private string GenerateVolunteerPassword(string name, string dateOfBirth)
        {
            try
            {
                // Remove spaces and special characters from name
                string cleanName = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());

                // Parse date of birth and format as DDMMYYYY
                if (DateTime.TryParse(dateOfBirth, out DateTime dob))
                {
                    string dobString = dob.ToString("ddMMyyyy");
                    return $"{cleanName}{dobString}";
                }
                else
                {
                    // Fallback: use the date string as provided
                    string cleanDob = new string(dateOfBirth.Where(c => char.IsDigit(c)).ToArray());
                    return $"{cleanName}{cleanDob}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating password: {ex.Message}");
                // Fallback password
                return $"{name.Replace(" ", "")}123";
            }
        }

        // Reject volunteer application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVolunteerApplication(string applicationId, string volunteerName, string volunteerEmail)
        {
            try
            {
                bool success = await applicationContext.UpdateVolunteerApplicationStatus(applicationId, "Declined");

                if (success)
                {
                    // Send rejection email to the applicant
                    bool emailSent = await _emailService.SendVolunteerRejectionEmailAsync(volunteerEmail, volunteerName);

                    if (emailSent)
                    {
                        TempData["SuccessMessage"] = $"Volunteer application for {volunteerName} declined successfully and notification email sent.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Volunteer application for {volunteerName} declined successfully, but failed to send notification email.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to decline volunteer application.";
                }

                return RedirectToAction("VolunteerApplications");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error declining volunteer application: {ex.Message}");
                TempData["ErrorMessage"] = $"Error declining volunteer application: {ex.Message}";
                return RedirectToAction("VolunteerApplications");
            }
        }

        // Approve volunteer registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVolunteerRegistration(string registrationId, string volunteerName, string volunteerEmail)
        {
            try
            {
                bool success = await eventsContext.UpdateVolunteerRegistrationStatus(registrationId, "Approved");

                if (success)
                {
                    TempData["SuccessMessage"] = $"Volunteer registration for {volunteerName} approved successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve volunteer registration.";
                }

                return RedirectToAction("VolunteerRegistrations");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving volunteer registration: {ex.Message}";
                return RedirectToAction("VolunteerRegistrations");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVolunteerRegistration(string registrationId, string volunteerName, string volunteerEmail)
        {
            try
            {
                bool success = await eventsContext.UpdateVolunteerRegistrationStatus(registrationId, "Declined");

                if (success)
                {
                    TempData["SuccessMessage"] = $"Volunteer registration for {volunteerName} declined successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to decline volunteer registration.";
                }

                return RedirectToAction("VolunteerRegistrations");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error declining volunteer registration: {ex.Message}";
                return RedirectToAction("VolunteerRegistrations");
            }
        }
    }
}