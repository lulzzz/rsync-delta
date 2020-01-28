﻿using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Rdiff : IRdiff
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly StreamPipeReaderOptions _readerOptions;
        private readonly StreamPipeWriterOptions _writerOptions;

        public Rdiff(MemoryPool<byte>? memoryPool = null)
        {
            _memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
            _readerOptions = new StreamPipeReaderOptions(_memoryPool, leaveOpen: true);
            _writerOptions = new StreamPipeWriterOptions(_memoryPool, leaveOpen: true);
        }

        public Task Signature(
            PipeReader oldFile,
            PipeWriter signature,
            SignatureOptions? options,
            CancellationToken ct)
        {
            if (oldFile == null)
            {
                throw new ArgumentNullException(nameof(oldFile));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            return SignatureAsync();

            async Task SignatureAsync()
            {
                using var writer = new Signature.SignatureWriter(
                    oldFile,
                    signature,
                    options ?? default,
                    _memoryPool);
                await writer.Write(ct).ConfigureAwait(false);
            }
        }

        public Task Signature(
            Stream oldFile,
            PipeWriter signature,
            SignatureOptions? options,
            CancellationToken ct) =>
            Signature(
                PipeReader.Create(oldFile, _readerOptions),
                signature,
                options,
                ct);

        public Task Signature(
            PipeReader oldFile,
            Stream signature,
            SignatureOptions? options,
            CancellationToken ct) =>
            Signature(
                oldFile,
                PipeWriter.Create(signature, _writerOptions),
                options,
                ct);

        public Task Signature(
            Stream oldFile,
            Stream signature,
            SignatureOptions? options,
            CancellationToken ct) =>
            Signature(
                PipeReader.Create(oldFile, _readerOptions),
                PipeWriter.Create(signature, _writerOptions),
                options,
                ct);

        public Task Delta(
            PipeReader signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct)
        {
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            if (newFile == null)
            {
                throw new ArgumentNullException(nameof(newFile));
            }
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }
            return DeltaAsync();

            async Task DeltaAsync()
            {
                var reader = new Delta.SignatureReader(signature, _memoryPool);
                using var matcher = await reader.Read(ct).ConfigureAwait(false);
                var writer = new Delta.DeltaWriter(matcher, newFile, delta);
                await writer.Write(ct).ConfigureAwait(false);
            }
        }

        public Task Delta(
            Stream signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct) =>
            Delta(
                PipeReader.Create(signature, _readerOptions),
                newFile,
                delta,
                ct);

        public Task Delta(
            PipeReader signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct) =>
            Delta(
                signature,
                PipeReader.Create(newFile, _readerOptions),
                delta,
                ct);

        public Task Delta(
            PipeReader signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct) =>
            Delta(
                signature,
                newFile,
                PipeWriter.Create(delta, _writerOptions),
                ct);

        public Task Delta(
            Stream signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct) =>
            Delta(
                PipeReader.Create(signature, _readerOptions),
                PipeReader.Create(newFile, _readerOptions),
                delta,
                ct);

        public Task Delta(
            Stream signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct) =>
            Delta(
                PipeReader.Create(signature, _readerOptions),
                newFile,
                PipeWriter.Create(delta, _writerOptions),
                ct);

        public Task Delta(
            PipeReader signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct) =>
            Delta(
                signature,
                PipeReader.Create(newFile, _readerOptions),
                PipeWriter.Create(delta, _writerOptions),
                ct);

        public Task Delta(
            Stream signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct) =>
            Delta(
                PipeReader.Create(signature, _readerOptions),
                PipeReader.Create(newFile, _readerOptions),
                PipeWriter.Create(delta, _writerOptions),
                ct);

        public Task Patch(
            Stream oldFile,
            PipeReader delta,
            PipeWriter newFile,
            CancellationToken ct)
        {
            if (oldFile == null)
            {
                throw new ArgumentNullException(nameof(oldFile));
            }
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }
            if (newFile == null)
            {
                throw new ArgumentNullException(nameof(newFile));
            }
            return PatchAsync();

            async Task PatchAsync()
            {
                var copier = new Patch.Copier(oldFile, newFile, _readerOptions);
                var patcher = new Patch.Patcher(delta, newFile, copier);
                await patcher.Patch(ct).ConfigureAwait(false);
            }
        }

        public Task Patch(
            Stream oldFile,
            Stream delta,
            PipeWriter newFile,
            CancellationToken ct) =>
            Patch(
                oldFile,
                PipeReader.Create(delta, _readerOptions),
                newFile,
                ct);

        public Task Patch(
            Stream oldFile,
            PipeReader deltaReader,
            Stream newFile,
            CancellationToken ct = default) =>
            Patch(
                oldFile,
                deltaReader,
                PipeWriter.Create(newFile, _writerOptions),
                ct);

        public Task Patch(
            Stream oldFile,
            Stream delta,
            Stream newFile,
            CancellationToken ct = default) =>
            Patch(
                oldFile,
                PipeReader.Create(delta, _readerOptions),
                PipeWriter.Create(newFile, _writerOptions),
                ct);
    }
}
