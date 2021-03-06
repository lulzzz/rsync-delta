﻿using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class AllBlocksTests
    {
        [Theory]
        [MemberData(nameof(MutationTest.TestCases), MemberType = typeof(MutationTest))]
        public async Task AllBlocks(BlockSequence blocks, Mutation mutation, SignatureOptions options)
        {
            using var files = new TestDirectory(nameof(AllBlocks), blocks, mutation);
            await MutationTest.Test(files, blocks, mutation, options);
        }
    }
}
