using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("API Tokens")]
public class ApiTokensController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ApiTokensController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ApiTokenDto>>> GetTokens()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var tokens = await _unitOfWork.ApiTokens.GetByUserAsync(_currentUser.UserId.Value);
        var tokenDtos = tokens.Select(t => new ApiTokenDto
        {
            Id = t.Id,
            Name = t.Name,
            TokenPrefix = t.TokenPrefix,
            Scope = t.Scope.ToString(),
            IsActive = t.IsActive,
            LastUsedAt = t.LastUsedAt,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt
        });

        return Ok(tokenDtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateApiTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateApiTokenResponse>> CreateToken(CreateApiTokenRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var token = Convert.ToBase64String(tokenBytes);
        var tokenPrefix = token[..8];

        var apiToken = ApiToken.Create(
            _currentUser.UserId.Value,
            request.Name,
            token,
            tokenPrefix,
            request.Scope,
            request.ExpiresAt
        );

        await _unitOfWork.ApiTokens.AddAsync(apiToken);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTokens), new CreateApiTokenResponse
        {
            Id = apiToken.Id,
            Name = apiToken.Name,
            Token = token,
            TokenPrefix = tokenPrefix,
            Scope = apiToken.Scope.ToString(),
            ExpiresAt = apiToken.ExpiresAt,
            CreatedAt = apiToken.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken(Guid id)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        await _unitOfWork.ApiTokens.RevokeTokenAsync(id);
        return NoContent();
    }
}