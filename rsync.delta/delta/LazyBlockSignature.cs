using System;
using System.Buffers;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal class LazyBlockSignature : IDisposable
    {
        private readonly SignatureOptions _options;
        private readonly Blake2b _blake2b;
        private readonly IMemoryOwner<byte> _strongHashBuffer;

        private BufferedBlock _block;
        private RollingHash _rollingHash;
        private bool _recalculateStrongHash;

        public LazyBlockSignature(SignatureOptions options, MemoryPool<byte> pool) 
        {
            _options = options;
            _strongHashBuffer = pool.Rent(options.StrongHashLength);
            _blake2b = new Blake2b(pool);
        }
        public uint RollingHash => _rollingHash.Value;

        public ReadOnlyMemory<byte> StrongHash
        {
            get
            {
                if (_recalculateStrongHash)
                {
                    _blake2b.Hash(
                        _block.CurrentBlock, 
                        _strongHashBuffer.Memory.Span);
                    _recalculateStrongHash = false;
                }
                return _strongHashBuffer.Memory;
            }
        }

        public BufferedBlock Block
        {
            get => _block;
            set
            {
                _block = value;
                UpdateRollingHash();
                _recalculateStrongHash = true;
            }
        }

        private void UpdateRollingHash()
        {
            if (_block.PendingLiteral.IsEmpty)
            {
                _rollingHash = new RollingHash();
                _rollingHash.RotateIn(_block.CurrentBlock);
            }
            else if (_block.CurrentBlock.Length == _options.BlockLength)
            {
                _rollingHash.Rotate(
                    remove: _block.PendingLiteral.PeekLast(),
                    add: _block.CurrentBlock.PeekLast());
            }
            else
            {
                _rollingHash.RotateOut(_block.PendingLiteral.PeekLast());
            }
        }

        public void Dispose() 
        {
            _strongHashBuffer.Dispose();
            _blake2b.Dispose();
        }
    }
}