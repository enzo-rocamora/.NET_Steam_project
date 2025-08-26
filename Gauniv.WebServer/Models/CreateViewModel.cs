using System.Runtime.InteropServices;

namespace Gauniv.WebServer.Models
{
    public class CreateViewModel()
    {
        public string Name { get; set; }
        public float Price { get; set; }
        public IFormFile Content { get; set; }
        public int[] Categories { get; set; }
    }
    public class EditViewModel()
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public float? Price { get; set; }
        public IFormFile? Content { get; set; }
        public int[]? Categories { get; set; }
    }
}
