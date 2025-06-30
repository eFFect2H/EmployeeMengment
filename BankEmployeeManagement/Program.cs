using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BankEmployeeManagement.Services;
using System;
using BankEmployeeManagement;
using BankEmployeeManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// ��������� ��������� �� appsettings.json
var configuration = builder.Configuration;

// ��������� ���� ������ PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins() // ������� ����� ����������� ������� �������� "http://192.168.1.101:8080"
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ���������� �������� ��� ������������ � �����������
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
//builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// ��������� JWT
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

// ���������� Swagger ��� API-������������
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.WebHost.UseUrls("http://0.0.0.0:7118", "https://0.0.0.0:5005"); // �������

var app = builder.Build();

// ��������� Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ����������� CORS
app.UseCors("AllowClient");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles(); // �������� ������� ������
app.UseStaticFiles(); // ��� ������ � HTML, CSS � JS � wwwroot

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    context.Database.Migrate();
    SeedData.Initialize(context);
}

app.Run();
