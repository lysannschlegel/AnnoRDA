using System;
using System.IO;
using System.Collections.Generic;

using AnnoRDA.FileDB.Structs;

namespace AnnoRDA.FileDB.Reader
{
    public class TagsSectionReader
    {
        private readonly BinaryReader reader;

        public TagsSectionReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public long FindTagsSection()
        {
            this.reader.BaseStream.Seek(-4, SeekOrigin.End);
            var result = this.reader.ReadUInt32();
            return result;
        }

        public Tags ReadTagsSection()
        {
            var Result = new Tags();
            Result.AddEntries(ReadTagDictionary(Tag.TagType.StructureStart));
            Result.AddEntries(ReadTagDictionary(Tag.TagType.Attribute));
            return Result;
        }

        private Dictionary<ushort, Tag> ReadTagDictionary(Tag.TagType expectedTagType)
        {
            var result = new Dictionary<ushort, Tag>();

            int length = this.reader.Read7BitEncodedInt();
            for (int i = 0; i < length; ++i) {
                var position = this.reader.BaseStream.Position;
                var name = this.reader.ReadZeroTerminatedASCIIString();
                var tagId = this.reader.ReadUInt16();
                var tag = new Tag(tagId, name);
                if (tag.Type != expectedTagType) {
                    throw new FormatException(String.Format("Unexpected tag type at {0}; was: {1}, expected: {2}", position, tag.Type, expectedTagType));
                }
                result.Add(tagId, tag);
            }

            return result;
        }
    }
}
