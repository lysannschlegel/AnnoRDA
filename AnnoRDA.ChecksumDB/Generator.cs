using System.Collections.Generic;
using System.Linq;

namespace AnnoRDA.ChecksumDB
{
    public static class Generator
    {
        public static byte[] ComputeChecksum(System.IO.Stream fileDBStream)
        {
            // Checksum is the hex representation of the MD5 of "<file.db MD5 as hex><STAGE2_SUFFIX_BYTES>"
            // Note that the hex representations must use lowercase letters.

            using (var md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] stage1Checksum = md5.ComputeHash(fileDBStream);
                IEnumerable<byte> stage1ChecksumHex = MakeHexData(stage1Checksum);

                byte[] stage2Data = stage1ChecksumHex.Concat(STAGE2_SUFFIX_BYTES).ToArray();
                byte[] stage2Checksum = md5.ComputeHash(stage2Data);
                byte[] stage2ChecksumHex = MakeHexData(stage2Checksum).ToArray();
                return stage2ChecksumHex;
            }
        }

        private static IEnumerable<byte> MakeHexData(byte[] data)
        {
            // { 0x9a, 0x34 } -> { 0x39, 0x61, 0x33, 0x34 } == { '9', 'a', '3', '4' }
            return data.SelectMany(b => new byte[] { HEX_ALPHABET[b >> 4], HEX_ALPHABET[b & 0xF] });
        }
        
        static readonly byte[] HEX_ALPHABET = { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 }; // 0123456789abcdef

        static readonly byte[] STAGE2_SUFFIX_BYTES = { 0x0D, 0x0A, 0x35, 0x34, 0x34, 0x33, 0x30, 0x38, 0x33, 0x33, 0x36, 0x38, 0x63, 0x39, 0x62, 0x65, 0x33, 0x33, 0x62, 0x35, 0x30, 0x65, 0x33, 0x66, 0x64, 0x62, 0x33, 0x61, 0x38, 0x66, 0x61, 0x32, 0x38, 0x37 }; // "\r\n5443083368c9be33b50e3fdb3a8fa287"
    }
}
