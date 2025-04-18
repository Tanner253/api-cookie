using System;

namespace Api.Data.Dtos
{
    public class PlayerDto
    {
        public long PlayerId { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string? ChatDeviceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        // Add other essential non-sensitive fields if needed upon identification
    }
} 