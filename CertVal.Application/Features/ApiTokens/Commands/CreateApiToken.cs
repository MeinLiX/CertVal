using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using CertVal.Core.Utils;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.ApiTokens.Commands;

public record CreateApiTokenCommand : IRequest<Result<CreateApiTokenResponse>>
{
    public string Name { get; init; } = string.Empty;
    public ApiTokenScope Scope { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public class CreateApiTokenCommandValidator : AbstractValidator<CreateApiTokenCommand>
{
    public CreateApiTokenCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Token name is required")
            .MaximumLength(100).WithMessage("Token name must not exceed 100 characters");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Scope must be a valid API token scope");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }
}

public class CreateApiTokenCommandHandler : IRequestHandler<CreateApiTokenCommand, Result<CreateApiTokenResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateApiTokenCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateApiTokenResponse>> Handle(CreateApiTokenCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CreateApiTokenResponse>("User not authenticated");

        var existingTokens = await _unitOfWork.ApiTokens.GetByUserAsync(_currentUser.UserId.Value, cancellationToken);
        var activeTokens = existingTokens.Where(t => t.IsActive && (!t.ExpiresAt.HasValue || t.ExpiresAt > DateTime.UtcNow));

        if (activeTokens.Count() >= 10)
            return Result.Failure<CreateApiTokenResponse>("Maximum number of active API tokens reached (10)");

        if (activeTokens.Any(t => t.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<CreateApiTokenResponse>("API token with this name already exists");

        var (token, tokenHash) = TokenGenerator.GenerateApiToken();
        var tokenPrefix = token[..8];

        var apiToken = ApiToken.Create(
            _currentUser.UserId.Value,
            request.Name,
            tokenHash,
            tokenPrefix,
            request.Scope,
            request.ExpiresAt
        );

        await _unitOfWork.ApiTokens.AddAsync(apiToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateApiTokenResponse
        {
            Id = apiToken.Id,
            Name = apiToken.Name,
            Token = token,
            TokenPrefix = apiToken.TokenPrefix,
            Scope = apiToken.Scope.ToString(),
            ExpiresAt = apiToken.ExpiresAt,
            CreatedAt = apiToken.CreatedAt
        });
    }
}