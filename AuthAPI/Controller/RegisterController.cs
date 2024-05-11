using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public RegisterController(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }
        [HttpPost]
        public async Task<IActionResult> PostAsync(UserRegister newUser)
        {
            var user = _mapper.Map<User>(newUser);
            Console.WriteLine("Parola secreta este " + newUser.Password);
            var result = await _userManager.CreateAsync(user, newUser.Password);
            if (result.Succeeded)
                return Ok("User created sucessfully " + newUser.Email);
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { Errors = errors });
        }
    }
}