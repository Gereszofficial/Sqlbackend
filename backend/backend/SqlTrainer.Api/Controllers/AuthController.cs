using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SqlTrainer.Api.Auth;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Dtos;
using SqlTrainer.Api.Models;
using System.IdentityModel.Tokens.Jwt;
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

    public AuthController(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = NormalizeEmail(req.Email);

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

        var csrf = SignIn(user);
        return Ok(new AuthResponse(ToCurrentUser(user), csrf));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = NormalizeEmail(req.Email);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Hibás email vagy jelszó.");

        var csrf = SignIn(user);
        return Ok(new AuthResponse(ToCurrentUser(user), csrf));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken ct)
    {
        var email =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Email) ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized();

        email = NormalizeEmail(email);

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, ct);

        if (user is null)
            return Unauthorized();

        return Ok(ToCurrentUser(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearAuthCookies();
        return NoContent();
    }

    private string SignIn(User user)
    {
        var token = _jwt.CreateToken(user);
        var csrf = GenerateCsrfToken();

        Response.Cookies.Append(AuthCookieName, token, BuildAuthCookieOptions());
        Response.Cookies.Append(CsrfCookieName, csrf, BuildCsrfCookieOptions());

        return csrf;
    }

    private static CookieOptions BuildAuthCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            MaxAge = TimeSpan.FromHours(2),
            IsEssential = true,
            Path = "/"
        };
    }

    private static CookieOptions BuildCsrfCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            MaxAge = TimeSpan.FromHours(2),
            IsEssential = true,
            Path = "/"
        };
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AuthCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });

        Response.Cookies.Delete(CsrfCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
    }

    private static string GenerateCsrfToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static CurrentUserResponse ToCurrentUser(User user)
    {
        return new CurrentUserResponse(user.Email, user.Role);
    }
}