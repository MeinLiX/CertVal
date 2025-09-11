using CertVal.Application.Common.Interfaces;
using CertVal.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CertVal.Infrastructure.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsApiClient => _httpContextAccessor.HttpContext?.User?.FindFirst("client_type")?.Value == "api";

    public string? ApiTokenId => _httpContextAccessor.HttpContext?.User?.FindFirst("api_token_id")?.Value;

    public ApiTokenScope? ApiTokenScope
    {
        get
        {
            var scopeClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("api_scope")?.Value;
            return Enum.TryParse<ApiTokenScope>(scopeClaim, out var scope) ? scope : null;
        }
    }
}