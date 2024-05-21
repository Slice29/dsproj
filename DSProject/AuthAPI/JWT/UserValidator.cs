using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.JWT
{
    public class UserValidator : UserValidator<User>
    {
        public override async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
        {
            return IdentityResult.Success;
        }
    }
}