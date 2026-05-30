using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record RefreshTokenCommand : IRequest<Result<LoginResponse>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IAuthTokenService _authTokenService;

    public RefreshTokenCommandHandler(IAuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    public Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return _authTokenService.RefreshAsync(request.RefreshToken, request.IpAddress, cancellationToken);
    }
}
