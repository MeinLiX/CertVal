using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.Configuration;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ICertificateStorageService _storageService;
    private readonly CertificateStorageConfiguration _config;
    private readonly ILogger<DeleteCertificateCommandHandler> _logger;

    public DeleteCertificateCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateStorageService storageService,
        IOptions<CertificateStorageConfiguration> config,
        ILogger<DeleteCertificateCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storageService = storageService;
        _config = config.Value;
        _logger = logger;
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
                var shouldDeleteFile = false;
                string? filePathToDelete = null;

                if (certificate.IsBundle)
                {
                    shouldDeleteFile = true;
                    filePathToDelete = certificate.FilePath;

                    if (request.DeleteBundleChildren)
                    {
                        var childCertificates = await _unitOfWork.Certificates.GetBundleContentsAsync(request.CertificateId, cancellationToken);
                        foreach (var child in childCertificates)
                        {
                            await _unitOfWork.Certificates.DeleteAsync(child.Id, cancellationToken);
                            _logger.LogInformation("Deleted child certificate {CertificateId} from bundle", child.Id);
                        }
                    }
                }
                else if (certificate.ParentCertificateId.HasValue)
                {
                    var parentCertificate = await _unitOfWork.Certificates.GetByIdAsync(certificate.ParentCertificateId.Value, cancellationToken);
                    if (parentCertificate != null)
                    {
                        var remainingChildren = await _unitOfWork.Certificates.GetBundleContentsAsync(certificate.ParentCertificateId.Value, cancellationToken);
                        var remainingChildrenAfterDeletion = remainingChildren.Where(c => c.Id != request.CertificateId).ToList();

                        if (!remainingChildrenAfterDeletion.Any())
                        {
                            shouldDeleteFile = true;
                            filePathToDelete = parentCertificate.FilePath;

                            await _unitOfWork.Certificates.DeleteAsync(parentCertificate.Id, cancellationToken);
                            _logger.LogInformation("Deleted parent bundle certificate {CertificateId} as it was the last remaining child", parentCertificate.Id);
                        }
                        else
                        {
                            shouldDeleteFile = false;
                            _logger.LogInformation("Certificate {CertificateId} is part of bundle with other certificates, file will be preserved", request.CertificateId);
                        }
                    }
                }
                else
                {
                    shouldDeleteFile = true;
                    filePathToDelete = certificate.FilePath;
                }

                await _unitOfWork.Certificates.DeleteAsync(request.CertificateId, cancellationToken);
                _logger.LogInformation("Deleted certificate {CertificateId} from database", request.CertificateId);

                if (shouldDeleteFile && _config.DeleteOnCertificateRemoval && !string.IsNullOrEmpty(filePathToDelete))
                {
                    try
                    {
                        await _storageService.DeleteCertificateAsync(filePathToDelete, cancellationToken);
                        _logger.LogInformation("Successfully deleted certificate file {ObjectKey} from storage", filePathToDelete);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete certificate file {ObjectKey} from storage, but certificate was removed from database", filePathToDelete);
                    }
                }
                else if (!shouldDeleteFile)
                {
                    _logger.LogDebug("Certificate file {ObjectKey} preserved as it contains other certificates", certificate.FilePath);
                }
            }, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting certificate {CertificateId}", request.CertificateId);
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