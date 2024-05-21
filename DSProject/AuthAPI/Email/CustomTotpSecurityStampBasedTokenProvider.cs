// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Identity;

// namespace AuthAPI.Email
// {
//     public class CustomTotpSecurityStampBasedTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
// {
//      public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
//     {
//         // Implement your logic to determine if a two-factor token can be generated
//         // For example, you might only allow 2FA tokens to be generated if the user has a verified email
//         return Task.FromResult(true);
//     }
//     public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
//     {
//         // Extend the time window or adjust it based on your test requirements
//         var validWindowStart = DateTime.UtcNow.AddHours(-100); // 10 minutes before the current time
//         var validWindowEnd = DateTime.UtcNow.AddHours(100); // 10 minutes after the current time

//         var isValid = await base.ValidateAsync(purpose, token, manager, user);

//         if (!isValid)
//         {
//             // Custom logic to check within the extended window
//             // This is pseudocode; actual implementation may differ based on how tokens are generated and stored
//         }

//         return isValid;
//     }
// }

// }