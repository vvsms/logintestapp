using Microsoft.AspNetCore.SignalR;
using Server.Hubs;
using Server.Interfaces;
using Shared.DTOs;

namespace Server.Services
{
    public class MenuService(IHubContext<MenuHub> hub) : IMenuService
    {
        private readonly IHubContext<MenuHub> _hub = hub;

        // TODO: replace in-memory with EF Core implementation
        private static readonly List<MenuItemDto> _items = new();

        public Task<List<MenuItemDto>> GetMenuForUserAsync(string userId)
            => Task.FromResult(_items); // Filter by roles/policies here

        public Task<List<MenuItemDto>> GetAllAsync() => Task.FromResult(_items);

        public async Task<MenuItemDto> CreateAsync(MenuItemDto dto)
        {
            dto.Id = (_items.LastOrDefault()?.Id ?? 0) + 1;
            _items.Add(dto);
            await _hub.Clients.All.SendAsync("MenuChanged");
            return dto;
        }

        public async Task<MenuItemDto> UpdateAsync(MenuItemDto dto)
        {
            var idx = _items.FindIndex(x => x.Id == dto.Id);
            if (idx >= 0) _items[idx] = dto;
            await _hub.Clients.All.SendAsync("MenuChanged");
            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var removed = _items.RemoveAll(x => x.Id == id) > 0;
            if (removed) await _hub.Clients.All.SendAsync("MenuChanged");
            return removed;
        }
    }
}