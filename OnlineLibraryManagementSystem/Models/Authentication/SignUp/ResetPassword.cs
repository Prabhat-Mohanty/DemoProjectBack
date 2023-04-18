using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryManagementSystem.Models.Authentication.SignUp
{
    public class ResetPassword
    {
        [Required]
        public string Password { get; set; } = null!;

        [Compare("Password", ErrorMessage = "This password and confirm password do not match.")]
        public string ConfirmPassword { get; set;} = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}