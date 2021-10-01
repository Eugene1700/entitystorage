using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityStorage.Core
{
    public abstract class StandardEntity : IEntity
    {
        [Column("ID")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("I_CREATION_TIME")]
        public DateTime CreationTime { get; set; }
        
        [Column("I_SORT_DATE")]
        public DateTime SortDate { get; set; }
    }
}