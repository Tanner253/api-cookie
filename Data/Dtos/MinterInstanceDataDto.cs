#nullable enable
using System;

namespace Api.Data.Dtos
{
    // [Serializable] // Typically not needed for modern API DTOs with System.Text.Json
    public class MinterInstanceDataDto
    {
        public int InstanceId { get; set; }
        public MinterStateDto State { get; set; }
        public float TimeRemainingSeconds { get; set; }
        public bool IsUnlocked { get; set; }
        // public DateTime? LastCycleStartTimeUTC { get; set; } // Optional to send to client
    }
} 