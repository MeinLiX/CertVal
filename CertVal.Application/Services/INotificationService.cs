using CertVal.Application.Common.Models;
using CertVal.Core.Entities;

namespace CertVal.Application.Services;

public interface INotificationService
{
    Task<Result> SendNotificationAsync(NotificationHistory notification, CancellationToken cancellationToken = default);
    Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default);
}
