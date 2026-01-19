using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using H4G_Project.Models;
using System.Linq;

namespace H4G_Project.DAL
{
    public class ApplicationDAL
    {
        private readonly FirestoreDb db;
        private readonly string bucketName;

        public ApplicationDAL()
        {
            // Use the helper to get credentials
            string projectId = FirebaseHelper.GetProjectId();
            bucketName = "squad-60b0b.firebasestorage.app";

            Console.WriteLine($"Using bucket: {bucketName}");

            db = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                JsonCredentials = FirebaseHelper.GetCredentialsJson()
            }.Build();
        }

        /// <summary>
        /// Add a new application and upload files to Firebase Storage
        /// </summary>
        public async Task<bool> AddApplication(Application application, IFormFile medicalReport, IFormFile idDocument)
        {
            try
            {
                string medicalReportUrl = null;
                string idDocumentUrl = null;

                // Use helper for credentials
                var credential = FirebaseHelper.GetCredential();
                var storageClient = await StorageClient.CreateAsync(credential);
                Console.WriteLine("Firebase Storage client created successfully");

                // Upload medical report
                if (medicalReport != null && medicalReport.Length > 0)
                {
                    Console.WriteLine($"Uploading medical report: {medicalReport.FileName} ({medicalReport.Length} bytes)");
                    var fileName = $"medicalReports/{Guid.NewGuid()}_{medicalReport.FileName}";

                    using var stream = medicalReport.OpenReadStream();

                    var obj = await storageClient.UploadObjectAsync(
                        bucketName,
                        fileName,
                        medicalReport.ContentType,
                        stream
                    );

                    medicalReportUrl = $"https://storage.googleapis.com/{bucketName}/{fileName}";
                    Console.WriteLine($"Medical report uploaded successfully: {medicalReportUrl}");
                }
                else
                {
                    Console.WriteLine("No medical report file provided");
                }

                // Upload ID document
                if (idDocument != null && idDocument.Length > 0)
                {
                    Console.WriteLine($"Uploading ID document: {idDocument.FileName} ({idDocument.Length} bytes)");
                    var fileName = $"idDocuments/{Guid.NewGuid()}_{idDocument.FileName}";

                    using var stream = idDocument.OpenReadStream();

                    var obj = await storageClient.UploadObjectAsync(
                        bucketName,
                        fileName,
                        idDocument.ContentType,
                        stream
                    );

                    idDocumentUrl = $"https://storage.googleapis.com/{bucketName}/{fileName}";
                    Console.WriteLine($"ID document uploaded successfully: {idDocumentUrl}");
                }
                else
                {
                    Console.WriteLine("No ID document file provided");
                }

                // Save application data to Firestore
                Console.WriteLine("Saving application data to Firestore");
                DocumentReference docRef = db.Collection("applicationForms").Document();
                Dictionary<string, object> NewApplication = new Dictionary<string, object>
                {
                    {"CaregiverName", application.CaregiverName ?? ""},
                    {"ContactNumber", application.ContactNumber ?? ""},
                    {"DisabilityType", application.DisabilityType ?? ""},
                    {"Email", application.Email ?? ""},
                    {"FamilyMemberName", application.FamilyMemberName ?? ""},
                    {"DateOfBirth", application.DateOfBirth ?? ""},
                    {"Notes", application.Notes ?? ""},
                    {"Occupation", application.Occupation ?? ""},
                    {"MedicalReportUrl", medicalReportUrl ?? ""},
                    {"IdDocumentUrl", idDocumentUrl ?? ""},
                    {"Status", "Pending"}
                };

                Console.WriteLine($"Document data prepared. Document ID: {docRef.Id}");
                await docRef.SetAsync(NewApplication);
                Console.WriteLine("Application saved to Firestore successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding application: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> AddVolunteerApplication(VolunteerApplication application, IFormFile resume)
        {
            try
            {
                string resumeUrl = null;

                var credential = FirebaseHelper.GetCredential();
                var storageClient = await StorageClient.CreateAsync(credential);
                Console.WriteLine("Firebase Storage client created successfully");

                if (resume != null && resume.Length > 0)
                {
                    var fileName = $"resumes/{Guid.NewGuid()}_{resume.FileName}";

                    using var stream = resume.OpenReadStream();

                    var obj = await storageClient.UploadObjectAsync(
                        bucketName,
                        fileName,
                        resume.ContentType,
                        stream
                    );

                    resumeUrl = $"https://storage.googleapis.com/{bucketName}/{fileName}";
                    Console.WriteLine($"Resume uploaded successfully: {resumeUrl}");
                }
                else
                {
                    Console.WriteLine("No resume file provided");
                }

                Console.WriteLine("Saving application data to Firestore");
                DocumentReference docRef = db.Collection("volunteerApplicationForms").Document();
                Dictionary<string, object> NewApplication = new Dictionary<string, object>
                {
                    {"Name", application.Name ?? ""},
                    {"ContactNumber", application.ContactNumber ?? ""},
                    {"DateOfBirth", application.DateOfBirth ?? ""},
                    {"Email", application.Email ?? ""},
                    {"Notes", application.Notes ?? ""},
                    {"Occupation", application.Occupation ?? ""},
                    {"ResumeUrl", resumeUrl ?? ""},
                    {"Status", "Pending"}
                };

                Console.WriteLine($"Document data prepared. Document ID: {docRef.Id}");
                await docRef.SetAsync(NewApplication);
                Console.WriteLine("Application saved to Firestore successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding application: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<Application>> GetAllApplications()
        {
            List<Application> applicationList = new List<Application>();

            try
            {
                Console.WriteLine("=== GETTING ALL APPLICATIONS DEBUG ===");
                Query allApplicationsQuery = db.Collection("applicationForms");
                QuerySnapshot snapshot = await allApplicationsQuery.GetSnapshotAsync();

                Console.WriteLine($"Total documents found in applicationForms collection: {snapshot.Documents.Count}");

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        try
                        {
                            Console.WriteLine($"Processing document ID: {document.Id}");

                            var data = new Application
                            {
                                Id = document.Id,
                                CaregiverName = GetFieldValueSafely(document, "CaregiverName"),
                                ContactNumber = GetFieldValueSafely(document, "ContactNumber"),
                                DisabilityType = GetFieldValueSafely(document, "DisabilityType"),
                                Email = GetFieldValueSafely(document, "Email"),
                                FamilyMemberName = GetFieldValueSafely(document, "FamilyMemberName"),
                                Notes = GetFieldValueSafely(document, "Notes"),
                                Occupation = GetFieldValueSafely(document, "Occupation"),
                                MedicalReportUrl = GetFieldValueSafely(document, "MedicalReportUrl"),
                                IdDocumentUrl = GetFieldValueSafely(document, "IdDocumentUrl"),
                                Status = GetFieldValueSafely(document, "Status", "Pending")
                            };

                            Console.WriteLine($"Document {document.Id} - Caregiver: {data.CaregiverName}, Family Member: {data.FamilyMemberName}");

                            try
                            {
                                if (document.ContainsField("DateOfBirth"))
                                {
                                    var dobValue = document.GetValue<object>("DateOfBirth");
                                    if (dobValue is Google.Cloud.Firestore.Timestamp timestamp)
                                    {
                                        data.DateOfBirth = timestamp.ToDateTime().ToString("yyyy-MM-dd");
                                        Console.WriteLine($"Document {document.Id} - DateOfBirth (Timestamp): {data.DateOfBirth}");
                                    }
                                    else if (dobValue is string dobString)
                                    {
                                        data.DateOfBirth = dobString;
                                        Console.WriteLine($"Document {document.Id} - DateOfBirth (String): {data.DateOfBirth}");
                                    }
                                    else
                                    {
                                        data.DateOfBirth = "";
                                        Console.WriteLine($"Document {document.Id} - DateOfBirth: Unknown type, set to empty");
                                    }
                                }
                                else
                                {
                                    data.DateOfBirth = "";
                                    Console.WriteLine($"Document {document.Id} - DateOfBirth: Field not found, set to empty");
                                }
                            }
                            catch (Exception dobEx)
                            {
                                Console.WriteLine($"Error processing DateOfBirth for document {document.Id}: {dobEx.Message}");
                                data.DateOfBirth = "";
                            }

                            if (!string.IsNullOrEmpty(data.MedicalReportUrl))
                            {
                                data.MedicalReportUrl = await GenerateSignedUrl(data.MedicalReportUrl);
                            }

                            if (!string.IsNullOrEmpty(data.IdDocumentUrl))
                            {
                                data.IdDocumentUrl = await GenerateSignedUrl(data.IdDocumentUrl);
                            }

                            applicationList.Add(data);
                            Console.WriteLine($"Successfully added document {document.Id} to list");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR processing document {document.Id}: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Document {document.Id} does not exist, skipping");
                    }
                }

                Console.WriteLine($"Total applications successfully retrieved: {applicationList.Count}");
                Console.WriteLine("=== END GETTING ALL APPLICATIONS DEBUG ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetAllApplications: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return applicationList;
        }

        private string GetFieldValueSafely(DocumentSnapshot document, string fieldName, string defaultValue = "")
        {
            try
            {
                if (document.ContainsField(fieldName))
                {
                    return document.GetValue<string>(fieldName) ?? defaultValue;
                }
                else
                {
                    Console.WriteLine($"Field '{fieldName}' not found in document {document.Id}, using default value");
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting field '{fieldName}' from document {document.Id}: {ex.Message}");
                return defaultValue;
            }
        }

        public async Task<List<VolunteerApplication>> GetAllVolunteerApplications()
        {
            List<VolunteerApplication> applicationList = new List<VolunteerApplication>();

            Query allApplicationsQuery = db.Collection("volunteerApplicationForms");
            QuerySnapshot snapshot = await allApplicationsQuery.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (document.Exists)
                {
                    VolunteerApplication data = document.ConvertTo<VolunteerApplication>();
                    data.Id = document.Id;

                    if (!string.IsNullOrEmpty(data.ResumeUrl))
                    {
                        data.ResumeUrl = await GenerateSignedUrl(data.ResumeUrl);
                    }

                    applicationList.Add(data);
                }
            }

            return applicationList;
        }

        private async Task<string> GenerateSignedUrl(string storageUrl)
        {
            try
            {
                var uri = new Uri(storageUrl);
                var pathSegments = uri.AbsolutePath.Split('/');
                var fileName = string.Join("/", pathSegments.Skip(2));

                var credential = FirebaseHelper.GetCredential();
                var urlSigner = UrlSigner.FromCredential(credential);

                var signedUrl = await urlSigner.SignAsync(bucketName, fileName, TimeSpan.FromHours(1), HttpMethod.Get);
                return signedUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating signed URL: {ex.Message}");
                return storageUrl;
            }
        }

        public async Task<bool> UpdateApplicationStatus(string applicationId, string status)
        {
            try
            {
                DocumentReference docRef = db.Collection("applicationForms").Document(applicationId);
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    {"Status", status}
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating application status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateVolunteerApplicationStatus(string applicationId, string status)
        {
            try
            {
                DocumentReference docRef = db.Collection("volunteerApplicationForms").Document(applicationId);
                Dictionary<string, object> updates = new()
                {
                    {"Status", status}
                };

                await docRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating volunteer application status: {ex.Message}");
                return false;
            }
        }

        public async Task<VolunteerApplication> GetVolunteerApplicationById(string applicationId)
        {
            try
            {
                DocumentReference docRef = db.Collection("volunteerApplicationForms").Document(applicationId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    VolunteerApplication data = snapshot.ConvertTo<VolunteerApplication>();
                    data.Id = snapshot.Id;

                    if (!string.IsNullOrEmpty(data.ResumeUrl))
                    {
                        data.ResumeUrl = await GenerateSignedUrl(data.ResumeUrl);
                    }

                    return data;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting volunteer application by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<Application> GetApplicationByEmail(string email)
        {
            try
            {
                Query query = db.Collection("applicationForms").WhereEqualTo("Email", email);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        Application data = document.ConvertTo<Application>();
                        data.Id = document.Id;
                        return data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting application by email: {ex.Message}");
                return null;
            }
        }
    }
}