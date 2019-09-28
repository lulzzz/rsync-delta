using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class SignatureTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", null, null)]
        [InlineData("hello", 1, null)]
        [InlineData("hello", 2, null)]
        [InlineData("hello_hellooo", null, 16)]
        [InlineData("hello", null, 15)]
        [InlineData("hello", null, 17)]
        public async Task Signature(
            string text, int? blockLength, int? strongHashLength)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using TempFile rdiffOut = await Rdiff.Signature(
                new MemoryStream(bytes), blockLength, strongHashLength);
            var expected = BitConverter.ToString(await rdiffOut.Bytes());

            var options = new SignatureOptions(
                blockLength ?? SignatureOptions.Default.BlockLength,
                strongHashLength ?? SignatureOptions.Default.StrongHashLength);
            var libraryOut = new MemoryStream();
            await _rsync.GenerateSignature(new MemoryStream(bytes), libraryOut, options);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}