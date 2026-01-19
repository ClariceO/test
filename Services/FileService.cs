using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;

namespace H4G_Project.Services
{
    public class FileService
    {
        private readonly string bucketName = "squad-60b0b.appspot.com";
        private readonly string serviceAccountPath = "path-to-service-account.json";

        public async Task<string> UploadFileToFirebase(IFormFile file)
        {
            var credential = GoogleCredential.FromFile(serviceAccountPath);
            var storageClient = await StorageClient.CreateAsync(credential);

            var fileName = $"medicalReport/{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();

            await storageClient.UploadObjectAsync(
                bucketName,
                fileName,
                file.ContentType,
                stream
            );

            // Generate a signed URL valid for 1 hour
            UrlSigner urlSigner = UrlSigner.FromServiceAccountPath(serviceAccountPath);
            string signedUrl = urlSigner.Sign(
                bucketName,
                fileName,
                TimeSpan.FromHours(1), // validity period
                HttpMethod.Get
            );

            return signedUrl;
        }
    }
}