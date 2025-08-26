using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services
{
    public class OnlineService(IHubContext<OnlineHub> hubContext) : IHostedService
    {
        private readonly IHubContext<OnlineHub> hubContext = hubContext;
        private Task? task;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
