using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageMenus")]
public class AdminController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleMgr;
    public AdminController(RoleManager<IdentityRole> roleMgr) { _roleMgr = roleMgr; }

    [HttpGet("roles")]
    public IActionResult GetRoles() => Ok(_roleMgr.Roles.Select(r => r.Name));

    [HttpGet("policies")]
    public IActionResult GetPolicies()
    {
        var policies = new[] { "CanManageMenus", "CanViewReports" };
        return Ok(policies);
    }
}