using System;

namespace RimerApi.Domain.Entities
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        
        /// <summary>
        /// All, Student, Academician, Staff, Department
        /// </summary>
        public string TargetAudience { get; set; } = "All";
        
        public Guid? TargetDepartmentId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Department? TargetDepartment { get; set; }
    }
}
