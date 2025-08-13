using Shared.DTOs;

namespace Server.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItemDto>> GetMenuForUserAsync(string userId);
        Task<List<MenuItemDto>> GetAllMenusAsync();
        Task<MenuItemDto> CreateMenuAsync(MenuItemDto dto);
        Task<MenuItemDto> UpdateMenuAsync(MenuItemDto dto);
        Task<bool> DeleteMenuAsync(int id);
    }
}
