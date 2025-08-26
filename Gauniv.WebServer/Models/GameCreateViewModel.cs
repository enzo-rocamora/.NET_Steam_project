using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.Models
{
    public class GameCreateViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Price { get; set; }

        [Display(Name = "Game File")]
        public IFormFile? GameFile { get; set; }

        // Pour l'édition
        public string? ExistingPayloadPath { get; set; }
        public long ExistingFileSize { get; set; }

        public List<int> SelectedCategories { get; set; } = new();
    }
}