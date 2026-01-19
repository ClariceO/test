using FirebaseAdmin.Messaging;
using System.Threading.Tasks;

namespace H4G_Project.Services
{
    public class NotificationService
    {
        public async Task SendNotificationAsync(string deviceToken, string title, string body)
        {
            try
            {
                var message = new Message()
                {
                    Token = deviceToken, // FCM device token from client app
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine("Successfully sent message: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }

        public async Task SendNotificationToMultipleAsync(List<string> deviceTokens, string title, string body)
        {
            try
            {
                var message = new MulticastMessage()
                {
                    Tokens = deviceTokens,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                Console.WriteLine($"Successfully sent {response.SuccessCount} messages out of {deviceTokens.Count}");

                if (response.FailureCount > 0)
                {
                    Console.WriteLine($"Failed to send {response.FailureCount} messages");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notifications: {ex.Message}");
            }
        }
    }
}