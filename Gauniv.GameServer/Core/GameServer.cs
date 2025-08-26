using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Gauniv.Network.ServerApi;
using MessagePack;
using static System.Net.WebRequestMethods;

namespace Gauniv.GameServer.Core
{
    internal class GameServer
    {
        // API Call for Login
        private ServerApi _serverApi;
        private HttpClient _httpClient;
        private string apiURL = "https://localhost:62966";

        // TCP Server for Game
        private readonly TcpListener _listener;
        private readonly Dictionary<Guid, GameInfo> _games;
        private readonly Dictionary<TcpClient, ClientSession> _clients;
        private bool _running;

        public GameServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            
            _games = [];

            Player p1 = new() { Id = Guid.NewGuid(), Name = "Thibo", Ready = true };
            GameInfo g1 = new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Lobby 1",
                GridColumn = 8,
                GridRow = 8,
                Creator = p1,
                State = GameState.InProgress,
                SelectedCell = (0, 0),
                Players = new Dictionary<Guid, Player>
                {
                    { p1.Id, p1 },
                    { Guid.NewGuid(), new Player { Name = "Enzo", Ready = true } },
                    { Guid.NewGuid(), new Player { Name = "Dylan", Ready = false } }
                }
            };
            _games.Add(g1.GameId, g1);

            Player p2 = new() { Name = "Thibo", Ready = true };
            GameInfo g2 = new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Lobby 2",
                Creator = p2,
                GridColumn = 4,
                GridRow = 4,
                State = GameState.InProgress,
                SelectedCell = (0, 0),
                Players = []
            };
            _games.Add(g2.GameId, g2);

            Player p3 = new() { Name = "Thibo", Ready = true };
            GameInfo g3 = new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Lobby 3",
                Creator = p3,
                GridColumn = 6,
                GridRow = 6,
                State = GameState.Finished,
                SelectedCell = (0, 0),
                Players = []
            };
            _games.Add(g3.GameId, g3);

            _clients = [];
        }

        public async Task Start()
        {
            _running = true;
            _listener.Start();

            _ = CleanupGamesAsync(TimeSpan.FromMinutes(5));

            while (_running)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client : {ex.Message}");
                }
            }
        }

        async Task HandleClientAsync(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint as System.Net.IPEndPoint;
            Console.WriteLine($"New client connected from : {clientEndpoint}");

            try
            {
                using var stream = client.GetStream();

                while (_running)
                {
                    using var streamReader = new MessagePackStreamReader(stream);
                    while (await streamReader.ReadAsync(default) is ReadOnlySequence<byte> msgpack)
                    {
                        await HandleMessageAsync(client, MessagePackSerializer.Deserialize<IGameMessage>(msgpack, cancellationToken: default));
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
                await HandleDisconnectAsync(client);
            }
            catch (SocketException e)
            { 
                Console.WriteLine("SocketException: {0}", e);
                await HandleDisconnectAsync(client);
            }
            finally
            {
                await HandleDisconnectAsync(client);
            }
        }

        private async Task HandleDisconnectAsync(TcpClient client)
        {
            if (_clients.TryGetValue(client, out ClientSession? clientSession))
            {

                Console.WriteLine($"Player {clientSession.Player.Name} connection closed!");
                _clients.Remove(client);

                if (clientSession.CurrentGame != null)
                {
                    var gameId = clientSession.CurrentGame;
                    var game = _games[gameId.Value];
                    var player = game.Players.FirstOrDefault(p => p.Value.Name == clientSession.Player.Name);

                    if (player.Key != Guid.Empty)
                    {
                        game.Players.Remove(player.Key);
                        Console.WriteLine($"Player {clientSession.Player.Name} removed from Game : {game.GameName} : {game.GameId}");
                        await UpdatePlayerListAsync((Guid)gameId);

                        if (game.Creator.Id == clientSession.Player.Id)
                        {
                            Console.WriteLine("Game Creator disconnected!");
                            foreach (var otherClient in _clients.Keys)
                            {
                                if (_clients[otherClient].CurrentGame == game.GameId)
                                {
                                    Console.WriteLine($"Removing Player : {_clients[otherClient].Player.Name}");
                                    game.Players.Remove(_clients[otherClient].Player.Id);
                                    _clients[otherClient].CurrentGame = null;

                                    var request = new DisconnectPlayer
                                    {
                                        GameId = game.GameId
                                    };

                                    await SendMessageAsync(otherClient, request);
                                }
                            }
                        }

                        if (game.Players.Count == 0)
                        {
                            _games.Remove(game.GameId);
                            Console.WriteLine($"Game {game.GameName} : {gameId} removed because no players left");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Client disconnected!");
            }
            client.Close();
        }

        private async Task HandleMessageAsync(TcpClient client, IGameMessage message)
        {
            switch (message)
            {
                case AuthenticationRequest m:
                    await HandleAuthenticationAsync(client, m);
                    break;

                case ServerListRequest:
                    await HandleServerListAsync(client);
                    break;

                case CreateNewGameRequest m:
                    await HandleCreateNewGameAsync(client, m);
                    break;

                case JoinGameRequest m:
                    await HandleJoinGameAsync(client, m);
                    break;

                case PlayerReadyRequest m:
                    await HandlePlayerReadyRequest(client, m);
                    break;

                case StartGameRequest m:
                    await HandleStartGameRequest(client, m);
                    break;

                case GameMasterCellSelection m:
                    await HandleGameMasterCellSelection(m);
                    break;

                case GameMasterBoardSelection m:
                    Console.WriteLine("Game Master Board Selection");
                    await HandleGameMasterBoardSelection(m);
                    break;

                case PlayerCellSelectionResponse m:
                    await ProcessPlayerResponse(m);
                    break;

                default:
                    Console.WriteLine($"Unknown message received from client");
                    break;
            }
        }
        
        private static async Task SendMessageAsync(TcpClient client, IGameMessage message)
        {
            var data = MessagePackSerializer.Serialize(message);
            await client.GetStream().WriteAsync(data);
        }

        private async Task HandleAuthenticationAsync(TcpClient client, AuthenticationRequest request)
        {
            // Log the attempt
            var clientEndpoint = client.Client.RemoteEndPoint as System.Net.IPEndPoint;
            Console.WriteLine($"Authentication attempt from {clientEndpoint} : {request.Username}");

            string message = "Invalid username or password";

            bool isValid = false;
            bool fallback = false;
            var username = request.Username.Split('@')[0];
            AccessTokenResponse? loginResponse = null;

            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiURL)
                };

                _serverApi = new ServerApi(_httpClient) { BaseUrl = apiURL };

                Console.WriteLine("Sending Login Request to ServerAPI.");
                var loginRequest = new LoginRequest
                {
                    Email = request.Username,
                    Password = request.Password
                };

                loginResponse = await _serverApi.LoginAsync(false, false, loginRequest);
                isValid = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Fallback to GameServer only Login!");
                fallback = true;
            }

            if (fallback)
            {
                isValid = request.Username.Length >= 4 &&
                   !username.Any(char.IsWhiteSpace) &&
                   username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-') &&
                   request.Password.Length >= 4 &&
                   !request.Password.Any(char.IsWhiteSpace);

                // Check if username already exists
                if (isValid)
                {
                    string usernameLower = username.ToLower();
                    isValid = !_clients.Values.Any(cs => cs.Player.Name.Equals(usernameLower, StringComparison.CurrentCultureIgnoreCase));
                    message = "User already logged in";
                }
            }

            if (isValid)
            {
                // Add the client to the _clients dictionary
                var clientSession = new ClientSession
                {
                    Player = new Player
                    {
                        Name = username,
                        Token = loginResponse?.AccessToken ?? "NoToken"
                    },
                    Client = client,
                };
                _clients.Add(client, clientSession);
                Console.WriteLine($"Created Player : {clientSession.Player.Name} with id : {clientSession.Player.Id}, token : {(loginResponse?.AccessToken ?? "NoToken")}");
                message = "Authentication successful";
            }

            var response = new AuthenticationResponse
            {
                Success = isValid,
                Player = isValid ? _clients[client].Player : new Player(),
                Message = message
            };

            Console.WriteLine($"Authentication {(isValid ? "Success" : "Failed")} from {clientEndpoint} : {request.Username} {(isValid ? ":" + _clients[client].Player.Id : null)}");
            await SendMessageAsync(client, response);
        }

        private async Task HandleServerListAsync(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint as System.Net.IPEndPoint;
            Console.WriteLine($"Server List request from : {clientEndpoint}");

            var serverList = new ServerListResponse
            {
                Servers = _games.Values.Select(game => new GameInfo
                {
                    GameId = game.GameId,
                    GameName = game.GameName,
                    GridRow = game.GridRow,
                    GridColumn = game.GridColumn,
                    Creator = game.Creator,
                    State = game.State,
                    Players = game.Players
                }).ToList()
            };
            await SendMessageAsync(client, serverList);
        }

        private async Task HandleCreateNewGameAsync(TcpClient client, CreateNewGameRequest m)
        {
            Console.WriteLine($"Create new game request from {client.Client.RemoteEndPoint} : {m.GameName}");
            _clients.TryGetValue(client, out ClientSession? clientSession);

            GameInfo newGame = new()
            {
                GameId = Guid.NewGuid(),
                GameName = m.GameName,
                Creator = clientSession.Player,
                GridColumn = m.GridColumn,
                GridRow = m.GridRow,
                State = GameState.WaitingForPlayers
            };

            clientSession.Player.Ready = true;
            newGame.Players.Add(clientSession.Player.Id, clientSession.Player);

            _games.Add(newGame.GameId, newGame);
            clientSession.CurrentGame = newGame.GameId;

            Console.WriteLine($"Game {newGame.GameName} created with Id : {newGame.GameId}");
            await SendMessageAsync(client, new CreateNewGameResponse
            {
                Success = true,
                Game = newGame,
                Message = "Game created successfully"
            });

            await UpdatePlayerListAsync(newGame.GameId);
        }

        private async Task HandleJoinGameAsync(TcpClient client, JoinGameRequest m)
        {
            Console.WriteLine($"Join game request : {m.GameId} from {client.Client.RemoteEndPoint}");

            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.State == GameState.WaitingForPlayers)
                {
                    // Add the client to the game session
                    _clients.TryGetValue(client, out ClientSession? clientSession);
                    game.Players.Add(clientSession.Player.Id, clientSession.Player);
                    clientSession.CurrentGame = game.GameId;

                    Console.WriteLine($"Player : {client.Client.RemoteEndPoint} added to game : {game.GameName}");
                    await SendMessageAsync(client, new JoinGameResponse
                    {
                        Success = true,
                        Message = "Joined game successfully"
                    });

                    await UpdatePlayerListAsync(game.GameId);
                }
                else
                {
                    Console.WriteLine($"Failed to join game : {m.GameId} for {client.Client.RemoteEndPoint} because {game.State}");
                    await SendMessageAsync(client, new JoinGameResponse
                    {
                        Success = false,
                        Message = "Game is not in a joinable state"
                    });
                }
            }
        }
        
        private async Task UpdatePlayerListAsync(Guid gameId)
        {
            var playerListUpdate = new PlayerListResponse
            {
                Players = _games[gameId].Players
            };

            foreach (var client in _clients.Keys)
            {
                if (_clients[client].CurrentGame == gameId)
                {
                    Console.WriteLine($"Sending Player List update to : {client.Client.RemoteEndPoint} for Game : {gameId}");
                    await SendMessageAsync(client, playerListUpdate);
                }
            }
        }

        private async Task HandlePlayerReadyRequest(TcpClient client, PlayerReadyRequest m)
        {
            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.Players.TryGetValue(m.PlayerId, out Player? player))
                {
                    player.Ready = true;
                    Console.WriteLine($"Player {player.Name} is now ready in game {game.GameName}.");
                    await UpdatePlayerListAsync(m.GameId);
                }
            }
        }

        private async Task HandleStartGameRequest(TcpClient client, StartGameRequest m)
        {
            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.Creator.Id == _clients[client].Player.Id && game.State == GameState.WaitingForPlayers)
                {
                    game.State = GameState.InProgress;
                    Console.WriteLine($"Game {game.GameName} started by {game.Creator.Name}.");

                    bool allPlayersReady = true;
                    foreach (var player in game.Players.Values)
                    {
                        if (!player.Ready)
                        {
                            allPlayersReady = false;
                        }
                    }

                    if (allPlayersReady)
                    {
                        // Select a random player to be the game master
                        var random = new Random();
                        var randomPlayer = game.Players.Values.ElementAt(random.Next(game.Players.Count));
                        game.GameMaster = randomPlayer;
                        Console.WriteLine($"Player {randomPlayer.Name} is selected as the game master for game {game.GameName}.");

                        var playerListUpdate = new StartGameResponse
                        {
                            Success = true,
                            Game = game,
                            Message = "Game started"
                        };

                        foreach (var otherClient in _clients.Keys)
                        {
                            if (_clients[otherClient].CurrentGame == game.GameId)
                            {
                                await SendMessageAsync(otherClient, playerListUpdate);
                            }
                        }
                    }
                    else
                    {
                        var playerListUpdate = new StartGameResponse
                        {
                            Success = false,
                            Message = "Not all players are ready in game " + game.GameName
                        };
                        Console.WriteLine($"Not all players are ready in game {game.GameName}.");
                        await SendMessageAsync(client, playerListUpdate);
                    }
                }
            }
        }

        private async Task HandleGameMasterCellSelection(GameMasterCellSelection m)
        {
            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.GameMaster.Id == m.PlayerId)
                {
                    game.SelectedCell = m.SelectedCell;
                    var cellSelectionResponse = new GameMasterCellSelectionResponse
                    {
                        GameId = m.GameId,
                        SelectedCell = m.SelectedCell
                    };

                    Console.WriteLine($"Game Master {m.PlayerId} Cell Selection : {m.SelectedCell} for game : {m.GameId}");

                    foreach (var client in _clients.Keys)
                    {
                        if (_clients[client].CurrentGame == m.GameId && _clients[client].Player.Id != m.PlayerId)
                        {
                            await SendMessageAsync(client, cellSelectionResponse);
                        }
                    }
                }
            }
        }

        private async Task HandleGameMasterBoardSelection(GameMasterBoardSelection m)
        {
            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.GameMaster.Id == m.PlayerId)
                {
                    var cellSelectionResponse = new GameMasterBoardResponse
                    {
                        GameId = m.GameId,
                        GameGrid = m.GameGrid
                    };

                    Console.WriteLine($"Game Master {m.PlayerId} sending board for game : {m.GameId}");

                    foreach (var client in _clients.Keys)
                    {
                        if (_clients[client].CurrentGame == m.GameId && _clients[client].Player.Id != m.PlayerId)
                        {
                            await SendMessageAsync(client, cellSelectionResponse);
                        }
                    }
                }
            }
        }

        static bool IsEligible(int pos, string name)
        {
            Stopwatch sw = new();
            sw.Start();
            ECDsa key = ECDsa.Create();
            key.GenerateKey(ECCurve.NamedCurves.nistP521);
            int t = 5000 / pos;
            var k = new byte[t];
            var d = Encoding.UTF8.GetBytes(name);
            for (int i = 0; i < t; i++)
            {
                var s = key.SignData([.. d, .. BitConverter.GetBytes(pos)], HashAlgorithmName.SHA512);
                k[i] = s[i % s.Length];
            }
            var res = key.SignData(k, HashAlgorithmName.SHA512);
            sw.Stop();
            Console.WriteLine($"{pos} {sw.ElapsedMilliseconds} {res}");
            if (res[(int)Math.Truncate(res.Length / 4.0)] > 0x7F)
                return true;
            return false;
        }

        private async Task ProcessPlayerResponse(PlayerCellSelectionResponse m)
        {
            Console.WriteLine($"Player {m.PlayerId} Cell Selection for game : {m.GameId} in {m.ResponseTime}ms");

            if (_games.TryGetValue(m.GameId, out GameInfo? game))
            {
                if (game.Players.TryGetValue(m.PlayerId, out Player? player))
                {
                    if (player.Position == 0)
                    {
                        int maxPosition = game.Players.Values.Max(p => p.Position);
                        player.Position = maxPosition + 1;
                        Console.WriteLine($"Assigned position {player.Position} to player {player.Name} in game {game.GameName}.");
                    }

                    int playersWithPositionZero = game.Players.Values.Count(p => p.Position == 0);
                    if (playersWithPositionZero == 1)
                    {
                        Console.WriteLine($"Game : {game.GameName} finished !");
                        var gameResults = new List<GameResult>();

                        var tasks = game.Players.Values
                            .Where(p => p.Position != 0)
                            .Select(async p =>
                            {
                                Console.WriteLine($"Player Name: {p.Name}, Position: {p.Position}");
                                bool isEligible = await Task.Run(() => IsEligible(p.Position, p.Name));
                                Console.WriteLine($"Eligibility check for player {p.Name} with position {p.Position}: {isEligible}");
                                if (!isEligible)
                                {
                                    p.Position = -1;
                                }
                                gameResults.Add(new GameResult { PlayerId = p.Id, PlayerName = p.Name, Position = p.Position, IsEligible = isEligible });
                            });
                        await Task.WhenAll(tasks);

                        // Recalculate positions for eligible players
                        var eligiblePlayers = game.Players.Values.Where(p => p.Position != -1).OrderBy(p => p.Position).ToList();
                        for (int i = 0; i < eligiblePlayers.Count; i++)
                        {
                            eligiblePlayers[i].Position = i + 1;
                        }

                        // Print the new player list
                        var eligiblePlayerNames = eligiblePlayers.Select(p => p.Name);
                        var ineligiblePlayerNames = game.Players.Values.Where(p => p.Position == -1).Select(p => p.Name);
                        Console.WriteLine($"Game {game.GameId} : Eligible players: {string.Join(", ", eligiblePlayerNames)} | Ineligible players: {string.Join(", ", ineligiblePlayerNames)}");

                        var response = new FinishedGameResponse
                        {
                            GameId = m.GameId,
                            GameResult = gameResults
                        };

                        foreach (var client in _clients.Keys)
                        {
                            if (_clients[client].CurrentGame == m.GameId)
                            {
                                Console.WriteLine($"Sending Game Result to : {client.Client.RemoteEndPoint} for Game : {m.GameId}");
                                await SendMessageAsync(client, response);
                                _clients[client].CurrentGame = null;
                                _clients[client].Player.Position = 0;
                                _clients[client].Player.Ready = false;
                            }
                        }
                        game.State = GameState.Finished;
                    }
                }
            }
        }

        async Task CleanupGamesAsync(TimeSpan interval)
        {
            while (_running)
            {
                await Task.Delay(interval);

                var emptyGames = _games.Where(g => g.Value.Players.Count == 0 || g.Value.State == GameState.Finished).ToList();
                if (emptyGames.Count != 0) { Console.WriteLine("Server Cleanup"); }
                foreach (var game in emptyGames)
                {
                    Console.WriteLine($"Removing empty or finished game: {game.Value.GameName} (ID: {game.Key})");
                    _games.Remove(game.Key);
                }
            }
        }
    }
}
