using System.ComponentModel.DataAnnotations;
using SqlTrainer.Api.Models;

namespace SqlTrainer.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(6), MaxLength(128)] string Password
);

public record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(1), MaxLength(128)] string Password
);

public record CurrentUserResponse(string Email, UserRole Role);
public record AuthResponse(CurrentUserResponse User, string CsrfToken);

