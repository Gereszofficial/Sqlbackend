using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Auth;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SqlTrainer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public const string AuthCookieName = "sqltrainer_auth";
    public const string CsrfCookieName = "sqltrainer_csrf";

    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IWebHostEnvironment _env;

    public AuthController(AppDbContext db, IJwtTokenService jwt, IWebHostEnvironment env)
    {
        _db = db;
        _jwt = jwt;
        _env = env;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = req.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email, ct))
            return BadRequest("Már létezik felhasználó ezzel az emaillel.");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            Role = UserRole.Student
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        SignIn(user);
        return Ok(new AuthResponse(ToCurrentUser(user)));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (user is null) return Unauthorized("Hibás email vagy jelszó.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Hibás email vagy jelszó.");

        SignIn(user);
        return Ok(new AuthResponse(ToCurrentUser(user)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(email))
            email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email);

        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, ct);
        if (user is null) return Unauthorized();

        return Ok(ToCurrentUser(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearAuthCookies();
        return NoContent();
    }

    private void SignIn(User user)
    {
        var token = _jwt.CreateToken(user);
        var isSecure = !_env.IsDevelopment() || Request.IsHttps;
        var sameSite = SameSiteMode.Strict;

        Response.Cookies.Append(AuthCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            MaxAge = TimeSpan.FromHours(2),
            IsEssential = true,
            Path = "/"
        });

        Response.Cookies.Append(CsrfCookieName, GenerateCsrfToken(), new CookieOptions
        {
            HttpOnly = false,
            Secure = isSecure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            MaxAge = TimeSpan.FromHours(2),
            IsEssential = true,
            Path = "/"
        });
    }

    private void ClearAuthCookies()
    {
        var isSecure = !_env.IsDevelopment() || Request.IsHttps;
        var sameSite = SameSiteMode.Strict;

        Response.Cookies.Delete(AuthCookieName, new CookieOptions
        {
            Secure = isSecure,
            SameSite = sameSite,
            Path = "/"
        });

        Response.Cookies.Delete(CsrfCookieName, new CookieOptions
        {
            Secure = isSecure,
            SameSite = sameSite,
            Path = "/"
        });
    }

    private static string GenerateCsrfToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static CurrentUserResponse ToCurrentUser(User user) => new(user.Email, user.Role);
}
