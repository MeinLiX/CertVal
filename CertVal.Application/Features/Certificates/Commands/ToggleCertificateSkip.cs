using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Certificates.Commands;

public record ToggleCertificateSkipCommand(Guid WorkspaceId, Guid CertificateId, bool IsSkipped) : IRequest<Result<Unit>>;

public class ToggleCertificateSkipCommandValidator : AbstractValidator<ToggleCertificateSkipCommand>
{
    public ToggleCertificateSkipCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CertificateId).NotEmpty();
    }
}

public class ToggleCertificateSkipCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser) : IRequestHandler<ToggleCertificateSkipCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ToggleCertificateSkipCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<Unit>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<Unit>("Access denied to this workspace");

        var certificate = await unitOfWork.Certificates.GetByIdAsync(request.CertificateId, cancellationToken);

        if (certificate == null || certificate.WorkspaceId != request.WorkspaceId)
        {
            return Result.Failure<Unit>("Certificate not found");
        }

        if (!request.IsSkipped)
        {
            var nextVersion = await unitOfWork.Certificates.GetNextVersionAsync(certificate.Id, cancellationToken);
            if (nextVersion != null)
            {
                return Result.Failure<Unit>("Cannot enable monitoring because a newer version of this certificate exists.");
            }
        }

        certificate.ToggleSkipMonitoring(request.IsSkipped);
        await unitOfWork.Certificates.UpdateAsync(certificate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
