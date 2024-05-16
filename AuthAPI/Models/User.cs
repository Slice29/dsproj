using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Models
{
    //* Custom class inherited for Identity user to add some extra attributes
    public class User : IdentityUser
    {
    public string favoriteVideoGame {get; set;}
    public bool isWeeb {get; set;}
    public bool IsAzureADUser { get; set; }
    }
}