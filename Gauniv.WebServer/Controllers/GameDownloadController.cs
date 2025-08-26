using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/games")]
    [ApiController]
    public class GameDownloadController : ControllerBase
    {
        private readonly GameFileService _gameFileService;
        private readonly GameService _gameService;

        public GameDownloadController(
            GameFileService gameFileService,
            GameService gameService)
        {
            _gameFileService = gameFileService;
            _gameService = gameService;
        }

        [HttpGet("{id}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadGame(int id)
        {
            try
            {
                // Verify game exists and user owns it
                var game = await _gameService.GetByIdAsync(id);
                if (game == null)
                    return NotFound("Game not found");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!game.Owners.Any(o => o.Id == userId))
                    return Forbid("You must own this game to download it");

                if (string.IsNullOrEmpty(game.PayloadPath))
                    return NotFound("Game file not found");

                // Open a file stream
                var fileStream = await _gameFileService.OpenGameFileStreamAsync(game.PayloadPath);

                // Set response headers for chunked transfer
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{game.Title}{Path.GetExtension(game.PayloadPath)}\"");
                Response.Headers.Append("X-Content-Length", game.FileSize.ToString());

                // Return file stream for chunked download
                return File(fileStream, "application/octet-stream", enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Game file not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error downloading game: {ex.Message}");
            }
        }
    }
}