﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedDeskConnect.Models;
using SharedDeskConnect.Services;


namespace SharedDeskConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService; // Assuming you have an email service

        public NotificationController(DataContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        [HttpGet("Notify")]
        public async Task<IActionResult> GetNotify([FromQuery] int spaceId, [FromQuery] string userId)
        {
            var existingPreference = _context.NotificationPreferences
                .FirstOrDefault(n => n.SpaceId == spaceId && n.UserId == userId);

            if (existingPreference != null)
            {
                return Ok(new { notify = true });
            }
            return Ok(new { notify = false });
        }

        [HttpPost("Notify")]
        public async Task<IActionResult> Notify([FromBody] NotifyRequest request)
        {
            // Store the notification preference in the database
            var notification = new NotificationPreference
            {
                SpaceId = request.SpaceId,
                UserId = request.UserId,
                Email = request.Email
            };
            _context.NotificationPreferences.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification preference set" });
        }

        [HttpDelete("Notify")]
        public async Task<IActionResult> RemoveNotify([FromBody] NotificationPreference notificationPreference)
        {
            var existingPreference = _context.NotificationPreferences
                .FirstOrDefault(n => n.SpaceId == notificationPreference.SpaceId && n.UserId == notificationPreference.UserId);

            if (existingPreference != null)
            {
                _context.NotificationPreferences.Remove(existingPreference);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Notification preference removed" });
            }
            return NotFound(new { message = "Notification preference not found" });
        }

        [HttpPost("UpdateAvailability")]
        public async Task<IActionResult> UpdateAvailability([FromBody] UpdateAvailabilityRequest request)
        {
            // Update the space availability in the database
            var space = await _context.Spaces.FindAsync(request.SpaceId);
            space.AvailableCapacity = request.NewCapacity;
            await _context.SaveChangesAsync();

            if (request.NewCapacity > 0)
            {
                // Get users who want to be notified
                var usersToNotify = _context.NotificationPreferences
                    .Where(n => n.SpaceId == request.SpaceId)
                    .Select(n => n.Email)
                    .ToList();

                foreach (var userEmail in usersToNotify)
                {
                    await _emailService.SendEmailAsync(userEmail, "A spot has opened up!",
                        "A spot has opened up in the space you wanted. Book it now!");
                }
            }

            return Ok(new { message = "Space availability updated and notifications sent" });
        }
    }

    public class NotifyRequest
    {
        public int SpaceId { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
    }

    public class UpdateAvailabilityRequest
    {
        public int SpaceId { get; set; }
        public int NewCapacity { get; set; }
    }

}