
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.Services.Admin;
using PROTOTYPE_backend.Services.Auth;
using PROTOTYPE_backend.Services.Calendar;
using PROTOTYPE_backend.Services.Email;
using PROTOTYPE_backend.Services.SoftDelete;
using PROTOTYPE_backend.Services.Storage;
using PROTOTYPE_backend.Services.Token;
using PROTOTYPE_backend.Services.Users;
using PROTOTYPE_backend.Services.Workspaces;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Illeszd be a JWT tokent!",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",              
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISoftDeleteService, SoftDeleteService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IEmailService, EmailService>();


var jwtSection = builder.Configuration.GetSection("JwtSettings");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-sensitive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                   
                Window = TimeSpan.FromMinutes(1),  
                QueueLimit = 0
            }));
});

builder.Services.AddAuthorization();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();