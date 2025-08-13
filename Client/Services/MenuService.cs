using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.DTOs;
using System.Net.Http.Json;

namespace Client.Services
{
    public class MenuService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly NavigationManager _nav;
        private HubConnection? _hub;

        public List<MenuItemDto> Items { get; private set; } = new();

        public event Action? OnChange;

        public MenuService(IHttpClientFactory httpFactory, NavigationManager nav)
        {
            _httpFactory = httpFactory;
            _nav = nav;

            _hub = new HubConnectionBuilder()
                .WithUrl(_nav.ToAbsoluteUri("/hubs/menu"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On("MenuUpdated", async () =>
            {
                await LoadAsync();
                OnChange?.Invoke();
            });

            _ = _hub.StartAsync();
        }

        public async Task LoadAsync()
        {
            var client = _httpFactory.CreateClient("ApiClient");
            try
            {
                Items = await client.GetFromJsonAsync<List<MenuItemDto>>("api/menu") ?? new();
            }
            catch
            {
                Items = new List<MenuItemDto>();
            }
            OnChange?.Invoke();
        }
    }
}