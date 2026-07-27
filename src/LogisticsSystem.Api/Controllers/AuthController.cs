using LogisticsSystem.Application.Authentication.Commands.ChangePassword;
using LogisticsSystem.Application.Authentication.Commands.ConfirmEmail;
using LogisticsSystem.Application.Authentication.Commands.ForgotPassword;
using LogisticsSystem.Application.Authentication.Commands.Login;
using LogisticsSystem.Application.Authentication.Commands.Logout;
using LogisticsSystem.Application.Authentication.Commands.RefreshToken;
using LogisticsSystem.Application.Authentication.Commands.Register;
using LogisticsSystem.Application.Authentication.Commands.ResetPassword;
using LogisticsSystem.Application.Common.Models.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _sender.Send(new RegisterCommand(request));

            return Ok(result);
        }

        [HttpPost("login")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _sender.Send(new LoginCommand(request));

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _sender.Send(command);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
        {
            await _sender.Send(command);

            return NoContent();
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] Guid userId,
            [FromQuery] string token)
        {
            await _sender.Send(
                new ConfirmEmailCommand(userId, token));

            return Ok(new
            {
                Message = "Email confirmed successfully."
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command)
        {
            await _sender.Send(command);

            return Ok(new
            {
                Message = "If the email exists, a password reset link has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
        {
            await _sender.Send(command);

            return Ok(new
            {
                Message = "Password reset successfully."
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
        {
            await _sender.Send(command);

            return Ok(new
            {
                Message = "Password changed successfully."
            });
        }
    }
}
