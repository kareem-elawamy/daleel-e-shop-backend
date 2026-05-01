using daleel_e_shop.BLL.DTOs.Dashboard;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
