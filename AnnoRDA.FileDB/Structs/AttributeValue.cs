using System;

namespace AnnoRDA.FileDB.Structs
{
    public struct AttributeValue
    {
        public byte[] Bytes { get; private set; }

        public AttributeValue(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        public string GetUnicodeString()
        {
            if (this.Bytes.Length < 2) {
                return "";
            }
            return System.Text.Encoding.Unicode.GetString(this.Bytes);
        }
        public UInt32 GetUInt32()
        {
            return System.BitConverter.ToUInt32(this.Bytes, 0);
        }
        public UInt64 GetUInt64()
        {
            return System.BitConverter.ToUInt64(this.Bytes, 0);
        }
    }
}
