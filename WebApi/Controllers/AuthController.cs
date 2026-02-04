using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public ActionResult<TokenResponse> CreateToken(TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest("UserName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Password is required.");
        }

        var users = _configuration.GetSection("Users").Get<List<AppUser>>() ?? new List<AppUser>();
        var user = users.FirstOrDefault(u =>
            string.Equals(u.UserName, request.UserName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(u.Password, request.Password, StringComparison.Ordinal));

        if (user is null)
        {
            return Unauthorized("Invalid username or password.");
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");
        var key = jwtSection.GetValue<string>("Key");
        if (string.IsNullOrWhiteSpace(key))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "JWT signing key is missing.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserName),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        return Ok(new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires
        });
    }
}
