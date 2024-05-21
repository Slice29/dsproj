using System.Text;
using AuthAPI.Data;
using AuthAPI.Email;
using AuthAPI.JWT;
using AuthAPI.Models;
using AuthAPI.Services;
using AutoMapper;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using AuthAPI.MyRateLimiting;
using System;

var builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

bool IsRunningInContainer()
{
    return bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) && inContainer;
}


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("ServiceConnectionLocal");
    if (IsRunningInContainer() == true) connectionString = builder.Configuration.GetConnectionString("ServiceConnectionContainer");
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(true);
});


builder.Services.AddScoped<TokenService>();

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;
    options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddUserValidator<UserValidator>();

//.AddTokenProvider<CustomTotpSecurityStampBasedTokenProvider<User>>("Email");

// Configure the default cookie used by the sign-in manager
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = false;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/login";  // Adjust according to your application's route
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
});


// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("MyPolicy", builder =>
//     {
//         builder.WithOrigins("http://localhost:5250")
//                .AllowAnyHeader()
//                .AllowAnyMethod()
//                .AllowCredentials(); // Important for cookies
//     });
// });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();


builder.Services.AddHttpClient();

builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<AzureAuthenticationService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
{
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("ECT_JWT_ISSUER"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ECT_JWT_KEY")))
    };
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.LoginPath = "/login";
});


builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

});
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ExponentialBackoffRateLimiter>>();
    return new ExponentialBackoffRateLimiter(permitLimit: 5, logger);
});


builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("admin", "true"));
});

builder.Services.AddControllers();
var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRateLimiter();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/login/verify-2fa"), appBuilder =>
{
    var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<ExponentialBackoffRateLimitingMiddleware>>();
    appBuilder.UseMiddleware<ExponentialBackoffRateLimitingMiddleware>(logger);
});

//app.UseCors("MyPolicy");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();


    foreach (var role in new[] { "Admin", "PromoUser" })
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //* Configuring Swagger UI
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        // Other Swagger configurations...
    });
}

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.Run();
