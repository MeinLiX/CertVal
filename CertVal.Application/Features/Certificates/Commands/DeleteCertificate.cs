using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Certificates.Commands;

public record DeleteCertificateCommand(Guid CertificateId, bool DeleteBundleChildren = true) : IRequest<Result>;

public class DeleteCertificateCommandValidator : AbstractValidator<DeleteCertificateCommand>
{
    public DeleteCertificateCommandValidator()
    {
        RuleFor(x => x.CertificateId)
            .NotEmpty().WithMessage("Certificate ID is required");
    }
}

public class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteCertificateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCertificateCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure("Certificate not found");

        if (!await CanManageCertificates(certificate.WorkspaceId, cancellationToken))
            return Result.Failure("Access denied - insufficient permissions");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (certificate.IsBundle && request.DeleteBundleChildren)
                {
                    var childCertificates = await _unitOfWork.Certificates.GetBundleContentsAsync(request.CertificateId, cancellationToken);
                    foreach (var child in childCertificates)
                    {
                        await _unitOfWork.Certificates.DeleteAsync(child.Id, cancellationToken);
                    }
                }

                await _unitOfWork.Certificates.DeleteAsync(request.CertificateId, cancellationToken);
            }, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error deleting certificate: {ex.Message}");
        }
    }

    private async Task<bool> CanManageCertificates(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        if (_currentUser.IsApiClient &&
            (_currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.ReadWrite ||
             _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin))
            return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken: cancellationToken);
        if (membership != null)
            return membership.CanManageCertificates;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        return workspace?.OwnerId == _currentUser.UserId.Value;
    }
}