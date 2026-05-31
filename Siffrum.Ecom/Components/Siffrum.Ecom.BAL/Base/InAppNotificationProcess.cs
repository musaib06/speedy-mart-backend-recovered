using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Base
{
    public class InAppNotificationProcess : SiffrumBalBase
    {
        public InAppNotificationProcess(IMapper mapper, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext)
        {
        }

        public async Task<List<InAppNotificationSM>> GetNotifications(int recipientType, long? recipientId, int skip = 0, int take = 20)
        {
            var query = _apiDbContext.InAppNotifications
                .Where(n => n.RecipientType == recipientType);

            if (recipientId.HasValue && recipientId.Value > 0)
                query = query.Where(n => n.RecipientId == recipientId.Value || n.RecipientId == null);
            else
                query = query.Where(n => n.RecipientId == null);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return items.Select(n => new InAppNotificationSM
            {
                Id = n.Id,
                RecipientType = n.RecipientType,
                RecipientId = n.RecipientId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public async Task<int> GetUnreadCount(int recipientType, long? recipientId)
        {
            var query = _apiDbContext.InAppNotifications
                .Where(n => n.RecipientType == recipientType && !n.IsRead);

            if (recipientId.HasValue && recipientId.Value > 0)
                query = query.Where(n => n.RecipientId == recipientId.Value || n.RecipientId == null);
            else
                query = query.Where(n => n.RecipientId == null);

            return await query.CountAsync();
        }

        public async Task MarkAsRead(long notificationId, int recipientType, long? recipientId)
        {
            var notification = await _apiDbContext.InAppNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId &&
                                          n.RecipientType == recipientType);

            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _apiDbContext.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(int recipientType, long? recipientId)
        {
            var query = _apiDbContext.InAppNotifications
                .Where(n => n.RecipientType == recipientType && !n.IsRead);

            if (recipientId.HasValue && recipientId.Value > 0)
                query = query.Where(n => n.RecipientId == recipientId.Value || n.RecipientId == null);
            else
                query = query.Where(n => n.RecipientId == null);

            var unread = await query.ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }
            await _apiDbContext.SaveChangesAsync();
        }

        public async Task CreateNotification(int recipientType, long? recipientId, string title, string message, string type, string? referenceId = null)
        {
            var notification = new InAppNotificationDM
            {
                RecipientType = recipientType,
                RecipientId = recipientId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                IsRead = false
            };

            _apiDbContext.InAppNotifications.Add(notification);
            await _apiDbContext.SaveChangesAsync();
        }

        public async Task NotifyAllAdmins(string title, string message, string type, string? referenceId = null)
        {
            var notification = new InAppNotificationDM
            {
                RecipientType = 1,
                RecipientId = null,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                IsRead = false
            };

            _apiDbContext.InAppNotifications.Add(notification);
            await _apiDbContext.SaveChangesAsync();
        }

        public async Task NotifySeller(long sellerId, string title, string message, string type, string? referenceId = null)
        {
            await CreateNotification(3, sellerId, title, message, type, referenceId);
        }

        public async Task NotifyDeliveryBoy(long deliveryBoyId, string title, string message, string type, string? referenceId = null)
        {
            await CreateNotification(5, deliveryBoyId, title, message, type, referenceId);
        }

        public async Task NotifyUser(long userId, string title, string message, string type, string? referenceId = null)
        {
            await CreateNotification(4, userId, title, message, type, referenceId);
        }

        public async Task NotifyAllSellers(string title, string message, string type, string? referenceId = null)
        {
            var notification = new InAppNotificationDM
            {
                RecipientType = 3,
                RecipientId = null,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                IsRead = false
            };

            _apiDbContext.InAppNotifications.Add(notification);
            await _apiDbContext.SaveChangesAsync();
        }

        public async Task NotifySellerDeliveryBoys(long sellerId, string title, string message, string type, string? referenceId = null)
        {
            var boyIds = await _apiDbContext.DeliveryBoy
                .Where(d => d.SellerId == sellerId &&
                            d.Status == DomainModels.Enums.DeliveryBoyStatusDM.Active)
                .Select(d => d.Id)
                .ToListAsync();

            foreach (var boyId in boyIds)
            {
                await CreateNotification(5, boyId, title, message, type, referenceId);
            }
        }
    }
}
