namespace CertVal.Application.DTOs;

public record ExternalUserInfo
{
    public string Email { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PictureUrl { get; init; }
}
