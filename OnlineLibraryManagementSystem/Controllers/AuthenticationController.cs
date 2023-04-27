using EmailVerrificationService.Models;
using EmailVerrificationService.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OnlineLibraryManagementSystem.Models;
using OnlineLibraryManagementSystem.Models.Authentication.Login;
using OnlineLibraryManagementSystem.Models.Authentication.SignUp;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;
using System;
using OnlineLibraryManagementSystem.ViewModels;

namespace OnlineLibraryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;


        public AuthenticationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IEmailService emailService, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            _signInManager = signInManager;
        }


        [HttpPost]
        public async Task<IActionResult> Register([FromQuery] string role, [FromForm] RegisterUser registerUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userExist = await _userManager.FindByEmailAsync(registerUser.Email);
            if (userExist != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new Response { Status = "Error", Message = "User Already Exists!" });
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "This Role Does Not Exists." });
            }

            var user = new ApplicationUser()
            {
                FirstName = registerUser.FirstName,
                MiddleName = registerUser.MiddleName,
                LastName = registerUser.LastName,
                Email = registerUser.Email,
                PhoneNumber = registerUser.PhoneNumber,
                DOB = registerUser.DOB,
                Gender = registerUser.Gender,
                City = registerUser.City,
                State = registerUser.State,
                Pincode = registerUser.Pincode,
                FullAddress = registerUser.FullAddress,
                UserName = registerUser.Email,
                Password = registerUser.Password,
                ProfilePicture = GetFileName(registerUser),
                TwoFactorEnabled = true
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "Failed To Create User." });
            }

            await _userManager.AddToRoleAsync(user, role).ConfigureAwait(false);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication", new { token, email = user.Email }, Request.Scheme);
            var message = new Message(new string[] { user.Email! }, "Confirmation email link", $"<a href='{confirmationLink!}'>Click here to reset your password</a>");
            _emailService.SendEmail(message);

            return StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = $"User created & Email has sent to {user.Email} Successfully!!" });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = "Email Verified Successfully!!!" });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "This User Doesnot exists" });
        }


        [HttpPost]
        [Route("updateProfile")]
        public async Task<IActionResult> UpdateProfile([FromForm] RegisterUser registerUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(registerUser.Email);

            user.FirstName = registerUser.FirstName;
            user.LastName = registerUser.LastName;
            user.Email = registerUser.Email;
            user.PhoneNumber = registerUser.PhoneNumber;
            user.DOB = registerUser.DOB;
            user.Gender = registerUser.Gender;
            user.City = registerUser.City;
            user.State = registerUser.State;
            user.Pincode = registerUser.Pincode;
            user.FullAddress = registerUser.FullAddress;

            if (registerUser.ProfilePicture != null)
            {
                user.ProfilePicture = GetFileName(registerUser);
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new Response { Status = "Success", Message = "Profile updated successfully!" });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "Failed to update profile." });
        }

        [Authorize]
        [HttpGet]
        [Route("updateProfile")]
        public async Task<IActionResult> UpdateProfile()
        {
            var user = await _userManager.FindByEmailAsync(HttpContext?.User?.Identity?.Name);
            return Ok(user);
        }


        [NonAction]
        private string GetFileName(RegisterUser registerUser)
        {
            if (registerUser.ProfilePicture != null)
            {
                string path = _webHostEnvironment.WebRootPath + "\\uploads\\" + registerUser.FirstName + registerUser.Email + "\\";
                string fileName = registerUser.ProfilePicture.FileName;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    Directory.Delete(path, true);
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, fileName);

                using (FileStream fileStream = System.IO.File.Create(filePath))
                {
                    registerUser.ProfilePicture.CopyTo(fileStream);
                    fileStream.Flush();
                    return filePath;
                }
            }
            return "Not Uploaded";
        }


        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            // Check the User
            var user = await _userManager.FindByEmailAsync(loginModel.Email);

            // Check the Password
            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                // ClaimList Creation
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var userRoles = await _userManager.GetRolesAsync(user);

                //We Add Roles To The List
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                //Generate The Token With The Claims
                var jwtToken = GetToken(authClaims);


                //Return The Token
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expiration = jwtToken.ValidTo
                });
            }

            return Unauthorized(new Response() { Status = "Error", Message = "Invalid Email or Password" });
        }


        [Authorize]
        [HttpPost]
        [Route("user-reset-password")]
        public async Task<IActionResult> UserResetPassword(UserResetPasswordVM userResetPasswordVM)
        {
            var user = await _userManager.FindByEmailAsync(HttpContext.User.Identity!.Name);
            var checkOldPassword = _userManager.CheckPasswordAsync(user, userResetPasswordVM.oldPassword);

            if (await checkOldPassword)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetPassResult = await _userManager.ResetPasswordAsync(user, token, userResetPasswordVM.newPassword);
                return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"Password has been changed." });
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest, new Response() { Status = "Error", Message = $"Your Old password is not correct, please try again." });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("forget-password")]
        public async Task<IActionResult> ForgetPassword([Required] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var encodedToken = HttpUtility.UrlEncode(token);

                var forgotPasswordLink = "http://localhost:4200/reset-password?token=" + encodedToken + "&email=" + email;

                //var forgotPasswordLink = Url.Action("ResetPassword", "Authentication", new {token, email = user.Email}, Request.Scheme);

                var message = new Message(new string[] { user.Email! }, "Reset your password", $"<a href='{forgotPasswordLink!}'>Click here to reset your password</a>");
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"Password change request has been accepted and mail has been sent on {user.Email}. Please check your gmail for further steps." });
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest, new Response() { Status = "Error", Message = $"{user?.Email} this email is not registered." });
            }
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPassword { Token = token, Email = email };

            return Ok(new
            {
                model
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user != null)
            {
                var receivedToken = HttpUtility.UrlDecode(resetPassword.Token);
                var tokenwithoutspace = receivedToken.Replace(" ", "+");
                var resetPassResult = await _userManager.ResetPasswordAsync(user, tokenwithoutspace, resetPassword.Password);

                if (!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return Ok(ModelState);
                }
                return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"Password has been changed." });
            }
            return StatusCode(StatusCodes.Status400BadRequest, new Response() { Status = "Error", Message = $"Couldnot send link {resetPassword.Email}, please try again." });
        }

        [NonAction]
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddDays(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return token;
        }


        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }


    }
}
