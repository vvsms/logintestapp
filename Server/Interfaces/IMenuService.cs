using Shared.DTOs;

namespace Server.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItemDto>> GetMenuForUserAsync(string userId);
        Task<List<MenuItemDto>> GetAllAsync();
        Task<MenuItemDto> CreateAsync(MenuItemDto dto);
        Task<MenuItemDto> UpdateAsync(MenuItemDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
