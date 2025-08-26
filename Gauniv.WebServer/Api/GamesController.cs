using AutoMapper;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;
using CommunityToolkit.HighPerformance.Memory;
using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Identity;

namespace Gauniv.WebServer.Api
{
    [Route("api/1.0.0/[controller]/[action]")]
    [ApiController]
    public class GamesController(ApplicationDbContext appDbContext, IMapper mapper, UserManager<User> userManager) : ControllerBase
    {
        private readonly ApplicationDbContext appDbContext = appDbContext;
        private readonly IMapper mapper = mapper;
        private readonly UserManager<User> userManager = userManager;
    }
}
