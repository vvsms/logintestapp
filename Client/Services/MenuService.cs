using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.DTOs;
using System.Net.Http.Json;

namespace Client.Services
{
    public class MenuService
    {
        private readonly IHttpClientFactory _httpFactory;
        private HubConnection? _hub;

        public event Action? MenuChanged;

        public MenuService(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<List<MenuItemDto>> GetMenuAsync()
        {
            var http = _httpFactory.CreateClient("authorized-api");
            var items = await http.GetFromJsonAsync<List<MenuItemDto>>("api/menu");
            return items ?? [];
        }

        public async Task ConnectHubAsync(string hubBase)
        {
            // hubBase e.g. the same origin
            _hub = new HubConnectionBuilder()
                .WithUrl($"{hubBase}menuhub")
                .WithAutomaticReconnect()
                .Build();

            _hub.On("MenuChanged", () => MenuChanged?.Invoke());
            await _hub.StartAsync();
        }
    }
}