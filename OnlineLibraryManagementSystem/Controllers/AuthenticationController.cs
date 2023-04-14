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
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using static System.Net.WebRequestMethods;

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
            var message = new Message(new string[] { user.Email! }, "Confirmation email link", confirmationLink!);
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

        [NonAction]
        private string GetFileName(RegisterUser registerUser)
        {
            if (registerUser.ProfilePicture != null)
            {
                string path = _webHostEnvironment.WebRootPath + "\\uploads\\" + registerUser.FirstName + registerUser.LastName + "\\";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (FileStream fileStream = System.IO.File.Create(path + registerUser.ProfilePicture.FileName))
                {
                    registerUser.ProfilePicture.CopyTo(fileStream);
                    fileStream.Flush();
                    return path + registerUser.ProfilePicture.FileName;
                }
            }
            return "Not Uploaded";
        }



        //if (user.TwoFactorEnabled)
        //{
        //    await _signInManager.SignOutAsync();
        //    await _signInManager.PasswordSignInAsync(user, loginModel.Password, false, true);
        //    var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        //    var message = new Message(new string[] { user.Email! }, "OTP Confirmation", token);
        //    _emailService.SendEmail(message);

        //    return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"We have sent an OTP to your email {user.Email}" });
        //}

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            // Check the User
            var user = await _userManager.FindByEmailAsync(loginModel.Email);
            //-------- 


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

        [HttpPost]
        [Route("login2FA")]
        public async Task<IActionResult> LoginWithOTP(string code, string email)
        {
            try
            {

                // Validate input
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
                {
                    return BadRequest(new Response() { Status = "Error", Message = "Invalid input parameters" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new Response() { Status = "Error", Message = "Email not confirmed" });
                }


                var signIn = await _signInManager.TwoFactorSignInAsync("Email", code, false, false); //string provider, string code, bool isPersistent, bool rememberClient

                if (signIn.Succeeded)
                {
                    if (user != null)
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

                        var jwtToken = GetToken(authClaims);

                        //Return The Token
                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                            expiration = jwtToken.ValidTo
                        });
                    }
                }

                return StatusCode(StatusCodes.Status404NotFound, new Response() { Status = "Error", Message = "Invalid code" });
            }
            catch (Exception ex)
            {
                // Handle specific exceptions
                if (ex is ArgumentNullException || ex is ArgumentOutOfRangeException)
                {
                    return BadRequest(new Response { Status = "Error", Message = "Invalid input parameters" });
                }
                else if (ex is InvalidOperationException || ex is NotSupportedException || ex is DbException || ex is IOException || ex is HttpRequestException || ex is SmtpException)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = ex.Message });
                }
                else
                {
                    throw; // re-throw unexpected exceptions
                }
            }
        }


        //[HttpPost]
        //[Route("login2FA")]
        //public async Task<IActionResult> LoginWithOTP(string code, string email)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);
        //    if (!await _userManager.IsEmailConfirmedAsync(user))
        //    {
        //        return BadRequest("You cannot logged in because you have not confirmed your email address which we have sent you when you signed in.");
        //    }

        //    if(!user.EmailConfirmed)
        //    {
        //        return BadRequest("You cannot logged in because you have not confirmed your email address which we have sent you when you signed in.");
        //    }

        //    var signIn = await _signInManager.TwoFactorSignInAsync("Email", code, false, false); //string provider, string code, bool isPersistent, bool rememberClient
        //    if (signIn.Succeeded)
        //    {
        //        if (user != null)
        //        {
        //            // ClaimList Creation
        //            var authClaims = new List<Claim>
        //            {
        //                new Claim(ClaimTypes.Name, user.UserName),
        //                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //            };

        //            var userRoles = await _userManager.GetRolesAsync(user);

        //            //We Add Roles To The List
        //            foreach (var role in userRoles)
        //            {
        //                authClaims.Add(new Claim(ClaimTypes.Role, role));
        //            }


        //            var jwtToken = GetToken(authClaims);


        //            //Return The Token
        //            return Ok(new
        //            {
        //                token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
        //                expiration = jwtToken.ValidTo
        //            });
        //        }
        //    }
        //    return StatusCode(StatusCodes.Status404NotFound, new Response() { Status = "Error", Message = "Invalid Code " });
        //}


        //[HttpPost]
        //[Route("login")]
        //public async Task<IActionResult> Login([FromBody] LoginModel loginModel, bool rememberMe)
        //{
        //    // Check the User
        //    var user = await _userManager.FindByEmailAsync(loginModel.Email);

        //    //Generate The Token With The Claims
        //    if (user.TwoFactorEnabled)
        //    {
        //        await _signInManager.SignOutAsync();
        //        await _signInManager.PasswordSignInAsync(user, loginModel.Password, false, true);
        //        var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        //        var message = new Message(new string[] { user.Email! }, "OTP Confirmation", token);
        //        _emailService.SendEmail(message);

        //        return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"We have sent an OTP to your email {user.Email}" });
        //    }

        //    // Check the Password
        //    if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
        //    {
        //        // ClaimList Creation
        //        var authClaims = new List<Claim>
        //{
        //    new Claim(ClaimTypes.Name, user.UserName),
        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //};

        //        var userRoles = await _userManager.GetRolesAsync(user);

        //        //We Add Roles To The List
        //        foreach (var role in userRoles)
        //        {
        //            authClaims.Add(new Claim(ClaimTypes.Role, role));
        //        }

        //        var jwtToken = GetToken(authClaims);

        //        // Store OTP in cookie if rememberMe is true
        //        if (rememberMe)
        //        {
        //            var options = new CookieOptions
        //            {
        //                Expires = DateTime.Now.AddDays(1),
        //                IsEssential = true,
        //                HttpOnly = true,
        //                SameSite = SameSiteMode.Strict,
        //                Secure = true // Set to true for HTTPS only
        //            };
        //            Response.Cookies.Append("otp", jwtToken.ToString(), options);
        //        }

        //        //Return The Token
        //        return Ok(new
        //        {
        //            token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
        //            expiration = jwtToken.ValidTo
        //        });
        //    }

        //    return Unauthorized();
        //}


        //[HttpPost]
        //[Route("login-2FA")]
        //public async Task<IActionResult> LoginWithOTP(string code, string email)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);

        //    // Check if OTP is in cookie
        //    if (Request.Cookies.TryGetValue("otp", out var otp))
        //    {
        //        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(otp);
        //        if (jwtToken.ValidTo >= DateTime.UtcNow)
        //        {
        //            // Remove OTP from cookie
        //            Response.Cookies.Delete("otp");

        //            if (user != null)
        //            {
        //                // ClaimList Creation
        //                var authClaims = new List<Claim>
        //        {
        //            new Claim(ClaimTypes.Name, user.UserName),
        //            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        };

        //                var userRoles = await _userManager.GetRolesAsync(user);

        //                //We Add Roles To The List
        //                foreach (var role in userRoles)
        //                {
        //                    authClaims.Add(new Claim(ClaimTypes.Role, role));
        //                }

        //                var jwtTokenWithClaims = GetToken(authClaims);

        //                //Return The Token
        //                return Ok(new
        //                {
        //                    token = new JwtSecurityTokenHandler().WriteToken(jwtTokenWithClaims),
        //                    expiration = jwtTokenWithClaims.ValidTo
        //                });
        //            }
        //        }
        //    }

        //    var signIn = await _signInManager.TwoFactorSignInAsync("Email", code, false, false);
        //    if (signIn.Succeeded)
        //    {
        //        if (user != null)
        //        {
        //            // ClaimList Creation
        //            var authClaims = new List<Claim>
        //    {
        //        new Claim(ClaimTypes.Name, user.UserName),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //    };

        //            var userRoles = await _userManager.GetRolesAsync(user);

        //            //We Add Roles To The List
        //            foreach (var role in userRoles)
        //            {
        //                authClaims.Add(new Claim(ClaimTypes.Role, role));
        //            }

        //            var jwtToken = GetToken(authClaims);

        //            //Return The Token
        //            return Ok(new
        //            {
        //                token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
        //                expiration = jwtToken.ValidTo
        //            });
        //        }
        //    }
        //    return Unauthorized();
        //}






        [HttpPost]
        [AllowAnonymous]

        [Route("forget-password")]
        public async Task<IActionResult> ForgetPassword([Required] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var forgotPasswordLink = Url.Action(nameof(ResetPassword), "Authentication", new { token, email = user.Email }, Request.Scheme);

                var message = new Message(new string[] { user.Email! }, "Click this below link", forgotPasswordLink!);
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status200OK, new Response() { Status = "Success", Message = $"Password Change Request Is Sent to {user.Email}. Please Open Your Gmail And Click The Link." });
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
        public async Task<IActionResult> ResetPassword(ResetPassword resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user != null)
            {
                var resetPassResult = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);

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
