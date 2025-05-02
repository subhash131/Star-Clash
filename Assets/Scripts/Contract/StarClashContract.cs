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
using SonicHunt;
using SonicHunt.Program;
using SonicHunt.Errors;
using SonicHunt.Accounts;

namespace SonicHunt
{
    namespace Accounts
    {
        public partial class User
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17022084798167872927UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{159, 117, 95, 227, 239, 151, 58, 236};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "TfwwBiNJtao";
            public PublicKey Authority { get; set; }

            public ulong Funds { get; set; }

            public string Username { get; set; }

            public static User Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                User result = new User();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Funds = _data.GetU64(offset);
                offset += 8;
                offset += _data.GetBorshString(offset, out var resultUsername);
                result.Username = resultUsername;
                return result;
            }
        }

        public partial class Master
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 16950038599372494248UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{168, 213, 193, 12, 77, 162, 58, 235};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "VEuppfoFnRQ";
            public PublicKey Owner { get; set; }

            public static Master Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Master result = new Master();
                result.Owner = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum SonicHuntErrorKind : uint
        {
            InsufficientFunds = 6000U,
            Unauthorized = 6001U
        }
    }

    public partial class SonicHuntClient : TransactionalBaseClient<SonicHuntErrorKind>
    {
        public SonicHuntClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>> GetUsersAsync(string programAddress, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = User.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>(res);
            List<User> resultingAccounts = new List<User>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => User.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Master>>> GetMastersAsync(string programAddress, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Master.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Master>>(res);
            List<Master> resultingAccounts = new List<Master>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Master.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Master>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<User>> GetUserAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<User>(res);
            var resultingAccount = User.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<User>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Master>> GetMasterAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Master>(res);
            var resultingAccount = Master.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Master>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeUserAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, User> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                User parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = User.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeMasterAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Master> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Master parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Master.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<SonicHuntErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<SonicHuntErrorKind>>{{6000U, new ProgramError<SonicHuntErrorKind>(SonicHuntErrorKind.InsufficientFunds, "Insufficient funds for withdrawal")}, {6001U, new ProgramError<SonicHuntErrorKind>(SonicHuntErrorKind.Unauthorized, "Unauthorized: Only the user's authority can withdraw funds")}, };
        }
    }

    namespace Program
    {
        public class InitMasterAccounts
        {
            public PublicKey Master { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddUserAccounts
        {
            public PublicKey User { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddFundsAccounts
        {
            public PublicKey User { get; set; }

            public PublicKey Master { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Owner { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class WithdrawFundsAccounts
        {
            public PublicKey User { get; set; }

            public PublicKey Master { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey Withdrawer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UpdateResultsAccounts
        {
            public PublicKey User { get; set; }

            public PublicKey Master { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class SonicHuntProgram
        {
            public const string ID = "";
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitMaster(InitMasterAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Master, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1760688535391056296UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddUser(AddUserAccounts accounts, string username, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12735381194343172111UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(username, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddFunds(AddFundsAccounts accounts, ulong funds, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Master, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Owner, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9994343337740266884UL, offset);
                offset += 8;
                _data.WriteU64(funds, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction WithdrawFunds(WithdrawFundsAccounts accounts, ulong amount, PublicKey userAddress, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Master, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Withdrawer, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15665806283886109937UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                _data.WritePubKey(userAddress, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateResults(UpdateResultsAccounts accounts, long value, PublicKey userAddress, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Master, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15410034308892790618UL, offset);
                offset += 8;
                _data.WriteS64(value, offset);
                offset += 8;
                _data.WritePubKey(userAddress, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}