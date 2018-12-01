using System.Collections.Generic;
using System.Linq;

namespace AnnoRDA.FileDB.Structs
{
    public class Tags
    {
        private readonly Dictionary<ushort, Tag> tags = new Dictionary<ushort, Tag>();

        public Tags()
        {
            this.AddEntries(BuiltinTags.AllEntries);
        }

        public void AddEntries(IEnumerable<KeyValuePair<ushort, string>> entries)
        {
            AddEntries(entries.Select(entry => new KeyValuePair<ushort, Tag>(entry.Key, new Tag(entry.Key, entry.Value))));
        }
        public void AddEntries(IEnumerable<KeyValuePair<ushort, Tag>> entries)
        {
            foreach (KeyValuePair<ushort, Tag> entry in entries)
            {
                this.tags.Add(entry.Key, entry.Value);
            }
        }

        public bool TryGetTagById(ushort tagId, out Tag value)
        {
            return this.tags.TryGetValue(tagId, out value);
        }
        public bool TryGetTagByName(string name, out Tag value)
        {
            value = this.tags.FirstOrDefault(kv => kv.Value.Name == name).Value;
            return value != null;
        }

        public IEnumerable<Tag> GetAllCustomTags()
        {
            return this.tags.Where(kv => !BuiltinTags.AllEntries.Keys.Contains(kv.Key))
                            .Select(kv => kv.Value);
        }
        public struct BuiltinTags
        {
            public struct IDs
            {
                public const ushort StructureEnd = 0x0000;
                public const ushort List = 0x0001;
                public const ushort String = 0x8000;
            }
            public struct Names
            {
                public const string StructureEnd = "StructureEnd";
                public const string List = "List";
                public const string String = "String";
            }
            public static readonly IDictionary<ushort, string> AllEntries = new Dictionary<ushort, string>
            {
                [IDs.StructureEnd] = Names.StructureEnd,
                [IDs.List] = Names.List,
                [IDs.String] = Names.String,
            };
        }
    }
}
