using System;

namespace RimerApi.Application.DTOs.Announcement
{
    public class AnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string TargetAudience { get; set; } = null!;
        public Guid? TargetDepartmentId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateAnnouncementDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string TargetAudience { get; set; } = "All"; // All, Student, Academician, Staff, Department
        public Guid? TargetDepartmentId { get; set; }
    }

    public class UpdateAnnouncementDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string TargetAudience { get; set; } = null!;
        public Guid? TargetDepartmentId { get; set; }
    }
}
