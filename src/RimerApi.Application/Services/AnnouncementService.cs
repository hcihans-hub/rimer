using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimerApi.Application.DTOs.Announcement;
using RimerApi.Application.Interfaces;
using RimerApi.Application.Common;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Application.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IRepository<Announcement> _repository;

        public AnnouncementService(IRepository<Announcement> repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult<List<AnnouncementDto>>> GetAllAdminAsync()
        {
            var announcements = await _repository.GetAllAsync();
            var dtos = announcements.Select(MapToDto).OrderByDescending(a => a.CreatedAt).ToList();
            return ServiceResult<List<AnnouncementDto>>.Success(dtos);
        }

        public async Task<ServiceResult<List<AnnouncementDto>>> GetActiveAnnouncementsForUserAsync(CurrentUser currentUser)
        {
            var now = DateTime.UtcNow;
            
            var allActive = await _repository.FindAsync(a => 
                a.IsActive && 
                a.PublishedAt <= now && 
                a.ExpiresAt > now);

            var filtered = allActive.Where(a => IsUserInTargetAudience(currentUser, a)).ToList();
            var dtos = filtered.Select(MapToDto).OrderByDescending(a => a.PublishedAt).ToList();

            return ServiceResult<List<AnnouncementDto>>.Success(dtos);
        }

        public async Task<ServiceResult<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto, CurrentUser currentUser)
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Content = dto.Content,
                PublishedAt = dto.PublishedAt,
                ExpiresAt = dto.ExpiresAt,
                IsActive = true,
                TargetAudience = dto.TargetAudience,
                TargetDepartmentId = dto.TargetAudience == "Department" ? dto.TargetDepartmentId : null,
                CreatedByUserId = currentUser.Id
            };

            await _repository.AddAsync(announcement);
            await _repository.SaveChangesAsync();
            return ServiceResult<AnnouncementDto>.Success(MapToDto(announcement));
        }

        public async Task<ServiceResult<AnnouncementDto>> UpdateAsync(int id, UpdateAnnouncementDto dto)
        {
            var results = await _repository.FindAsync(a => a.Id == id);
            var announcement = results.FirstOrDefault();
            
            if (announcement == null)
                return ServiceResult<AnnouncementDto>.NotFound("Duyuru bulunamadı.");

            announcement.Title = dto.Title;
            announcement.Content = dto.Content;
            announcement.PublishedAt = dto.PublishedAt;
            announcement.ExpiresAt = dto.ExpiresAt;
            announcement.IsActive = dto.IsActive;
            announcement.TargetAudience = dto.TargetAudience;
            announcement.TargetDepartmentId = dto.TargetAudience == "Department" ? dto.TargetDepartmentId : null;

            _repository.Update(announcement);
            await _repository.SaveChangesAsync();
            return ServiceResult<AnnouncementDto>.Success(MapToDto(announcement));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var results = await _repository.FindAsync(a => a.Id == id);
            var announcement = results.FirstOrDefault();
            
            if (announcement == null)
                return ServiceResult<bool>.NotFound("Duyuru bulunamadı.");

            _repository.Delete(announcement);
            await _repository.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ToggleActiveStatusAsync(int id)
        {
            var results = await _repository.FindAsync(a => a.Id == id);
            var announcement = results.FirstOrDefault();
            
            if (announcement == null)
                return ServiceResult<bool>.NotFound("Duyuru bulunamadı.");

            announcement.IsActive = !announcement.IsActive;
            _repository.Update(announcement);
            await _repository.SaveChangesAsync();
            return ServiceResult<bool>.Success(announcement.IsActive);
        }

        private AnnouncementDto MapToDto(Announcement a)
        {
            return new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                PublishedAt = a.PublishedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive,
                TargetAudience = a.TargetAudience,
                TargetDepartmentId = a.TargetDepartmentId,
                CreatedByUserId = a.CreatedByUserId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            };
        }

        private bool IsUserInTargetAudience(CurrentUser user, Announcement a)
        {
            if (a.TargetAudience == "All") return true;

            if (a.TargetAudience == "Student" && user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)) return true;
            if (a.TargetAudience == "Academician" && user.Role.Equals("Academician", StringComparison.OrdinalIgnoreCase)) return true;
            if (a.TargetAudience == "Staff" && user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)) return true;
            if (a.TargetAudience == "Department" && a.TargetDepartmentId.HasValue && a.TargetDepartmentId.Value == user.DepartmentId) return true;

            return false;
        }
    }
}
