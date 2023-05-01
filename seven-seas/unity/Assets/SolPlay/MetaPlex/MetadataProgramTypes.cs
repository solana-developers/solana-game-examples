using Solana.Unity.Programs.Utilities;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Metaplex
{
    /// <summary>
    /// Uses class - Consumable NFT
    /// </summary>
    public class Uses
    {

        /// <summary>
        ///  useMethod Enum (0 = burn, 1 = multiple, 2 = single)
        /// </summary>
        public byte useMethod;

        /// <summary>
        ///  remaining uses - usually the same as total on creation
        /// </summary>
        public string remaining;

        /// <summary>
        ///  total NFT uses
        /// </summary>
        public string total;

        /// <summary>
        /// Uses Constructor
        /// </summary>
        /// <param name="_useMethod"> useMethod</param>
        /// <param name="_remaining"> remaining</param>
        /// <param name="_total"> total</param>
        public Uses(byte _useMethod, string _remaining, string _total)
        {
            useMethod = _useMethod;
            remaining = _remaining;
            total = _total;
        }
    }
    /// <summary>
    /// Collection class
    /// </summary>
    public class Collection
    {
        /// <summary>
        /// Collection public key.
        /// </summary>
        public PublicKey key;

        /// <summary>
        ///  Did the collection sign?
        /// </summary>
        public bool verified;


        /// <summary>
        ///  Collection data byte length in an account data.
        /// </summary>
        public static int length = 33;

        /// <summary>
        ///  Collection constructor.
        /// </summary>
        /// <param name="key"> Public key of the collection</param>
        /// <param name="verified"> Did the collection sign?</param>/
        public Collection(PublicKey key, bool verified = false)
        {
            this.key = key;
            this.verified = verified;

        }

        /// <summary>
        ///  Construct a Collection from a byte array ( deserialize ).
        /// </summary>
        /// <param name="encoded"></param>
        public Collection(ReadOnlySpan<byte> encoded)
        {
            this.key = encoded.GetPubKey(1);
            this.verified = Convert.ToBoolean(encoded.GetU8(0));

        }

        /// <summary>
        ///  Encode Collection data ( serialize ).
        /// </summary>
        /// <returns></returns>
        public byte[] Encode()
        {
            byte[] encodedBuffer = new byte[length];

            encodedBuffer.WriteU8(Convert.ToByte(verified), 0);
            encodedBuffer.WritePubKey(key, 1);

            return encodedBuffer;
        }
    }

    /// <summary>
    /// Creator class.
    /// </summary>
    public class Creator
    {
        /// <summary>
        /// Creators public key.
        /// </summary>
        public PublicKey key;

        /// <summary>
        ///  Did the creator sign?
        /// </summary>
        public bool verified;

        /// <summary>
        /// Creators share in percentages.
        /// </summary>
        public byte share;

        /// <summary>
        ///  Creator data byte lenght in an account data.
        /// </summary>
        public static int length = 34;

        /// <summary>
        ///  Creator constructor.
        /// </summary>
        /// <param name="key"> Public key of the creator</param>
        /// <param name="share"> Creators share in percentages</param>
        /// <param name="verified"> Did the creator sign?</param>/
        public Creator(PublicKey key, byte share, bool verified = false)
        {
            this.key = key;
            this.verified = verified;
            this.share = share;
        }

        /// <summary>
        ///  Construct a Creator from a byte array ( deserialize ).
        /// </summary>
        /// <param name="encoded"></param>
        public Creator(ReadOnlySpan<byte> encoded)
        {
            this.key = encoded.GetPubKey(0);
            this.verified = Convert.ToBoolean(encoded.GetU8(32));
            this.share = encoded.GetU8(33);
        }

        /// <summary>
        ///  Encode Creators data ( serialize ).
        /// </summary>
        /// <returns></returns>
        public byte[] Encode()
        {
            byte[] encodedBuffer = new byte[34];

            encodedBuffer.WritePubKey(key, 0);
            encodedBuffer.WriteU8(Convert.ToByte(verified), 32);
            encodedBuffer.WriteU8((byte)share, 33);

            return encodedBuffer;
        }
    }

    /// <summary>
    /// Metadata V1 Data class for instructions
    /// </summary>
    public class MetadataV1
    {
        /// <summary>  Name or discription. Max 32 bytes. </summary>
        public string name;
        /// <summary>  Symbol. Max 10 bytes. </summary>
        public string symbol;
        /// <summary>  Uri. Max 100 bytes. </summary>
        public string uri;
        /// <summary>  Seller fee basis points for secondary sales. </summary>
        public uint sellerFeeBasisPoints;
        /// <summary>  List of creators. </summary>
        public List<Creator> creators;
    }
    /// <summary>
    /// Metadata V3 Data class for instructions
    /// </summary>
    public class MetadataV3
    {
        /// <summary>  Name or discription. Max 32 bytes. </summary>
        public string name;
        /// <summary>  Symbol. Max 10 bytes. </summary>
        public string symbol;
        /// <summary>  Uri. Max 100 bytes. </summary>
        public string uri;
        /// <summary>  Seller fee basis points for secondary sales. </summary>
        public uint sellerFeeBasisPoints;
        /// <summary>  List of creators. </summary>
        public List<Creator> creators;
        /// <summary>  Collection Address and Verification </summary>
        public Collection collection;
        /// <summary> NFT Uses </summary>
        public Uses uses;
    }
}
