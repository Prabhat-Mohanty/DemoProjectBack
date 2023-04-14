using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.Admin.Book
{
    public class Book
    {
        public int Id { get; set; }     //Id

        [Required(ErrorMessage = "Book name is required")]
        public string BookName { get; set; } = string.Empty;        //BookName

        [Required(ErrorMessage = "Genre is required")]
        public string Genre { get; set; } = string.Empty;      //Genre

        [Required(ErrorMessage = "PublisherId is required")]            //PublisherName
        [Range(0, int.MaxValue, ErrorMessage = "Current Publisher ID should be greater than or equal to 0")]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Publish date is required")]
        [DataType(DataType.Date)]
        public DateTime PublishDate { get; set; }      //PublishDate

        [Required(ErrorMessage = "Language is required")]
        public string Language { get; set; } = string.Empty;        //Language

        [Required(ErrorMessage = "Edition is required")]
        public int Edition { get; set; }        //Edition

        [Required(ErrorMessage = "Book cost is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Book cost should be greater than 0")]
        public int BookCost { get; set; }       //BookCost

        [Required(ErrorMessage = "Number of pages is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of pages should be greater than 0")]
        public int NumberOfPages { get; set; }      //NumberOfPages 

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;     //Description     

        [Required(ErrorMessage = "Actual stocks is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Actual stocks should be greater than 0")]
        public int ActualStocks { get; set; }       //ActualStocks

        [Range(1, 5, ErrorMessage = "Ratings must be between 1 and 5")]
        public int Ratings { get; set; }        //Ratings

        
        // Navigation property
        public ICollection<BookAuthor>? BookAuthors { get; set; } = new HashSet<BookAuthor>();
        public Publisher? Publisher { get; set; }
    }
}