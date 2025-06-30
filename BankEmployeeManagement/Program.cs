using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BankEmployeeManagement.Services;
using System;
using BankEmployeeManagement;
using BankEmployeeManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Добавляем настройки из appsettings.json
var configuration = builder.Configuration;

// Настройка базы данных PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins() // Укажите адрес клиентского сервера например "http://192.168.1.101:8080"
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Добавление сервисов для контроллеров и авторизации
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
//builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Настройка JWT
var jwtSettings = configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true
        };
    });

// Добавление Swagger для API-документации
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.WebHost.UseUrls("http://0.0.0.0:7118", "https://0.0.0.0:5005"); // хостинг

var app = builder.Build();

// Настройка Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Используйте CORS
app.UseCors("AllowClient");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles(); // проверка наличия файлов
app.UseStaticFiles(); // Для работы с HTML, CSS и JS в wwwroot

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    context.Database.Migrate();
    SeedData.Initialize(context);
}

app.Run();
