using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using OnlineLibraryManagementSystem.Data;
using OnlineLibraryManagementSystem.Models;
using OnlineLibraryManagementSystem.Models.Admin.Book;
using OnlineLibraryManagementSystem.Models.User;
using System.Text.Json;
using static Humanizer.On;

namespace OnlineLibraryManagementSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }


        [HttpPost]
        public async Task<IActionResult> RentABook(IssueBook issueBook)
        {
            try
            {
                var bookExists = BookExists(issueBook.BookId);

                if (bookExists)
                {
                    var book = await _context.Books.Where(b => b.Id == issueBook.BookId).FirstOrDefaultAsync();
                    var user = issueBook.userEmail;
                    var requestBook = new IssueBook()
                    {
                        BookId = issueBook.BookId,
                        userEmail = user!,
                        days = issueBook.days
                    };

                    _context.IssueBooks.Add(requestBook);
                    await _context.SaveChangesAsync();

                    // Find the book by its Id and subtract the actual stock by 1
                    //var selectedBook = await _context.Books.FindAsync(issueBook.BookId);
                    if (book != null)
                    {
                        book.ActualStocks -= 1;
                        await _context.SaveChangesAsync();
                    }
                    return Ok(requestBook);
                }

                return NotFound($"Book with Id = '{issueBook.BookId}' not found.");
            }
            catch
            {
                return BadRequest();
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var orders = await _context.IssueBooks.Where(x => x.status == "Pending").ToListAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        //[HttpGet]
        //[Route("listOfOrders")]
        //public async Task<IActionResult> listOfOrders([FromQuery] string email, [FromQuery] List<string> status, string? search)

        //{
        //    var user = await _userManager.FindByEmailAsync(email);

        //    if (user != null)
        //    {
        //        var bookids = await _context.IssueBooks.Where(b => b.userEmail == email && status.Contains(b.status)).
        //        Include(x => x.Book)
        //        .Select(y => new
        //        {
        //                BookName = y.Book!.BookName,
        //                AppliedDate = y.issued_Date,
        //                DueDate = y.issued_Date,
        //                Images = y.Book.BookImages.Select(b => b.ImageUrl).ToList(),
        //                Status = y.status
        //            })
        //            .ToListAsync();
        //        if (bookids.Count > 0)
        //        {
        //            return Ok(bookids);
        //        }
        //        return NoContent();
        //    }
        //    return NotFound($"No user found with {email} this email");
        //}

        [HttpGet]
        [Route("listOfOrders")]
        public async Task<IActionResult> listOfOrders([FromQuery] string email, [FromQuery] List<string> status, string? search)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var bookids = await _context.IssueBooks.Where(b => b.userEmail == email && status.Contains(b.status))
                    .Include(x => x.Book)
                    .Select(y => new
                    {
                        BookName = y.Book!.BookName,
                        AppliedDate = y.issued_Date,
                        DueDate = y.issued_Date,
                        Images = y.Book.BookImages.Select(b => b.ImageUrl).ToList(),
                        Status = y.status
                    })
                    .ToListAsync();

                if (!string.IsNullOrEmpty(search))
                {
                    bookids = bookids.Where(b => b.BookName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (bookids.Count > 0)
                {
                    return Ok(bookids);
                }

                return NoContent();
            }

            return NotFound($"No user found with {email} this email");
        }




        [HttpPost]
        [Route("Comment/{id}")]
        public async Task<IActionResult> CommentOnBook(int id, string comment)
        {
            var book = await _context.Books.FindAsync(id);

            if (book?.Id != null && comment.Length > 0)
            {
                var addcomment = new BookComment()
                {
                    BookId = book.Id,
                    UserEmail = HttpContext.User.Identity!.Name!,
                    Comment = comment
                };

                //_context.BookComments.Add(addcomment);
                await _context.SaveChangesAsync();
                return Ok("Comment Added Successfully");
            }
            return NotFound($"Book with id = {id} not found");
        }
    }
}