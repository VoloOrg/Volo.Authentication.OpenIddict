using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("UserManager")]
    public class UserManagerController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagerController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [HttpPost]
        [Route("AddRole")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult> AddRole([FromBody] RoleData role, CancellationToken cancellationToken)
        {
            if (!await _roleManager.RoleExistsAsync(role.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role.Role));
                return Ok();
            }

            return BadRequest(400);
        }

        [HttpGet]
        [Route("GetRoles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<IdentityRole>>> GetRoles(CancellationToken cancellationToken)
        {
            return await _roleManager.Roles.ToListAsync(cancellationToken);
        }

        [HttpPost]
        [Route("AddRoleToUser")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AddRoleToUser([FromBody] UserRoleData data, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(data.Email);
            var roleExists = await _roleManager.RoleExistsAsync(data.Role);
            if (user != null && roleExists)
            {
                var result = await _userManager.AddToRoleAsync(user, data.Role);
                return result.Succeeded ? Ok(result) : BadRequest(result.Errors);
            }

            return BadRequest("user or role missing");
        }
    }


    public class RoleData
    {
        public string Role { get; set; }
    }

    public class UserRoleData
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
