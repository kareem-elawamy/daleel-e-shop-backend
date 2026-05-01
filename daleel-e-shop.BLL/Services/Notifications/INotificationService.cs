using daleel_e_shop.BLL.DTOs.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Notifications
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type = "General", int? referenceId = null);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(string userId, int notificationId);
        Task<bool> MarkAllAsReadAsync(string userId);
    }
}
