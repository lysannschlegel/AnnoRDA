using AnnoRDA.FileDB.Structs;
using System.Collections.Generic;

namespace AnnoRDA.FileDB.Reader
{
    public interface IContentReaderDelegate
    {
        void OnStructureStart(Tag tag);
        void OnStructureEnd(Tag tag);
        void OnAttribute(Tag tag, AttributeValue value);
    }

    public class ContentReader
    {
        private readonly BinaryReader reader;
        private readonly Tags tags;

        public ContentReader(BinaryReader reader, Tags tags)
        {
            this.reader = reader;
            this.tags = tags;
        }

        public void ReadContent(IContentReaderDelegate contentDelegate)
        {
            Tag tag = this.ReadTag();
            if (tag.Type != Tag.TagType.StructureStart) {
                throw new System.FormatException(System.String.Format("Root node should be structure, but tag was {0} ({1})", tag.Type, tag.Name));
            }
            this.ReadStructure(tag, contentDelegate);
        }

        public Tag ReadTag()
        {
            var tagId = this.reader.ReadUInt16();

            Tag tag;
            if (!this.tags.TryGetTagById(tagId, out tag)) {
                throw new System.FormatException(System.String.Format("Unexpected tag found: {0}", tagId));
            } else {
                return tag;
            }
        }

        private void ReadStructure(Tag tag, IContentReaderDelegate contentDelegate)
        {
            contentDelegate.OnStructureStart(tag);

            while (true) {
                Tag innerTag = this.ReadTag();
                switch (innerTag.Type) {
                    case Tag.TagType.Attribute: {
                        this.ReadAttribute(innerTag, contentDelegate);
                        break;
                    }
                    case Tag.TagType.StructureStart: {
                        this.ReadStructure(innerTag, contentDelegate);
                        break;
                    }
                    case Tag.TagType.StructureEnd: {
                        contentDelegate.OnStructureEnd(innerTag);
                        return;
                    }
                }
            }
        }

        private void ReadAttribute(Tag tag, IContentReaderDelegate contentDelegate)
        {
            var numBytes = this.reader.Read7BitEncodedInt();
            byte[] bytes = this.reader.ReadBytes(numBytes);
            contentDelegate.OnAttribute(tag, new AttributeValue(bytes));
        }
    }
}
