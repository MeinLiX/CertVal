using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Certificates.Commands;

public record SetCertificateTagsCommand(Guid WorkspaceId, Guid CertificateId, List<string> Tags)
    : IRequest<Result<List<string>>>;

public class SetCertificateTagsCommandValidator : AbstractValidator<SetCertificateTagsCommand>
{
    public SetCertificateTagsCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.CertificateId).NotEmpty();
        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= Certificate.MaxTags)
            .WithMessage($"A certificate may have at most {Certificate.MaxTags} tags.");
    }
}

public class SetCertificateTagsCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<SetCertificateTagsCommand, Result<List<string>>>
{
    public async Task<Result<List<string>>> Handle(SetCertificateTagsCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<List<string>>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<List<string>>("Access denied to this workspace");

        var certificate = await unitOfWork.Certificates.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate == null || certificate.WorkspaceId != request.WorkspaceId)
            return Result.Failure<List<string>>("Certificate not found");

        certificate.SetTags(request.Tags);
        await unitOfWork.Certificates.UpdateAsync(certificate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(certificate.Tags);
    }
}
