// See https://aka.ms/new-console-template for more information
using Gauniv.GameServer.Core;

class Program
{
    public static async Task Main(string[] args)
    {
        const int PORT = 5000;  // You can change this directly in the code

        try
        {
            Console.WriteLine($"Starting game server on port {PORT}...");
            var server = new GameServer(PORT);

            // Handle shutdown gracefully
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Start the server
            _ = server.Start(); // Run in background

            Console.WriteLine("Server is running...");

            // Wait for cancellation
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}