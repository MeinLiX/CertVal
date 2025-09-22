using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace CertVal.Application.Features.Notifications.Commands;

public record CreateNotificationRuleCommand : IRequest<Result<NotificationRuleDto>>
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DaysBeforeExpiry { get; init; }
    public NotificationChannelType ChannelType { get; init; }
    public string ChannelConfig { get; init; } = string.Empty;
    public NotificationFrequency Frequency { get; init; }
    public List<Guid>? RecipientUserIds { get; init; }
}

public class CreateNotificationRuleCommandValidator : AbstractValidator<CreateNotificationRuleCommand>
{
    public CreateNotificationRuleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.DaysBeforeExpiry)
            .GreaterThan(0).WithMessage("Days before expiry must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Days before expiry must not exceed 365 days");

        RuleFor(x => x.ChannelType)
            .IsInEnum().WithMessage("Channel type must be a valid notification channel");

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Frequency must be a valid notification frequency");

        RuleFor(x => x.ChannelConfig)
            .NotEmpty().WithMessage("Channel configuration is required")
            .Must(BeValidJson).WithMessage("Channel configuration must be valid JSON");

        RuleFor(x => x.RecipientUserIds)
            .NotEmpty().WithMessage("Recipient user IDs are required for email notifications")
            .When(x => x.ChannelType == NotificationChannelType.Email);
    }

    private static bool BeValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class CreateNotificationRuleCommandHandler : IRequestHandler<CreateNotificationRuleCommand, Result<NotificationRuleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateNotificationRuleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationRuleDto>> Handle(CreateNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<NotificationRuleDto>("Access denied - insufficient permissions to manage this workspace");

        string channelConfig;
        var existingRules = (await _unitOfWork.NotificationRules.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken)).ToList();

        if (request.ChannelType == NotificationChannelType.Email)
        {
            if (request.RecipientUserIds == null || !request.RecipientUserIds.Any())
            {
                return Result.Failure<NotificationRuleDto>("Recipient user IDs are required for email notifications.");
            }

            var distinctUserIds = request.RecipientUserIds.Distinct().ToList();

            var workspaceMembers = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
            var workspaceOwner = (await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken))?.Owner;
            var validUserIds = workspaceMembers.Select(m => m.UserId).ToList();
            if (workspaceOwner != null)
            {
                validUserIds.Add(workspaceOwner.Id);
            }

            var invalidUserIds = distinctUserIds.Except(validUserIds).ToList();
            if (invalidUserIds.Any())
            {
                return Result.Failure<NotificationRuleDto>($"The following user IDs are not members of this workspace: {string.Join(", ", invalidUserIds)}");
            }

            var existingEmailUserIds = existingRules
                .Where(r => r.ChannelType == NotificationChannelType.Email)
                .SelectMany(r =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(r.ChannelConfig);
                        if (doc.RootElement.TryGetProperty("userIds", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<Guid>();
                            foreach (var el in arr.EnumerateArray())
                            {
                                if (el.ValueKind == JsonValueKind.String &&
                                    Guid.TryParse(el.GetString(), out var g))
                                {
                                    list.Add(g);
                                }
                            }
                            return list;
                        }
                    }
                    catch
                    {
                    }
                    return new List<Guid>();
                })
                .ToHashSet();

            var duplicateUserIds = distinctUserIds.Where(existingEmailUserIds.Contains).ToList();
            if (duplicateUserIds.Any())
            {
                var userNames = new List<string>();
                foreach (var dupId in duplicateUserIds)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(dupId, cancellationToken);
                    userNames.Add(user?.FullName ?? dupId.ToString());
                }

                return Result.Failure<NotificationRuleDto>($"Notification rule already exists for the following users: {string.Join(", ", userNames)}");
            }

            channelConfig = JsonSerializer.Serialize(new { userIds = distinctUserIds });
        }
        else if (request.ChannelType == NotificationChannelType.Webhook)
        {
            try
            {
                using var newDoc = JsonDocument.Parse(request.ChannelConfig);
                if (!newDoc.RootElement.TryGetProperty("url", out var urlElement) || string.IsNullOrWhiteSpace(urlElement.GetString()))
                {
                    return Result.Failure<NotificationRuleDto>("Webhook URL is missing in channel configuration.");
                }

                var newUrl = urlElement.GetString()!.Trim();

                var existingWebhookUrls = existingRules
                    .Where(r => r.ChannelType == NotificationChannelType.Webhook)
                    .Select(r =>
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(r.ChannelConfig);
                            return doc.RootElement.TryGetProperty("url", out var existingUrlEl)
                                ? existingUrlEl.GetString()
                                : null;
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (existingWebhookUrls.Contains(newUrl))
                {
                    return Result.Failure<NotificationRuleDto>($"A notification rule with the URL '{newUrl}' already exists in this workspace.");
                }

                channelConfig = request.ChannelConfig;
            }
            catch (JsonException)
            {
                return Result.Failure<NotificationRuleDto>("Invalid JSON format for webhook channel configuration.");
            }
        }
        else
        {
            channelConfig = request.ChannelConfig;
        }

        var rule = NotificationRule.Create(
            request.WorkspaceId,
            request.Name,
            request.DaysBeforeExpiry,
            request.ChannelType,
            channelConfig,
            request.Frequency
        );

        await _unitOfWork.NotificationRules.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var ruleDto = new NotificationRuleDto
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            DaysBeforeExpiry = rule.DaysBeforeExpiry,
            Frequency = rule.Frequency.ToString(),
            ChannelType = rule.ChannelType.ToString(),
            ChannelConfig = rule.ChannelConfig,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return Result.Success(ruleDto);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
        return membership?.CanManageWorkspace ?? false;
    }
}