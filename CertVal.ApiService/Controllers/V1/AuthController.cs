using CertVal.Application.DTOs;
using CertVal.Application.Services;
using CertVal.Core.Repositories;
using CertVal.Core.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IEmailNotificationPublisher _emailPublisher;

    public AuthController(
        IUserService userService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IEmailNotificationPublisher emailPublisher)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _emailPublisher = emailPublisher;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);

        if (!result.IsSuccess)
        {
            var errors = new Dictionary<string, string[]>();
            if (result.Errors != null)
            {
                foreach (var error in result.Errors)
                {
                    errors.Add("error", [error]);
                }
            }
            return BadRequest(new ErrorResponseDto(result.Error, errors));
        }

        return CreatedAtAction(nameof(GetProfile), null, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(Application.DTOs.LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponseDto("Invalid email or password"));

        if (!user.IsEmailConfirmed)
            return BadRequest(new ErrorResponseDto("Email not confirmed"));

        user.UpdateLastLogin();
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsEmailConfirmed = user.IsEmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                Status = user.Status.ToString(),
                TimeZone = user.TimeZone,
                Language = user.Language,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled,
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var result = await _userService.GetCurrentUserAsync();

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var result = await _userService.ConfirmEmailAsync(request.Token);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(new MessageResponseDto("Email confirmed successfully"));
    }

    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendEmailConfirmation(ResendConfirmationRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return Ok(new MessageResponseDto("If the email exists and is not confirmed, a confirmation link has been sent"));

        if (user.IsEmailConfirmed)
            return BadRequest(new ErrorResponseDto("Email is already confirmed"));

        var newToken = Guid.NewGuid().ToString();
        user.SetEmailConfirmationToken(newToken);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _emailPublisher.PublishUserRegisteredAsync(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            newToken);

        return Ok(new MessageResponseDto("If the email exists and is not confirmed, a confirmation link has been sent"));
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(Application.DTOs.ForgotPasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user != null)
        {
            var resetToken = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            user.SetPasswordResetToken(resetToken, expiresAt);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailPublisher.PublishPasswordResetAsync(
                user.Email,
                user.FirstName,
                resetToken,
                expiresAt);
        }

        return Ok(new MessageResponseDto("If the email exists, a password reset link has been sent"));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(Application.DTOs.ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(request.Token, request.NewPassword);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(new MessageResponseDto("Password reset successfully"));
    }

    [HttpPost("validate-reset-token")]
    [ProducesResponseType(typeof(ValidateResetTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateResetToken(ValidateResetTokenRequest request)
    {
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(request.Token);
        if (user == null)
            return BadRequest(new ErrorResponseDto("Invalid or expired reset token"));

        return Ok(new ValidateResetTokenResponse
        {
            Message = "Token is valid",
            Email = user.Email,
            ExpiresAt = user.PasswordResetTokenExpiresAt
        });
    }

    private string GenerateJwtToken(Core.Entities.User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim("client_type", "web")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}