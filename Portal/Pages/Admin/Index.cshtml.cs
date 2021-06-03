using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal.Pages.Admin
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public int? AssumeRole { get; set; }
        
        public void OnGet()
        {
            
        }

        public async Task OnPostAsync()
        {
            // TODO - this should be moved to mediatr handler with claims generation in a shared service
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, User.GetClaimValue(ClaimTypes.Name)),
                new (ClaimTypes.NameIdentifier, User.GetClaimValue(ClaimTypes.NameIdentifier)),
                new (ClaimsPrincipalUtils.Claims.Customer, AssumeRole.ToString()),
                new (ClaimsPrincipalUtils.Claims.Customer, User.GetCustomerId().ToString()),
                new (ClaimTypes.Role, ClaimsPrincipalUtils.Roles.Customer),
                new (ClaimTypes.Role, ClaimsPrincipalUtils.Roles.Admin),
                new (ClaimTypes.Role, "Assuming"),
            };

            var apiCreds = User.GetApiCredentials();
            if (!string.IsNullOrEmpty(apiCreds))
            {
                claims.Add(new Claim(ClaimsPrincipalUtils.Claims.ApiCredentials, apiCreds));
            }

            var newClaimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(newClaimsIdentity), 
                new AuthenticationProperties());
        }
    }
}