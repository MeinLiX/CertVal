using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record LogoutCommand : IRequest<Result>
{
    public string? RefreshToken { get; init; }
    public string? IpAddress { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthTokenService _authTokenService;

    public LogoutCommandHandler(IAuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _authTokenService.RevokeAsync(request.RefreshToken, request.IpAddress, cancellationToken);

        return Result.Success();
    }
}
