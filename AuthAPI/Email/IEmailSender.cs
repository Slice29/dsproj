using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Email
{
   public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body);
}
}