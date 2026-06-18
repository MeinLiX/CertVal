using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.Common.Notifications;
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
    public RecipientAggregationMode RecipientAggregationMode { get; init; } = RecipientAggregationMode.Individual;
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
            .LessThanOrEqualTo(89).WithMessage("Days before expiry must not exceed 89 days");

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

        // Validate the channel-specific configuration shape at the API boundary.
        // Email recipients are supplied via RecipientUserIds and the config is built
        // server-side, so only the non-email channels are shape-checked here.
        RuleFor(x => x.ChannelConfig)
            .Custom((channelConfig, context) =>
            {
                var command = context.InstanceToValidate;
                if (command.ChannelType == NotificationChannelType.Email)
                    return;

                var result = NotificationChannelConfigValidator.Validate(command.ChannelType, channelConfig);
                if (result.IsFailure)
                    context.AddFailure(nameof(command.ChannelConfig), result.Error);
            });
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
    private readonly IWebhookSecurityService _webhookSecurity;

    public CreateNotificationRuleCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IWebhookSecurityService webhookSecurity)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _webhookSecurity = webhookSecurity;
    }

    public async Task<Result<NotificationRuleDto>> Handle(CreateNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<NotificationRuleDto>("Access denied - insufficient permissions to manage this workspace");

        var existingRules = (await _unitOfWork.NotificationRules.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken)).ToList();

        var channelConfigResult = request.ChannelType switch
        {
            NotificationChannelType.Email => await BuildEmailChannelConfigAsync(request, existingRules, cancellationToken),
            NotificationChannelType.Webhook => await BuildWebhookChannelConfigAsync(request, existingRules, cancellationToken),
            _ => Result.Failure<string>($"Channel type '{request.ChannelType}' is not implemented.")
        };

        if (channelConfigResult.IsFailure)
            return Result.Failure<NotificationRuleDto>(channelConfigResult.Error);

        var rule = NotificationRule.Create(
            request.WorkspaceId,
            request.Name,
            request.DaysBeforeExpiry,
            request.ChannelType,
            channelConfigResult.Value,
            request.Frequency
        );

        rule.SetRecipientAggregationMode(request.RecipientAggregationMode);

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
            RecipientAggregationMode = rule.RecipientAggregationMode,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return Result.Success(ruleDto);
    }

    private async Task<Result<string>> BuildEmailChannelConfigAsync(
        CreateNotificationRuleCommand request,
        List<NotificationRule> existingRules,
        CancellationToken cancellationToken)
    {
        if (request.RecipientUserIds == null || request.RecipientUserIds.Count == 0)
            return Result.Failure<string>("Recipient user IDs are required for email notifications.");

        var distinctUserIds = request.RecipientUserIds.Distinct().ToList();

        var workspaceMembers = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var workspaceOwner = (await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken))?.Owner;
        var validUserIds = workspaceMembers.Select(m => m.UserId).ToList();
        if (workspaceOwner != null) validUserIds.Add(workspaceOwner.Id);

        var invalidUserIds = distinctUserIds.Except(validUserIds).ToList();
        if (invalidUserIds.Count != 0)
            return Result.Failure<string>($"The following user IDs are not members of this workspace: {string.Join(", ", invalidUserIds)}");

        var existingEmailUserIds = existingRules
            .Where(r => r.ChannelType == NotificationChannelType.Email)
            .SelectMany(r => ExtractEmailUserIds(r.ChannelConfig))
            .ToHashSet();

        var duplicateUserIds = distinctUserIds.Where(existingEmailUserIds.Contains).ToList();
        if (duplicateUserIds.Count != 0)
        {
            var userNames = new List<string>();
            foreach (var dupId in duplicateUserIds)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(dupId, cancellationToken);
                userNames.Add(user?.FullName ?? dupId.ToString());
            }
            return Result.Failure<string>($"Notification rule already exists for the following users: {string.Join(", ", userNames)}");
        }

        var configJson = JsonSerializer.Serialize(new { userIds = distinctUserIds });
        return Result.Success(configJson);
    }

    private async Task<Result<string>> BuildWebhookChannelConfigAsync(
        CreateNotificationRuleCommand request,
        List<NotificationRule> existingRules,
        CancellationToken cancellationToken)
    {
        try
        {
            using var newDoc = JsonDocument.Parse(request.ChannelConfig);
            if (!newDoc.RootElement.TryGetProperty("url", out var urlElement) || string.IsNullOrWhiteSpace(urlElement.GetString()))
                return Result.Failure<string>("Webhook URL is missing in channel configuration.");

            var newUrlRaw = urlElement.GetString()!.Trim();

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
                    catch { return null; }
                })
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingWebhookUrls.Contains(newUrlRaw))
                return Result.Failure<string>($"A notification rule with the URL '{newUrlRaw}' already exists in this workspace.");

            var (isValid, uri, error) = await _webhookSecurity.ValidateUrlAsync(newUrlRaw, cancellationToken);
            if (!isValid || uri == null)
                return Result.Failure<string>($"Webhook URL rejected: {error}");

            Dictionary<string, string>? sanitizedHeaders = null;
            if (newDoc.RootElement.TryGetProperty("headers", out var headersElement) && headersElement.ValueKind == JsonValueKind.Object)
            {
                var temp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in headersElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        temp[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
                sanitizedHeaders = _webhookSecurity.SanitizeHeaders(temp).ToDictionary(k => k.Key, v => v.Value);
            }

            var serialized = sanitizedHeaders is { Count: > 0 }
                ? JsonSerializer.Serialize(new { url = uri.ToString(), headers = sanitizedHeaders })
                : JsonSerializer.Serialize(new { url = uri.ToString() });

            return Result.Success(serialized);
        }
        catch (JsonException)
        {
            return Result.Failure<string>("Invalid JSON format for webhook channel configuration.");
        }
    }

    private static IEnumerable<Guid> ExtractEmailUserIds(string channelConfig)
    {
        using var doc = JsonDocument.Parse(channelConfig);
        if (doc.RootElement.TryGetProperty("userIds", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var g))
                    yield return g;
            }
        }
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken: cancellationToken);
        return membership?.CanManageWorkspace ?? false;
    }
}