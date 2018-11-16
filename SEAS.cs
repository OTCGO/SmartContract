using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class SEAS : Framework.SmartContract
    {
        //Token Settings
        public static string Name() => "Share of SEA";
        public static string Symbol() => "SEAS";
        public static readonly byte[] Owner = "AUkVH4k8gPowAEpvQVAmNEkriX96CrKzk9".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;
        private const uint bonus_start_height = 2000000;
        private const uint bonus_end_height = 17000000;//bonus_start_height + 1500 0000
        private static readonly byte[] BONUS_BASIC = "BS-".AsByteArray();
        private static readonly byte[] SEAC_CONTRACT = "SEAC".AsByteArray();
        public delegate object NEP5Contract(string method, object[] args);
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;

        //ICO Settings
        private static readonly byte[] gseas_asset_id = { 234, 81, 101, 147, 88, 115, 123, 111, 43, 52, 78, 224, 151, 17, 212, 56, 208, 61, 117, 214, 234, 127, 72, 121, 32, 36, 165, 82, 142, 160, 183, 187 };//testnet
        //private static readonly byte[] gseas_asset_id = { 212, 79, 241, 19, 187, 199, 206, 203, 163, 223, 13, 211, 61, 76, 202, 195, 16, 193, 103, 15, 214, 81, 150, 19, 136, 242, 73, 194, 107, 99, 233, 48 };//mainnet
        private const ulong total_amount = 100000000 * factor; // total token amount

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("bonusshare")]
        public static event Action<byte[], BigInteger> BonusShared;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                var type = tx.Type;
                if (type != INVOCATION_TRANSACTION_TYPE) return false;
                bool result = CheckSender();
                if (!result) return false;
                ulong contribute_value = GetContributeValue();
                if (contribute_value == 0) return false;
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy")
                {
                    if (args.Length != 1) return false;
                    byte[] contract = (byte[])args[0];
                    return Deploy(contract);
                }
                if (operation == "mintTokens") return MintTokens();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "decimals") return Decimals();
            }
            return false;
        }

        // initialization parameters, only once
        // 初始化参数
        public static bool Deploy(byte[] contract)
        {
            if (contract.Length != 20) return false;
            if (!Runtime.CheckWitness(Owner)) return false;
            byte[] seac_contract = Storage.Get(Storage.CurrentContext, SEAC_CONTRACT);
            if (seac_contract.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, SEAC_CONTRACT, contract);
            Storage.Put(Storage.CurrentContext, "totalSupply", 0);
            return true;
        }

        public static bool MintTokens()
        {
            byte[] sender = GetSender();
            if (sender.Length == 0)
            {
                return false;
            }
            ulong contribute_value = GetContributeValue();
            ulong token = contribute_value;
            if (token == 0)
            {
                return false;
            }
            BigInteger balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
            Storage.Put(Storage.CurrentContext, sender, token + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", token + totalSupply);
            Transferred(null, sender, token);
            return true;
        }

        // get the total token supply
        // 获取已发行token总量
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        // 流转token调用
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (from.Length != 20) return false;
            if (to.Length != 20) return false;
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;

            BigInteger current_height = Blockchain.GetHeight();

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            BigInteger from_bonus = ComputeBonus(current_height, from, from_value);

            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            BigInteger to_bonus = 0;
            if (to_value > 0 && from != to) to_bonus = ComputeBonus(current_height, to, to_value);

            if (from_bonus > 0)
            {
                bool result = ShareBonus(from, from_bonus);
                if (!result) return false;
                BonusShared(from, from_bonus);
            }

            if (to_bonus > 0)
            {
                bool result = ShareBonus(to, to_bonus);
                if (!result) return false;
                BonusShared(to, to_bonus);
            }

            Storage.Put(Storage.CurrentContext, BONUS_BASIC.Concat(from), current_height);
            Storage.Put(Storage.CurrentContext, BONUS_BASIC.Concat(to), current_height);

            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        public static BigInteger ComputeBonus(BigInteger current_height, byte[] addr, BigInteger value)
        {
            if (current_height <= bonus_start_height) return 0;
            BigInteger start_height = 0;
            byte[] start_bonus = Storage.Get(Storage.CurrentContext, BONUS_BASIC.Concat(addr));
            if (start_bonus.Length != 0) start_height = start_bonus.AsBigInteger();
            if (start_height < bonus_start_height) start_height = bonus_start_height;
            if (start_height >= bonus_end_height) return 0;
            if (current_height > bonus_end_height) current_height = bonus_end_height;
            BigInteger b = (current_height - start_height) * 6 * (value - value % 100000000) / 100000000;
            return b;
        }

        public static bool ShareBonus(byte[] addr, BigInteger value)
        {
            byte[] seac_contract = Storage.Get(Storage.CurrentContext, SEAC_CONTRACT);
            var bonus_args = new object[] { addr, value };
            var contract = (NEP5Contract)seac_contract.ToDelegate();
            bool result = (bool)contract("bonus", bonus_args);
            return result;
        }

        // get the account balance of another account with address
        // 根据地址获取token的余额
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == gseas_asset_id) return output.ScriptHash;
            }
            return new byte[] { };
        }

        private static bool CheckSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            ulong count = 0;
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == gseas_asset_id)
                {
                    if (output.ScriptHash == GetReceiver()) return false;
                    count += 1;
                }
            }
            if (count != 1) return false;
            return true;
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // get all you contribute global seas amount
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // 获取转入智能合约地址的申一股总量
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == gseas_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }
    }
}
