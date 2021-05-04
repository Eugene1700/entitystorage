using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityStorage
{
    public abstract class ModifiableEntity : StandardEntity
    {
        [Column("I_MODIFICATION_TIME")]
        public DateTime ModificationTime { get; set; }

        [Column("I_VERSION")]
        public int Version { get; set; }
    }
}