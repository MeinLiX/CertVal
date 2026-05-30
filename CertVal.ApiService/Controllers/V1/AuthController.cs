using CertVal.Application.DTOs;
using CertVal.Application.Features.Auth.Commands;
using CertVal.Application.Features.Auth.Queries;
using CertVal.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return CreatedAtAction(nameof(GetProfile), null, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request with { IpAddress = GetClientIpAddress() }, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost("login/google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginWithGoogle([FromBody] LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request with { IpAddress = GetClientIpAddress() }, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> ConfirmEmail([FromBody] ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request with { IpAddress = GetClientIpAddress() }, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request with { IpAddress = GetClientIpAddress() }, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand request, CancellationToken cancellationToken)
    {
        await _mediator.Send(request with { IpAddress = GetClientIpAddress() }, cancellationToken);
        return Ok(new MessageResponseDto("Logged out successfully"));
    }

    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(new MessageResponseDto("If the email exists and is not confirmed, a confirmation link has been sent"));
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(new MessageResponseDto("If the email exists, a password reset link has been sent"));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(new MessageResponseDto("Password reset successfully"));
    }

    [HttpPost("validate-reset-token")]
    [ProducesResponseType(typeof(ValidateResetTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateResetToken([FromBody] ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    private string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && forwardedFor.Count > 0)
        {
            var first = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}