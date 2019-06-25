using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class DEX : Framework.SmartContract
    {
        public static readonly byte[] OWNER = "AUkVH4k8gPowAEpvQVAmNEkriX96CrKzk9".ToScriptHash();
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;
        private const int LENGTH_OF_SCRIPTHASH = 20;
        private const int LENGTH_OF_AMOUNT = 8;
        private const int LENGTH_OF_PRICE = 8;
        private static readonly byte[] BYTE1 = { 0 };
        private static readonly byte[] BYTE2 = { 0, 0 };
        private static readonly byte[] BYTE3 = { 0, 0, 0 };
        private static readonly byte[] BYTE4 = { 0, 0, 0, 0 };
        private static readonly byte[] BYTE5 = { 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE6 = { 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE7 = { 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] BYTE8 = { 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] RETURN_PREFIX = "RETURN".AsByteArray();
        private static readonly byte[] STATUS = "STATUS".AsByteArray();
        public delegate object NEP5Contract(string method, object[] args);

        [DisplayName("setstatus")]
        public static event Action<byte[]> StatusSetted;
        [DisplayName("assetsupport")]
        public static event Action<byte[]> AssetSupported;
        [DisplayName("assetunsupport")]
        public static event Action<byte[]> AssetUNSupported;
        [DisplayName("neworder")]
        public static event Action<byte[], byte[], byte[], byte[], BigInteger> NewOrdered;
        [DisplayName("tradeorder")]
        public static event Action<byte[], byte[], BigInteger, BigInteger> OrderTraded;
        [DisplayName("cancelorder")]
        public static event Action<byte[], byte[], byte[], byte[], BigInteger> OrderCanelled;
        [DisplayName("claim")]
        public static event Action<byte[], byte[], BigInteger> Claimed;
        [DisplayName("setreturn")]
        public static event Action<byte[], BigInteger> ReturnSet;
        [DisplayName("return")]
        public static event Action<byte[], BigInteger> Returned;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                byte[] me = GetReceiver();
                Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                var type = tx.Type;
                if (type != INVOCATION_TRANSACTION_TYPE) return false;
                var itx = (InvocationTransaction)tx;

                byte storage_status = GetStatus();

                if (29 == itx.Script.Length) // status
                {
                    if (itx.Script[0] != 0x01) return false;
                    if (itx.Script.Range(2, 7) != new byte[] { 0x51, 0xc1, 0x03, 0x73, 0x65, 0x74, 0x67 }) return false;
                    if (itx.Script.Range(9, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte status = itx.Script[1];
                    if (status != 0x59 || status != 0x4e) return false;
                    if (storage_status == status) return false;
                    return true;
                }
                if (storage_status != 0x59) return false;
                if (52 == itx.Script.Length) // support
                {
                    if (itx.Script[0] != 0x14) return false;
                    if (itx.Script.Range(21, 11) != new byte[] { 0x51, 0xc1, 0x07, 0x73, 0x75, 0x70, 0x70, 0x6f, 0x72, 0x74, 0x67 }) return false;
                    if (itx.Script.Range(32, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte[] asset = itx.Script.Range(1, LENGTH_OF_SCRIPTHASH);
                    byte[] old = GetAssetInfo(asset);
                    if (old == null) return true;
                    return false;
                }
                if (54 == itx.Script.Length) // unsupport
                {
                    if (itx.Script[0] != 0x14) return false;
                    if (itx.Script.Range(21, 13) != new byte[] { 0x51, 0xc1, 0x09, 0x75, 0x6e, 0x73, 0x75, 0x70, 0x70, 0x6f, 0x72, 0x74, 0x67 }) return false;
                    if (itx.Script.Range(34, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte[] asset = itx.Script.Range(1, LENGTH_OF_SCRIPTHASH);
                    byte[] ai = GetAssetInfo(asset);
                    if (ai != null) return true;
                    return false;
                }
                if (140 == itx.Script.Length) // new
                {
                    if (itx.Script[0] != 0x08) return false;
                    if (itx.Script[9] != 0x14) return false;
                    if (itx.Script.Range(30, 7) != new byte[] { 0x52, 0xc1, 0x03, 0x6e, 0x65, 0x77, 0x67 }) return false;
                    if (itx.Script[57] != 0x08) return false;
                    if (itx.Script[66] != 0x14) return false;
                    if (itx.Script[87] != 0x14) return false;
                    if (itx.Script.Range(108, 12) != new byte[] { 0x53, 0xc1, 0x08, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x66, 0x65, 0x72, 0x67 }) return false;

                    byte[] price = itx.Script.Range(1, LENGTH_OF_PRICE);
                    if (price.AsBigInteger() <= 0) return false;

                    byte[] assetB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    byte[] aiB = GetAssetInfo(assetB);
                    if (aiB == null) return false;

                    if (itx.Script.Range(37, LENGTH_OF_SCRIPTHASH) != me) return false;

                    BigInteger amount = itx.Script.Range(58, LENGTH_OF_AMOUNT).AsBigInteger();
                    if (amount <= 0) return false;

                    if (itx.Script.Range(67, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte[] from = itx.Script.Range(88, LENGTH_OF_SCRIPTHASH);

                    byte[] assetS = itx.Script.Range(120, LENGTH_OF_SCRIPTHASH);
                    byte[] aiS = GetAssetInfo(assetS);
                    if (aiS == null) return false;

                    if (from == me) return false;
                    if (me == assetS) return false;
                    if (me == assetB) return false;
                    if (from == assetS) return false;
                    if (from == assetB) return false;
                    if (assetB == assetS) return false;

                    return true;
                }
                if (185 == itx.Script.Length) // trade
                {
                    if (185 != itx.Script.Length) return false;
                    if (itx.Script[0] != 0x08) return false;
                    if (itx.Script[9] != 0x44) return false;
                    if (itx.Script[78] != 0x08) return false;
                    if (itx.Script[87] != 0x44) return false;
                    if (itx.Script.Range(156, 9) != new byte[] { 0x54, 0xc1, 0x05, 0x74, 0x72, 0x61, 0x64, 0x65, 0x67 }) return false;
                    if (itx.Script.Range(69, LENGTH_OF_SCRIPTHASH) != me) return false;

                    BigInteger makerAmount = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();
                    if (makerAmount <= 0) return false;

                    byte[] makerInfo = itx.Script.Range(10, 68);
                    BigInteger amountM = Storage.Get(Storage.CurrentContext, makerInfo).AsBigInteger();
                    if (amountM <= 0 || amountM < makerAmount) return false;

                    BigInteger takerAmount = itx.Script.Range(79, LENGTH_OF_AMOUNT).AsBigInteger();
                    if (takerAmount <= 0) return false;

                    byte[] takerInfo = itx.Script.Range(88, 68);
                    BigInteger amountT = Storage.Get(Storage.CurrentContext, takerInfo).AsBigInteger();
                    if (amountT <= 0 || amountT < takerAmount) return false;

                    byte[] taker = takerInfo.Range(0, LENGTH_OF_SCRIPTHASH);
                    byte[] assetS = takerInfo.Range(20, LENGTH_OF_SCRIPTHASH);
                    byte[] assetB = takerInfo.Range(40, LENGTH_OF_SCRIPTHASH);
                    byte[] maker = makerInfo.Range(0, LENGTH_OF_SCRIPTHASH);

                    if (taker == maker) return false;
                    if (assetS != makerInfo.Range(40, LENGTH_OF_SCRIPTHASH)) return false;
                    if (assetB != makerInfo.Range(20, LENGTH_OF_SCRIPTHASH)) return false;

                    BigInteger takerPrice = takerInfo.Range(60, LENGTH_OF_AMOUNT).AsBigInteger();
                    BigInteger makerPrice = makerInfo.Range(60, LENGTH_OF_AMOUNT).AsBigInteger();

                    byte[] aiS = GetAssetInfo(assetS);
                    if (aiS == null) return false;
                    BigInteger decimalsS = GetDecimals(aiS);
                    if (decimalsS < 0 || decimalsS > 8) return false;
                    BigInteger totalS = GetTotal(aiS);
                    if (totalS < takerAmount) return false;

                    byte[] aiB = GetAssetInfo(assetB);
                    if (aiB == null) return false;
                    BigInteger decimalsB = GetDecimals(aiB);
                    if (decimalsB < 0 || decimalsB > 8) return false;
                    BigInteger totalB = GetTotal(aiB);
                    if (totalB < makerAmount) return false;

                    if (takerPrice * makerPrice > 10000000000000000) return false;
                    if (makerPrice * makerAmount * BigInteger.Pow(10, (int)decimalsS) != takerAmount * BigInteger.Pow(10, (int)decimalsB)) return false;
                    if (takerPrice * takerAmount * BigInteger.Pow(10, (int)decimalsB) > makerAmount * BigInteger.Pow(10, (int)decimalsS)) return false;

                    return true;
                }
                if (102 == itx.Script.Length) // cancel
                {
                    if (itx.Script[0] != 0x08) return false;
                    if (itx.Script[9] != 0x14) return false;
                    if (itx.Script[30] != 0x14) return false;
                    if (itx.Script[51] != 0x14) return false;
                    if (itx.Script.Range(72, 10) != new byte[] { 0x54, 0xc1, 0x06, 0x63, 0x61, 0x6e, 0x63, 0x65, 0x6c, 0x67 }) return false;
                    if (itx.Script.Range(82, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte[] price = itx.Script.Range(1, LENGTH_OF_PRICE);
                    if (price.AsBigInteger() <= 0) return false;
                    byte[] assetB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    byte[] assetS = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
                    byte[] from = itx.Script.Range(52, LENGTH_OF_SCRIPTHASH);
                    if (from == me) return false;
                    if (me == assetS) return false;
                    if (me == assetB) return false;
                    if (from == assetB) return false;
                    if (from == assetS) return false;
                    if (assetS == assetB) return false;

                    byte[] order = from.Concat(assetS).Concat(assetB).Concat(price);
                    BigInteger amount = Storage.Get(Storage.CurrentContext, order).AsBigInteger();

                    if (amount > 0) return true;
                    return false;
                }
                if (112 == itx.Script.Length) // claim
                {
                    if (itx.Script.Range(0, 9) != new byte[] { 0x00, 0xc1, 0x05, 0x63, 0x6c, 0x61, 0x69, 0x6d, 0x67 }) return false;
                    if (itx.Script.Range(9, LENGTH_OF_SCRIPTHASH) != me) return false;
                    if (itx.Script[29] != 0x08) return false;
                    if (itx.Script[38] != 0x14) return false;
                    if (itx.Script[59] != 0x14) return false;
                    if (itx.Script.Range(60, LENGTH_OF_SCRIPTHASH) != me) return false;
                    if (itx.Script.Range(80, 12) != new byte[] { 0x53, 0xc1, 0x08, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x66, 0x65, 0x72, 0x67 }) return false;

                    BigInteger amount = itx.Script.Range(30, LENGTH_OF_AMOUNT).AsBigInteger();
                    if (amount <= 0) return false;

                    byte[] to = itx.Script.Range(39, LENGTH_OF_SCRIPTHASH);
                    if (to == me) return false;

                    byte[] asset = itx.Script.Range(96, LENGTH_OF_SCRIPTHASH);
                    if (asset == me) return false;
                    if (to == asset) return false;

                    byte[] amount_byte = GetClaimInfo(to, asset);
                    if (amount_byte == null) return false;
                    BigInteger amount_claim = amount_byte.AsBigInteger();
                    if (amount_claim != amount) return false;

                    byte[] aiS = GetAssetInfo(asset);
                    if (aiS == null) return false;
                    BigInteger total = GetTotal(aiS);
                    if (total < amount) return false;

                    return true;
                }
                if (51 == itx.Script.Length) // return
                {
                    if (itx.Script[0] != 0x14) return false;
                    byte[] asset = itx.Script.Range(1, LENGTH_OF_SCRIPTHASH);
                    if (itx.Script.Range(21, 10) != new byte[] { 0x51, 0xc1, 0x06, 0x72, 0x65, 0x74, 0x75, 0x72, 0x6e, 0x67 }) return false;
                    if (itx.Script.Range(31, LENGTH_OF_SCRIPTHASH) != me) return false;

                    byte[] ai = GetAssetInfo(asset);
                    if (ai == null) return false;
                    byte[] amount_byte = GetClaimInfo(OWNER, asset);
                    if (amount_byte != null) return false;
                    return true;
                }

                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                byte[] callscript = ExecutionEngine.CallingScriptHash;
                if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callscript.AsBigInteger()) return false;
                if (operation == "set")
                {
                    // set Y|N
                    if (args.Length != 1) return false;
                    byte[] status = (byte[])args[0];
                    return Deploy(status);
                }
                if (operation == "support")
                {
                    // support new asset
                    if (args.Length != 1) return false;
                    byte[] asset = (byte[])args[0];
                    return SupportAsset(asset);
                }
                if (operation == "unsupport")
                {
                    // unsupport asset
                    if (args.Length != 1) return false;
                    byte[] asset = (byte[])args[0];
                    return UNSupportAsset(asset);
                }
                if (operation == "new")
                {
                    // new order
                    if (args.Length != 2) return false;
                    return NewOrder();
                }
                if (operation == "trade")
                {
                    // trade
                    if (args.Length != 4) return false;
                    return Trading();
                }
                if (operation == "cancel")
                {
                    // cancel order
                    if (args.Length != 4) return false;
                    return CancelOrder();
                }
                if (operation == "claim")
                {
                    // claim assetA|assetB to buyer
                    if (args.Length != 1) return false;
                    return Claiming();
                }
                if (operation == "return")
                {
                    // return asset to OWNER
                    if (args.Length != 1) return false;
                    byte[] asset = (byte[])args[0];
                    return SetReturnAsset(asset);
                }
            }
            return false;
        }
        // get fixed 8 length byte array
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
        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }
        // get asset info
        private static byte[] GetAssetInfo(byte[] asset)
        {
            if (asset.Length != LENGTH_OF_SCRIPTHASH) return null;
            byte[] assetInfo = Storage.Get(Storage.CurrentContext, asset); //0.1
            if (assetInfo.Length != 16) return null;
            return assetInfo;
        }
        // set asset info
        private static bool SetAssetInfo(byte[] asset, BigInteger decimals, BigInteger total)
        {
            if (asset.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (decimals < 0) return false;
            if (decimals > 8) return false;
            if (total < 0) return false;
            byte[] d8 = GetFixed8(decimals);
            byte[] t8 = GetFixed8(total);
            byte[] dt16 = d8.Concat(t8);
            Storage.Put(Storage.CurrentContext, asset, dt16); //1
            return true;
        }
        // delete claim info
        private static bool DelClaimInfo(byte[] address, byte[] asset)
        {
            byte[] claimInfo = RETURN_PREFIX.Concat(address).Concat(asset);
            Storage.Delete(Storage.CurrentContext, claimInfo);
            return true;
        }
        // set claim info
        private static bool SetClaimInfo(byte[] address, byte[] asset, BigInteger amount)
        {
            if (address.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (asset.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (amount <= 0) return false;
            byte[] claimInfo = RETURN_PREFIX.Concat(address).Concat(asset);
            BigInteger old_amount = Storage.Get(Storage.CurrentContext, claimInfo).AsBigInteger();
            if (old_amount < 0) return false;
            Storage.Put(Storage.CurrentContext, claimInfo, old_amount + amount);
            return true;
        }
        // get claim info
        private static byte[] GetClaimInfo(byte[] address, byte[] asset)
        {
            if (address.Length != LENGTH_OF_SCRIPTHASH) return null;
            if (asset.Length != LENGTH_OF_SCRIPTHASH) return null;
            byte[] claimInfo = RETURN_PREFIX.Concat(address).Concat(asset);
            byte[] amount = Storage.Get(Storage.CurrentContext, claimInfo);
            if (amount.AsBigInteger() <= 0) return null;
            return amount;
        }
        // get decimals from asset info
        private static BigInteger GetDecimals(byte[] info)
        {
            if (info == null)
            {
                throw new InvalidOperationException("The parameter address SHOULD NOT be null.");
            }
            if (info.Length != 16)
            {
                throw new InvalidOperationException("The parameter info's length SHOULD == 16.");
            }
            return info.Range(0, 8).AsBigInteger();
        }
        // get total from asset info
        private static BigInteger GetTotal(byte[] info)
        {
            if (info == null)
            {
                throw new InvalidOperationException("The parameter address SHOULD NOT be null.");
            }
            if (info.Length != 16)
            {
                throw new InvalidOperationException("The parameter info's length SHOULD == 16.");
            }
            return info.Range(8, 8).AsBigInteger();
        }
        private static bool SetStatus(byte[] status)
        {
            if (status.Length != 1) return false;
            if (status[0] != 0x59 || status[0] != 0x4e) return false; // Y|N
            Storage.Put(Storage.CurrentContext, STATUS, status);
            return true;
        }
        private static byte GetStatus()
        {
            byte[] status = Storage.Get(Storage.CurrentContext, STATUS);
            if (status.Length != 1) return 0x00;
            return status[0];
        }
        private static bool SetReturnAmount(byte[] asset, BigInteger delta)
        {
            byte[] key = RETURN_PREFIX.Concat(asset);
            Storage.Put(Storage.CurrentContext, key, delta); //1
            return true;
        }
        private static BigInteger GetReturnAmount(byte[] asset)
        {
            byte[] key = RETURN_PREFIX.Concat(asset);
            BigInteger delta = Storage.Get(Storage.CurrentContext, key).AsBigInteger();//0.1
            return delta;
        }
        private static bool DelReturnAmount(byte[] asset)
        {
            byte[] key = RETURN_PREFIX.Concat(asset);
            Storage.Delete(Storage.CurrentContext, key);
            return true;
        }

        public static bool Deploy(byte[] status)
        {
            if (!Runtime.CheckWitness(OWNER)) return false;

            byte cr = GetStatus();
            if (cr == status[0]) return false;
            else
            {
                bool s = SetStatus(status);
                if (s == true)
                {
                    StatusSetted(status);
                    return true;
                }
            }
            return false;
        }
        public static bool SupportAsset(byte[] asset)
        {
            if (!Runtime.CheckWitness(OWNER)) return false;

            byte[] old = GetAssetInfo(asset);
            if (old == null)
            {
                var decimalsArgs = new object[] { };
                var contract = (NEP5Contract)asset.ToDelegate();
                BigInteger decimalsResult = (BigInteger)contract("decimals", decimalsArgs);

                bool sai = SetAssetInfo(asset, decimalsResult, 0);
                if (sai == true)
                {
                    AssetSupported(asset);
                    return true;
                }
            }
            return false;
        }
        public static bool UNSupportAsset(byte[] asset)
        {
            if (!Runtime.CheckWitness(OWNER)) return false;

            byte[] ai = GetAssetInfo(asset);
            if (ai != null)
            {
                BigInteger total = GetTotal(ai);
                if (total != 0) return false;

                Storage.Delete(Storage.CurrentContext, asset); //0.1
                AssetUNSupported(asset);
                return true;
            }
            return false;
        }
        public static bool NewOrder()
        {
            var itx = (InvocationTransaction)ExecutionEngine.ScriptContainer; // length:140

            byte[] price = itx.Script.Range(1, LENGTH_OF_PRICE);

            byte[] assetB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
            byte[] aiB = GetAssetInfo(assetB);
            if (aiB == null) return StopExecution();

            BigInteger amount = itx.Script.Range(58, LENGTH_OF_AMOUNT).AsBigInteger();

            byte[] from = itx.Script.Range(88, LENGTH_OF_SCRIPTHASH);

            byte[] assetS = itx.Script.Range(120, LENGTH_OF_SCRIPTHASH);
            byte[] aiS = GetAssetInfo(assetS);
            if (aiS == null) return StopExecution();

            if (!Runtime.CheckWitness(from)) return StopExecution();

            var balanceArgs = new object[] { from };
            var contract = (NEP5Contract)assetS.ToDelegate();
            BigInteger balanceResult = (BigInteger)contract("balanceOf", balanceArgs);
            if (amount > balanceResult) return StopExecution();

            byte[] new_order = from.Concat(assetS).Concat(assetB).Concat(price);
            BigInteger old_amount = Storage.Get(Storage.CurrentContext, new_order).AsBigInteger();
            Storage.Put(Storage.CurrentContext, new_order, old_amount + amount);

            BigInteger decimals = GetDecimals(aiS);
            BigInteger total = GetTotal(aiS);
            SetAssetInfo(assetS, decimals, total + amount);

            NewOrdered(from, assetS, assetB, price, amount);
            return true;
        }
        public static bool Trading()
        {
            var itx = (InvocationTransaction)ExecutionEngine.ScriptContainer; //length:185

            BigInteger makerAmount = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();

            byte[] makerInfo = itx.Script.Range(10, 68);
            BigInteger amountM = Storage.Get(Storage.CurrentContext, makerInfo).AsBigInteger();
            if (amountM <= 0 || amountM < makerAmount) return false;

            BigInteger takerAmount = itx.Script.Range(79, LENGTH_OF_AMOUNT).AsBigInteger();

            byte[] takerInfo = itx.Script.Range(88, 68);
            BigInteger amountT = Storage.Get(Storage.CurrentContext, takerInfo).AsBigInteger();
            if (amountT <= 0 || amountT < takerAmount) return false;

            byte[] taker = takerInfo.Range(0, LENGTH_OF_SCRIPTHASH);
            byte[] assetS = takerInfo.Range(20, LENGTH_OF_SCRIPTHASH);
            byte[] assetB = takerInfo.Range(40, LENGTH_OF_SCRIPTHASH);
            byte[] maker = makerInfo.Range(0, LENGTH_OF_SCRIPTHASH);

            BigInteger takerPrice = takerInfo.Range(60, LENGTH_OF_AMOUNT).AsBigInteger();
            BigInteger makerPrice = makerInfo.Range(60, LENGTH_OF_AMOUNT).AsBigInteger();

            byte[] aiS = GetAssetInfo(assetS);
            if (aiS == null) return false;
            BigInteger decimalsS = GetDecimals(aiS);
            BigInteger totalS = GetTotal(aiS);
            if (totalS < takerAmount) return false;

            byte[] aiB = GetAssetInfo(assetB);
            if (aiB == null) return false;
            BigInteger decimalsB = GetDecimals(aiB);
            BigInteger totalB = GetTotal(aiB);
            if (totalB < makerAmount) return false;

            if (takerAmount < amountT) Storage.Put(Storage.CurrentContext, takerInfo, amountT - takerAmount);
            if (takerAmount == amountT) Storage.Delete(Storage.CurrentContext, takerInfo);
            if (makerAmount < amountM) Storage.Put(Storage.CurrentContext, makerInfo, amountM - makerAmount);
            if (makerAmount == amountM) Storage.Delete(Storage.CurrentContext, makerInfo);

            bool t = SetClaimInfo(taker, assetB, makerAmount);
            if (t == false) return false;
            bool m = SetClaimInfo(maker, assetS, takerAmount);
            if (m == false) return false;

            OrderTraded(takerInfo.Range(20, 40), taker, 0, takerAmount);
            OrderTraded(makerInfo.Range(20, 40), maker, makerPrice, makerAmount);
            return true;
        }
        public static bool CancelOrder()
        {
            var itx = (InvocationTransaction)ExecutionEngine.ScriptContainer; // length:102

            byte[] price = itx.Script.Range(1, LENGTH_OF_PRICE);
            byte[] assetB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
            byte[] assetS = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
            byte[] from = itx.Script.Range(52, LENGTH_OF_SCRIPTHASH);

            byte[] order = from.Concat(assetS).Concat(assetB).Concat(price);
            BigInteger amount = Storage.Get(Storage.CurrentContext, order).AsBigInteger();

            if (amount > 0)
            {
                Storage.Delete(Storage.CurrentContext, order);
                bool r = SetClaimInfo(from, assetS, amount);
                if (r == true)
                {
                    OrderCanelled(from, assetS, assetB, price, amount);
                    return true;
                }
            }
            return false;
        }
        private static bool StopExecution()
        {
            throw new InvalidOperationException("Stop Execution.");
            return false;
        }
        public static bool Claiming()
        {
            byte[] me = GetReceiver();
            var itx = (InvocationTransaction)ExecutionEngine.ScriptContainer;

            BigInteger amount = itx.Script.Range(30, LENGTH_OF_AMOUNT).AsBigInteger();
            byte[] to = itx.Script.Range(39, LENGTH_OF_SCRIPTHASH);
            byte[] asset = itx.Script.Range(96, LENGTH_OF_SCRIPTHASH);

            byte[] amount_byte = GetClaimInfo(to, asset);
            if (amount_byte == null) return StopExecution();
            BigInteger amount_claim = amount_byte.AsBigInteger();
            if (amount_claim != amount) return StopExecution();

            var balanceArgs = new object[] { me };
            var contract = (NEP5Contract)asset.ToDelegate();
            BigInteger balanceResult = (BigInteger)contract("balanceOf", balanceArgs);
            if (balanceResult < amount) return StopExecution();

            byte[] aiS = GetAssetInfo(asset);
            if (aiS == null) return StopExecution();
            BigInteger decimals = GetDecimals(aiS);
            BigInteger total = GetTotal(aiS);
            if (total < amount) return StopExecution();
            SetAssetInfo(asset, decimals, total - amount);

            DelClaimInfo(to, asset);

            Claimed(to, asset, amount);
            return true;
        }
        public static bool SetReturnAsset(byte[] asset)
        {
            byte[] me = GetReceiver();
            if (asset.Length != LENGTH_OF_SCRIPTHASH) return false;

            BigInteger total = 0;
            byte[] ai = GetAssetInfo(asset);
            if (ai == null) return false;
            total = GetTotal(ai);
            var balanceArgs = new object[] { me };
            var contract = (NEP5Contract)asset.ToDelegate();
            BigInteger balanceResult = (BigInteger)contract("balanceOf", balanceArgs);
            if (balanceResult <= total) return false;

            byte[] amount_byte = GetClaimInfo(OWNER, asset);
            if (amount_byte != null) return false;

            BigInteger delta = balanceResult - total;
            SetClaimInfo(OWNER, asset, delta);
            ReturnSet(asset, delta);
            return true;
        }
    }
}
