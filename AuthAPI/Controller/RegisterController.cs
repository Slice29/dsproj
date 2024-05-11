using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthAPI.Email;
using AuthAPI.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;

        public RegisterController(UserManager<User> userManager, IMapper mapper, IEmailSender emailSender)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailSender = emailSender;
        }
        [HttpPost]
        public async Task<IActionResult> PostAsync(UserRegister newUser)
        {
            var user = _mapper.Map<User>(newUser);
            Console.WriteLine("Parola secreta este " + newUser.Password); // Consider removing sensitive data logging
            var result = await _userManager.CreateAsync(user, newUser.Password);

            if (result.Succeeded)
            {
                // Generate the email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Create confirmation link
                var confirmationLink = Url.Action(nameof(VerifyEmail), "Register", new { userId = user.Id, token = token }, Request.Scheme);

                // Send confirmation email
                await _emailSender.SendEmailAsync(newUser.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");

                return Ok(new { Message = "User created successfully. Please check your email to confirm your account.", newUser.Email });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { Errors = errors });
        }

        [HttpGet("verifyemail")]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("Invalid email confirmation request.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok("Email confirmed successfully.");
            }

            return BadRequest("Error confirming your email.");
        }

    }
}