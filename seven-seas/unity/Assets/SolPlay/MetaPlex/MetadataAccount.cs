using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs.Utilities;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using SolPlay.DeeplinksNftExample.Utils;

namespace Solnet.Metaplex
{
    /// <summary> version and type fo metadata account </summary>
    public enum MetadataKey
    {
        /// <summary> Uninitialized </summary>
        Uninitialized = 0,
        /// <summary> Metadata V1 </summary>
        MetadataV1 = 4,
        /// <summary> Edition V1 </summary>
        EditionV1 = 1,
        /// <summary> Master Edition V2 </summary>
        MasterEditionV1 = 2,
        /// <summary> Master Edition V2 </summary>
        MasterEditionV2 = 6,
        /// <summary> Edition marker </summary>
        EditionMarker = 7
    }

    /// <summary> Category </summary>
    public class MetadataCategory
    {
        /// <summary> Audio </summary>
        public string Audio = "audio";
        /// <summary> Video </summary>
        public string Video = "video";
        /// <summary> Image </summary>
        public string Image = "image";
        /// <summary> Virtual reality, 3D </summary>
        public string VR = "vr";
        /// <summary> html </summary>
        public string HTML = "html";
        /// <summary> Epubs </summary>
        public string Document = "document";
    }

    /// <summary> Metatada file struct </summary>
    public struct MetadataFile
    {
        /// <summary> uri </summary>
        public string uri;
        /// <summary> type </summary>
        public string type;
    }

    /// <summary> Metadata Onchain DataV1</summary>
    public class OnchainDataV1
    {
        /// <summary> name </summary>
        public string name;
        /// <summary> short symbol </summary>
        public string symbol;
        /// <summary> uri of metadata </summary>
        public string uri;
        /// <summary> Seller cut </summary>
        public uint sellerFeeBasisPoints;
        /// <summary> Has Creators </summary>
        public bool hasCreators;
        /// <summary> Creators array </summary>
        public IList<Creator> creators;
        ///<summary> isMutable </summary>
        public bool isMutable;
        ///<summary> Edition Type </summary>
        public int editionNonce;
        ///<summary> Token Standard - Fungible / non-fungible </summary>
        public bool primarySaleHappened;
        ///<summary> metadata json </summary>
        public string metadata;

        /// <summary> Constructor </summary>
        public OnchainDataV1(string _name, string _symbol, string _uri, uint _sellerFee, IList<Creator> _creators, bool _primarySaleHappened, int _editionNonce, bool _isMutable)
        {
            name = _name;
            symbol = _symbol;
            uri = _uri;
            sellerFeeBasisPoints = _sellerFee;
            creators = _creators;
            isMutable = _isMutable;
            editionNonce = _editionNonce;
            primarySaleHappened = _primarySaleHappened;
        }

        /// <summary> Tries to get a json file from the uri </summary>
        public async Task<string> FetchMetadata()
        {
            if (uri is null)
                return null;

            if (metadata is null)
            {
                using var http = new HttpClient();
                var res = await http.GetStringAsync(uri);
                metadata = res;
            }

            return metadata;
        }
    }
    /// <summary> Metadata Onchain DataV3 structure  </summary>
    public class OnChainDataV3
    {
        /// <summary> name </summary>
        public string name;
        /// <summary> short symbol </summary>
        public string symbol;
        /// <summary> uri of metadata </summary>
        public string uri;
        /// <summary> Seller cut </summary>
        public uint sellerFeeBasisPoints;
        /// <summary> Has Creators </summary>
        public bool hasCreators;
        /// <summary> Creators array </summary>
        public IList<Creator> creators;
        ///<summary> Collection link </summary>
        public Collection collectionLink;
        ///<summary> USEs </summary>
        public Uses uses;
        ///<summary> isMutable </summary>
        public bool isMutable;
        ///<summary> Edition Type </summary>
        public int editionNonce;
        ///<summary> Token Standard - Fungible / non-fungible </summary>
        public int tokenStandard;
        ///<summary> metadata json </summary>
        public string metadata;

        /// <summary> Constructor </summary>
        public OnChainDataV3(string _name, string _symbol, string _uri, uint _sellerFee, IList<Creator> _creators, int _editionNonce, int _tokenStandard, Collection _collection, Uses useInfo, bool _isMutable)
        {
            name = _name;
            symbol = _symbol;
            uri = _uri;
            sellerFeeBasisPoints = _sellerFee;
            creators = _creators;
            collectionLink = _collection;
            uses = useInfo;
            isMutable = _isMutable;
            editionNonce = _editionNonce;
            tokenStandard = _tokenStandard;
        }

        /// <summary> Tries to get a json file from the uri </summary>
        public async Task<string> FetchMetadata()
        {
            if (uri is null)
                return null;

            if (metadata is null)
            {
                using var http = new HttpClient();
                var res = await http.GetStringAsync(uri);
                metadata = res;
            }

            return metadata;
        }
    }
    /// <summary> Metadata account class V2 </summary>
    public class MetadataAccount
    {
        /// <summary> metadata public key </summary>
        public PublicKey metadataKey;
        /// <summary> update authority key </summary>
        public PublicKey updateAuthority;
        /// <summary> mint public key </summary>
        public string mint;
        /// <summary> data struct </summary>
        public OnchainDataV1 metadataV1;
        /// <summary> data struct </summary>
        public OnChainDataV3 metadataV3;
        /// <summary> version of metadata. V1 or V3</summary>
        public int MetadataVersion;
        /// <summary> standard Solana account info </summary>
        public AccountInfo accInfo;
        /// <summary> owner, should be Metadata program</summary>
        public PublicKey owner;

        /// <summary> Constructor </summary>
        /// <param name="accInfo"> Soloana account info </param>
        /// /// <param name="MetadataVersion"> Metadata Account Version - Either 1 or 3</param>
        public MetadataAccount(AccountInfo accInfo, int MetadataVersion)
        {
            try
            {
                this.owner = new PublicKey(accInfo.Owner);
                if(MetadataVersion == 1)
                    this.metadataV1 = ParseDataV1(accInfo.Data);
                if (MetadataVersion == 3)
                    this.metadataV3 = ParseDataV3(accInfo.Data);
                var data = Convert.FromBase64String(accInfo.Data[0]);
                this.updateAuthority = new PublicKey(data.Slice(1, 33));
                this.mint = new PublicKey(data.Slice(33, 65));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary> Parse version 1 Data used for V1 metadata accounts </summary>
        /// <param name="data"> data </param>
        /// <returns> data struct </returns>
        /// <remarks> parses an array of bytes into a data struct </remarks>
        public static OnchainDataV1 ParseDataV1(List<string> data)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(data[0]);
                ReadOnlySpan<byte> binData = new(bytes);

                string name;
                string symbol;
                string uri;

                int nameLength = binData.GetBorshString(MetadataAccountLayout.nameOffset, out name);
                int symbolLength = binData.GetBorshString(MetadataAccountLayout.symbolOffset, out symbol);
                int uriLength = binData.GetBorshString(MetadataAccountLayout.uriOffset, out uri);
                uint sellerFee = binData.GetU16(MetadataAccountLayout.feeBasisOffset);
                bool hasCreators = binData.GetBool(MetadataAccountLayout.creatorSwitchOffset);
                var numOfCreators = binData.GetU8(MetadataAccountLayout.creatorsCountOffset);
                bool primarySaleHappened;
                int o = 0;
                IList<Creator> creators = null;
                if (hasCreators == true)
                {
                    creators = MetadataProgramData.DecodeCreators(binData.GetSpan(MetadataAccountLayout.creatorsCountOffset + 5, numOfCreators * (32 + 1 + 1)));
                    o = MetadataAccountLayout.creatorsCountOffset + 5 + (numOfCreators * (32 + 1 + 1));
                }
                else
                {
                    o = MetadataAccountLayout.creatorSwitchOffset;
                    o++;
                }
                primarySaleHappened = binData.GetBool(o);
                o++;
                var isMutable = binData.GetBool(o);
                o++;
                o++;
                var editionNonce = binData.GetU8(o);

                name = name.TrimEnd('\0');
                symbol = symbol.TrimEnd('\0');
                uri = uri.TrimEnd('\0');
                var res = new OnchainDataV1(name, symbol, uri, sellerFee, creators, primarySaleHappened, editionNonce, isMutable);

                return res;
            }
            catch (Exception ex)
            {
                throw new Exception("could not decode account data from base64", ex);
            }
        }

        /// <summary> Parse version 2 Data used for V3 metadata accounts</summary>
        /// <param name="data"> data </param>
        /// <returns> data struct </returns>
        /// <remarks> parses an array of bytes into a data struct </remarks>
        public static OnChainDataV3 ParseDataV3(List<string> data)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(data[0]);
                ReadOnlySpan<byte> binData = new(bytes);

                string name;
                string symbol;
                string uri;

                int nameLength = binData.GetBorshString(MetadataAccountLayout.nameOffset, out name);
                int symbolLength = binData.GetBorshString(MetadataAccountLayout.symbolOffset, out symbol);
                int uriLength = binData.GetBorshString(MetadataAccountLayout.uriOffset, out uri);
                uint sellerFee = binData.GetU16(MetadataAccountLayout.feeBasisOffset);
                var hasCreators = binData.GetBool(MetadataAccountLayout.creatorSwitchOffset);
                var numOfCreators = binData.GetU8(MetadataAccountLayout.creatorsCountOffset);
                int o = 0;
                IList<Creator> creators = null;
                if (hasCreators == true)
                {
                    creators = MetadataProgramData.DecodeCreators(binData.GetSpan(MetadataAccountLayout.creatorsCountOffset + 5, numOfCreators * (32 + 1 + 1)));
                    o = MetadataAccountLayout.creatorsCountOffset + 5 + (numOfCreators * (32 + 1 + 1));
                  
                }
                else
                {
                    o = MetadataAccountLayout.creatorSwitchOffset;
                    o++;
                }
                var primarySaleHappened = binData.GetBool(o);
                o++;
                var isMutable = binData.GetBool(o);
                o++;
                o++;
                var editionNonce = binData.GetU8(o);
                o++;
                var tokenStandard = binData.GetU8(o);
                o++;
                o++;
                bool hasCollectionlink = binData.GetBool(o);
                o++;
               
                Collection collectionLink = null;
                if (hasCollectionlink == true)
                {
                    var verified = binData.GetBool(o);
                    o++;
                    var key = binData.GetPubKey(o);
                    o = o + 32;
                    
                    collectionLink = new Collection(key, verified); 
                }
                else
                {
                    o++;
                }
              
                bool isConsumable = binData.GetBool(o);
                Uses usesInfo = null;
                if (isConsumable == true)
                {
                   o++;
                   var useMethodENUM = binData.GetBytes(o, 1)[0];
                   o++;
                   var remaining = binData.GetU64(o).ToString("x");
                   o = o + 8;
                   var total = binData.GetU64(o).ToString("x");
                   o = o + 8;
                   o++;
                   usesInfo = new Uses(useMethodENUM, remaining, total);
                }
                else
                {
                    o++;
                }
              
                name = name.TrimEnd('\0');
                symbol = symbol.TrimEnd('\0');
                uri = uri.TrimEnd('\0');
                var res = new OnChainDataV3(name, symbol, uri, sellerFee, creators, editionNonce, tokenStandard, collectionLink, usesInfo, isMutable);

                return res;
            }
            catch (Exception ex)
            {
                throw new Exception("could not decode account data from base64", ex);
            }
        }

        /// <summary> Tries to parse a metadata account </summary>
        /// <param name="client"> solana rpcclient </param>
        /// <param name="pk"> public key of a account to parse </param>
        /// /// <param name="metadataAccountVersion"> MetadataAccount Version | 1 or 3</param>
        /// <returns> Metadata account </returns>
        /// <remarks> it will try to find a metadata even from a token associated account </remarks>
        async public static Task<MetadataAccount> GetAccount(IRpcClient client, PublicKey pk, int metadataAccountVersion)
        {
            var accInfoResponse = await client.GetAccountInfoAsync(pk.Key);

            if (accInfoResponse.WasSuccessful)
            {
                var accInfo = accInfoResponse.Result.Value;
                //Account Inception loop to retrieve metadata
                if (accInfo.Owner.Contains("meta"))
                {
                    //Triggered after first jump using token account address & metadata address has been retrieved from the first run
                    return new MetadataAccount(accInfo, metadataAccountVersion);
                }
                else //Account Inception first jump - if metadata address doesnt return null
                {
                    var readdata = Convert.FromBase64String(accInfo.Data[0]);

                    PublicKey mintAccount;

                    if (readdata.Length == 165)
                    {
                        mintAccount = new PublicKey(readdata.Slice(0, 32));
                    }
                    else
                    {
                        mintAccount = pk;
                    }

                    PublicKey metadataAddress;
                    byte nonce;
                    PublicKey.TryFindProgramAddress(new List<byte[]>() 
                        {
                            Encoding.UTF8.GetBytes("metadata"),
                            MetadataProgram.ProgramIdKey,
                            mintAccount
                        },
                        MetadataProgram.ProgramIdKey,
                        out metadataAddress,
                        out nonce
                    );
                    //Loops back & handles it as a metadata address rather than a token account to retrieve metadata
                    return await GetAccount(client, metadataAddress, metadataAccountVersion);
                }
            }
            else
            {
                return null;
            }
        }
    }
}