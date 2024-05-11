using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public LoginController(UserManager<User> userManager, IMapper mapper, SignInManager<User> signInManager, TokenService tokenService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _signInManager = signInManager;
             _tokenService = tokenService;
        }

         [HttpPost]
        public async Task<IActionResult> PostAsync(UserLogin userLogin)
        {
            var result = await _signInManager.PasswordSignInAsync(
                userLogin.Email,
                userLogin.Password,
                userLogin.RememberMe,
                false
            );
            
            if(result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(userLogin.Email);
                var JwtHelper = new JwtHelper(_tokenService, _userManager);
                var token = await JwtHelper.GenerateTokenAsync(user);

                //? Deprecated
                //var products = await _publisher.GetAllProducts(token);
                
                return Ok(token);
            }
            else return NotFound();
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