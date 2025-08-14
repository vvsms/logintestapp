using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Server.Hubs
{
    [Authorize] // only authenticated clients
    public class MenuHub : Hub
    {
    }
}
