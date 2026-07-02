using Identity.Application.Features.Identities.Commands;
using Identity.Application.Features.Identities.DTOs;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly JwtService _jwtService;
    public AuthController(IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _config = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = new JwtService(_config, userManager);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = new ApplicationUser();
        user = await _userManager.FindByNameAsync(model.Username);
        user = await _userManager.FindByEmailAsync(model.Username);
        if(user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshToken(user, refreshToken);
            return Ok(new
            {
                accessToken,
                refreshToken
            });
        }
        return Unauthorized("Invalid credentials");
    }
    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
        };
        if(string.IsNullOrEmpty(model.RoleName))
        {
            return BadRequest("RoleName is required");
        }
        var roleManager = new ApplicationRole();
        if ("superadmin".Equals(model.RoleName.Trim(), StringComparison.CurrentCultureIgnoreCase))
        {
            roleManager = await _roleManager.FindByNameAsync("SuperAdmin");
        }
        else if("admin".Equals(model.RoleName.Trim(), StringComparison.CurrentCultureIgnoreCase))
        {
            roleManager = await _roleManager.FindByNameAsync("Admin");
        }
        else if ("customer".Equals(model.RoleName.Trim(), StringComparison.CurrentCultureIgnoreCase))
        {
            roleManager = await _roleManager.FindByNameAsync("Customer");
        }
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleManager.Name);

        if (!roleResult.Succeeded)
        {
            return BadRequest(roleResult.Errors);
        }
        return Ok("User registered successfully");
    }
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDto request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        var storedToken = user?.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken && t.IsActive);
        if (storedToken == null)
            return Unauthorized("Invalid refresh token");

        // Invalidate old
        storedToken.Revoked = DateTime.UtcNow;

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        await _jwtService.SaveRefreshToken(user, newRefreshToken);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            ),
            ValidateLifetime = false // ignore expiration
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        return principal;
    }
}
