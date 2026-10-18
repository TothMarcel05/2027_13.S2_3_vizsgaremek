using System;

namespace PROTOTYPE_backend.Models
{
    public class StorageNodeLabel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NodeId { get; set; }
        public string Label { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StorageNode? Node { get; set; }
    }
}