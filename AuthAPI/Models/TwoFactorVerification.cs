using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Models
{
   public class TwoFactorVerification
{
    public string Email { get; set; }
    public string Code { get; set; }
    public bool RememberClient { get; set; }
}
}