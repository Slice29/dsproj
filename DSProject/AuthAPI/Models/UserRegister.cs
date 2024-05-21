using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Models
{
    public class UserRegister
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber {get; set;}
        public string favoriteVideoGame {get; set;}
    }
}