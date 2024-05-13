using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthAPI.Email;
using AuthAPI.JWT;
using AuthAPI.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;

        private readonly TokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public LoginController(UserManager<User> userManager, IMapper mapper, SignInManager<User> signInManager, TokenService tokenService, IEmailSender emailSender)
        {
            _userManager = userManager;
            _mapper = mapper;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }


        [HttpPost]
        public async Task<IActionResult> PostAsync(UserLogin userLogin)
        {
            var user = await _userManager.FindByEmailAsync(userLogin.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, userLogin.Password, false, true);
            Console.WriteLine("Direct login: " + result.Succeeded);
            if (result.Succeeded)
            {
                var JwtHelper = new JwtHelper(_tokenService, _userManager);
                var jwtToken = await JwtHelper.GenerateTokenAsync(user);
                return Ok(new { Token = jwtToken });
            }
            else if (result.RequiresTwoFactor)
            {
                Console.WriteLine("Cere 2 factori");
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                await _emailSender.SendEmailAsync(user.Email, "Your security code", $"Your security code is: {token}");
                return StatusCode(203, new { Message = "Two-factor authentication required. Please verify with the code sent to your email." });
            }
            else if (result.IsLockedOut)
            {
                return StatusCode(423, "Account is locked. Please try again later.");
            }
            else if (result.IsNotAllowed)
            {
                return StatusCode(401, "Login not allowed. Ensure the account has necessary permissions and is verified.");
            }
            else
            {
                return StatusCode(400, "Invalid login attempt.");
            }
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactorCode(TwoFactorVerification verification)
        {
             Console.WriteLine($"Received 2FA verification request: Email={verification.Email}, Code={verification.Code}, RememberClient={verification.RememberClient}");
            var user = await _userManager.FindByEmailAsync(verification.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _signInManager.TwoFactorSignInAsync("Email", verification.Code, isPersistent: false, false);
            if (result.RequiresTwoFactor);
               Console.WriteLine("Cere 2 factori");
            Console.WriteLine("UITE AICI " + result.ToString());
            if (result.Succeeded)
            {
                var JwtHelper = new JwtHelper(_tokenService, _userManager);
                var jwtToken = await JwtHelper.GenerateTokenAsync(user);
                return Ok(jwtToken);
            }
            else if (result.IsLockedOut)
            {
                return StatusCode(423, "Account is locked. Please try again later.");
            }
            else if (result.IsNotAllowed)
            {
                return StatusCode(401, "Login not allowed. Ensure the account has necessary permissions and is verified.");
            }
            else
            {
                return BadRequest("Invalid 2FA code.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(UserLogout userLogout)
        {
            var user = await _userManager.FindByEmailAsync(userLogout.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Sign out the user
            await _signInManager.SignOutAsync();
            await _signInManager.ForgetTwoFactorClientAsync();

            // If using JWT tokens, you might also want to maintain a server-side list of revoked tokens or implement a token expiration strategy.
            Console.WriteLine("User logged out successfully: " + user.Email);

            return Ok("User logged out successfully.");
        }

        public class UserLogout
        {
            public string Email { get; set; }
        }
        // [HttpPost]
        // public async Task<IActionResult> PostAsync(UserLogin userLogin)
        // {
        //     var result = await _signInManager.PasswordSignInAsync(
        //         userLogin.Email,
        //         userLogin.Password,
        //         userLogin.RememberMe,
        //         lockoutOnFailure: false // Prevents lockout on failure; adjust as necessary
        //     );

        //     if (result.Succeeded)
        //     {
        //         // The user has been successfully authenticated.
        //         // Here you can redirect the user to the appropriate page or return a successful response.
        //         return Ok(new { Message = "Login successful" });
        //     }
        //     else if (result.IsLockedOut)
        //     {
        //         // If the user is locked out, you might want to handle it specifically.
        //         // Note: To use lockout, you need to enable it in your Identity configuration.
        //         return StatusCode(403, new { Message = "User account locked." });
        //     }
        //     else
        //     {
        //         // Authentication failed. Return an appropriate response or view.
        //         return Unauthorized(new { Message = "Login failed. Please check your credentials." });
        //     }
        // }

    }
}