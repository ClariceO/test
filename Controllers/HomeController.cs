using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using H4G_Project.Models;
using H4G_Project.DAL;
using System.Threading.Tasks;
using System;

namespace H4G_Project.Controllers
{
    public class HomeController : Controller
    {
        private UserDAL userContext = new UserDAL();
        private StaffDAL staffContext = new StaffDAL();

        // GET: HomeController
        // Login page (no time for an actual homepage)
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        public ActionResult CreateVolunteer()
        {
            return View();
        }

        // check two different forms to see if there are any values in the text field.
        // Run login function to check the db for confirmation on the user for the form with details
        // go to respective pages when login is a success and set storage details

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(IFormCollection formData)
        {
            string memberemail = formData["memberlogin"];
            string staffemail = formData["stafflogin"];
            string staffpassword = formData["staffpassword"];

            Console.WriteLine($"Login attempt - Member email: '{memberemail}', Staff email: '{staffemail}'");

            // Check member login
            if (!string.IsNullOrEmpty(memberemail))
            {
                Console.WriteLine($"Attempting member login for: {memberemail}");
                var user = await userContext.GetUserByEmail(memberemail);

                if (user != null)
                {
                    Console.WriteLine($"User found: {user.Email}, Username: {user.Username}, Role: {user.Role}");

                    // Check if user is deactivated (has LastDayOfService set)
                    if (!string.IsNullOrEmpty(user.LastDayOfService))
                    {
                        Console.WriteLine($"BLOCKING LOGIN - User access denied - deactivated on {user.LastDayOfService}");
                        TempData["MemberMessage"] = "Access denied. Your account has been deactivated.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Set proper session variables for user
                    HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
                    HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                    HttpContext.Session.SetString("UserRole", user.Role ?? "Participant");

                    Console.WriteLine($"User logged in: {user.Email} ({user.Role})");
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    Console.WriteLine($"User not found for email: {memberemail}");
                    TempData["MemberMessage"] = "Invalid member email";
                }
            }

            // Check staff login with LastDayOfService validation
            if (!string.IsNullOrEmpty(staffemail))
            {
                Console.WriteLine($"Attempting staff login for: {staffemail}");
                var staff = await staffContext.GetStaffByEmail(staffemail);

                if (staff != null)
                {
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
                                TempData["StaffMessage"] = "Access denied. Your employment period has ended.";
                                return RedirectToAction("Index", "Home");
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

                    // Set proper session variables for staff
                    HttpContext.Session.SetString("StaffEmail", staff.Email ?? string.Empty);
                    HttpContext.Session.SetString("StaffUsername", staff.Username ?? string.Empty);
                    HttpContext.Session.SetString("UserRole", "Staff");

                    Console.WriteLine($"Staff logged in: {staff.Email} (Staff)");
                    return RedirectToAction("Index", "Staff");
                }
                else
                {
                    Console.WriteLine($"Staff not found for email: {staffemail}");
                    TempData["StaffMessage"] = "Invalid staff email";
                }
            }

            if (string.IsNullOrEmpty(memberemail) && string.IsNullOrEmpty(staffemail))
            {
                TempData["MemberMessage"] = "Please enter an email address";
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: HomeController/Create
        // Create a member page
        // Additional dropdown list details are added for country and cities (special feature)

    }
}