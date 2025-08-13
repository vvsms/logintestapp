using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Hubs;
using Server.Models;
using Shared.DTOs;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<MenuHub> _hub;
    public MenuController(ApplicationDbContext db, IHubContext<MenuHub> hub) { _db = db; _hub = hub; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.MenuItems.OrderBy(m => m.Order).ToListAsync();
        var dtos = items.Select(i => new MenuItemDto(i.Id, i.Title, i.Url, (i.AllowedRolesCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries), (i.AllowedPoliciesCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries), i.IsVisible, i.Order)).ToList();
        return Ok(dtos);
    }

    [Authorize(Policy = "CanManageMenus")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuItemDto dto)
    {
        var entity = new MenuItem
        {
            Title = dto.Title,
            Url = dto.Url,
            AllowedRolesCsv = string.Join(',', dto.AllowedRoles),
            AllowedPoliciesCsv = string.Join(',', dto.AllowedPolicies),
            IsVisible = dto.IsVisible,
            Order = dto.Order
        };
        _db.MenuItems.Add(entity);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("MenuUpdated");
        return Ok(entity.Id);
    }

    [Authorize(Policy = "CanManageMenus")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MenuItemDto dto)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();
        item.Title = dto.Title; item.Url = dto.Url;
        item.AllowedRolesCsv = string.Join(',', dto.AllowedRoles);
        item.AllowedPoliciesCsv = string.Join(',', dto.AllowedPolicies);
        item.IsVisible = dto.IsVisible;
        item.Order = dto.Order;
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("MenuUpdated");
        return Ok();
    }

    [Authorize(Policy = "CanManageMenus")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("MenuUpdated");
        return Ok();
    }
}