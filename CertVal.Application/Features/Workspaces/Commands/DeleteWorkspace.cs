using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.Configuration;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CertVal.Application.Features.Workspaces.Commands;

public record DeleteWorkspaceCommand(Guid WorkspaceId) : IRequest<Result>;

public class DeleteWorkspaceCommandValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public DeleteWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");
    }
}

public class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICertificateStorageService _storageService;
    private readonly CertificateStorageConfiguration _config;
    private readonly ILogger<DeleteWorkspaceCommandHandler> _logger;

    public DeleteWorkspaceCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateStorageService storageService,
        IOptions<CertificateStorageConfiguration> config,
        ILogger<DeleteWorkspaceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storageService = storageService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (workspace.OwnerId != _currentUser.UserId.Value && !IsAdminUser())
            return Result.Failure("Access denied - only workspace owner can delete workspace");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);

                var uniqueFilePaths = certificates
                    .Where(c => !string.IsNullOrEmpty(c.FilePath))
                    .Select(c => c.FilePath)
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Found {CertificateCount} certificates with {UniqueFileCount} unique files in workspace {WorkspaceId}",
                    certificates.Count(), uniqueFilePaths.Count, request.WorkspaceId);

                await _unitOfWork.Workspaces.DeleteAsync(request.WorkspaceId, cancellationToken);

                if (_config.DeleteOnCertificateRemoval && uniqueFilePaths.Any())
                {
                    try
                    {
                        await _storageService.DeleteWorkspaceCertificatesAsync(request.WorkspaceId, cancellationToken);
                        _logger.LogInformation("Successfully cleaned up all certificate files for deleted workspace {WorkspaceId}", request.WorkspaceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bulk deletion failed, falling back to individual file deletion for workspace {WorkspaceId}", request.WorkspaceId);

                        var successCount = 0;
                        var errorCount = 0;

                        foreach (var filePath in uniqueFilePaths)
                        {
                            try
                            {
                                await _storageService.DeleteCertificateAsync(filePath, cancellationToken);
                                successCount++;
                            }
                            catch (Exception fileEx)
                            {
                                _logger.LogWarning(fileEx, "Failed to delete file {FilePath} for workspace {WorkspaceId}", filePath, request.WorkspaceId);
                                errorCount++;
                            }
                        }

                        _logger.LogInformation("Individual file cleanup for workspace {WorkspaceId} completed: {SuccessCount} succeeded, {ErrorCount} failed",
                            request.WorkspaceId, successCount, errorCount);
                    }
                }
            }, cancellationToken);

            _logger.LogInformation("Successfully deleted workspace {WorkspaceId} and cleaned up associated resources", request.WorkspaceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workspace {WorkspaceId}", request.WorkspaceId);
            return Result.Failure($"Error deleting workspace: {ex.Message}");
        }
    }

    private bool IsAdminUser()
    {
        return _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin;
    }
}