using System;

namespace PROTOTYPE_backend.Models
{
    public class SoftDeleteLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string DeletedByUserId { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
        public DateTime RetentionExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    }
}