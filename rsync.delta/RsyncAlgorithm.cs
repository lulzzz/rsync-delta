using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public interface IRsyncAlgorithm
    {
        ValueTask GenerateSignature(
            PipeReader fileReader,
            PipeWriter signatureWriter,
            SignatureOptions? options = null,
            CancellationToken ct = default);
        
        ValueTask GenerateSignature(
            Stream fileStream,
            PipeWriter signatureWriter,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        ValueTask GenerateSignature(
            PipeReader fileReader,
            Stream signatureStream,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        ValueTask GenerateSignature(
            Stream fileStream,
            Stream signatureStream,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            PipeReader signatureReader,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            Stream signatureStream,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            PipeReader signatureReader,
            Stream fileStream,
            PipeWriter deltaWriter,
            CancellationToken ct = default);
        
        ValueTask GenerateDelta(
            PipeReader signatureReader,
            PipeReader fileReader,
            Stream deltaStream,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            Stream signatureStream,
            PipeReader fileReader,
            Stream deltaStream,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            PipeReader signatureReader,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct = default);

        ValueTask GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct = default);

        ValueTask Patch(
            PipeReader deltaReader,
            Stream oldFileStream,
            PipeWriter newFileWriter,
            CancellationToken ct = default);

        ValueTask Patch(
            Stream deltaStream,
            Stream oldFileStream,
            PipeWriter newFileWriter,
            CancellationToken ct = default);

        ValueTask Patch(
            PipeReader deltaReader,
            Stream oldFileStream,
            Stream newFileStream,
            CancellationToken ct = default);

        ValueTask Patch(
            Stream deltaStream,
            Stream oldFileStream,
            Stream newFileStream,
            CancellationToken ct = default);
    }

    public class RsyncAlgorithm : IRsyncAlgorithm
    {
        private readonly StreamPipeReaderOptions _fileReadOptions;
        private readonly StreamPipeWriterOptions _fileWriteOptions;
        private readonly StreamPipeReaderOptions _sigReadOptions;
        private readonly StreamPipeWriterOptions _sigWriteOptions;
        private readonly StreamPipeReaderOptions _deltaReadOptions;
        private readonly StreamPipeWriterOptions _deltaWriteOptions;

        public RsyncAlgorithm(
            StreamPipeReaderOptions? fileStreamReadOptions = null,
            StreamPipeWriterOptions? fileStreamWriteOptions = null,
            StreamPipeReaderOptions? signatureStreamReadOptions = null,
            StreamPipeWriterOptions? signatureStreamWriteOptions = null,
            StreamPipeReaderOptions? deltaStreamReadOptions = null,
            StreamPipeWriterOptions? deltaStreamWriteOptions = null)
        {
            _fileReadOptions = fileStreamReadOptions ?? new StreamPipeReaderOptions();
            _fileWriteOptions = fileStreamWriteOptions ?? new StreamPipeWriterOptions();
            _sigReadOptions = signatureStreamReadOptions ?? new StreamPipeReaderOptions();
            _sigWriteOptions = signatureStreamWriteOptions ?? new StreamPipeWriterOptions();
            _deltaReadOptions = deltaStreamReadOptions ?? new StreamPipeReaderOptions();
            _deltaWriteOptions = deltaStreamWriteOptions ?? new StreamPipeWriterOptions();
        }

        public async ValueTask GenerateSignature(
            PipeReader fileReader, 
            PipeWriter signatureWriter,
            SignatureOptions? options, 
            CancellationToken ct)
        {
            if (fileReader == null)
            {
                throw new ArgumentNullException(nameof(fileReader));
            }
            if (signatureWriter == null)
            {
                throw new ArgumentNullException(nameof(signatureWriter));
            }

            var writer = new SignatureWriter(
                fileReader, 
                signatureWriter, 
                options ?? SignatureOptions.Default);
            await writer.Write(ct);
        }

        public ValueTask GenerateSignature(
            Stream fileStream, 
            PipeWriter signatureWriter, 
            SignatureOptions? options,
            CancellationToken ct) =>
            GenerateSignature(
                PipeReader.Create(fileStream, _fileReadOptions),
                signatureWriter,
                options,
                ct);

        public ValueTask GenerateSignature(
            PipeReader fileReader, 
            Stream signatureStream, 
            SignatureOptions? options, 
            CancellationToken ct) =>
            GenerateSignature(
                fileReader,
                PipeWriter.Create(signatureStream, _sigWriteOptions),
                options,
                ct);

        public ValueTask GenerateSignature(
            Stream fileStream, 
            Stream signatureStream, 
            SignatureOptions? options, 
            CancellationToken ct) =>
            GenerateSignature(
                PipeReader.Create(fileStream, _fileReadOptions),
                PipeWriter.Create(signatureStream, _sigWriteOptions),
                options,
                ct);

        public async ValueTask GenerateDelta(
            PipeReader signatureReader, 
            PipeReader fileReader, 
            PipeWriter deltaWriter, 
            CancellationToken ct)
        {
            if (signatureReader == null) 
            {
                throw new ArgumentNullException(nameof(signatureReader)); 
            }
            if (fileReader == null)
            {
                throw new ArgumentNullException(nameof(fileReader));
            }
            if (deltaWriter == null)
            {
                throw new ArgumentNullException(nameof(deltaWriter));
            }

            var builder = new BlockMatcher.Builder();
            var reader = new SignatureReader(builder, signatureReader);
            var matcher = await reader.Read(ct);
            var writer = new DeltaWriter(matcher, fileReader, deltaWriter);
            await writer.Write(ct);
        }

        public ValueTask GenerateDelta(
            Stream signatureStream, 
            PipeReader fileReader, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _sigReadOptions),
                fileReader,
                deltaWriter,
                ct);

        public ValueTask GenerateDelta(
            PipeReader signatureReader, 
            Stream fileStream, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                PipeReader.Create(fileStream, _fileReadOptions),
                deltaWriter,
                ct);

        public ValueTask GenerateDelta(
            PipeReader signatureReader, 
            PipeReader fileReader,
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                fileReader,
                PipeWriter.Create(deltaStream, _deltaWriteOptions),
                ct);

        public ValueTask GenerateDelta(
            Stream signatureStream, 
            Stream fileStream, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _sigReadOptions),
                PipeReader.Create(fileStream, _fileReadOptions),
                deltaWriter,
                ct);

        public ValueTask GenerateDelta(
            Stream signatureStream, 
            PipeReader fileReader, 
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _sigReadOptions),
                fileReader,
                PipeWriter.Create(deltaStream, _deltaWriteOptions),
                ct);

        public ValueTask GenerateDelta(
            PipeReader signatureReader, 
            Stream fileStream, 
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                PipeReader.Create(fileStream, _fileReadOptions),
                PipeWriter.Create(deltaStream, _deltaWriteOptions),
                ct);

        public ValueTask GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _sigReadOptions),
                PipeReader.Create(fileStream, _fileReadOptions),
                PipeWriter.Create(deltaStream, _deltaWriteOptions),
                ct);

        public async ValueTask Patch(
            PipeReader deltaReader, 
            Stream oldFileStream, 
            PipeWriter newFileWriter,
            CancellationToken ct = default)
        {
            if (deltaReader == null)
            {
                throw new ArgumentNullException(nameof(deltaReader));
            }
            if (oldFileStream == null)
            {
                throw new ArgumentNullException(nameof(oldFileStream));
            }
            if (newFileWriter == null)
            {
                throw new ArgumentNullException(nameof(newFileWriter));
            }
            var copier = new Copier(oldFileStream, newFileWriter);
            var patcher = new Patcher(deltaReader, newFileWriter, copier);
            await patcher.Patch(ct);
        }

        public ValueTask Patch(
            Stream deltaStream, 
            Stream oldFileStream, 
            PipeWriter newFileWriter, 
            CancellationToken ct = default) =>
            Patch(
                PipeReader.Create(deltaStream, _deltaReadOptions),
                oldFileStream,
                newFileWriter,
                ct);

        public ValueTask Patch(
            PipeReader deltaReader, 
            Stream oldFileStream, 
            Stream newFileStream, 
            CancellationToken ct = default) =>
            Patch(
                deltaReader,
                oldFileStream,
                PipeWriter.Create(newFileStream, _fileWriteOptions),
                ct);

        public ValueTask Patch(
            Stream deltaStream, 
            Stream oldFileStream, 
            Stream newFileStream, 
            CancellationToken ct = default) =>
            Patch(
                PipeReader.Create(deltaStream, _deltaReadOptions),
                oldFileStream,
                PipeWriter.Create(newFileStream, _fileWriteOptions),
                ct);
    }
}