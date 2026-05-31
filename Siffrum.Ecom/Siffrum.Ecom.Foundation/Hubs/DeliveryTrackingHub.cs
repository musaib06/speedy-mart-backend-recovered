using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Siffrum.Ecom.Foundation.Security;

namespace Siffrum.Ecom.Foundation.Hubs
{
    [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema)]
    public class DeliveryTrackingHub : Hub
    {
        /// <summary>
        /// User or DeliveryBoy joins a tracking group for a specific order.
        /// Group name: "order_{orderId}"
        /// </summary>
        public async Task JoinOrderTracking(long orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
            await Clients.Caller.SendAsync("Joined", orderId);
        }

        /// <summary>
        /// User or DeliveryBoy leaves a tracking group.
        /// </summary>
        public async Task LeaveOrderTracking(long orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        /// <summary>
        /// DeliveryBoy pushes live location directly through the hub (ultra-low-latency path).
        /// This broadcasts to all group members without hitting the DB.
        /// </summary>
        public async Task SendLocation(long orderId, double lat, double lng)
        {
            await Clients.OthersInGroup($"order_{orderId}").SendAsync("ReceiveLocation", new
            {
                orderId,
                latitude = lat,
                longitude = lng,
                timestamp = DateTime.UtcNow
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
