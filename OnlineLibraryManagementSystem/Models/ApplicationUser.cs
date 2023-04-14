using Microsoft.AspNetCore.Identity;

namespace OnlineLibraryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DOB { get; set; } 
        public string Gender { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public int Pincode { get; set; }
        public string FullAddress { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ProfilePicture { get; set; } = null!;
    }
}
