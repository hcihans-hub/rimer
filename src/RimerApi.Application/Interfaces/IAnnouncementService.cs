using System.Threading.Tasks;
using RimerApi.Application.DTOs.Announcement;
using RimerApi.Application.Common;

namespace RimerApi.Application.Interfaces
{
    public interface IAnnouncementService
    {
        Task<ServiceResult<List<AnnouncementDto>>> GetAllAdminAsync();
        Task<ServiceResult<List<AnnouncementDto>>> GetActiveAnnouncementsForUserAsync(CurrentUser currentUser);
        Task<ServiceResult<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto, CurrentUser currentUser);
        Task<ServiceResult<AnnouncementDto>> UpdateAsync(int id, UpdateAnnouncementDto dto);
        Task<ServiceResult<bool>> DeleteAsync(int id);
        Task<ServiceResult<bool>> ToggleActiveStatusAsync(int id);
    }
}
