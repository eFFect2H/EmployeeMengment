using BankEmployeeManagement.Data;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankEmployeeManagement.Services
{
    public interface IAuthService
    {
        Task<(string? AccessToken, string? RefreshToken)> Authenticate(UserLoginDTO loginDto);
        Task<(bool Success, string? ErrorMessage)> RegisterUser(UserRegisterDTO registerDto);
    }
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string? AccessToken, string? RefreshToken)> Authenticate(UserLoginDTO loginDto)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Username == loginDto.Username)
                    .Select(u => new { u.UserId, u.Username, u.Password, RoleName = u.Role.NameRole })
                    .FirstOrDefaultAsync();

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                {
                    _logger.LogWarning("Неудачная попытка входа для пользователя: {Username}", loginDto.Username);
                    return (null, null);
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
                var tokenLifetimeHours = double.Parse(_configuration["Jwt:LifetimeHours"] ?? "2", CultureInfo.InvariantCulture);

                // Устанавливаем текущее время для iat и exp
                var now = DateTimeOffset.UtcNow;
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.RoleName),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                    }),
                    NotBefore = now.UtcDateTime, // nbf
                    Expires = now.AddHours(tokenLifetimeHours).UtcDateTime, // exp
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var accessToken = tokenHandler.WriteToken(token);

                var refreshToken = Guid.NewGuid().ToString();
                _context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.UserId,
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Токен выдан для пользователя: {Username}. AccessToken: {AccessToken}, RefreshToken: {RefreshToken}",
                    user.Username, accessToken, refreshToken);
                return (accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при аутентификации пользователя: {Username}", loginDto.Username);
                return (null, null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterUser(UserRegisterDTO registerDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(registerDto.Username) || registerDto.Username.Length < 3)
                    return (false, "Имя пользователя должно содержать минимум 3 символа.");

                if (string.IsNullOrWhiteSpace(registerDto.Password) || registerDto.Password.Length < 8)
                    return (false, "Пароль должен содержать минимум 8 символов.");

                if (!_context.Roles.Any(r => r.Id == registerDto.RoleId))
                    return (false, "Указанная роль не существует.");

                if (_context.Users.Any(u => u.Username == registerDto.Username))
                    return (false, "Пользователь с таким именем уже существует.");

                var newUser = new User
                {
                    Username = registerDto.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    RoleId = registerDto.RoleId
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Зарегистрирован новый пользователь: {Username}", registerDto.Username);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя: {Username}", registerDto.Username);
                return (false, "Произошла ошибка при регистрации.");
            }
        }
    }
}
