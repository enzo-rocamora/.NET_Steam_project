using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Dtos.Users;
using Microsoft.AspNetCore.Identity;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize]
    public class UserApiController : ControllerBase
    {
        private readonly UserApiService _userService;
        private readonly UserManager<User> _userManager;

        public UserApiController(UserApiService userService, UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        [HttpGet("friends")]
        public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var friends = await _userService.GetUserFriendsAsync(userId);
                return Ok(friends);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("friends")]
        public async Task<IActionResult> AddFriend([FromBody] FriendRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            if (string.IsNullOrWhiteSpace(request.Id) &&
                string.IsNullOrWhiteSpace(request.Email) &&
                string.IsNullOrWhiteSpace(request.UserName))
            {
                return BadRequest("At least one identifier (Id, Email, or UserName) is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _userService.AddFriendAsync(userId, request);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("set")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur à partir du token
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Récupérer l'utilisateur
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Mettre à jour les informations
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;

                // Sauvegarder les modifications
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new
                    {
                        message = "Profile updated successfully",
                        user = new
                        {
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            email = user.Email
                        }
                    });
                }

                return BadRequest(new
                {
                    message = "Error updating profile",
                    errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile" });
            }
        }
    }
}