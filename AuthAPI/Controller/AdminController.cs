using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace UserApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("{id}/roles")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetUserRoles(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return NotFound(email);
            }
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        [HttpPut("{id}/roles")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> PutUserRole(string email, UserAddRemoveRole param)
        {
            var user = await _userManager.FindByIdAsync(email);
            if (user is null)
            {
                return NotFound(email);
            }
            try {
                await _userManager.AddToRoleAsync(user, param.RoleName);
            } catch(Exception e) {
                return BadRequest(e.Message);
            }
            return Ok();
        }

        [HttpDelete("{id}/roles")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteUserRole(string email, UserAddRemoveRole param)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return NotFound(email);
            }
            try {
                await _userManager.RemoveFromRoleAsync(user, param.RoleName);
            } catch(Exception e) {
                return BadRequest(e.Message);
            }
            return Ok();
        }
    }
}