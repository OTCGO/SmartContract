using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class SEAC : Framework.SmartContract
    {
        //Token Settings
        public static string Name() => "Coin of SEA";
        public static string Symbol() => "SEAC";
        public static readonly byte[] Owner = "AUkVH4k8gPowAEpvQVAmNEkriX96CrKzk9".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private static readonly byte[] SEAS_CONTRACT = "SEAS".AsByteArray();
        public delegate object NEP5Contract(string method, object[] args);
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;

        //ICO Settings
        private static readonly byte[] gseac_asset_id = { 193, 234, 114, 196, 147, 225, 180, 208, 206, 208, 202, 129, 101, 209, 8, 45, 157, 22, 150, 60, 219, 26, 192, 111, 225, 22, 231, 202, 220, 64, 52, 225 };//testnet
        //private static readonly byte[] gseac_asset_id = { 63, 166, 52, 213, 43, 133, 21, 182, 236, 78, 146, 203, 95, 72, 194, 43, 76, 11, 5, 92, 99, 76, 169, 18, 35, 221, 194, 182, 153, 62, 46, 165 };//mainnet
        //private const ulong total_amount = 100000000 * factor; // total token amount

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
                if (operation == "bonus"){
                    if (args.Length !=2) return false;
                    byte[] addr = (byte[])args[0];
                    BigInteger value = (BigInteger)args[1];
                    return Bonus(addr, value);
                }
            }
            return false;
        }

        // initialization parameters, only once
        // 初始化参数
        public static bool Deploy(byte[] contract)
        {
            if (contract.Length != 20) return false;
            if (!Runtime.CheckWitness(Owner)) return false;
            byte[] seas_contract = Storage.Get(Storage.CurrentContext, SEAS_CONTRACT);
            if (seas_contract.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, SEAS_CONTRACT, contract);
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

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;

            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();

            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
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
                if (output.AssetId == gseac_asset_id) return output.ScriptHash;
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
                if (output.AssetId == gseac_asset_id)
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
            // 获取转入智能合约地址的申一币总量
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == gseac_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }

        private static bool Bonus(byte[] addr, BigInteger value)
        {
            if (addr.Length != 20) return false;
            if (value <= 0) return false;
            byte[] seas_contract = Storage.Get(Storage.CurrentContext, SEAS_CONTRACT);
            if (seas_contract != ExecutionEngine.CallingScriptHash) return false;
            BigInteger balance = Storage.Get(Storage.CurrentContext, addr).AsBigInteger();
            Storage.Put(Storage.CurrentContext, addr, value + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", value + totalSupply);
            BonusShared(addr, value);
            return true;

        }
    }
}
