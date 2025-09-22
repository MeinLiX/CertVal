using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.ApiTokens.Queries;

public record GetApiTokensQuery : IRequest<Result<IEnumerable<ApiTokenDto>>>
{
    public bool IncludeInactive { get; init; } = false;
}

public class GetApiTokensQueryValidator : AbstractValidator<GetApiTokensQuery>
{
    public GetApiTokensQueryValidator()
    {
    }
}

public class GetApiTokensQueryHandler : IRequestHandler<GetApiTokensQuery, Result<IEnumerable<ApiTokenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetApiTokensQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<ApiTokenDto>>> Handle(GetApiTokensQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<ApiTokenDto>>("User not authenticated");

        var tokens = await _unitOfWork.ApiTokens.GetByUserAsync(_currentUser.UserId.Value, cancellationToken);

        if (!request.IncludeInactive)
        {
            tokens = tokens.Where(t => t.IsActive && (!t.ExpiresAt.HasValue || t.ExpiresAt > DateTime.UtcNow));
        }

        var tokenDtos = tokens.Select(t => new ApiTokenDto
        {
            Id = t.Id,
            Name = t.Name,
            TokenPrefix = t.TokenPrefix,
            Scope = t.Scope.ToString(),
            IsActive = t.IsActive,
            LastUsedAt = t.LastUsedAt,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt
        }).OrderByDescending(t => t.CreatedAt).ToList();

        return Result.Success<IEnumerable<ApiTokenDto>>(tokenDtos);
    }
}