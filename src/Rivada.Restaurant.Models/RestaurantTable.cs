using System;

namespace Rivada.Restaurant.Models
{
    public class RestaurantTable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Area { get; set; }
        public TableStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
