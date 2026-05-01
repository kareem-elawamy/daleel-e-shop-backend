using daleel_e_shop.BLL.DTOs.Notifications;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type = "General", int? referenceId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await _unitOfWork.Notifications.FindAsync(
                n => n.UserId == userId);

            return notifications.OrderByDescending(n => n.CreatedAt).Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var unread = await _unitOfWork.Notifications.FindAsync(
                n => n.UserId == userId && !n.IsRead);
            return unread.Count();
        }

        public async Task<bool> MarkAsReadAsync(string userId, int notificationId)
        {
            var notification = await _unitOfWork.Notifications.FindSingleAsync(
                n => n.Id == notificationId && n.UserId == userId);
            if (notification == null) return false;

            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var notifications = await _unitOfWork.Notifications.FindAsync(
                n => n.UserId == userId && !n.IsRead);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}
