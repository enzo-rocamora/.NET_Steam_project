using Microsoft.AspNetCore.Http;

namespace Gauniv.WebServer.Services
{
    public class GameFileService
    {
        private readonly string _uploadDirectory;
        private readonly ILogger<GameFileService> _logger;
        private const int BUFFER_SIZE = 81920; // 80KB buffer for better performance

        public GameFileService(IWebHostEnvironment environment, ILogger<GameFileService> logger)
        {
            _uploadDirectory = Path.Combine(environment.WebRootPath, "games");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
            _logger = logger;
        }

        public async Task<(string fileName, long fileSize)> SaveGameFileAsync(IFormFile file)
        {
            try
            {
                // Generate unique filename
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_uploadDirectory, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                    BUFFER_SIZE, FileOptions.Asynchronous);
                await CopyFileInChunksAsync(file, fileStream);

                var fileInfo = new FileInfo(filePath);
                _logger.Log(LogLevel.Information, new EventId(), $"File saved with size: {fileInfo.Length}", null, (state, exception) => state.ToString());
                return (uniqueFileName, fileInfo.Length);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving game file: {ex.Message}", ex);
            }
        }

        public async Task<Stream> OpenGameFileStreamAsync(string fileName)
        {
            string filePath = Path.Combine(_uploadDirectory, fileName);
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("Game file not found", fileName);
            }

            // Return a buffered file stream optimized for web streaming
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BUFFER_SIZE,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
        }

        public void DeleteGameFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string filePath = Path.Combine(_uploadDirectory, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private async Task CopyFileInChunksAsync(IFormFile sourceFile, Stream destinationStream)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;

            using (var sourceStream = sourceFile.OpenReadStream())
            {
                while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
                {
                    await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
        }
    }
}