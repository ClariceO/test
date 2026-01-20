using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using H4G_Project.DAL;
using H4G_Project.Services;


var builder = WebApplication.CreateBuilder(args);

// Firebase initialization for auth
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(
            Path.Combine(Directory.GetCurrentDirectory(), "DAL", "config", "squad-60b0b-firebase-adminsdk-fbsvc-cff3f594d5.json")
        )
    });
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
