using EntityStorage.Core;
using LinqToDB.Mapping;

namespace EntityStorage.Tests
{
    [Table("TEST_MODEL")]
    public class TestModel : ModifiableEntity
    {
        [Column("COUNT")]
        public int Count { get; set; }
    }
}