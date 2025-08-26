using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Dtos.Games;
using Gauniv.WebServer.Dtos.Common;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/games")]
    [ApiController]
    [AllowAnonymous]
    public class GamesApiController : ControllerBase
    {
        private readonly GameApiService _gameService;

        public GamesApiController(GameApiService gameService)
        {
            _gameService = gameService;
        }

        /// <summary>
        /// Get games with optional pagination and category filters
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<GameDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<GameDto>>> GetGames(
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            [FromQuery(Name = "category[]")] int[]? categories)
        {
            var parameters = new GameQueryParameters
            {
                Offset = offset,
                Limit = limit,
                Categories = categories
            };

            var result = await _gameService.GetGamesAsync(parameters);
            return Ok(result);
        }
    }
}