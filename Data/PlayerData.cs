
using System.ComponentModel.DataAnnotations;

namespace Api.Data
{
    public class PlayerData
    {
        //define table columns - let EF core handle standup
        [Key]
        public int Id { get; set; }

        public decimal GoldNuggets { get; set; }

        public long LastSavedTimestampTicks { get; set; }

    }
}
