using System;

namespace PROTOTYPE_backend.Models
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; set; }
    }
}