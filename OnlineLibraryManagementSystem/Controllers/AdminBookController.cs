using Humanizer.Localisation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using OnlineLibraryManagementSystem.Data;
using OnlineLibraryManagementSystem.Models.Admin.Book;
using OnlineLibraryManagementSystem.Models.Authentication.SignUp;
using OnlineLibraryManagementSystem.ViewModels;
using System;
using System.Net;
using System.Text;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace OnlineLibraryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminBookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminBookController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        // For Upload Images
        [NonAction]
        private async Task<string?> UploadImageAsync(IFormFile image, string bname)
        {
            if (image != null || image!.Length > 0)
            {
                string path = _webHostEnvironment.WebRootPath + "\\bookImages\\" + bname + "\\";
                string fileName = image.FileName;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var filePath = Path.Combine(path, fileName);

                using (FileStream fileStream = System.IO.File.Create(filePath))
                {
                    await image.CopyToAsync(fileStream);
                    fileStream.Flush();
                    //return filePath;
                    return "bookImages/" + bname + "/" + fileName;
                }
            }
            return null;
        }

        //  For Update Uploaded Images
        int counter = 0;
        [NonAction]
        private async Task<string?> UploadUpdatedImageAsync(IFormFile image, string bname)
        {
            if (image != null || image!.Length > 0)
            {
                string path = _webHostEnvironment.WebRootPath + "\\bookImages\\" + bname + "\\";
                string fileName = image.FileName;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                else if (counter == 0)
                {
                    Directory.Delete(path, true);
                    Directory.CreateDirectory(path);
                    counter++;
                }


                var filePath = Path.Combine(path, fileName);

                using (FileStream fileStream = System.IO.File.Create(filePath))
                {
                    await image.CopyToAsync(fileStream);
                    fileStream.Flush();
                    return "bookImages/" + bname + "/" + fileName;
                }
            }
            return null;
        }

        //  For Deleting Uploaded Images
        [NonAction]
        private string? DeleteUploadedImageAsync(string bname)
        {
            if (bname != null || bname!.Length > 0)
            {
                string path = _webHostEnvironment.WebRootPath + "\\bookImages\\" + bname + "\\";

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);

                    return "book deleted succesfully";
                }
            }
            return null;
        }

        //-------------------------------MainActions-------------------------------
        
        //-------------------------------BOOKS-------------------------------
        
        //  GetAllBooksWithAuthorId
        [HttpGet]
        [Route("getAllBooksWithAuthorId")]
        public async Task<IActionResult> GetAllBooksWithAuthorId()
        {
            try
            {
                var books = await _context.Books
                .Include(b => b.BookAuthors!)
                .ThenInclude(ba => ba.Author)
                .Include(p => p.Publisher!)
                .Include(b => b.BookImages)
                .Select(x => new
                {
                    Id = x.Id,
                    BookName = x.BookName!,
                    Genre = x.Genre!,
                    //PublisherId = x.PublisherId!,
                    PublisherId = x.Publisher!.PublisherName,
                    PublishDate = x.PublishDate!,
                    Language = x.Language!,
                    Edition = x.Edition!,
                    BookCost = x.BookCost!,
                    NumberOfPages = x.NumberOfPages!,
                    Description = x.Description!,
                    ActualStocks = x.ActualStocks!,
                    Ratings = x.Ratings!,
                    AuthorIds = x.BookAuthors!.Select(ba => ba.Author!.AuthorName).ToList(),
                    Images = x.BookImages.Select(b => b.ImageUrl).ToList(),
                })
                .ToListAsync();
                                
                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


        //  GetBookById
        [HttpGet]
        [Route("GetBookById/{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            try
            {
                var bookExists = BookExists(id);

                if (bookExists)
                {
                    var book = await _context.Books
                        .Where(b => b.Id == id)
                    .Include(b => b.BookAuthors!)
                    .ThenInclude(ba => ba.Author)
                    .Include(b => b.BookImages)
                    .Select(x => new
                    {
                        Id = x.Id,
                        BookName = x.BookName!,
                        Genre = x.Genre!,
                        PublisherId = x.PublisherId!,
                        PublishDate = x.PublishDate!,
                        Language = x.Language!,
                        Edition = x.Edition!,
                        BookCost = x.BookCost!,
                        NumberOfPages = x.NumberOfPages!,
                        Description = x.Description!,
                        ActualStocks = x.ActualStocks!,
                        Ratings = x.Ratings!,
                        //AuthorIds = x.BookAuthors!.Select(ba => ba.Author!.AuthorName).ToList(),
                        AuthorIds = x.BookAuthors!.Select(ba => ba.Author!.AuthorName).ToList(),
                        Images = x.BookImages.Select(b => b.ImageUrl).ToList(),
                    })
                    .FirstOrDefaultAsync();

                    return Ok(book);
                }

                return NotFound($"Book with Id = '{id}' not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


        // AddBook
        [HttpPost]
        [Route("addbook")]
        public async Task<IActionResult> AddBook([FromForm] BookVM addBookVM, List<IFormFile> images)
        {
            try
            {
                if (addBookVM == null)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                // Check if book name already exists in the database
                var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.BookName == addBookVM.BookName);
                // Check if publisher id is present or not in the database
                var publisherExists = await _context.Publisher.FirstOrDefaultAsync(p => p.Id == addBookVM.PublisherId);

                if (existingBook != null)
                {
                    return Conflict($"Book with name '{addBookVM.BookName}' already exists.");
                }
                else if (publisherExists == null)
                {
                    return NotFound($"Publisher with Id = '{addBookVM.PublisherId}' does not exist.");
                }

                var book = new Book()
                {
                    BookName = addBookVM.BookName,
                    Genre = addBookVM.Genre,
                    PublisherId = addBookVM.PublisherId,
                    PublishDate = addBookVM.PublishDate,
                    Language = addBookVM.Language,
                    Edition = addBookVM.Edition,
                    BookCost = addBookVM.BookCost,
                    NumberOfPages = addBookVM.NumberOfPages,
                    Description = addBookVM.Description,
                    ActualStocks = addBookVM.ActualStocks,
                    Ratings = addBookVM.Ratings,
                    BookAuthors = new List<BookAuthor>(),
                    BookImages = new List<BookImage>()
                };

                foreach (var authorId in addBookVM.AuthorIds!)
                {
                    var author = await _context.Authors.FindAsync(authorId);

                    if (author != null)
                    {
                        book.BookAuthors.Add(new BookAuthor()
                        {
                            AuthorId = authorId,
                        });
                    }
                    else
                    {
                        return NotFound($"Author Id '{authorId}' does not exist.");
                    }
                }

                foreach (var image in images)
                {
                    var imageUrl = await UploadImageAsync(image, addBookVM.BookName);

                    book.BookImages.Add(new BookImage()
                    {
                        ImageUrl = imageUrl!
                    });
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                return Ok(addBookVM);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //  UpdateBook
        [HttpPut]
        [Route("updatebook/{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromForm] BookVM bookVM, List<IFormFile> images)
        {
            try
            {
                var bookExists = BookExists(id);

                if (!bookExists)
                {
                    return NotFound($"Book with Id = '{id}' not found.");
                }

                var book = await _context.Books.Include(b => b.BookAuthors).Include(b => b.BookImages).FirstOrDefaultAsync(b => b.Id == id);

                if (book == null)
                {
                    return NotFound($"Book with Id = '{id}' not found.");
                }

                var publisherExists = await _context.Publisher.FirstOrDefaultAsync(p => p.Id == bookVM.PublisherId);
                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                // Check if publisher id is present or not in the database
                else if (publisherExists == null)
                {
                    return NotFound($"Publisher with Id = '{bookVM.PublisherId}' does not exist.");
                }

                book.BookName = bookVM.BookName;
                book.Genre = bookVM.Genre;
                book.PublisherId = bookVM.PublisherId;
                book.PublishDate = bookVM.PublishDate;
                book.Language = bookVM.Language;
                book.Edition = bookVM.Edition;
                book.BookCost = bookVM.BookCost;
                book.NumberOfPages = bookVM.NumberOfPages;
                book.Description = bookVM.Description;
                book.ActualStocks = bookVM.ActualStocks;
                book.Ratings = bookVM.Ratings;

                // Update book authors
                var existingAuthorIds = book.BookAuthors!.Select(ba => ba.AuthorId);
                var newAuthorIds = bookVM.AuthorIds!.Except(existingAuthorIds);

                foreach (var authorId in newAuthorIds)
                {
                    var author = await _context.Authors.FindAsync(authorId);

                    if (author != null)
                    {
                        book.BookAuthors!.Add(new BookAuthor()
                        {
                            AuthorId = authorId,
                        });
                    }
                    else
                    {
                        return NotFound($"Author Id '{authorId}' does not exist.");
                    }
                }

                foreach (var bookAuthor in book.BookAuthors!.ToList())
                {
                    if (!bookVM.AuthorIds!.Contains(bookAuthor.AuthorId))
                    {
                        book.BookAuthors!.Remove(bookAuthor);
                    }
                }

                // Update book images
                foreach (var bookImage in book.BookImages)
                {
                    _context.BookImages.Remove(bookImage);
                }

                foreach (var image in images)
                {
                    var imageUrl = await UploadUpdatedImageAsync(image, bookVM.BookName);

                    book.BookImages.Add(new BookImage()
                    {
                        ImageUrl = imageUrl!
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(bookVM);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //DeleteBookById
        [HttpDelete]
        [Route("deleteBook/{id}")]
        public async Task<IActionResult> DeleteBookById(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);

                if (book == null)
                {
                    return NotFound("Book not found.");
                }

                var deleteImages = DeleteUploadedImageAsync(book.BookName);
                if (deleteImages != null)
                {
                    _context.Books.Remove(book);
                    await _context.SaveChangesAsync();
                    return Ok("Book deleted successfully.");
                }
                return BadRequest("Book cannot be deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting book: {ex.Message}");
            }
        }


        // Get Books by Genre
        //[HttpGet]
        //[Route("category")]
        //public async Task<IActionResult> GetBooksByGenre([FromQuery] List<string> genres, [FromQuery])
        //{
        //    try
        //    {
        //        var books = await _context.Books.Where(b => genres.Contains(b.Genre))
        //        .Include(b => b.BookAuthors!)
        //        .ThenInclude(ba => ba.Author)
        //        .Include(b => b.BookImages)
        //        .Select(x => new
        //        {
        //            Id = x.Id,
        //            BookName = x.BookName!,
        //            Genre = x.Genre!,
        //            PublisherId = x.PublisherId!,
        //            PublishDate = x.PublishDate!,
        //            Language = x.Language!,
        //            Edition = x.Edition!,
        //            BookCost = x.BookCost!,
        //            NumberOfPages = x.NumberOfPages!,
        //            Description = x.Description!,
        //            ActualStocks = x.ActualStocks!,
        //            Ratings = x.Ratings!,
        //            AuthorIds = x.BookAuthors!.Select(ba => ba.Author!.AuthorName).ToList(),
        //            Images = x.BookImages.Select(b => b.ImageUrl).ToList(),
        //        }).ToListAsync();


        //        if (books != null && books.Count > 0)
        //        {
        //            return Ok(books);
        //        }
        //        StringBuilder myStringBuilder = new StringBuilder("[");
        //        foreach (var genre in genres)
        //        {
        //            myStringBuilder.Append(genre+",");
        //        }

        //        myStringBuilder.Remove(myStringBuilder.Length - 1,1);
        //        myStringBuilder.Insert(myStringBuilder.Length, ']');

        //        return NotFound($"Book having this Genres = {myStringBuilder} are not found.");
        //    }

        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex}");
        //    }
        //}



        //-------------------------------AUTHOR-------------------------------


        //GetAllAuthor


        [HttpGet]
        [Route("category")]
        public async Task<IActionResult> GetBooksByGenre([FromQuery] string? search, [FromQuery] List<string> genres)
        {
            try
            {
                var books = await _context.Books
            .Where(b => genres.Contains(b.Genre) && (string.IsNullOrEmpty(search) || b.BookName.Contains(search)))
            .Include(b => b.BookAuthors!)
            .ThenInclude(ba => ba.Author)
            .Include(b => b.BookImages)
            .Select(x => new
            {
                Id = x.Id,
                BookName = x.BookName!,
                Genre = x.Genre!,
                PublisherId = x.PublisherId!,
                PublishDate = x.PublishDate!,
                Language = x.Language!,
                Edition = x.Edition!,
                BookCost = x.BookCost!,
                NumberOfPages = x.NumberOfPages!,
                Description = x.Description!,
                ActualStocks = x.ActualStocks!,
                Ratings = x.Ratings!,
                AuthorIds = x.BookAuthors!.Select(ba => ba.Author!.AuthorName).ToList(),
                Images = x.BookImages.Select(b => b.ImageUrl).ToList(),
            })
            .ToListAsync();

                if (books != null && books.Count > 0)
                {
                    return Ok(books);
                }

                StringBuilder myStringBuilder = new StringBuilder("[");
                foreach (var genre in genres)
                {
                    myStringBuilder.Append(genre + ",");
                }
                myStringBuilder.Remove(myStringBuilder.Length - 1, 1);
                myStringBuilder.Insert(myStringBuilder.Length, ']');

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpGet]
        [Route("getauthorbyid/{id}")]
        public async Task<IActionResult> GetAuthorById(int id)
        {
            var author = await _context.Authors.FindAsync(id);

            if(author == null)
            {
                return NotFound();
            }
            return Ok(author);
        }

        [HttpGet]
        [Route("getAllAuthor")]
        public async Task<IActionResult> GetAllAuthor()
        {
            try
            {
                var author = await _context.Authors.Select(x => new AuthorVM
                {
                    //AuthorId = x.Id,
                    //AuthorName = x.AuthorName,

                    Id = x.Id,
                    AuthorName = x.AuthorName,

                }).ToListAsync();

                if (author == null)
                {
                    return NotFound("No Author available.");
                }
                return Ok(author);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


        //AddAuthor
        [HttpPost]
        [Route("addAuthor")]
        public async Task<IActionResult> AddAuthor([FromBody] Author author)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var authorExists = await _context.Authors.FirstOrDefaultAsync(b => b.AuthorName == author.AuthorName);
                if (authorExists != null)
                {
                    return Conflict($"Author with name '{author.AuthorName}' already exists.");
                }

                _context.Authors.Add(author);
                await _context.SaveChangesAsync();

                var authors = await _context.Authors.Select(x => new AuthorVM
                {
                    //AuthorId = x.Id,
                    //AuthorName = x.AuthorName,

                    Id = x.Id,
                    AuthorName = x.AuthorName,

                }).ToListAsync();
                if (authors == null)
                {
                    return NotFound("No Author available.");
                }
                return Ok(authors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding the author to database: {ex.Message}");
            }
        }


        //UpdatingAuthorByAuthorId
        [HttpPut]
        [Route("updateAuthor/{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, [FromBody] string name)
        {
            try
            {
                var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
                if (existingAuthor == null)
                {
                    return NotFound("Author Not Exists");
                }

                existingAuthor.AuthorName = name;

                _context.Authors.Update(existingAuthor!);
                await _context.SaveChangesAsync();
                var authors = await _context.Authors.Select(x => new AuthorVM
                {
                    //AuthorId = x.Id,
                    //AuthorName = x.AuthorName,

                    Id = x.Id,
                    AuthorName = x.AuthorName,

                }).ToListAsync();
                if (authors == null)
                {
                    return NotFound("No Author available.");
                }
                return Ok(authors);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the author: {ex.Message}");
            }
        }


        //DeleteAuthorById
        [HttpDelete]
        [Route("deleteAuthor/{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            try
            {
                var author = await _context.Authors.FindAsync(id);
                if (author == null)
                {
                    return NotFound("Author does not exist.");
                }

                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                var authors = await _context.Authors.Select(x => new AuthorVM
                {
                    //AuthorId = x.Id,
                    //AuthorName = x.AuthorName,

                    Id = x.Id,
                    AuthorName = x.AuthorName,

                }).ToListAsync();
                if (authors == null)
                {
                    return NotFound("No Author available.");
                }
                return Ok(authors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the author: {ex.Message}");
            }
        }



        //-------------------------------PUBLISHER-------------------------------

        [HttpGet]
        [Route("getpublisherbyid/{id}")]
        public async Task<IActionResult> GetPublisherById(int id)
        {
            var publisher = await _context.Publisher.FindAsync(id);

            if (publisher == null)
            {
                return NotFound();
            }
            return Ok(publisher);
        }


        //GetAllAuthor
        [HttpGet]
        [Route("getAllPublisher")]
        public async Task<IActionResult> GetAllPublisher()
        {
            try
            {
                var publisher = await _context.Publisher.Select(x => new PublisherVM
                {
                    Id = x.Id,
                    PublisherName = x.PublisherName,

                }).ToListAsync();

                if (publisher == null)
                {
                    return NotFound("No Publisher available.");
                }
                return Ok(publisher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


        //AddPublisher
        [HttpPost]
        [Route("addPublisher")]
        public async Task<IActionResult> AddPublisher([FromBody] Publisher publisher)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var publisherExists = await _context.Publisher.FirstOrDefaultAsync(b => b.PublisherName == publisher.PublisherName);
                if (publisherExists != null)
                {
                    return Conflict($"Publisher with name '{publisher.PublisherName}' already exists.");
                }
                _context.Publisher.Add(publisher);
                await _context.SaveChangesAsync();

                var publishers = await _context.Publisher.Select(x => new PublisherVM
                {
                    Id = x.Id,
                    PublisherName = x.PublisherName,

                }).ToListAsync();

                if (publishers == null)
                {
                    return NotFound("No Publisher available.");
                }
                return Ok(publishers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding the publisher to database: {ex.Message}");
            }
        }


        //UpdatingPublisherByPublisherId
        [HttpPut]
        [Route("updatePublisher/{id}")]
        public async Task<IActionResult> UpdatePublisher(int id, [FromBody] PublisherVM publisherVM)
        {
            try
            {
                var existingPublisher = await _context.Publisher.FirstOrDefaultAsync(a => a.Id == id);
                if (existingPublisher == null)
                {
                    return NotFound("Publisher Not Exists");
                }

                existingPublisher.PublisherName = publisherVM.PublisherName;

                _context.Publisher.Update(existingPublisher!);
                await _context.SaveChangesAsync();

                var publishers = await _context.Publisher.Select(x => new PublisherVM
                {
                    Id = x.Id,
                    PublisherName = x.PublisherName,

                }).ToListAsync();

                if (publishers == null)
                {
                    return NotFound("No Publisher available.");
                }
                return Ok(publishers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the publisher: {ex.Message}");
            }
        }


        //DeletePublisherById
        [HttpDelete]
        [Route("deletePublisher/{id}")]
        public async Task<IActionResult> DeletePublisher(int id)
        {
            try
            {
                var publisher = await _context.Publisher.FindAsync(id);
                if (publisher == null)
                {
                    return NotFound("Publisher does not exist.");
                }

                _context.Publisher.Remove(publisher);
                await _context.SaveChangesAsync();

                var publishers = await _context.Publisher.Select(x => new PublisherVM
                {
                    Id = x.Id,
                    PublisherName = x.PublisherName,

                }).ToListAsync();

                if (publishers == null)
                {
                    return NotFound("No Publisher available.");
                }
                return Ok(publishers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the publisher: {ex.Message}");
            }
        }

        //-------------------------------ISSUED BOOK-------------------------------

        //GetRequestToBook
        [HttpGet]
        [Route("getpendingrequest")]
        public async Task<IActionResult> GetAllRequest()
        {
            var requests = await _context.IssueBooks.Where(x => x.status == "pending").ToListAsync();
            if (requests == null)
            {
                return NotFound("No requests");
            }
            return Ok(requests);
        }


        [HttpPatch]
        [Route("status/{id}")]
        public async Task<IActionResult> Approved(int reqid, [FromBody] JsonPatchDocument approvebook)
        {
            try
            {
                var reqBook = await _context.IssueBooks.FindAsync(reqid);
                if (reqBook != null)
                {
                    approvebook.ApplyTo(reqBook);
                    await _context.SaveChangesAsync();
                }
                return Ok(reqBook);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}