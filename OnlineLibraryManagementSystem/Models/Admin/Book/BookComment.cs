using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class BookComment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Book ID is required")]
        public int BookId { get; set; }
        
        [Required(ErrorMessage = "User Email is required")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Comment is required")]
        public string Comment { get; set; } = string.Empty;
        
        public Book? Book { get; set; }
    }
}
