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

        [HttpGet("{email}")]
        public async Task<IActionResult> GetUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("{email}/roles")]
       // [Authorize(Policy = "AdminPolicy")]
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

      [HttpPost("{email}/roles/update")]
public async Task<IActionResult> UpdateUserRoles(string email, [FromBody] UserRolesUpdateDto rolesUpdate)
{
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
    {
        return NotFound(email);
    }

    var result = IdentityResult.Success;

    if (rolesUpdate.RolesToRemove != null && rolesUpdate.RolesToRemove.Any())
    {
        var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesUpdate.RolesToRemove);
        if (!removeResult.Succeeded)
            result = removeResult;
    }

    if (rolesUpdate.RolesToAdd != null && rolesUpdate.RolesToAdd.Any())
    {
        var addResult = await _userManager.AddToRolesAsync(user, rolesUpdate.RolesToAdd);
        if (!addResult.Succeeded)
            result = addResult;
    }

    if (!result.Succeeded)
    {
        return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
    }

    return Ok();
}

    }
}