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
using Lumberjack;
using Lumberjack.Program;
using Lumberjack.Errors;
using Lumberjack.Accounts;
using Lumberjack.Types;

namespace Lumberjack
{
    namespace Accounts
    {
        public partial class BoardAccount
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17376089564643394824UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{8, 5, 241, 133, 101, 69, 36, 241};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "2LqSnViaMd2";
            public TileData[][] Data { get; set; }

            public ulong ActionId { get; set; }

            public ulong Wood { get; set; }

            public ulong Stone { get; set; }

            public ulong DammLevel { get; set; }

            public bool Initialized { get; set; }

            public bool EvilWon { get; set; }

            public bool GoodWon { get; set; }

            public static BoardAccount Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                BoardAccount result = new BoardAccount();
                result.Data = new TileData[10][];
                for (uint resultDataIdx = 0; resultDataIdx < 10; resultDataIdx++)
                {
                    result.Data[resultDataIdx] = new TileData[10];
                    for (uint resultDataresultDataIdxIdx = 0; resultDataresultDataIdxIdx < 10; resultDataresultDataIdxIdx++)
                    {
                        offset += TileData.Deserialize(_data, offset, out var resultDataresultDataIdxresultDataresultDataIdxIdx);
                        result.Data[resultDataIdx][resultDataresultDataIdxIdx] = resultDataresultDataIdxresultDataresultDataIdxIdx;
                    }
                }

                result.ActionId = _data.GetU64(offset);
                offset += 8;
                result.Wood = _data.GetU64(offset);
                offset += 8;
                result.Stone = _data.GetU64(offset);
                offset += 8;
                result.DammLevel = _data.GetU64(offset);
                offset += 8;
                result.Initialized = _data.GetBool(offset);
                offset += 1;
                result.EvilWon = _data.GetBool(offset);
                offset += 1;
                result.GoodWon = _data.GetBool(offset);
                offset += 1;
                return result;
            }
        }

        public partial class GameActionHistory
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 8873408368832920456UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{136, 187, 67, 235, 229, 173, 36, 123};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "PsU5aGMBQbU";
            public ulong IdCounter { get; set; }

            public ulong ActionIndex { get; set; }

            public GameAction[] GameActions { get; set; }

            public static GameActionHistory Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                GameActionHistory result = new GameActionHistory();
                result.IdCounter = _data.GetU64(offset);
                offset += 8;
                result.ActionIndex = _data.GetU64(offset);
                offset += 8;
                result.GameActions = new GameAction[30];
                for (uint resultGameActionsIdx = 0; resultGameActionsIdx < 30; resultGameActionsIdx++)
                {
                    offset += GameAction.Deserialize(_data, offset, out var resultGameActionsresultGameActionsIdx);
                    result.GameActions[resultGameActionsIdx] = resultGameActionsresultGameActionsIdx;
                }

                return result;
            }
        }

        public partial class PlayerData
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 9264901878634267077UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{197, 65, 216, 202, 43, 139, 147, 128};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "ZzeEvyxXcpF";
            public PublicKey Authority { get; set; }

            public PublicKey Avatar { get; set; }

            public string Name { get; set; }

            public byte Level { get; set; }

            public ulong Xp { get; set; }

            public ulong Energy { get; set; }

            public long LastLogin { get; set; }

            public static PlayerData Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerData result = new PlayerData();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Avatar = _data.GetPubKey(offset);
                offset += 32;
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                result.Level = _data.GetU8(offset);
                offset += 1;
                result.Xp = _data.GetU64(offset);
                offset += 8;
                result.Energy = _data.GetU64(offset);
                offset += 8;
                result.LastLogin = _data.GetS64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum LumberjackErrorKind : uint
        {
            NotEnoughEnergy = 6000U,
            TileAlreadyOccupied = 6001U,
            TileCantBeUpgraded = 6002U,
            TileHasNoTree = 6003U,
            WrongAuthority = 6004U,
            TileCantBeCollected = 6005U,
            ProductionNotReadyYet = 6006U,
            BuildingTypeNotCollectable = 6007U,
            NotEnoughStone = 6008U,
            NotEnoughWood = 6009U
        }
    }

    namespace Types
    {
        public partial class TileData
        {
            public byte BuildingType { get; set; }

            public uint BuildingLevel { get; set; }

            public PublicKey BuildingOwner { get; set; }

            public long BuildingStartTime { get; set; }

            public long BuildingStartUpgradeTime { get; set; }

            public long BuildingStartCollectTime { get; set; }

            public long BuildingHealth { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8(BuildingType, offset);
                offset += 1;
                _data.WriteU32(BuildingLevel, offset);
                offset += 4;
                _data.WritePubKey(BuildingOwner, offset);
                offset += 32;
                _data.WriteS64(BuildingStartTime, offset);
                offset += 8;
                _data.WriteS64(BuildingStartUpgradeTime, offset);
                offset += 8;
                _data.WriteS64(BuildingStartCollectTime, offset);
                offset += 8;
                _data.WriteS64(BuildingHealth, offset);
                offset += 8;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out TileData result)
            {
                int offset = initialOffset;
                result = new TileData();
                result.BuildingType = _data.GetU8(offset);
                offset += 1;
                result.BuildingLevel = _data.GetU32(offset);
                offset += 4;
                result.BuildingOwner = _data.GetPubKey(offset);
                offset += 32;
                result.BuildingStartTime = _data.GetS64(offset);
                offset += 8;
                result.BuildingStartUpgradeTime = _data.GetS64(offset);
                offset += 8;
                result.BuildingStartCollectTime = _data.GetS64(offset);
                offset += 8;
                result.BuildingHealth = _data.GetS64(offset);
                offset += 8;
                return offset - initialOffset;
            }
        }

        public partial class GameAction
        {
            public ulong ActionId { get; set; }

            public byte ActionType { get; set; }

            public byte X { get; set; }

            public byte Y { get; set; }

            public TileData Tile { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Avatar { get; set; }

            public ulong Amount { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU64(ActionId, offset);
                offset += 8;
                _data.WriteU8(ActionType, offset);
                offset += 1;
                _data.WriteU8(X, offset);
                offset += 1;
                _data.WriteU8(Y, offset);
                offset += 1;
                offset += Tile.Serialize(_data, offset);
                _data.WritePubKey(Player, offset);
                offset += 32;
                _data.WritePubKey(Avatar, offset);
                offset += 32;
                _data.WriteU64(Amount, offset);
                offset += 8;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out GameAction result)
            {
                int offset = initialOffset;
                result = new GameAction();
                result.ActionId = _data.GetU64(offset);
                offset += 8;
                result.ActionType = _data.GetU8(offset);
                offset += 1;
                result.X = _data.GetU8(offset);
                offset += 1;
                result.Y = _data.GetU8(offset);
                offset += 1;
                offset += TileData.Deserialize(_data, offset, out var resultTile);
                result.Tile = resultTile;
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.Avatar = _data.GetPubKey(offset);
                offset += 32;
                result.Amount = _data.GetU64(offset);
                offset += 8;
                return offset - initialOffset;
            }
        }
    }

    public partial class LumberjackClient : TransactionalBaseClient<LumberjackErrorKind>
    {
        public LumberjackClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BoardAccount>>> GetBoardAccountsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = BoardAccount.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BoardAccount>>(res);
            List<BoardAccount> resultingAccounts = new List<BoardAccount>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => BoardAccount.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BoardAccount>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameActionHistory>>> GetGameActionHistorysAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = GameActionHistory.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameActionHistory>>(res);
            List<GameActionHistory> resultingAccounts = new List<GameActionHistory>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => GameActionHistory.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameActionHistory>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>> GetPlayerDatasAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerData.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>(res);
            List<PlayerData> resultingAccounts = new List<PlayerData>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerData.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<BoardAccount>> GetBoardAccountAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<BoardAccount>(res);
            var resultingAccount = BoardAccount.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<BoardAccount>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<GameActionHistory>> GetGameActionHistoryAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<GameActionHistory>(res);
            var resultingAccount = GameActionHistory.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<GameActionHistory>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>> GetPlayerDataAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>(res);
            var resultingAccount = PlayerData.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeBoardAccountAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, BoardAccount> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                BoardAccount parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = BoardAccount.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeGameActionHistoryAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, GameActionHistory> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                GameActionHistory parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = GameActionHistory.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribePlayerDataAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerData> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerData parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerData.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitPlayerAsync(InitPlayerAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.InitPlayer(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendRestartGameAsync(RestartGameAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.RestartGame(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendChopTreeAsync(ChopTreeAccounts accounts, byte x, byte y, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.ChopTree(accounts, x, y, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendBuildAsync(BuildAccounts accounts, byte x, byte y, byte buildingType, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.Build(accounts, x, y, buildingType, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpgradeAsync(UpgradeAccounts accounts, byte x, byte y, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.Upgrade(accounts, x, y, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendCollectAsync(CollectAccounts accounts, byte x, byte y, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.Collect(accounts, x, y, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateAsync(UpdateAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.Update(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendRefillEnergyAsync(RefillEnergyAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.LumberjackProgram.RefillEnergy(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<LumberjackErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<LumberjackErrorKind>>{{6000U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.NotEnoughEnergy, "Not enough energy")}, {6001U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.TileAlreadyOccupied, "Tile Already Occupied")}, {6002U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.TileCantBeUpgraded, "Tile cant be upgraded")}, {6003U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.TileHasNoTree, "Tile has no tree")}, {6004U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.WrongAuthority, "Wrong Authority")}, {6005U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.TileCantBeCollected, "Tile cant be collected")}, {6006U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.ProductionNotReadyYet, "Production not ready yet")}, {6007U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.BuildingTypeNotCollectable, "Building type not collectable")}, {6008U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.NotEnoughStone, "Not enough stone")}, {6009U, new ProgramError<LumberjackErrorKind>(LumberjackErrorKind.NotEnoughWood, "Not enough wood")}, };
        }
    }

    namespace Program
    {
        public class InitPlayerAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class RestartGameAccounts
        {
            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ChopTreeAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Avatar { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }
        }

        public class BuildAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Avatar { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }
        }

        public class UpgradeAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Avatar { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }
        }

        public class CollectAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Avatar { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }
        }

        public class UpdateAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Board { get; set; }

            public PublicKey GameActions { get; set; }

            public PublicKey Avatar { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }
        }

        public class RefillEnergyAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class LumberjackProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitPlayer(InitPlayerAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4819994211046333298UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RestartGame(RestartGameAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(10140096924326872336UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ChopTree(ChopTreeAccounts accounts, byte x, byte y, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Avatar, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2027946759707441272UL, offset);
                offset += 8;
                _data.WriteU8(x, offset);
                offset += 1;
                _data.WriteU8(y, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Build(BuildAccounts accounts, byte x, byte y, byte buildingType, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Avatar, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1817356094846029497UL, offset);
                offset += 8;
                _data.WriteU8(x, offset);
                offset += 1;
                _data.WriteU8(y, offset);
                offset += 1;
                _data.WriteU8(buildingType, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Upgrade(UpgradeAccounts accounts, byte x, byte y, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Avatar, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1920037355368607471UL, offset);
                offset += 8;
                _data.WriteU8(x, offset);
                offset += 1;
                _data.WriteU8(y, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Collect(CollectAccounts accounts, byte x, byte y, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Avatar, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17028780968808427472UL, offset);
                offset += 8;
                _data.WriteU8(x, offset);
                offset += 1;
                _data.WriteU8(y, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Update(UpdateAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Board, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameActions, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Avatar, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9222597562720635099UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RefillEnergy(RefillEnergyAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12683479030383825657UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}