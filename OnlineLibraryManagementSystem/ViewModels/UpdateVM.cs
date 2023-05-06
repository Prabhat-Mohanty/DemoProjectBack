using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.ViewModels
{
    public class UpdateVM
    {

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string MiddleName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime DOB { get; set; }

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public int Pincode { get; set; }

        [Required]
        public string FullAddress { get; set; } = string.Empty;

        public IFormFile? ProfilePicture { get; set; }
    }
}




  
