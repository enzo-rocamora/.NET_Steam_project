using System.Net.Sockets;

namespace Gauniv.GameServer.Core
{
    internal class ClientSession
    {
        public required Player Player { get; set; }
        public required TcpClient Client { get; set; }
        public Guid? CurrentGame { get; set; } = null;
        public String? Token { get; set; } = string.Empty;
    }
}
