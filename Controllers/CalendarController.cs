using Microsoft.AspNetCore.Mvc;
using H4G_Project.DAL;
using H4G_Project.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using Google.Cloud.Firestore;

namespace H4G_Project.Controllers
{
    public class CalendarController : Controller
    {
        private EventsDAL eventsDAL = new EventsDAL();

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await eventsDAL.GetAllEvents();

            return Json(events.Select(e => new
            {
                id = e.Id,
                title = e.Name,
                start = e.Start.ToDateTime().ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.End.HasValue ? e.End.Value.ToDateTime().ToString("yyyy-MM-ddTHH:mm:ss") : null
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetEventDetails(string id)
        {
            var events = await eventsDAL.GetAllEvents();
            var eventDetails = events.FirstOrDefault(e => e.Id == id);

            if (eventDetails == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = eventDetails.Id,
                name = eventDetails.Name,
                details = eventDetails.Details,
                eventPhoto = eventDetails.eventPhoto,
                start = eventDetails.Start.ToDateTime().ToString("yyyy-MM-dd HH:mm"),
                end = eventDetails.End.HasValue ? eventDetails.End.Value.ToDateTime().ToString("yyyy-MM-dd HH:mm") : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] EventRegistrationRequest request)
        {
            try
            {
                // Get event details
                var events = await eventsDAL.GetAllEvents();
                var eventDetails = events.FirstOrDefault(e => e.Id == request.EventId);

                if (eventDetails == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // Check if registration is still open
                if (eventDetails.RegistrationDueDate.ToDateTime() < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Registration for this event has closed" });
                }

                // Check if user is already registered for this event (check this first before engagement limits)
                var existingRegistrations = await eventsDAL.GetAllRegistrations();
                var existingRegistration = existingRegistrations.FirstOrDefault(r => 
                    r.EventId == request.EventId && 
                    r.Email == request.Email && 
                    r.WaitlistStatus != "Cancelled");

                if (existingRegistration != null)
                {
                    return Json(new { success = false, message = "You are already registered for this event" });
                }

                // For participants, check engagement limits and validate required fields
                if (request.Role == "Participant")
                {
                    // Validate mandatory phone number for participants
                    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        return Json(new { success = false, message = "Phone number is required for participants" });
                    }

                    // Validate phone number format (8 digits)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^\d{8}$"))
                    {
                        return Json(new { success = false, message = "Phone number must be exactly 8 digits" });
                    }

                    // Validate mandatory emergency contact fields for participants
                    if (string.IsNullOrWhiteSpace(request.EmergencyContactName))
                    {
                        return Json(new { success = false, message = "Emergency contact name is required for participants" });
                    }

                    if (string.IsNullOrWhiteSpace(request.EmergencyContact))
                    {
                        return Json(new { success = false, message = "Emergency contact number is required for participants" });
                    }

                    // Validate emergency contact number format (8 digits)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.EmergencyContact, @"^\d{8}$"))
                    {
                        return Json(new { success = false, message = "Emergency contact number must be exactly 8 digits" });
                    }

                    var userDAL = new UserDAL();
                    var user = await userDAL.GetUserByEmail(request.Email);
                    
                    if (user != null)
                    {
                        var (canRegister, currentCount, limit, message) = await eventsDAL.CheckUserEngagementLimit(request.Email, user.EngagementType, request.EventId);
                        
                        if (!canRegister)
                        {
                            return Json(new { success = false, message = message });
                        }
                    }
                }

                // Count current confirmed participants for this event (excluding volunteers from participant count)
                int confirmedParticipantCount = existingRegistrations.Count(r => 
                    r.EventId == request.EventId && 
                    r.WaitlistStatus == "Confirmed" && 
                    r.Role == "Participant");

                // Determine waitlist status based on event capacity
                string waitlistStatus;
                string statusMessage;

                if (request.Role == "Volunteer")
                {
                    // Volunteers are always confirmed and don't count toward participant limit
                    waitlistStatus = "Confirmed";
                    statusMessage = "Registration confirmed! Thank you for volunteering.";
                }
                else
                {
                    // Participants: check capacity (first come, first served)
                    if (confirmedParticipantCount < eventDetails.MaxParticipants)
                    {
                        waitlistStatus = "Confirmed";
                        statusMessage = $"Registration confirmed! You have secured spot {confirmedParticipantCount + 1} of {eventDetails.MaxParticipants}.";
                    }
                    else
                    {
                        waitlistStatus = "Waitlisted";
                        statusMessage = $"Event is full ({eventDetails.MaxParticipants} participants). You have been added to the waitlist.";
                    }
                }

                // Create registration object
                EventRegistration registration = new EventRegistration
                {
                    EventId = request.EventId,
                    EventName = request.EventName,
                    Role = request.Role,
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    NricLast4 = request.NricLast4,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    CitizenshipType = request.CitizenshipType,
                    // Legacy fields (keeping for backward compatibility)
                    PreferredRole = request.PreferredRole,
                    Skills = request.Skills,
                    // Participant fields
                    DietaryRequirements = request.DietaryRequirements,
                    EmergencyContact = request.EmergencyContact,
                    EmergencyContactName = request.EmergencyContactName,
                    PaymentStatus = waitlistStatus == "Confirmed" ? "Pending" : "Not Required", // Waitlisted users don't need to pay yet
                    PaymentAmount = request.Role == "Volunteer" ? 0.0 : (waitlistStatus == "Confirmed" ? 50.0 : 0.0),
                    RegistrationDate = Timestamp.FromDateTime(DateTime.UtcNow),
                    WaitlistStatus = waitlistStatus
                };

                // Generate mock QR code
                string mockQrCode = $"EVENT-{request.EventId.Substring(0, Math.Min(6, request.EventId.Length))}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                registration.QrCode = mockQrCode;

                // Save registration
                string registrationId = await eventsDAL.AddRegistration(registration);

                if (string.IsNullOrEmpty(registrationId))
                {
                    return Json(new { success = false, message = "Failed to create registration" });
                }

                // Auto-confirm volunteers (no payment needed)
                if (request.Role == "Volunteer")
                {
                    await eventsDAL.UpdatePaymentStatus(registrationId, "Completed", mockQrCode);
                }

                return Json(new
                {
                    success = true,
                    message = statusMessage,
                    registrationId = registrationId,
                    qrCode = mockQrCode,
                    paymentAmount = registration.PaymentAmount,
                    requiresPayment = request.Role == "Participant" && waitlistStatus == "Confirmed",
                    waitlistStatus = waitlistStatus,
                    isWaitlisted = waitlistStatus == "Waitlisted"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmationRequest request)
        {
            try
            {
                bool success = await eventsDAL.UpdatePaymentStatus(
                    request.RegistrationId,
                    "Completed",
                    request.QrCode
                );

                if (success)
                {
                    var registration = await eventsDAL.GetRegistrationById(request.RegistrationId);
                    return Json(new
                    {
                        success = true,
                        message = "Payment confirmed successfully",
                        registration = new
                        {
                            id = registration.Id,
                            eventName = registration.EventName,
                            fullName = registration.FullName,
                            role = registration.Role,
                            qrCode = registration.QrCode,
                            paymentStatus = registration.PaymentStatus
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to confirm payment" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    // Request models
    public class EventRegistrationRequest
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        
        // New volunteer fields
        public string NricLast4 { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string CitizenshipType { get; set; }
        
        // Legacy volunteer fields (keeping for backward compatibility)
        public string PreferredRole { get; set; }
        public string Skills { get; set; }
        
        // Participant fields
        public string DietaryRequirements { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyContactName { get; set; }
    }

    public class PaymentConfirmationRequest
    {
        public string RegistrationId { get; set; }
        public string QrCode { get; set; }
    }
}