using BankEmployeeManagement.Data;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using BankEmployeeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankEmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(AuthService authService, AppDbContext context, IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO loginDto)
        {
            var (accessToken, refreshToken) = await _authService.Authenticate(loginDto);

            if (accessToken == null || refreshToken == null)
                return Unauthorized(new { message = "Неверное имя пользователя или пароль." });

            return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO registerDto)
        {
            var result = await _authService.RegisterUser(registerDto);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Пользователь успешно зарегистрирован." });
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO refreshDto)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshDto.RefreshToken && !t.IsRevoked && t.Expires > DateTime.UtcNow);

            if (refreshToken == null) return Unauthorized(new { message = "Недействительный refresh-токен" });

            var user = await _context.Users
                .Where(u => u.UserId == refreshToken.UserId)
                .Select(u => new { u.UserId, u.Username, RoleName = u.Role.NameRole })
                .FirstOrDefaultAsync();

            if (user == null) return Unauthorized(new { message = "Пользователь не найден" });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { AccessToken = tokenHandler.WriteToken(token) });
        }
    }
}
