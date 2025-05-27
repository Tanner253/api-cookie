using Microsoft.AspNetCore.Mvc;

namespace Api.Data.Dtos
{
    public class AdMobSsvCallbackDto
    {
        [FromQuery(Name = "ad_network")]
        public string? AdNetwork { get; set; }

        [FromQuery(Name = "ad_unit")]
        public string? AdUnit { get; set; }

        [FromQuery(Name = "custom_data")]
        public string? CustomData { get; set; }

        [FromQuery(Name = "key_id")]
        public string? KeyId { get; set; }

        [FromQuery(Name = "reward_amount")]
        public string? RewardAmount { get; set; }

        [FromQuery(Name = "reward_item")]
        public string? RewardItem { get; set; }

        [FromQuery(Name = "signature")]
        public string? Signature { get; set; }

        [FromQuery(Name = "timestamp")]
        public string? Timestamp { get; set; }

        [FromQuery(Name = "transaction_id")]
        public string? TransactionId { get; set; }

        [FromQuery(Name = "user_id")]
        public string? UserId { get; set; }
    }
} 