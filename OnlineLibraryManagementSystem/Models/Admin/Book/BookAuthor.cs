using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class BookAuthor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Book ID is required")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Author ID is required")]
        public int AuthorId { get; set; }

        // Navigation properties
        public Book? Book { get; set; }
        public Author? Author { get; set; }
    }
}