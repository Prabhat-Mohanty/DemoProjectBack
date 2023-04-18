namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class BookImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public int BookId { get; set; }
        public Book? Book { get; set; }
    }

}