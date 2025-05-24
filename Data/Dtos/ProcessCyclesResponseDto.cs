#nullable enable
namespace Api.Data.Dtos
{
    public class ProcessCyclesResponseDto
    {
        public required MemeMintPlayerDataDto UpdatedMemeMintData { get; set; }
        // You could also include specific feedback, like how many GCM points were awarded in this batch
        // public decimal GcmPointsAwardedThisBatch { get; set; }
    }
} 
