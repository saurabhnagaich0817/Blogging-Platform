using InkWell.NewsletterService.DTOs;
using InkWell.NewsletterService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using InkWell.Shared.Responses;

namespace InkWell.NewsletterService.Controllers
{
    [ApiController]
    [Route("api/newsletter")]
    [Produces("application/json")]
    public class NewsletterController : ControllerBase
    {
        private readonly INewsletterService _newsletterService;

        public NewsletterController(INewsletterService newsletterService)
        {
            _newsletterService = newsletterService;
        }

        [HttpPost("subscribe")]
        [SwaggerOperation(Summary = "Subscribe to newsletter", Description = "Triggers a double opt-in workflow. Sets status to Pending and logs a confirmation URL to the console.")]
        [SwaggerResponse(200, "Subscription initiated")]
        [SwaggerResponse(400, "Invalid email format")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(new BaseResponse<string>(false, "Invalid request.", null));
            
            var result = await _newsletterService.SubscribeAsync(request);
            return Ok(new BaseResponse<string>(true, "Subscription initiated.", result));
        }

        [HttpGet("confirm")]
        [SwaggerOperation(Summary = "Confirm Subscription", Description = "Changes subscriber status from Pending to Active. Expects the GUID token from the simulated email.")]
        [SwaggerResponse(200, "Confirmed")]
        [SwaggerResponse(400, "Invalid or expired token")]
        public async Task<IActionResult> ConfirmSubscription([FromQuery] Guid token)
        {
            var success = await _newsletterService.ConfirmSubscriptionAsync(token);
            if (!success) return BadRequest(new BaseResponse<string>(false, "Invalid or expired confirmation token.", null));
            
            return Ok(new BaseResponse<string>(true, "Subscription successfully confirmed!", null));
        }

        [HttpGet("unsubscribe")]
        [SwaggerOperation(Summary = "Unsubscribe from newsletter", Description = "Changes subscriber status to Unsubscribed. Expects the active GUID token from the simulated email.")]
        [SwaggerResponse(200, "Unsubscribed")]
        [SwaggerResponse(400, "Invalid token or already unsubscribed")]
        public async Task<IActionResult> Unsubscribe([FromQuery] Guid token)
        {
            var success = await _newsletterService.UnsubscribeAsync(token);
            if (!success) return BadRequest(new BaseResponse<string>(false, "Invalid token or already unsubscribed.", null));
            
            return Ok(new BaseResponse<string>(true, "You have been successfully unsubscribed.", null));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all subscribers", Description = "Admin only. Fetches all newsletter subscribers and their statuses.")]
        [SwaggerResponse(200, "List of subscribers", typeof(IEnumerable<SubscriberResponseDTO>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        public async Task<IActionResult> GetAllSubscribers()
        {
            var subscribers = await _newsletterService.GetAllSubscribersAsync();
            return Ok(new BaseResponse<IEnumerable<SubscriberResponseDTO>>(true, "Subscribers fetched successfully", subscribers));
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Approve Subscriber", Description = "Admin only. Manually approves a subscriber.")]
        public async Task<IActionResult> ApproveSubscriber(Guid id)
        {
            var success = await _newsletterService.ApproveSubscriberAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Subscriber not found.", null));
            return Ok(new BaseResponse<string>(true, "Subscriber approved.", null));
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Reject Subscriber", Description = "Admin only. Rejects a subscriber.")]
        public async Task<IActionResult> RejectSubscriber(Guid id)
        {
            var success = await _newsletterService.RejectSubscriberAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Subscriber not found.", null));
            return Ok(new BaseResponse<string>(true, "Subscriber rejected.", null));
        }
    }
}
