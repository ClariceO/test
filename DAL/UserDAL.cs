using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using H4G_Project.Models;

namespace H4G_Project.DAL
{
    public class UserDAL
    {
        private readonly FirestoreDb db;

        public UserDAL()
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

        // Get user by username
        public async Task<User?> GetUserByUsername(string username)
        {
            CollectionReference usersRef = db.Collection("users");
            Query query = usersRef.WhereEqualTo("Username", username);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                DocumentSnapshot doc = snapshot.Documents[0];
                return doc.Exists ? doc.ConvertTo<User>() : null;
            }

            return null;
        }

        // Get user by email
        public async Task<User?> GetUserByEmail(string email)
        {
            CollectionReference usersRef = db.Collection("users");
            Query query = usersRef.WhereEqualTo("Email", email);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                DocumentSnapshot doc = snapshot.Documents[0];
                return doc.Exists ? doc.ConvertTo<User>() : null;
            }

            return null;
        }

        // Add new user
        public async Task<bool> AddUser(User user)
        {
            try
            {
                var userData = new Dictionary<string, object>
                {
                    { "Username", user.Username },
                    { "Email", user.Email },
                    { "PhoneNumber", user.PhoneNumber ?? "" }, // Add phone number field
                    { "DateOfBirth", user.DateOfBirth ?? "" }, // Add date of birth field
                    { "Role", user.Role },
                    { "EngagementType", user.EngagementType ?? "Ad hoc engagement" }, // Default engagement type
                    { "LastDayOfService", user.LastDayOfService ?? null } // Default to null
                };

                await db.Collection("users").AddAsync(userData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
                return false;
            }
        }

        // Get all users
        public async Task<List<User>> GetAllUsers()
        {
            CollectionReference usersRef = db.Collection("users");
            QuerySnapshot snapshot = await usersRef.GetSnapshotAsync();

            List<User> userList = new List<User>();
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                    userList.Add(doc.ConvertTo<User>());
            }

            return userList;
        }

        // Delete user by email
        public async Task<bool> DeleteUser(string email)
        {
            try
            {
                CollectionReference usersRef = db.Collection("users");
                Query query = usersRef.WhereEqualTo("Email", email);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    DocumentSnapshot doc = snapshot.Documents[0];
                    await doc.Reference.DeleteAsync();
                    return true;
                }

                return false; // User not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return false;
            }
        }

        // Update user LastDayOfService (for deactivation)
        public async Task<bool> UpdateUserLastDayOfService(string email, string lastDayOfService)
        {
            try
            {
                CollectionReference usersRef = db.Collection("users");
                Query query = usersRef.WhereEqualTo("Email", email);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    DocumentSnapshot doc = snapshot.Documents[0];
                    var updates = new Dictionary<string, object>
                    {
                        { "LastDayOfService", lastDayOfService ?? "" }
                    };
                    await doc.Reference.UpdateAsync(updates);
                    return true;
                }

                return false; // User not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user LastDayOfService: {ex.Message}");
                return false;
            }
        }

        // Update user engagement type
        public async Task<bool> UpdateEngagementType(string email, string engagementType)
        {
            try
            {
                CollectionReference usersRef = db.Collection("users");
                Query query = usersRef.WhereEqualTo("Email", email);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    DocumentSnapshot doc = snapshot.Documents[0];
                    await doc.Reference.UpdateAsync("EngagementType", engagementType);
                    return true;
                }

                return false; // User not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating engagement type: {ex.Message}");
                return false;
            }
        }
    }
}
