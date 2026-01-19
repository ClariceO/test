using Google.Apis.Auth.OAuth2;
using System;
using System.Text;

namespace H4G_Project.DAL
{
    public static class FirebaseHelper
    {
        private static string _credentialsJson;
        private const string ProjectId = "squad-60b0b";

        public static string GetCredentialsJson()
        {
            if (_credentialsJson != null)
                return _credentialsJson;

            var firebaseCredentialsBase64 = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_BASE64");

            if (string.IsNullOrEmpty(firebaseCredentialsBase64))
            {
                throw new InvalidOperationException("FIREBASE_CREDENTIALS_BASE64 environment variable is not set");
            }

            try
            {
                _credentialsJson = Encoding.UTF8.GetString(Convert.FromBase64String(firebaseCredentialsBase64));
                return _credentialsJson;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decode Firebase credentials: {ex.Message}", ex);
            }
        }

        public static GoogleCredential GetCredential()
        {
            return GoogleCredential.FromJson(GetCredentialsJson());
        }

        public static string GetProjectId()
        {
            return ProjectId;
        }
    }
}