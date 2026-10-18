using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.Services.SoftDelete;
using PROTOTYPE_backend.Services.Workspace;
using PROTOTYPE_backend.Services.Storage;
using PROTOTYPE_backend.Services.Admin;
using PROTOTYPE_backend.Services.Auth;
using PROTOTYPE_backend.Services.Calendar;
using PROTOTYPE_backend.Services.Token;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

// app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();