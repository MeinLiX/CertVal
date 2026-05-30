using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record ConfirmEmailCommand : IRequest<Result<LoginResponse>>
{
    public string Token { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthTokenService _authTokenService;

    public ConfirmEmailCommandHandler(IUnitOfWork unitOfWork, IAuthTokenService authTokenService)
    {
        _unitOfWork = unitOfWork;
        _authTokenService = authTokenService;
    }

    public async Task<Result<LoginResponse>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailConfirmationTokenAsync(request.Token, cancellationToken);
        if (user == null)
            return Result.Failure<LoginResponse>("Invalid or expired confirmation token");

        user.ConfirmEmail();
        user.UpdateLastLogin();
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        var response = await _authTokenService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        return Result.Success(response);
    }
}