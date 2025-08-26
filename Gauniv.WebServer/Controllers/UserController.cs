using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Services;

namespace Gauniv.WebServer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersWithGamesAsync();
            return View(users);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userService.GetUserDetailsAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["TotalPlayTime"] = _userService.GetTotalPlayTime(user);
            ViewData["MostPlayedGame"] = _userService.GetMostPlayedGame(user);
            ViewData["LastPlayTime"] = _userService.GetLastPlayTime(user);

            return View(user);
        }
    }
}