namespace CertVal.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsApiClient { get; }
    string? ApiTokenId { get; }
    Core.Enums.ApiTokenScope? ApiTokenScope { get; }
}