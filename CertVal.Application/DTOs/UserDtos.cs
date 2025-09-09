using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsEmailConfirmed { get; init; }
    public DateTime LastLoginAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? TimeZone { get; init; }
    public string? Language { get; init; }
    public bool EmailNotificationsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? TimeZone { get; init; }

    [MaxLength(10)]
    public string? Language { get; init; }
}

public record UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? TimeZone { get; init; }

    [MaxLength(10)]
    public string? Language { get; init; }

    public bool EmailNotificationsEnabled { get; init; }
}

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}