using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLibraryManagementSystem.Data;
using OnlineLibraryManagementSystem.Models.User;

namespace OnlineLibraryManagementSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
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
                    var user = HttpContext.User.Identity!.Name;

                    var requestBook = new IssueBook()
                    {
                        BookId = issueBook.BookId,
                        userEmail = user!,
                        days = issueBook.days
                    };

                    _context.IssueBooks.Add(requestBook);
                    await _context.SaveChangesAsync();  
                    return Ok(requestBook);
                }

                return NotFound($"Book with Id = '{issueBook.BookId}' not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
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

        [HttpPatch]
        [Route("status/{id}")]
        public async Task<IActionResult> Approved(int reqid, [FromBody] JsonPatchDocument approvebook)
        {
            try
            {
                var reqBook = await _context.IssueBooks.FindAsync(reqid);
                if(reqBook != null)
                {
                    approvebook.ApplyTo(reqBook);
                    await _context.SaveChangesAsync();
                }
                return Ok(reqBook);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}