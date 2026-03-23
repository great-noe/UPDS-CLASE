using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pw2_clase5.Data;
using pw2_clase5.Models;
using pw2_clase5.Request;
using System.Security.Claims;

namespace pw2_clase5.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TestController(ApplicationDbContext context) : ControllerBase
    {

        //Endpoint Público (sin JWT requerido)
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult Public()
        {
            return Ok(new { message = "Acceso público permitido", timestamp = DateTime.UtcNow });
        }

        //Endpoint Protegido (JWT válido)
        [HttpGet("protected")]
        [Authorize]
        public IActionResult Protected()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst("preferred_username")?.Value ?? "Unknown";
            var roles = User.FindAll(ClaimTypes.Role);

#pragma warning disable IDE0037 // Use inferred member name
            return Ok(new
            {
                message = "Acceso autorizado",
                userId = userId,
                username = username,
                roles = roles.Select(r => r.Value),
                timestamp = DateTime.UtcNow
            });
#pragma warning restore IDE0037 // Use inferred member name
        }

        //Endpoint sin Token JWT (401 Unauthorized)
        [HttpGet("unauthorized")]
        [Authorize]
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public IActionResult Unauthorized()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            return Ok(new { message = "No llegará aquí sin JWT" });
        }

        //Endpoint - Solo Admins (403 Forbidden para el resto) 
        [HttpGet("admin")]
        [Authorize(Policy ="AdminOnly")]
        public IActionResult AdminOnly()
        {
            var username = User.FindFirst("preferred_username")?.Value;
            return Ok(new { message = $"Solo Admin - {username}" });
        }

        [HttpGet("admin-or-user")]
        [Authorize(Policy = "AdminOrUser")]
        public IActionResult AdminOrUser()
        {
            var username = User.FindFirst("preferred_username")?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);
            return Ok(new { message = $"Admin o User - {username}", roles });
        }

        [HttpGet("user")]
        [Authorize(Policy = "UserOnly")]
        public IActionResult UserOnly()
        {
            var username = User.FindFirst("preferred_username")?.Value;
            return Ok(new { message = $"Solo User - {username}" });
        }

        //Endpoint para guardar Usuario en BBDD
        [HttpPost("users")]
        [Authorize]
        public async Task<IActionResult> SaveUser([FromBody] CreateUserRequest request)
        {
            var user = new User
            {
                Username = request.Username,
                Email = request.Email
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        //Obtener Usuario por ID
        [HttpGet("users/{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }
}
