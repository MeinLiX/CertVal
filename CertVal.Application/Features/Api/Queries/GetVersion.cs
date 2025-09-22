using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using MediatR;
using System.Reflection;

namespace CertVal.Application.Features.Api.Queries;

public record GetVersionQuery : IRequest<Result<VersionResponseDto>>;

public class GetVersionQueryHandler : IRequestHandler<GetVersionQuery, Result<VersionResponseDto>>
{
    public async Task<Result<VersionResponseDto>> Handle(GetVersionQuery request, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = new FileInfo(assembly.Location).LastWriteTime;

        var response = new VersionResponseDto
        {
            Version = version?.ToString() ?? "1.0.0",
            BuildDate = buildDate,
            CommitHash = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        };

        return await Task.FromResult(Result.Success(response));
    }
}