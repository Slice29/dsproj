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

            // Check the password first without signing in
            var result = await _signInManager.PasswordSignInAsync(user, userLogin.Password, true, true);

            // Check if two-factor is enabled and required
            if (result.RequiresTwoFactor)
            {
                Console.WriteLine("IS AICI");
                //_userManager.Options.Tokens.AuthenticatorTokenProvider = "Email";
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                
                await _emailSender.SendEmailAsync(user.Email, "Your security code", $"Your security code is: {token}");

                return Ok(new { Message = "Please verify the two-factor authentication code sent to your email." });
            }

            // If 2FA is not enabled, proceed with JWT generation
            var JwtHelper = new JwtHelper(_tokenService, _userManager);
            var jwtToken = await JwtHelper.GenerateTokenAsync(user);
            return Ok(new { Token = jwtToken });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactorCode(TwoFactorVerification verification)
        {
            var user = await _userManager.FindByEmailAsync(verification.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            Console.WriteLine("CODE: " + verification.Code);
            var result = await _signInManager.TwoFactorSignInAsync("Email", verification.Code, isPersistent: false, true);
            Console.WriteLine(user.TwoFactorEnabled);
            if (!result.Succeeded)
            {
                Console.WriteLine("Failed to verify 2FA code. Reason: {0}", result.RequiresTwoFactor); // Do not log sensitive data
                return BadRequest("Invalid 2FA code.");
            }

            var JwtHelper = new JwtHelper(_tokenService, _userManager);
            var jwtToken = await JwtHelper.GenerateTokenAsync(user);
            return Ok(new { Token = jwtToken });
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