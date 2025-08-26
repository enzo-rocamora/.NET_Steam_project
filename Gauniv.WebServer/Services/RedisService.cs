using Gauniv.WebServer.Data;
using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Text;

namespace Gauniv.WebServer.Services
{
    public class RedisService
    {
        public ConnectionMultiplexer Redis { get; private set; }
        public IDatabase Database { get; private set; }
        public RedisService(string connectionString)
        {
            ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect(connectionString);
            Database = Redis.GetDatabase(0);
        }
    }
}
