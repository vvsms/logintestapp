using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Hubs;
using Server.Interfaces;
using Server.Models;
using Shared.DTOs;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController(IMenuService service) : ControllerBase
{
    private readonly IMenuService _service = service;

    [Authorize]
    [HttpGet]
    public Task<List<MenuItemDto>> Get() => _service.GetMenuForUserAsync(User.Identity?.Name ?? string.Empty);

    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public Task<List<MenuItemDto>> GetAll() => _service.GetAllAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public Task<MenuItemDto> Create(MenuItemDto dto) => _service.CreateAsync(dto);

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public Task<MenuItemDto> Update(MenuItemDto dto) => _service.UpdateAsync(dto);

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public Task<bool> Delete(int id) => _service.DeleteAsync(id);
}