using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.ViewModels
{
    public class AuthorVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        public string AuthorName { get; set; } = string.Empty;
    }
}
