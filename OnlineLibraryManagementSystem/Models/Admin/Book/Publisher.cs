namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class Publisher
    {
        public int Id { get; set; }
        public string PublisherName { get; set; } = string.Empty;
        public ICollection<Book>? Books { get; set; }
    }
}
