#nullable enable
namespace Api.Data.Dtos
{
    public class StartMinterResponseDto
    {
        public required MinterInstanceDataDto UpdatedMinterInstance { get; set; }
        public required string NewGoldBarBalance { get; set; } // Send back as string, client expects this
        public DateTime ServerTimeUtc { get; set; } // Useful for client to sync its timer start if needed
    }
} 
