using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace SEAS
{
    public class SEAS : SmartContract
    {
        //Token Settings
        public static string Name() => "Shares of SEA";
        public static string Symbol() => "SEAS";
        public static readonly byte[] Owner = "AUkVH4k8gPowAEpvQVAmNEkriX96CrKzk9".ToScriptHash();
        public static byte Decimals() => 0;
        private const ulong factor = 1; //decided by Decimals()
        private const uint bonus_start_height = 2000000;
        private const uint bonus_end_height = 17000000;//bonus_start_height + 1500 0000
        private static readonly byte[] SEAC_CONTRACT = "SEAC".AsByteArray();
        public delegate object NEP5Contract(string method, object[] args);
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;

        //ICO Settings
        private static readonly byte[] BYTE1 = { 0 };
        private static readonly byte[] BYTE2 = { 0, 0 };
        private static readonly byte[] BYTE3 = { 0, 0, 0 };
        private static readonly byte[] BYTE4 = { 0, 0, 0, 0 };
        private static readonly byte[] BYTE5 = { 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE6 = { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE7 = { 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE8 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] GOD = { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
        private static readonly byte[] gseas_asset_id = { 234, 81, 101, 147, 88, 115, 123, 111, 43, 52, 78, 224, 151, 17, 212, 56, 208, 61, 117, 214, 234, 127, 72, 121, 32, 36, 165, 82, 142, 160, 183, 187 };//testnet
        //private static readonly byte[] gseas_asset_id = { 212, 79, 241, 19, 187, 199, 206, 203, 163, 223, 13, 211, 61, 76, 202, 195, 16, 193, 103, 15, 214, 81, 150, 19, 136, 242, 73, 194, 107, 99, 233, 48 };//mainnet
        private const ulong total_amount = 100000000 * factor; // total token amount
        public static BigInteger TotalSupply() => total_amount;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

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
                    byte[] callscript = ExecutionEngine.CallingScriptHash;
                    if (from != callscript) return false;
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
            return true;
        }

        private static BigInteger GetBalance(byte[] info)
        {
            if (info == null)
            {
                throw new InvalidOperationException("The parameter address SHOULD NOT be null.");
            }
            if (info.Length != 16)
            {
                throw new InvalidOperationException("The parameter info's length SHOULD = 16.");
            }
            return info.Range(0,8).AsBigInteger();
        }

        private static BigInteger GetHeight(byte[] info)
        {
            if (info == null)
            {
                throw new InvalidOperationException("The parameter address SHOULD NOT be null.");
            }
            if (info.Length != 16)
            {
                throw new InvalidOperationException("The parameter info's length SHOULD = 16.");
            }
            return info.Range(8, 8).AsBigInteger();
        }

        private static byte[] GetAssetInfo(byte[] address)
        {
            if (address.Length != 20) return null;
            byte[] assetInfo = Storage.Get(Storage.CurrentContext, address); //0.1
            if (assetInfo.Length != 16) return null;
            return assetInfo;
        }

        private static byte[] GetFixed8(BigInteger value)
        {
            byte[] tmp = value.AsByteArray();
            if (tmp.Length > 8)
            {
                throw new InvalidOperationException("The parameter value's length SHOULD <= 8.");
            }
            if (tmp.Length == 7)
            {
                byte[] tmp8 = tmp.Concat(BYTE1);
                return tmp8;
            }
            if (tmp.Length == 6)
            {
                byte[] tmp8 = tmp.Concat(BYTE2);
                return tmp8;
            }
            if (tmp.Length == 5)
            {
                byte[] tmp8 = tmp.Concat(BYTE3);
                return tmp8;
            }
            if (tmp.Length == 4)
            {
                byte[] tmp8 = tmp.Concat(BYTE4);
                return tmp8;
            }
            if (tmp.Length == 3)
            {
                byte[] tmp8 = tmp.Concat(BYTE5);
                return tmp8;
            }
            if (tmp.Length == 2)
            {
                byte[] tmp8 = tmp.Concat(BYTE6);
                return tmp8;
            }
            if (tmp.Length == 1)
            {
                byte[] tmp8 = tmp.Concat(BYTE7);
                return tmp8;
            }
            if (tmp.Length == 0)
            {
                return BYTE8;
            }
            return tmp;
        }
        private static void SetAssetInfo(byte[] address, BigInteger balance, BigInteger height)
        {
            if (balance <= 0)
            {
                Storage.Delete(Storage.CurrentContext, address); //0.1
            }
            else
            {
                byte[] b8 = GetFixed8(balance);
                byte[] h8 = GetFixed8(height);
                byte[] bh16 = b8.Concat(h8);
                Storage.Put(Storage.CurrentContext, address, bh16); //1
            }
        }

        public static bool MintTokens()
        {
            byte[] to = GetSender();
            if (to.Length != 20) return false;
            if (!Runtime.CheckWitness(to)) return false;
            BigInteger token = (BigInteger)GetContributeValue();
            if (token <= 0) return false;

            BigInteger value = token / 100000000;

            byte[] seac_contract = Storage.Get(Storage.CurrentContext, SEAC_CONTRACT);
            if (seac_contract.Length != 20) return false;
            BigInteger current_height = Blockchain.GetHeight();
            BigInteger to_value = 0;
            BigInteger to_bonus = 0;
            BigInteger to_bonus_height = 0;

            to_bonus = ComputeBonus(current_height, value, 0);
            byte[] to_result = GetAssetInfo(to);
            if (to_result != null)
            {
                to_value = GetBalance(to_result);
                to_bonus_height = GetHeight(to_result);
            }
            to_bonus += ComputeBonus(current_height, to_value, to_bonus_height);

            if (to_bonus > 0)
            {
                bool result = ShareBonus(GOD, 0, to, to_bonus, seac_contract);
                if (!result) return false;
            }

            BigInteger new_to_value = to_value + value;
            SetAssetInfo(to, new_to_value, current_height);

            Transferred(GOD, to, value);
            return true;
        }


        // function that is always called when someone wants to transfer tokens.
        // 流转token调用
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (from.Length != 20) return false;
            if (to.Length != 20) return false;
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            byte[] seac_contract = Storage.Get(Storage.CurrentContext, SEAC_CONTRACT);
            if (seac_contract.Length != 20) return false;
            BigInteger current_height = Blockchain.GetHeight();
            BigInteger from_value = 0;
            BigInteger from_bonus = 0;
            BigInteger from_bonus_height = 0;
            BigInteger to_value = 0;
            BigInteger to_bonus = 0;
            BigInteger to_bonus_height = 0;

            byte[] from_result = GetAssetInfo(from);
            if (from_result == null) return false;
            from_value = GetBalance(from_result);
            from_bonus_height = GetHeight(from_result);
            if (from_value < value) return false;
            from_bonus = ComputeBonus(current_height, from_value, from_bonus_height);

            byte[] to_result = GetAssetInfo(to);
            if (to_result != null)
            {
                to_value = GetBalance(to_result);
                to_bonus_height = GetHeight(to_result);
            }

            if (from != to) to_bonus = ComputeBonus(current_height, to_value, to_bonus_height);

            if (from_bonus > 0 || to_bonus > 0)
            {
                bool result = ShareBonus(from, from_bonus, to, to_bonus, seac_contract);
                if (!result) return false;
            }

            BigInteger new_from_value = from_value - value;
            SetAssetInfo(from, new_from_value, current_height);
            if (from != to)
            {
                BigInteger new_to_value = to_value + value;
                SetAssetInfo(to, new_to_value, current_height);
            }

            Transferred(from, to, value);
            return true;
        }

        public static BigInteger ComputeBonus(BigInteger current_height, BigInteger value, BigInteger start_height)
        {
            if (value <= 0) return 0;
            if (current_height <= bonus_start_height) return 0;
            if (start_height < bonus_start_height) start_height = bonus_start_height;
            if (start_height >= bonus_end_height) return 0;
            if (current_height > bonus_end_height) current_height = bonus_end_height;
            if (current_height <= start_height) return 0;
            BigInteger b = (current_height - start_height) * 6 * value;
            return b;
        }

        public static bool ShareBonus(byte[] from, BigInteger from_bonus, byte[] to, BigInteger to_bonus, byte[] seac_contract)
        {
            var bonus_args = new object[] { from, from_bonus, to, to_bonus };
            var contract = (NEP5Contract)seac_contract.ToDelegate();
            bool result = (bool)contract("bonus", bonus_args);
            return result;
        }

        // get the account balance of another account with address
        // 根据地址获取token的余额
        public static BigInteger BalanceOf(byte[] address)
        {
            byte[] result = GetAssetInfo(address);
            if (result == null) return 0;
            return GetBalance(result);
        }

        // check whether asset is gseas and get sender script hash
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
