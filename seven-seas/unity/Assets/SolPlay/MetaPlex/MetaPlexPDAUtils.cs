using System;
using System.Text;
using Solana.Unity.Wallet;

namespace SolPlay.MetaPlex
{
    public class MetaPlexPDAUtils
    {
        public static readonly PublicKey TokenMetadataProgramId = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");

        public static PublicKey GetMetadataPDA(PublicKey mint)
        {
            if (!PublicKey.TryFindProgramAddress(
                    new[]
                    {
                        Encoding.UTF8.GetBytes("metadata"),
                        TokenMetadataProgramId.KeyBytes,
                        mint.KeyBytes,
                    },
                    TokenMetadataProgramId,
                    out PublicKey metadataAddress, out _))
            {
                throw new InvalidProgramException();
            }
            return metadataAddress;
        }
    }
}