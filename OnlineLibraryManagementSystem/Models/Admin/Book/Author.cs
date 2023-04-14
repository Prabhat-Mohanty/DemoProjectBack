using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class Author
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        public string AuthorName { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Book>? Books { get; set; }
    }
}

