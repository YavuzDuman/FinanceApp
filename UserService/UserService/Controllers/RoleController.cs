using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Business.Abstract;
using UserService.Entities.Concrete;
using Shared.Authorization;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tüm endpoint'ler için authentication gerekli
    public class RoleController : ControllerBase
    {
        private readonly IRoleManager _roleManager;

        public RoleController(IRoleManager roleManager)
        {
            _roleManager = roleManager;
        }

        /// <summary>
        /// Tüm rolleri listeler
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<List<Role>>> GetAllRoles()
        {
            var roles = await _roleManager.GetAllRolesAsync();
            return Ok(roles);
        }

        /// <summary>
        /// ID'ye göre rol getirir
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<Role>> GetRoleById(int id)
        {
            var role = await _roleManager.GetRoleByIdAsync(id);
            if (role == null)
                return NotFound("Rol bulunamadı");

            return Ok(role);
        }

        /// <summary>
        /// Yeni rol oluşturur (Sadece Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Role>> CreateRole([FromBody] string roleName)
        {
            try
            {
                var role = await _roleManager.CreateRoleAsync(roleName);
                return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kullanıcıya rol atar (Sadece Admin)
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleRequest request)
        {
            try
            {
                await _roleManager.AddRoleToUserAsync(request.UserId, request.RoleName);
                return Ok($"'{request.RoleName}' rolü kullanıcıya başarıyla atandı");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kullanıcıdan rol kaldırır (Sadece Admin)
        /// </summary>
        [HttpDelete("remove")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleRequest request)
        {
            try
            {
                await _roleManager.RemoveRoleFromUserAsync(request.UserId, request.RoleName);
                return Ok($"'{request.RoleName}' rolü kullanıcıdan başarıyla kaldırıldı");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kullanıcının rollerini listeler
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<List<UserRole>>> GetUserRoles(int userId)
        {
            var userRoles = await _roleManager.GetUserRolesAsync(userId);
            return Ok(userRoles);
        }

        /// <summary>
        /// Kullanıcının belirli bir role sahip olup olmadığını kontrol eder
        /// </summary>
        [HttpGet("user/{userId}/has-role/{roleName}")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<ActionResult<bool>> UserHasRole(int userId, string roleName)
        {
            var hasRole = await _roleManager.UserHasRoleAsync(userId, roleName);
            return Ok(hasRole);
        }

        /// <summary>
        /// Varsayılan rolleri oluşturur (Sadece Admin)
        /// </summary>
        [HttpPost("initialize-defaults")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> InitializeDefaultRoles()
        {
            await _roleManager.InitializeDefaultRolesAsync();
            return Ok("Varsayılan roller başarıyla oluşturuldu");
        }
    }

    public class AssignRoleRequest
    {
        public int UserId { get; set; }
        public string RoleName { get; set; }
    }

    public class RemoveRoleRequest
    {
        public int UserId { get; set; }
        public string RoleName { get; set; }
    }
}
