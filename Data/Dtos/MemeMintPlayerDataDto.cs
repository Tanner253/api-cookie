#nullable enable
using System;
using System.Collections.Generic;

namespace Api.Data.Dtos
{
    // [Serializable]
    public class MemeMintPlayerDataDto
    {
        public decimal PlayerGCMPMPoints { get; set; }
        public int SharedMintProgress { get; set; }
        public List<MinterInstanceDataDto> MinterInstances { get; set; } = new List<MinterInstanceDataDto>();
    }
} 