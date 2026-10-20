using DeliveryTracker.Server.Data;
using DeliveryTracker.Server.Identity;
using DeliveryTracker.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("DefaultConnection")));




//services 

QuestPDF.Settings.License = LicenseType.Community;
var fontPath = Path.Combine(
	builder.Environment.ContentRootPath,
	"Resources",
	"Fonts",
	"NotoNaskhArabic-Regular.ttf");

using (var fontStream = File.OpenRead(fontPath))
{
	FontManager.RegisterFont(fontStream);
}
builder.Services.AddSingleton<DeliveryReceiptPdfService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
		options.Password.RequiredLength = 10;
		options.Password.RequireDigit = true;
		options.Password.RequireLowercase = true;
		options.Password.RequireUppercase = true;
		options.Password.RequireNonAlphanumeric = true;
		options.User.RequireUniqueEmail = false;
		options.Lockout.DefaultLockoutTimeSpan =
			TimeSpan.FromMinutes(15);
		options.Lockout.MaxFailedAccessAttempts = 5;
		options.Lockout.AllowedForNewUsers = true;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddSignInManager()
	.AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
	options.ValidationInterval = TimeSpan.FromMinutes(1);
});

builder.Services.Configure<CookieAuthenticationOptions>(
	IdentityConstants.ApplicationScheme,
	options => 
	{
	options.Cookie.Name = "__Host-DeliveryTracker";
	options.Cookie.Path = "/";
	options.Cookie.Domain = null;
	options.Cookie.HttpOnly = true;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
	options.Cookie.SameSite = SameSiteMode.Lax;

	options.LoginPath = "/login";
	options.ExpireTimeSpan = TimeSpan.FromDays(7);
	options.SlidingExpiration = true;

	// إرجاع 401 بدلاً من تحويل طلبات API إلى صفحة الدخول
	options.Events.OnRedirectToLogin = context =>
	{
		context.Response.StatusCode = 401;
		return Task.CompletedTask;
	};

	// إرجاع 403 عند عدم امتلاك الصلاحية
	options.Events.OnRedirectToAccessDenied = context =>
	{
		context.Response.StatusCode = 403;
		return Task.CompletedTask;
	};

	// فحص صلاحية الجلسة وحالة المستخدم
	options.Events.OnValidatePrincipal = async context =>
	{
		// التحقق من SecurityStamp
		await SecurityStampValidator.ValidatePrincipalAsync(context);

		if (context.Principal?.Identity?.IsAuthenticated != true)
			return;

		var userManager = context.HttpContext.RequestServices
			.GetRequiredService<UserManager<ApplicationUser>>();

		var user = await userManager.GetUserAsync(context.Principal);

		// رفض الجلسة إذا تم حذف الحساب أو تعطيله
		if (user is null || !user.IsActive)
		{
			context.RejectPrincipal();

			await context.HttpContext.SignOutAsync(
				IdentityConstants.ApplicationScheme);
		}
	};
});

builder.Services.AddAuthorization(options =>
{
	options.FallbackPolicy =
		new AuthorizationPolicyBuilder()
			.RequireAuthenticatedUser()
			.Build();
});



builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = 429;

	options.AddPolicy("login", context =>RateLimitPartition.GetFixedWindowLimiter(partitionKey:	context.Connection.RemoteIpAddress?.ToString()?? "unknown",	factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 5,
				Window = TimeSpan.FromMinutes(5),
				QueueLimit = 0,
				AutoReplenishment = true
			}));
});



// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Antiforgery
builder.Services.AddAntiforgery(options =>
{
	options.HeaderName = "X-CSRF-TOKEN";
	options.Cookie.Name = "__Host-DeliveryTracker-CSRF";
	options.Cookie.HttpOnly = true;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
	options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	await IdentitySeeder.SeedAsync(
		app.Services,
		app.Configuration);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}



app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllers();

app.MapFallbackToFile("index.html")
   .AllowAnonymous();

app.Run();
