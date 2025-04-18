using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class UpgradeType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<Upgrade> Upgrades { get; set; } = new List<Upgrade>();
    }
} 