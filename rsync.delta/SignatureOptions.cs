using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct SignatureOptions
    {
        public const ushort Size = 8;

        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public readonly uint BlockLength;
        public readonly uint StrongHashLength;

        public SignatureOptions(ref ReadOnlySequence<byte> buffer)
        {
            BlockLength = buffer.ReadUIntBigEndian();
            StrongHashLength = buffer.ReadUIntBigEndian();
        }
        
        public SignatureOptions(uint blockLength, uint strongHashLength)
        {
            if (blockLength <= 0) // todo: max blocklen
            {
                throw new ArgumentOutOfRangeException(nameof(blockLength));
            }
            if (strongHashLength <= 0 || strongHashLength > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(strongHashLength));
            }
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, BlockLength);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), StrongHashLength);
        }
    }
}