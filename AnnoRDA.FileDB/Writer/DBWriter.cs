using System.Collections.Generic;
using System.Linq;

using AnnoRDA.FileDB.Structs;

namespace AnnoRDA.FileDB.Writer
{
    public class DBWriter : System.IDisposable
    {
        private readonly BinaryWriter writer;

        public DBWriter(System.IO.Stream stream, bool leaveOpen)
        {
            if (!stream.CanWrite) {
                throw new System.ArgumentException("stream must be writable");
            }

            this.writer = new BinaryWriter(stream, System.Text.Encoding.Unicode, leaveOpen);
        }
        public DBWriter(System.IO.Stream stream)
        : this(stream, false)
        { }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        public void WriteAttribute(ushort tagId, string value)
        {
            byte[] bytes;
            if (value.Length == 0) {
                bytes = new byte[1] { 0x0 };
            } else {
                bytes = System.Text.Encoding.Unicode.GetBytes(value);
            }
            this.WriteAttribute(tagId, bytes);
        }
        public void WriteAttribute(ushort tagId, int value)
        {
            byte[] bytes = System.BitConverter.GetBytes(value);
            this.WriteAttribute(tagId, bytes);
        }
        public void WriteAttribute(ushort tagId, long value)
        {
            byte[] bytes = System.BitConverter.GetBytes(value);
            this.WriteAttribute(tagId, bytes);
        }
        public void WriteAttribute(ushort tagId, byte[] value)
        {
            this.WriteTag(tagId);
            this.writer.Write7BitEncodedInt(value.Length);
            this.writer.Write(value);
        }
        
        public void WriteTag(ushort tagId)
        {
            this.writer.Write(tagId);
        }

        private void WriteTagsSections(Tags tags)
        {
            uint tagsSectionOffset = (uint)this.writer.BaseStream.Position;

            ICollection<Tag> customTags = tags.GetAllCustomTags().ToList();
            this.WriteTagsSection(customTags.Where(tag => tag.Type == Tag.TagType.StructureStart));
            this.WriteTagsSection(customTags.Where(tag => tag.Type == Tag.TagType.Attribute));

            this.writer.Write(tagsSectionOffset);
        }
        private void WriteTagsSection(IEnumerable<Tag> tags)
        {
            ICollection<Tag> tagCollection = tags.OrderBy(tag => tag.Name).ToList();

            this.writer.Write7BitEncodedInt(tagCollection.Count);

            foreach (Tag tag in tags.OrderBy(tag => tag.Name)) {
                this.writer.WriteZeroTerminatedASCIIString(tag.Name);
                this.writer.Write(tag.ID);
            }
        }

        public void FinalizeFile(Tags tags)
        {
            this.WriteTag(Tags.BuiltinTags.IDs.StructureEnd);

            this.WriteTagsSections(tags);
        }
    }
}
