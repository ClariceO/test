using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using H4G_Project.Models;

namespace H4G_Project.DAL
{
    public class StaffDAL
    {
        private readonly FirestoreDb db;

        public StaffDAL()
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

        // Get a staff by email
        public async Task<Staff?> GetStaffByEmail(string email)
        {
            CollectionReference staffRef = db.Collection("staff");
            Query query = staffRef.WhereEqualTo("Email", email);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                DocumentSnapshot doc = snapshot.Documents[0];
                return doc.Exists ? doc.ConvertTo<Staff>() : null;
            }

            return null;
        }

        // Add new staff
        public async Task<bool> AddStaff(Staff staff)
        {
            try
            {
                var staffData = new Dictionary<string, object>
                {
                    { "Username", staff.Username },
                    { "Email", staff.Email },
                    { "LastDayOfService", staff.LastDayOfService }
                };

                await db.Collection("staff").AddAsync(staffData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding staff: {ex.Message}");
                return false;
            }
        }

        // Get all staff
        public async Task<List<Staff>> GetAllStaff()
        {
            CollectionReference staffRef = db.Collection("staff");
            QuerySnapshot snapshot = await staffRef.GetSnapshotAsync();

            List<Staff> staffList = new List<Staff>();
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                    staffList.Add(doc.ConvertTo<Staff>());
            }

            return staffList;
        }

        // Update staff LastDayOfService
        public async Task<bool> UpdateStaffLastDayOfService(string email, string lastDayOfService)
        {
            try
            {
                CollectionReference staffRef = db.Collection("staff");
                Query query = staffRef.WhereEqualTo("Email", email);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Documents.Count == 0)
                {
                    return false; // Staff not found
                }

                DocumentSnapshot staffDoc = querySnapshot.Documents[0];
                DocumentReference staffDocRef = staffDoc.Reference;

                var updates = new Dictionary<string, object>
                {
                    { "LastDayOfService", lastDayOfService ?? "" }
                };

                await staffDocRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating staff LastDayOfService: {ex.Message}");
                return false;
            }
        }
    }
}
