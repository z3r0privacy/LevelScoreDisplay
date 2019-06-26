using LevelScoreBackend.AAA;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LevelScoreBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private ILoginProvider _loginProvider;

        public UserController(ILoginProvider loginProvider)
        {
            _loginProvider = loginProvider;
        }

        [AllowAnonymous]
        [HttpPost ("login")]
        public async Task<ActionResult> Login ([FromBody]string password)
        {
            if (ModelState.IsValid)
            {
                if (_loginProvider.LogIn(password, out var claims))
                {
                    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                    identity.AddClaims(claims);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                    return Ok();
                }
                else
                {
                    return Unauthorized();
                }
            }
            return BadRequest(ModelState);
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged Out");
        }
    }
}
