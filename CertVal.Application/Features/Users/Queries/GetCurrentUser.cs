using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Users.Queries;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<UserDto>("User not authenticated");

        var user = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>("User not found");

        return Result.Success(user.Adapt<UserDto>());
    }
}