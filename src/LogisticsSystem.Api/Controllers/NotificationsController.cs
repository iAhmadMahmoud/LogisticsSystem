using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead;
using LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.NotificationView)]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationResponse>>> GetMyNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetMyNotificationsQuery(pageNumber, pageSize), cancellationToken);

            return Ok(result);
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(
            Guid notificationId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MarkNotificationAsReadCommand(notificationId), cancellationToken);       

            return NoContent();
        }
    }
}
