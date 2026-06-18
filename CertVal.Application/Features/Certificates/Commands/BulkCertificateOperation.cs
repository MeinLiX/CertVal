using CertVal.Application.Common.Certificates;
using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Certificates.Commands;

public record BulkCertificateOperationCommand(
    Guid WorkspaceId,
    IReadOnlyList<Guid> CertificateIds,
    BulkCertificateOperationType Operation,
    IReadOnlyList<string>? Tags) : IRequest<Result<BulkOperationResultDto>>;

public class BulkCertificateOperationCommandValidator : AbstractValidator<BulkCertificateOperationCommand>
{
    public const int MaxItems = 500;

    public BulkCertificateOperationCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();

        RuleFor(x => x.CertificateIds)
            .NotEmpty().WithMessage("At least one certificate must be selected.")
            .Must(ids => ids.Count <= MaxItems).WithMessage($"Cannot process more than {MaxItems} certificates at once.");

        RuleFor(x => x.Tags)
            .NotEmpty().WithMessage("Tags are required for tag operations.")
            .When(x => x.Operation is BulkCertificateOperationType.AddTags or BulkCertificateOperationType.RemoveTags);
    }
}

public class BulkCertificateOperationCommandHandler(
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<BulkCertificateOperationCommand, Result<BulkOperationResultDto>>
{
    public async Task<Result<BulkOperationResultDto>> Handle(BulkCertificateOperationCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<BulkOperationResultDto>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<BulkOperationResultDto>("Access denied to this workspace");

        var failures = new List<BulkOperationItemResult>();
        var success = 0;

        // De-duplicate while preserving order.
        var ids = request.CertificateIds.Distinct().ToList();

        foreach (var certificateId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemResult = await ApplyAsync(request, certificateId, cancellationToken);
            if (itemResult.IsSuccess)
                success++;
            else
                failures.Add(new BulkOperationItemResult { CertificateId = certificateId, Error = itemResult.Error });
        }

        return Result.Success(new BulkOperationResultDto
        {
            TotalCount = ids.Count,
            SuccessCount = success,
            FailureCount = failures.Count,
            Failures = failures
        });
    }

    private async Task<Result> ApplyAsync(BulkCertificateOperationCommand request, Guid certificateId, CancellationToken cancellationToken)
    {
        switch (request.Operation)
        {
            case BulkCertificateOperationType.Delete:
                return await mediator.Send(new DeleteCertificateCommand(certificateId), cancellationToken);

            case BulkCertificateOperationType.Skip:
            {
                var r = await mediator.Send(new ToggleCertificateSkipCommand(request.WorkspaceId, certificateId, true), cancellationToken);
                return r.IsSuccess ? Result.Success() : Result.Failure(r.Error);
            }

            case BulkCertificateOperationType.Unskip:
            {
                var r = await mediator.Send(new ToggleCertificateSkipCommand(request.WorkspaceId, certificateId, false), cancellationToken);
                return r.IsSuccess ? Result.Success() : Result.Failure(r.Error);
            }

            case BulkCertificateOperationType.AddTags:
            case BulkCertificateOperationType.RemoveTags:
            {
                var certificate = await unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
                if (certificate is null || certificate.WorkspaceId != request.WorkspaceId)
                    return Result.Failure("Certificate not found");

                var tags = request.Tags ?? [];
                var merged = request.Operation == BulkCertificateOperationType.AddTags
                    ? TagMerge.Add(certificate.Tags, tags)
                    : TagMerge.Remove(certificate.Tags, tags);

                var r = await mediator.Send(new SetCertificateTagsCommand(request.WorkspaceId, certificateId, merged), cancellationToken);
                return r.IsSuccess ? Result.Success() : Result.Failure(r.Error);
            }

            default:
                return Result.Failure("Unsupported operation");
        }
    }
}
