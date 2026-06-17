using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Tools.Queries;

public record CheckSslQuery(string Host, int? Port) : IRequest<Result<SslCheckResultDto>>;

public class CheckSslQueryValidator : AbstractValidator<CheckSslQuery>
{
    public CheckSslQueryValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host is required")
            .MaximumLength(253).WithMessage("Host is too long");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535")
            .When(x => x.Port.HasValue);
    }
}

public class CheckSslQueryHandler : IRequestHandler<CheckSslQuery, Result<SslCheckResultDto>>
{
    private readonly ISslInspectionService _sslInspection;
    private readonly IWebhookSecurityService _webhookSecurity;

    public CheckSslQueryHandler(ISslInspectionService sslInspection, IWebhookSecurityService webhookSecurity)
    {
        _sslInspection = sslInspection;
        _webhookSecurity = webhookSecurity;
    }

    public async Task<Result<SslCheckResultDto>> Handle(CheckSslQuery request, CancellationToken cancellationToken)
    {
        var host = request.Host.Trim();

        // Allow "host:port" shorthand.
        var port = request.Port ?? 443;
        var colonIndex = host.LastIndexOf(':');
        if (request.Port is null && colonIndex > 0 && int.TryParse(host[(colonIndex + 1)..], out var parsedPort))
        {
            port = parsedPort;
            host = host[..colonIndex];
        }

        if (port is < 1 or > 65535)
            return Result.Failure<SslCheckResultDto>("Port must be between 1 and 65535.");

        // Reuse the existing SSRF protection: only public, resolvable hosts are allowed.
        var (isValid, _, error) = await _webhookSecurity.ValidateUrlAsync($"https://{host}:{port}", cancellationToken);
        if (!isValid)
            return Result.Failure<SslCheckResultDto>($"Host rejected: {error}");

        var result = await _sslInspection.InspectAsync(host, port, cancellationToken);
        return Result.Success(result);
    }
}
