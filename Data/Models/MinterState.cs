#nullable enable

namespace Api.Data.Models
{
    public enum MinterState
    {
        Idle,
        MintingInProgress,
        CycleCompleted // New state for manual collection
    }
} 