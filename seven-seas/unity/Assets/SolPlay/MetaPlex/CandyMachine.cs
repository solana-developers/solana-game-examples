using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using CandyMachine;
using CandyMachine.Program;
using CandyMachine.Errors;
using CandyMachine.Accounts;
using CandyMachine.Types;

namespace CandyMachine
{
    namespace Accounts
    {
        public partial class CandyMachine
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 13649831137213787443UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{51, 173, 177, 113, 25, 241, 109, 189};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "9eM5CfcKCCt";
            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey TokenMint { get; set; }

            public ulong ItemsRedeemed { get; set; }

            public CandyMachineData Data { get; set; }

            public static CandyMachine Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                CandyMachine result = new CandyMachine();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Wallet = _data.GetPubKey(offset);
                offset += 32;
                if (_data.GetBool(offset++))
                {
                    result.TokenMint = _data.GetPubKey(offset);
                    offset += 32;
                }

                result.ItemsRedeemed = _data.GetU64(offset);
                offset += 8;
                offset += CandyMachineData.Deserialize(_data, offset, out var resultData);
                result.Data = resultData;
                return result;
            }
        }

        public partial class CollectionPDA
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 3845182396760569650UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{50, 183, 127, 103, 4, 213, 92, 53};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "9V1x5Jvbgur";
            public PublicKey Mint { get; set; }

            public PublicKey CandyMachine { get; set; }

            public static CollectionPDA Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                CollectionPDA result = new CollectionPDA();
                result.Mint = _data.GetPubKey(offset);
                offset += 32;
                result.CandyMachine = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }

        public partial class FreezePDA
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17181566288669752050UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{242, 186, 252, 248, 129, 47, 113, 238};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "hbnmicJtkMs";
            public PublicKey CandyMachine { get; set; }

            public bool AllowThaw { get; set; }

            public ulong FrozenCount { get; set; }

            public long? MintStart { get; set; }

            public long FreezeTime { get; set; }

            public ulong FreezeFee { get; set; }

            public static FreezePDA Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                FreezePDA result = new FreezePDA();
                result.CandyMachine = _data.GetPubKey(offset);
                offset += 32;
                result.AllowThaw = _data.GetBool(offset);
                offset += 1;
                result.FrozenCount = _data.GetU64(offset);
                offset += 8;
                if (_data.GetBool(offset++))
                {
                    result.MintStart = _data.GetS64(offset);
                    offset += 8;
                }

                result.FreezeTime = _data.GetS64(offset);
                offset += 8;
                result.FreezeFee = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum CandyMachineErrorKind : uint
        {
            IncorrectOwner = 6000U,
            Uninitialized = 6001U,
            MintMismatch = 6002U,
            IndexGreaterThanLength = 6003U,
            NumericalOverflowError = 6004U,
            TooManyCreators = 6005U,
            UuidMustBeExactly6Length = 6006U,
            NotEnoughTokens = 6007U,
            NotEnoughSOL = 6008U,
            TokenTransferFailed = 6009U,
            CandyMachineEmpty = 6010U,
            CandyMachineNotLive = 6011U,
            HiddenSettingsConfigsDoNotHaveConfigLines = 6012U,
            CannotChangeNumberOfLines = 6013U,
            DerivedKeyInvalid = 6014U,
            PublicKeyMismatch = 6015U,
            NoWhitelistToken = 6016U,
            TokenBurnFailed = 6017U,
            GatewayAppMissing = 6018U,
            GatewayTokenMissing = 6019U,
            GatewayTokenExpireTimeInvalid = 6020U,
            NetworkExpireFeatureMissing = 6021U,
            CannotFindUsableConfigLine = 6022U,
            InvalidString = 6023U,
            SuspiciousTransaction = 6024U,
            CannotSwitchToHiddenSettings = 6025U,
            IncorrectSlotHashesPubkey = 6026U,
            IncorrectCollectionAuthority = 6027U,
            MismatchedCollectionPDA = 6028U,
            MismatchedCollectionMint = 6029U,
            SlotHashesEmpty = 6030U,
            MetadataAccountMustBeEmpty = 6031U,
            MissingSetCollectionDuringMint = 6032U,
            NoChangingCollectionDuringMint = 6033U,
            CandyCollectionRequiresRetainAuthority = 6034U,
            GatewayProgramError = 6035U,
            NoChangingFreezeDuringMint = 6036U,
            NoChangingAuthorityWithCollection = 6037U,
            NoChangingTokenWithFreeze = 6038U,
            InvalidThawNft = 6039U,
            IncorrectRemainingAccountsLen = 6040U,
            MissingFreezeAta = 6041U,
            IncorrectFreezeAta = 6042U,
            FreezePDAMismatch = 6043U,
            EnteredFreezeIsMoreThanMaxFreeze = 6044U,
            NoWithdrawWithFreeze = 6045U,
            NoWithdrawWithFrozenFunds = 6046U,
            MissingRemoveFreezeTokenAccounts = 6047U,
            InvalidFreezeWithdrawTokenAddress = 6048U,
            NoUnlockWithNFTsStillFrozen = 6049U,
            SizedCollectionMetadataMustBeMutable = 6050U,
            CannotSwitchFromHiddenSettings = 6051U
        }
    }

    namespace Types
    {
        public partial class CandyMachineData
        {
            public string Uuid { get; set; }

            public ulong Price { get; set; }

            public string Symbol { get; set; }

            public ushort SellerFeeBasisPoints { get; set; }

            public ulong MaxSupply { get; set; }

            public bool IsMutable { get; set; }

            public bool RetainAuthority { get; set; }

            public long? GoLiveDate { get; set; }

            public EndSettings EndSettings { get; set; }

            public Creator[] Creators { get; set; }

            public HiddenSettings HiddenSettings { get; set; }

            public WhitelistMintSettings WhitelistMintSettings { get; set; }

            public ulong ItemsAvailable { get; set; }

            public GatekeeperConfig Gatekeeper { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Uuid, offset);
                _data.WriteU64(Price, offset);
                offset += 8;
                offset += _data.WriteBorshString(Symbol, offset);
                _data.WriteU16(SellerFeeBasisPoints, offset);
                offset += 2;
                _data.WriteU64(MaxSupply, offset);
                offset += 8;
                _data.WriteBool(IsMutable, offset);
                offset += 1;
                _data.WriteBool(RetainAuthority, offset);
                offset += 1;
                if (GoLiveDate != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteS64(GoLiveDate.Value, offset);
                    offset += 8;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (EndSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += EndSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                _data.WriteS32(Creators.Length, offset);
                offset += 4;
                foreach (var creatorsElement in Creators)
                {
                    offset += creatorsElement.Serialize(_data, offset);
                }

                if (HiddenSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += HiddenSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (WhitelistMintSettings != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += WhitelistMintSettings.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                _data.WriteU64(ItemsAvailable, offset);
                offset += 8;
                if (Gatekeeper != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    offset += Gatekeeper.Serialize(_data, offset);
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out CandyMachineData result)
            {
                int offset = initialOffset;
                result = new CandyMachineData();
                offset += _data.GetBorshString(offset, out var resultUuid);
                result.Uuid = resultUuid;
                result.Price = _data.GetU64(offset);
                offset += 8;
                offset += _data.GetBorshString(offset, out var resultSymbol);
                result.Symbol = resultSymbol;
                result.SellerFeeBasisPoints = _data.GetU16(offset);
                offset += 2;
                result.MaxSupply = _data.GetU64(offset);
                offset += 8;
                result.IsMutable = _data.GetBool(offset);
                offset += 1;
                result.RetainAuthority = _data.GetBool(offset);
                offset += 1;
                if (_data.GetBool(offset++))
                {
                    result.GoLiveDate = _data.GetS64(offset);
                    offset += 8;
                }

                if (_data.GetBool(offset++))
                {
                    offset += EndSettings.Deserialize(_data, offset, out var resultEndSettings);
                    result.EndSettings = resultEndSettings;
                }

                int resultCreatorsLength = (int)_data.GetU32(offset);
                offset += 4;
                result.Creators = new Creator[resultCreatorsLength];
                for (uint resultCreatorsIdx = 0; resultCreatorsIdx < resultCreatorsLength; resultCreatorsIdx++)
                {
                    offset += Creator.Deserialize(_data, offset, out var resultCreatorsresultCreatorsIdx);
                    result.Creators[resultCreatorsIdx] = resultCreatorsresultCreatorsIdx;
                }

                if (_data.GetBool(offset++))
                {
                    offset += HiddenSettings.Deserialize(_data, offset, out var resultHiddenSettings);
                    result.HiddenSettings = resultHiddenSettings;
                }

                if (_data.GetBool(offset++))
                {
                    offset += WhitelistMintSettings.Deserialize(_data, offset, out var resultWhitelistMintSettings);
                    result.WhitelistMintSettings = resultWhitelistMintSettings;
                }

                result.ItemsAvailable = _data.GetU64(offset);
                offset += 8;
                if (_data.GetBool(offset++))
                {
                    offset += GatekeeperConfig.Deserialize(_data, offset, out var resultGatekeeper);
                    result.Gatekeeper = resultGatekeeper;
                }

                return offset - initialOffset;
            }
        }

        public partial class ConfigLine
        {
            public string Name { get; set; }

            public string Uri { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Name, offset);
                offset += _data.WriteBorshString(Uri, offset);
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out ConfigLine result)
            {
                int offset = initialOffset;
                result = new ConfigLine();
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                offset += _data.GetBorshString(offset, out var resultUri);
                result.Uri = resultUri;
                return offset - initialOffset;
            }
        }

        public partial class EndSettings
        {
            public EndSettingType EndSettingType { get; set; }

            public ulong Number { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8((byte)EndSettingType, offset);
                offset += 1;
                _data.WriteU64(Number, offset);
                offset += 8;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out EndSettings result)
            {
                int offset = initialOffset;
                result = new EndSettings();
                result.EndSettingType = (EndSettingType)_data.GetU8(offset);
                offset += 1;
                result.Number = _data.GetU64(offset);
                offset += 8;
                return offset - initialOffset;
            }
        }

        public partial class Creator
        {
            public PublicKey Address { get; set; }

            public bool Verified { get; set; }

            public byte Share { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(Address, offset);
                offset += 32;
                _data.WriteBool(Verified, offset);
                offset += 1;
                _data.WriteU8(Share, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out Creator result)
            {
                int offset = initialOffset;
                result = new Creator();
                result.Address = _data.GetPubKey(offset);
                offset += 32;
                result.Verified = _data.GetBool(offset);
                offset += 1;
                result.Share = _data.GetU8(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }

        public partial class HiddenSettings
        {
            public string Name { get; set; }

            public string Uri { get; set; }

            public byte[] Hash { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                offset += _data.WriteBorshString(Name, offset);
                offset += _data.WriteBorshString(Uri, offset);
                _data.WriteSpan(Hash, offset);
                offset += Hash.Length;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out HiddenSettings result)
            {
                int offset = initialOffset;
                result = new HiddenSettings();
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                offset += _data.GetBorshString(offset, out var resultUri);
                result.Uri = resultUri;
                result.Hash = _data.GetBytes(offset, 32);
                offset += 32;
                return offset - initialOffset;
            }
        }

        public partial class WhitelistMintSettings
        {
            public WhitelistMintMode Mode { get; set; }

            public PublicKey Mint { get; set; }

            public bool Presale { get; set; }

            public ulong? DiscountPrice { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8((byte)Mode, offset);
                offset += 1;
                _data.WritePubKey(Mint, offset);
                offset += 32;
                _data.WriteBool(Presale, offset);
                offset += 1;
                if (DiscountPrice != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU64(DiscountPrice.Value, offset);
                    offset += 8;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out WhitelistMintSettings result)
            {
                int offset = initialOffset;
                result = new WhitelistMintSettings();
                result.Mode = (WhitelistMintMode)_data.GetU8(offset);
                offset += 1;
                result.Mint = _data.GetPubKey(offset);
                offset += 32;
                result.Presale = _data.GetBool(offset);
                offset += 1;
                if (_data.GetBool(offset++))
                {
                    result.DiscountPrice = _data.GetU64(offset);
                    offset += 8;
                }

                return offset - initialOffset;
            }
        }

        public partial class GatekeeperConfig
        {
            public PublicKey GatekeeperNetwork { get; set; }

            public bool ExpireOnUse { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(GatekeeperNetwork, offset);
                offset += 32;
                _data.WriteBool(ExpireOnUse, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out GatekeeperConfig result)
            {
                int offset = initialOffset;
                result = new GatekeeperConfig();
                result.GatekeeperNetwork = _data.GetPubKey(offset);
                offset += 32;
                result.ExpireOnUse = _data.GetBool(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }

        public enum EndSettingType : byte
        {
            Date,
            Amount
        }

        public enum WhitelistMintMode : byte
        {
            BurnEveryTime,
            NeverBurn
        }
    }

    public partial class CandyMachineClient : TransactionalBaseClient<CandyMachineErrorKind>
    {
        public CandyMachineClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CandyMachine.Accounts.CandyMachine>>> GetCandyMachinesAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = CandyMachine.Accounts.CandyMachine.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CandyMachine.Accounts.CandyMachine>>(res);
            List<CandyMachine.Accounts.CandyMachine> resultingAccounts = new List<CandyMachine.Accounts.CandyMachine>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => CandyMachine.Accounts.CandyMachine.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CandyMachine.Accounts.CandyMachine>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CollectionPDA>>> GetCollectionPDAsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = CollectionPDA.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CollectionPDA>>(res);
            List<CollectionPDA> resultingAccounts = new List<CollectionPDA>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => CollectionPDA.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<CollectionPDA>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<FreezePDA>>> GetFreezePDAsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = FreezePDA.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<FreezePDA>>(res);
            List<FreezePDA> resultingAccounts = new List<FreezePDA>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => FreezePDA.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<FreezePDA>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<CandyMachine.Accounts.CandyMachine>> GetCandyMachineAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<CandyMachine.Accounts.CandyMachine>(res);
            var resultingAccount = CandyMachine.Accounts.CandyMachine.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<CandyMachine.Accounts.CandyMachine>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<CollectionPDA>> GetCollectionPDAAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<CollectionPDA>(res);
            var resultingAccount = CollectionPDA.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<CollectionPDA>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<FreezePDA>> GetFreezePDAAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<FreezePDA>(res);
            var resultingAccount = FreezePDA.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<FreezePDA>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeCandyMachineAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, CandyMachine.Accounts.CandyMachine> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                CandyMachine.Accounts.CandyMachine parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = CandyMachine.Accounts.CandyMachine.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeCollectionPDAAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, CollectionPDA> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                CollectionPDA parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = CollectionPDA.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeFreezePDAAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, FreezePDA> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                FreezePDA parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = FreezePDA.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeCandyMachineAsync(InitializeCandyMachineAccounts accounts, CandyMachineData data, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.InitializeCandyMachine(accounts, data, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateCandyMachineAsync(UpdateCandyMachineAccounts accounts, CandyMachineData data, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.UpdateCandyMachine(accounts, data, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateAuthorityAsync(UpdateAuthorityAccounts accounts, PublicKey newAuthority, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.UpdateAuthority(accounts, newAuthority, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAddConfigLinesAsync(AddConfigLinesAccounts accounts, uint index, ConfigLine[] configLines, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.AddConfigLines(accounts, index, configLines, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSetCollectionAsync(SetCollectionAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.SetCollection(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendRemoveCollectionAsync(RemoveCollectionAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.RemoveCollection(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMintNftAsync(MintNftAccounts accounts, byte creatorBump, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.MintNft(accounts, creatorBump, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSetCollectionDuringMintAsync(SetCollectionDuringMintAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.SetCollectionDuringMint(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendWithdrawFundsAsync(WithdrawFundsAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.WithdrawFunds(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSetFreezeAsync(SetFreezeAccounts accounts, long freezeTime, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.SetFreeze(accounts, freezeTime, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendRemoveFreezeAsync(RemoveFreezeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.RemoveFreeze(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendThawNftAsync(ThawNftAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.ThawNft(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUnlockFundsAsync(UnlockFundsAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.CandyMachineProgram.UnlockFunds(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<CandyMachineErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<CandyMachineErrorKind>>{{6000U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IncorrectOwner, "Account does not have correct owner!")}, {6001U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.Uninitialized, "Account is not initialized!")}, {6002U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MintMismatch, "Mint Mismatch!")}, {6003U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IndexGreaterThanLength, "Index greater than length!")}, {6004U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NumericalOverflowError, "Numerical overflow error!")}, {6005U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.TooManyCreators, "Can only provide up to 4 creators to candy machine (because candy machine is one)!")}, {6006U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.UuidMustBeExactly6Length, "Uuid must be exactly of 6 length")}, {6007U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NotEnoughTokens, "Not enough tokens to pay for this minting")}, {6008U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NotEnoughSOL, "Not enough SOL to pay for this minting")}, {6009U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.TokenTransferFailed, "Token transfer failed")}, {6010U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CandyMachineEmpty, "Candy machine is empty!")}, {6011U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CandyMachineNotLive, "Candy machine is not live!")}, {6012U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.HiddenSettingsConfigsDoNotHaveConfigLines, "Configs that are using hidden uris do not have config lines, they have a single hash representing hashed order")}, {6013U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CannotChangeNumberOfLines, "Cannot change number of lines unless is a hidden config")}, {6014U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.DerivedKeyInvalid, "Derived key invalid")}, {6015U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.PublicKeyMismatch, "Public key mismatch")}, {6016U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoWhitelistToken, "No whitelist token present")}, {6017U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.TokenBurnFailed, "Token burn failed")}, {6018U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.GatewayAppMissing, "Missing gateway app when required")}, {6019U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.GatewayTokenMissing, "Missing gateway token when required")}, {6020U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.GatewayTokenExpireTimeInvalid, "Invalid gateway token expire time")}, {6021U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NetworkExpireFeatureMissing, "Missing gateway network expire feature when required")}, {6022U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CannotFindUsableConfigLine, "Unable to find an unused config line near your random number index")}, {6023U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.InvalidString, "Invalid string")}, {6024U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.SuspiciousTransaction, "Suspicious transaction detected")}, {6025U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CannotSwitchToHiddenSettings, "Cannot Switch to Hidden Settings after items available is greater than 0")}, {6026U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IncorrectSlotHashesPubkey, "Incorrect SlotHashes PubKey")}, {6027U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IncorrectCollectionAuthority, "Incorrect collection NFT authority")}, {6028U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MismatchedCollectionPDA, "Collection PDA address is invalid")}, {6029U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MismatchedCollectionMint, "Provided mint account doesn't match collection PDA mint")}, {6030U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.SlotHashesEmpty, "Slot hashes Sysvar is empty")}, {6031U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MetadataAccountMustBeEmpty, "The metadata account has data in it, and this must be empty to mint a new NFT")}, {6032U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MissingSetCollectionDuringMint, "Missing set collection during mint IX for Candy Machine with collection set")}, {6033U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoChangingCollectionDuringMint, "Can't change collection settings after items have begun to be minted")}, {6034U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CandyCollectionRequiresRetainAuthority, "Retain authority must be true for Candy Machines with a collection set")}, {6035U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.GatewayProgramError, "Error within Gateway program")}, {6036U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoChangingFreezeDuringMint, "Can't change freeze settings after items have begun to be minted. You can only disable.")}, {6037U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoChangingAuthorityWithCollection, "Can't change authority while collection is enabled. Disable collection first.")}, {6038U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoChangingTokenWithFreeze, "Can't change token while freeze is enabled. Disable freeze first.")}, {6039U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.InvalidThawNft, "Cannot thaw NFT unless all NFTs are minted or Candy Machine authority enables thawing")}, {6040U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IncorrectRemainingAccountsLen, "The number of remaining accounts passed in doesn't match the Candy Machine settings")}, {6041U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MissingFreezeAta, "FreezePDA ATA needs to be passed in if token mint is enabled.")}, {6042U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.IncorrectFreezeAta, "Incorrect freeze ATA address.")}, {6043U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.FreezePDAMismatch, "FreezePDA doesn't belong to this Candy Machine.")}, {6044U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.EnteredFreezeIsMoreThanMaxFreeze, "Freeze time can't be longer than MAX_FREEZE_TIME.")}, {6045U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoWithdrawWithFreeze, "Can't withdraw Candy Machine while freeze is active. Disable freeze first.")}, {6046U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoWithdrawWithFrozenFunds, "Can't withdraw Candy Machine while frozen funds need to be redeemed. Unlock funds first.")}, {6047U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.MissingRemoveFreezeTokenAccounts, "Missing required remaining accounts for remove_freeze with token mint.")}, {6048U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.InvalidFreezeWithdrawTokenAddress, "Can't withdraw SPL Token from freeze PDA into itself")}, {6049U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.NoUnlockWithNFTsStillFrozen, "Can't unlock funds while NFTs are still frozen. Run thaw on all NFTs first.")}, {6050U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.SizedCollectionMetadataMustBeMutable, "Setting a sized collection requires the collection metadata to be mutable.")}, {6051U, new ProgramError<CandyMachineErrorKind>(CandyMachineErrorKind.CannotSwitchFromHiddenSettings, "Cannot remove Hidden Settings.")}, };
        }
    }

    namespace Program
    {
        public class InitializeCandyMachineAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }
        }

        public class UpdateCandyMachineAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }
        }

        public class UpdateAuthorityAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Wallet { get; set; }
        }

        public class AddConfigLinesAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class SetCollectionAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CollectionPda { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey Edition { get; set; }

            public PublicKey CollectionAuthorityRecord { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }
        }

        public class RemoveCollectionAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CollectionPda { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey CollectionAuthorityRecord { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }
        }

        public class MintNftAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey CandyMachineCreator { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey MintAuthority { get; set; }

            public PublicKey UpdateAuthority { get; set; }

            public PublicKey MasterEdition { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey Rent { get; set; }

            public PublicKey Clock { get; set; }

            public PublicKey RecentBlockhashes { get; set; }

            public PublicKey InstructionSysvarAccount { get; set; }
        }

        public class SetCollectionDuringMintAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Metadata { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey CollectionPda { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }

            public PublicKey Instructions { get; set; }

            public PublicKey CollectionMint { get; set; }

            public PublicKey CollectionMetadata { get; set; }

            public PublicKey CollectionMasterEdition { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CollectionAuthorityRecord { get; set; }
        }

        public class WithdrawFundsAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class SetFreezeAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey FreezePda { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class RemoveFreezeAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey FreezePda { get; set; }
        }

        public class ThawNftAccounts
        {
            public PublicKey FreezePda { get; set; }

            public PublicKey CandyMachine { get; set; }

            public PublicKey TokenAccount { get; set; }

            public PublicKey Owner { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey Edition { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey TokenMetadataProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UnlockFundsAccounts
        {
            public PublicKey CandyMachine { get; set; }

            public PublicKey Wallet { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey FreezePda { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class CandyMachineProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeCandyMachine(InitializeCandyMachineAccounts accounts, CandyMachineData data, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Wallet, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Rent, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9002738739736709518UL, offset);
                offset += 8;
                offset += data.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateCandyMachine(UpdateCandyMachineAccounts accounts, CandyMachineData data, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Wallet, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17255211928133630963UL, offset);
                offset += 8;
                offset += data.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateAuthority(UpdateAuthorityAccounts accounts, PublicKey newAuthority, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Wallet, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(6409549798474526240UL, offset);
                offset += 8;
                if (newAuthority != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WritePubKey(newAuthority, offset);
                    offset += 32;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddConfigLines(AddConfigLinesAccounts accounts, uint index, ConfigLine[] configLines, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7670484038798291679UL, offset);
                offset += 8;
                _data.WriteU32(index, offset);
                offset += 4;
                _data.WriteS32(configLines.Length, offset);
                offset += 4;
                foreach (var configLinesElement in configLines)
                {
                    offset += configLinesElement.Serialize(_data, offset);
                }

                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetCollection(SetCollectionAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionPda, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Rent, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Metadata, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Edition, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionAuthorityRecord, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16085651328043253440UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RemoveCollection(RemoveCollectionAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionPda, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Metadata, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionAuthorityRecord, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11539590303428785375UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MintNft(MintNftAccounts accounts, byte creatorBump, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.CandyMachineCreator, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Wallet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Metadata, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MintAuthority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.UpdateAuthority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MasterEdition, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Rent, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Clock, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.RecentBlockhashes, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.InstructionSysvarAccount, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18096548587977980371UL, offset);
                offset += 8;
                _data.WriteU8(creatorBump, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetCollectionDuringMint(SetCollectionDuringMintAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Metadata, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionPda, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Instructions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.CollectionMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CollectionMetadata, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.CollectionMasterEdition, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.CollectionAuthorityRecord, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4430802569245757799UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction WithdrawFunds(WithdrawFundsAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15665806283886109937UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetFreeze(SetFreezeAccounts accounts, long freezeTime, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.FreezePda, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16796896651748659402UL, offset);
                offset += 8;
                _data.WriteS64(freezeTime, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RemoveFreeze(RemoveFreezeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.FreezePda, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18099470480020919297UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ThawNft(ThawNftAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.FreezePda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Owner, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Edition, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMetadataProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13204561446405549148UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UnlockFunds(UnlockFundsAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.CandyMachine, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Wallet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.FreezePda, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3170313745533532079UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}