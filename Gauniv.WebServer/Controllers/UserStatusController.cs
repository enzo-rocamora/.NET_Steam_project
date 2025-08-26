using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/user/status")]
    [ApiController]
    [Authorize]
    public class UserStatusController : ControllerBase
    {
        private readonly UserApiService _userService;

        public UserStatusController(UserApiService userService)
        {
            _userService = userService;
        }

        [HttpPost("game/start/{gameId}")]
        public async Task<IActionResult> StartGame(int gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _userService.UpdateUserGameStatusAsync(userId, gameId);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("game/stop")]
        public async Task<IActionResult> StopGame()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _userService.UpdateUserGameStatusAsync(userId, null);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<UserStatus>> GetCurrentStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var status = await _userService.GetUserStatusAsync(userId);
                return Ok(status);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}