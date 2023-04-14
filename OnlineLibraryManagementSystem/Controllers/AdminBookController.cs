using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using OnlineLibraryManagementSystem.Data;
using OnlineLibraryManagementSystem.Models.Admin.Book;
using OnlineLibraryManagementSystem.ViewModels;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace OnlineLibraryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminBookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminBookController(ApplicationDbContext context)
        {
            _context = context;
        }

        //-------------------------------BOOKS-------------------------------

        // GetAllBooksWithAuthorId
        [HttpGet]
        [Route("getAllBooksWithAuthorId")]
        public async Task<IActionResult> GetAllBooksWithAuthorId()
        {
            try
            {
                var books = await _context.Books
                .Include(b => b.BookAuthors!)
                .ThenInclude(ba => ba.Author)
                .Select(x => new BookVM
                {
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
                    AuthorIds = x.BookAuthors!.Select(ba => ba.AuthorId).ToList()
                })
                .ToListAsync();

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


        //AddBook
        [HttpPost]
        [Route("addbook")]
        public async Task<IActionResult> AddBook([FromBody] BookVM addBookVM)
        {
            try
            {
                if (addBookVM == null) { return BadRequest(); }
                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                // Check if book name already exists in database
                var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.BookName == addBookVM.BookName);
                var PublisherExists = await _context.Publisher.FirstOrDefaultAsync(p => p.Id == addBookVM.PublisherId);

                if (existingBook != null)
                {
                    return Conflict($"Book with name '{addBookVM.BookName}' already exists.");
                }
                else if(PublisherExists == null)
                {
                    return NotFound($"Publisher with Id = '{addBookVM.PublisherId}' doesnot exists.");
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
                    BookAuthors = new List<BookAuthor>()
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
                        return NotFound($"Author Id '{authorId}' doesnot exists.");
                    }
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


        //UpdateBooksWithAuthorIdByBookId
        [HttpPut]
        [Route("updateBook")]
        public async Task<IActionResult> UpdateBooksWithAuthorIdByBookId([FromQuery] int id, [FromBody] BookVM vm)
        {
            var bookid = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);

            if (bookid == null)
            {
                return NotFound("Book not found.");
            }

            var PublisherExists = await _context.Publisher.FirstOrDefaultAsync(p => p.Id == vm.PublisherId);

            if (PublisherExists == null)
            {
                return NotFound($"Publisher with Id = '{vm.PublisherId}' doesnot exists.");
            }

            int bookId = bookid!.Id;

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            book!.BookName = vm.BookName;
            book.Genre = vm.Genre;
            book.PublisherId= vm.PublisherId;
            book.PublishDate = vm.PublishDate;
            book.Language = vm.Language;
            book.Edition = vm.Edition;
            book.BookCost = vm.BookCost;
            book.NumberOfPages = vm.NumberOfPages;
            book.Description = vm.Description;
            book.ActualStocks = vm.ActualStocks;
            
            book.Ratings = vm.Ratings;

            var authorIds = vm.AuthorIds!.ToList();

            var bookAuthorsToDelete = book.BookAuthors!
                .Where(ba => !authorIds.Contains(ba.AuthorId))
                .ToList();

            foreach (var bookAuthorToDelete in bookAuthorsToDelete)
            {
                book.BookAuthors!.Remove(bookAuthorToDelete);
            }

            var bookAuthorsToAdd = authorIds
                .Where(aid => !book.BookAuthors!.Any(ba => ba.AuthorId == aid))
                .Select(aid => new BookAuthor { BookId = book.Id, AuthorId = aid })
                .ToList();

            foreach (var bookAuthorToAdd in bookAuthorsToAdd)
            {
                var authorExists = await _context.Authors.FindAsync(bookAuthorToAdd.AuthorId);
                if(authorExists != null)
                {
                    book.BookAuthors!.Add(bookAuthorToAdd);
                }
                else
                {
                    return NotFound($"Author Id = '{bookAuthorToAdd.AuthorId}' doesnot exits");
                }    
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(bookId))
                {
                    return NotFound("Book not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating book: {ex.Message}");
            }

            return Ok("Book updated successfully.");
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
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

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                return Ok("Book deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting book: {ex.Message}");
            }
        }



        //-------------------------------AUTHOR-------------------------------

        //GetAllAuthor
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
                return Ok("Author added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding the author to database: {ex.Message}");
            }
        }


        //UpdatingAuthorByAuthorId
        [HttpPut]
        [Route("updateAuthor/{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, [FromBody] AuthorVM author)
        {
            try
            {
                var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
                if (existingAuthor == null)
                {
                    return NotFound("Author Not Exists");
                }

                existingAuthor.AuthorName = author.AuthorName;

                _context.Authors.Update(existingAuthor!);
                await _context.SaveChangesAsync();

                return Ok("Author update successfully.");
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

                return Ok("Author deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the author: {ex.Message}");
            }
        }



        //-------------------------------PUBLISHER-------------------------------

        //GetAllAuthor
        [HttpGet]
        [Route("getAllPublisher")]
        public async Task<IActionResult> GetAllPublisher()
        {
            try
            {
                var publisher = await _context.Publisher.Select(x => new PublisherVM
                {
                    //AuthorId = x.Id,
                    //AuthorName = x.AuthorName,

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
                return Ok("Publisher added successfully.");
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

                return Ok("Publisher update successfully.");
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

                return Ok("Publisher deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the publisher: {ex.Message}");
            }
        }


    }
}
