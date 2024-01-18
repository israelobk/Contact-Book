using ContactWebAPI.Domain.Models;
using ContactWebAPI.Helper;
using ContactWebAPI.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyContact.SqlServer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace ContactWebAPI.Controllers
{
    [ApiController]
    [Route("authentication")]
    public class AuthenticationController : ControllerBase
    {
        //private readonly UserManager<ApplicationUser> _userManager;
        //private readonly RoleManager<ApplicationIdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        private readonly IIdentityServerUserStore _userStore;

        public AuthenticationController(IIdentityServerUserStore userstore, IConfiguration configuration)
        {
            //_userManager = userManager;
            //_roleManager = roleManager;
            _configuration = configuration;
            _userStore = userstore;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            var user = await _userStore.FindUser(loginModel.Email);

            if (user != null && await _userStore.CheckPassword(user, loginModel.Password))
            {
                var userRoles = await _userStore.GetRoles(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                //JWT
                string token = JWTImplementation.GetJWTToken(_configuration, claims);

                return Ok(token);
            }

            return Unauthorized();
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegistrationModel model)
        {
            if (model.ConfirmPassword != model.Password)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    new AuthenticationResponse { Status = "Error", Message = "Password does not match" });
            }
            var userExists = await _userStore.FindUser(model.Email);

            if (userExists != null)
                return StatusCode(StatusCodes.Status409Conflict,
                    new AuthenticationResponse { Status = "Error", Message = "User already exists!" });

            IdentityUser user = new IdentityUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email
            };
            
            var result = await _userStore.RegisterUser(model.Password, user);
            if (!result.Succeeded)
            {
                string errors = string.Empty;

                foreach (var error in result.Errors)
                {
                    errors += error.Description;
                    errors += "@";

                }

                errors = errors.Replace("@", System.Environment.NewLine);

                return StatusCode(StatusCodes.Status400BadRequest,
                    new AuthenticationResponse { Status = "Error", Message = errors });
            }
                
            return Ok(new AuthenticationResponse { Status = "Success", Message = "User created successfully!" });
        }

        [HttpPost("register-admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegistrationModel model)
        {
            if(model.ConfirmPassword != model.Password)
            {
                return StatusCode(StatusCodes.Status400BadRequest, 
                    new AuthenticationResponse { Status = "Error", Message = "Password does not match" });
            }
            var userExists = await _userStore.FindUser(model.Email);

            if (userExists != null)
                return StatusCode(StatusCodes.Status409Conflict, 
                    new AuthenticationResponse { Status = "Error", Message = "User already exists!" });

            IdentityUser user = new IdentityUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email
            };

            var result = await _userStore.RegisterUser(model.Password, user);
            if (!result.Succeeded)
            {
                string errors = string.Empty;

                foreach(var error in result.Errors)
                {
                    errors += error.Description;
                    errors += "@";
                    
                }

                errors = errors.Replace("@", System.Environment.NewLine);

                return StatusCode(StatusCodes.Status400BadRequest,
                    new AuthenticationResponse { Status = "Error", Message = errors });
            }

            if (!await _userStore.CheckRole("Admin"))
                await _userStore.CreateRole(new IdentityRole(UserRoles.Admin));
            if (!await _userStore.CheckRole("User"))
                await _userStore.CreateRole(new IdentityRole(UserRoles.User));

            if (await _userStore.CheckRole(UserRoles.Admin))
            {
                await _userStore.AddToRole(user, UserRoles.Admin);
            }

            return Ok(new AuthenticationResponse { Status = "Success", Message = "User created successfully!" });
        }
    }
}
