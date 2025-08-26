using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Dtos.Users;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/user/games")]
    [ApiController]
    [Authorize]
    public class UserGamesApiController : ControllerBase
    {
        private readonly UserApiService _userService;

        public UserGamesApiController(UserApiService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserGameDto>>> GetOwnedGames()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var games = await _userService.GetUserGamesAsync(userId);
                return Ok(games);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{gameId}")]
        public async Task<IActionResult> PurchaseGame(int gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _userService.PurchaseGameAsync(userId, gameId);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}