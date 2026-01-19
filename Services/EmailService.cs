using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace H4G_Project.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendVolunteerApprovalEmailAsync(string toEmail, string volunteerName, string password)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "MINDS Volunteer System";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? "", fromName),
                    Subject = "Volunteer Application Approved - Welcome to MINDS!",
                    Body = CreateApprovalEmailBody(volunteerName, toEmail, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Approval email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending approval email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendVolunteerRejectionEmailAsync(string toEmail, string volunteerName)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "MINDS Volunteer System";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? "", fromName),
                    Subject = "Volunteer Application Status Update - MINDS",
                    Body = CreateRejectionEmailBody(volunteerName),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Rejection email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending rejection email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendClientApplicationApprovalEmailAsync(string toEmail, string clientName, string familyMemberName, string password)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "MINDS Client Services";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? "", fromName),
                    Subject = "Application Approved - Welcome to MINDS Services!",
                    Body = CreateClientApprovalEmailBody(clientName, familyMemberName, toEmail, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Client approval email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending client approval email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendClientApplicationRejectionEmailAsync(string toEmail, string clientName, string familyMemberName)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "MINDS Client Services";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? "", fromName),
                    Subject = "Application Status Update - MINDS Services",
                    Body = CreateClientRejectionEmailBody(clientName, familyMemberName),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Client rejection email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending client rejection email: {ex.Message}");
                return false;
            }
        }

        private string CreateApprovalEmailBody(string volunteerName, string email, string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .credentials {{ background-color: #e8f5e8; padding: 15px; border-left: 4px solid #4CAF50; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Congratulations!</h1>
            <h2>Your Volunteer Application Has Been Approved</h2>
        </div>
        
        <div class='content'>
            <p>Dear {volunteerName},</p>
            
            <p>We are delighted to inform you that your volunteer application with MINDS has been <strong>approved</strong>! Welcome to our volunteer community.</p>
            
            <p>Your account has been created and you can now access the volunteer portal using the following credentials:</p>
            
            <div class='credentials'>
                <h3>🔐 Login Credentials</h3>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Temporary Password:</strong> {password}</p>
            </div>
            
            <div class='warning'>
                <h3>⚠️ Important Security Notice</h3>
                <p><strong>Do remember to reset your password</strong> after your first login for security purposes.</p>
            </div>
            
            <p>You can now:</p>
            <ul>
                <li>Browse and register for volunteer events</li>
                <li>View your volunteer schedule</li>
                <li>Update your profile information</li>
                <li>Connect with other volunteers</li>
            </ul>
            
            <p>Thank you for choosing to volunteer with MINDS. Your contribution will make a meaningful difference in the lives of those we serve.</p>
            
            <p>If you have any questions or need assistance, please don't hesitate to contact us.</p>
            
            <p>Best regards,<br>
            <strong>MINDS Volunteer Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateRejectionEmailBody(string volunteerName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .message {{ background-color: #f8d7da; padding: 15px; border-left: 4px solid #dc3545; margin: 20px 0; }}
        .encouragement {{ background-color: #d1ecf1; padding: 15px; border-left: 4px solid #17a2b8; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Volunteer Application Status Update</h1>
            <h2>MINDS Volunteer Program</h2>
        </div>
        
        <div class='content'>
            <p>Dear {volunteerName},</p>
            
            <p>Thank you for your interest in volunteering with MINDS and for taking the time to submit your application.</p>
            
            <div class='message'>
                <h3>📋 Application Status</h3>
                <p>After careful consideration, we regret to inform you that we are unable to proceed with your volunteer application at this time.</p>
            </div>
            
            <p>Please know that this decision does not reflect on your character or your desire to help others. We receive many applications and have limited volunteer positions available that match specific requirements and timing.</p>
            
            <div class='encouragement'>
                <h3>💡 Future Opportunities</h3>
                <p>We encourage you to:</p>
                <ul>
                    <li>Check our website periodically for new volunteer opportunities</li>
                    <li>Consider applying again in the future when new positions become available</li>
                    <li>Follow us on social media to stay updated on our programs and events</li>
                    <li>Explore other ways to support our mission</li>
                </ul>
            </div>
            
            <p>We truly appreciate your willingness to contribute to our cause and support individuals with intellectual disabilities. Your interest in volunteering demonstrates your commitment to making a positive impact in the community.</p>
            
            <p>If you have any questions about this decision or would like feedback on your application, please feel free to contact us.</p>
            
            <p>Thank you once again for your interest in MINDS.</p>
            
            <p>Best regards,<br>
            <strong>MINDS Volunteer Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>For inquiries, please contact us through our official channels.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateClientApprovalEmailBody(string clientName, string familyMemberName, string email, string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .message {{ background-color: #d4edda; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; }}
        .credentials {{ background-color: #e8f5e8; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; }}
        .next-steps {{ background-color: #e2e3e5; padding: 15px; border-left: 4px solid #6c757d; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Application Approved!</h1>
            <h2>Welcome to MINDS Services</h2>
        </div>
        
        <div class='content'>
            <p>Dear {clientName},</p>
            
            <p>We are delighted to inform you that your application for MINDS services for <strong>{familyMemberName}</strong> has been <strong>approved</strong>!</p>
            
            <div class='message'>
                <h3>✅ Application Status: APPROVED</h3>
                <p>Your application has been reviewed and accepted. We look forward to supporting {familyMemberName} and your family through our programs and services.</p>
            </div>
            
            <p>Your client portal account has been created and you can now access our services using the following credentials:</p>
            
            <div class='credentials'>
                <h3>🔐 Login Credentials</h3>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Temporary Password:</strong> {password}</p>
            </div>
            
            <div class='warning'>
                <h3>⚠️ Important Security Notice</h3>
                <p><strong>Do remember to reset your password</strong> after your first login for security purposes.</p>
            </div>
            
            <div class='next-steps'>
                <h3>📋 Next Steps</h3>
                <p>Our team will be in touch with you within the next few business days to:</p>
                <ul>
                    <li>Schedule an initial consultation and assessment</li>
                    <li>Discuss available programs and services that best meet your needs</li>
                    <li>Provide you with detailed information about our support options</li>
                    <li>Answer any questions you may have about our services</li>
                    <li>Begin the enrollment process for appropriate programs</li>
                </ul>
            </div>
            
            <p>You can now:</p>
            <ul>
                <li>Access your client portal to view service information</li>
                <li>Update your family's profile and preferences</li>
                <li>Track service progress and appointments</li>
                <li>Communicate with your assigned case manager</li>
                <li>Access resources and support materials</li>
            </ul>
            
            <p><strong>What to expect:</strong></p>
            <ul>
                <li>A dedicated case manager will be assigned to your family</li>
                <li>Personalized service planning based on individual needs</li>
                <li>Access to our comprehensive range of support services</li>
                <li>Regular progress reviews and service adjustments as needed</li>
            </ul>
            
            <p>We understand that seeking support services is an important step for your family, and we are committed to providing compassionate, professional care that enhances quality of life and promotes independence.</p>
            
            <p>If you have any immediate questions or concerns, please don't hesitate to contact our client services team.</p>
            
            <p>Welcome to the MINDS family!</p>
            
            <p>Warm regards,<br>
            <strong>MINDS Client Services Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>For inquiries, please contact our client services team through our official channels.</p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task<bool> SendStaffAccountCreationEmailAsync(string toEmail, string staffName, string password)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "MINDS";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? "", fromName),
                    Subject = "Staff Account Created - Welcome to MINDS Team!",
                    Body = CreateStaffAccountEmailBody(staffName, toEmail, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Staff account creation email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending staff account creation email: {ex.Message}");
                return false;
            }
        }

        private string CreateStaffAccountEmailBody(string staffName, string email, string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .credentials {{ background-color: #e3f2fd; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to the MINDS Team!</h1>
            <h2>Your Staff Account Has Been Created</h2>
        </div>
        
        <div class='content'>
            <p>Dear {staffName},</p>
            
            <p>Welcome to MINDS! Your staff account has been successfully created and you now have access to the MINDS staff portal.</p>
            
            <p>You can now log in to the system using the following credentials:</p>
            
            <div class='credentials'>
                <h3>🔐 Login Credentials</h3>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
            </div>
            
            <div class='warning'>
                <h3>⚠️ Important Security Notice</h3>
                <p><strong>Do remember to reset your password</strong> after your first login for security purposes.</p>
            </div>
            
            <p>As a MINDS staff member, you can now:</p>
            <ul>
                <li>Manage volunteer and client applications</li>
                <li>Create and manage events</li>
                <li>View reports and analytics</li>
                <li>Manage user accounts and registrations</li>
                <li>Access the staff dashboard and tools</li>
            </ul>
            
            <p>If you have any questions about using the system or need assistance with your account, please don't hesitate to contact the IT administrator or your supervisor.</p>
            
            <p>We're excited to have you as part of the MINDS team and look forward to working with you to support our mission of helping individuals with intellectual disabilities.</p>
            
            <p>Best regards,<br>
            <strong>MINDS Administration Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateClientRejectionEmailBody(string clientName, string familyMemberName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .message {{ background-color: #f8d7da; padding: 15px; border-left: 4px solid #dc3545; margin: 20px 0; }}
        .support {{ background-color: #d1ecf1; padding: 15px; border-left: 4px solid #17a2b8; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Application Status Update</h1>
            <h2>MINDS Client Services</h2>
        </div>
        
        <div class='content'>
            <p>Dear {clientName},</p>
            
            <p>Thank you for your interest in MINDS services for {familyMemberName} and for taking the time to complete our application process.</p>
            
            <div class='message'>
                <h3>📋 Application Status</h3>
                <p>After careful review of your application, we regret to inform you that we are unable to provide services at this time. This decision may be due to various factors such as current capacity limitations, service area restrictions, or specific program requirements.</p>
            </div>
            
            <p>Please understand that this decision does not reflect on the value or importance of your family's needs. We recognize that seeking support services is a significant step, and we appreciate the trust you placed in MINDS during the application process.</p>
            
            <div class='support'>
                <h3>🤝 Alternative Support Options</h3>
                <p>While we cannot provide services at this time, we encourage you to:</p>
                <ul>
                    <li>Contact other disability service providers in your area</li>
                    <li>Reach out to local government disability support services</li>
                    <li>Connect with community support groups and advocacy organizations</li>
                    <li>Consider reapplying to MINDS in the future when circumstances may change</li>
                    <li>Stay informed about new programs and services we may offer</li>
                </ul>
            </div>
            
            <p>We understand this news may be disappointing, and we want to assure you that there are other resources and organizations that may be able to provide the support {familyMemberName} needs.</p>
            
            <p>If you have questions about this decision or would like information about other potential resources, please feel free to contact our client services team.</p>
            
            <p>We wish you and {familyMemberName} all the best in finding the appropriate support and services.</p>
            
            <p>Sincerely,<br>
            <strong>MINDS Client Services Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>For inquiries, please contact our client services team through our official channels.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}