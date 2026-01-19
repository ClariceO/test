using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using H4G_Project.DAL;
using H4G_Project.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Firebase initialization from base64 environment variable
if (FirebaseApp.DefaultInstance == null)
{
    var firebaseCredentialsBase64 = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_BASE64");

    if (string.IsNullOrEmpty(firebaseCredentialsBase64))
    {
        throw new InvalidOperationException("FIREBASE_CREDENTIALS_BASE64 environment variable is not set");
    }

    try
    {
        // Decode base64 to JSON string
        var credentialsJson = Encoding.UTF8.GetString(Convert.FromBase64String(firebaseCredentialsBase64));

        // Initialize Firebase
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(credentialsJson)
        });

        Console.WriteLine("Firebase initialized successfully from environment variable");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to initialize Firebase: {ex.Message}", ex);
    }
}

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<UserDAL>();
builder.Services.AddScoped<StaffDAL>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();