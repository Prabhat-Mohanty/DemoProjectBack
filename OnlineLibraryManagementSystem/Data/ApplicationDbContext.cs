using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineLibraryManagementSystem.Models;
using OnlineLibraryManagementSystem.Models.Admin.Book;

namespace OnlineLibraryManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
       public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Author> Authors { get; set; } = null!;
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<BookAuthor> BookAuthor { get; set; } = null!;

        public DbSet<Publisher> Publisher { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            SeedRoles(builder);
        }

        private static void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData
                (
                    new IdentityRole() { Name = "Admin", ConcurrencyStamp = "1", NormalizedName = "Admin" },
                    new IdentityRole() { Name = "User", ConcurrencyStamp = "2", NormalizedName = "User" }
                );
        }
    }   
}
