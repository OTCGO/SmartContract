using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System.Numerics;

namespace Neo.SmartContract
{
    public class EXV: Framework.SmartContract
    {
        private static readonly byte INVOCATION_TRANSACTION_TYPE = 0xd1;
        private const int LENGTH_OF_SCRIPTHASH = 20;
        private const int LENGTH_OF_AMOUNT = 8;
        private const int LENGTH_OF_PRICE = 8;
        public delegate object NEP5Contract(string method, object[] args);
        
        public static bool Main(byte[] owner, byte[] tokenSR, byte[] tokenBR, byte[] price)
        {
            if (owner.Length != LENGTH_OF_SCRIPTHASH || tokenSR.Length != LENGTH_OF_SCRIPTHASH || tokenBR.Length != LENGTH_OF_SCRIPTHASH || price.Length != LENGTH_OF_PRICE) return false;
            if (!CheckTx(owner, tokenSR, tokenBR, price)) return false;
            return true;
        }
        
        private static bool CheckTx(byte[] owner, byte[] tokenSR, byte[] tokenBR, byte[] price)
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            var type = tx.Type;
            if (type != INVOCATION_TRANSACTION_TYPE) return false;
            
            var itx = (InvocationTransaction)tx;
            if (83 == itx.Script.Length)
            {
                if (itx.Script[0] != 0x08) return false;
                if (itx.Script[9] != 0x14) return false;
                if (itx.Script[30] != 0x14) return false;
                if (itx.Script.Range(51, 12) != new byte[] { 0x53, 0xc1, 0x08, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x66, 0x65, 0x72, 0x67 }) return false;
                if (itx.Script.Range(10, LENGTH_OF_SCRIPTHASH) != owner) return false;
                return true;
            }
            else if (165 == itx.Script.Length)
            {
                if (price.AsBigInteger() <= 0) return false;
                if (tokenSR.AsBigInteger() == tokenBR.AsBigInteger()) return false;
                if (itx.Script[0] != 0x08) return false;
                if (itx.Script[9] != 0x14) return false;
                if (itx.Script[30] != 0x14) return false;
                if (itx.Script[51] != 0x14) return false;
                if (itx.Script[72] != 0x08) return false;
                if (itx.Script[81] != 0x14) return false;
                if (itx.Script[102] != 0x14) return false;
                if (itx.Script[123] != 0x14) return false;
                if (itx.Script.Range(144, 21) != new byte[] { 0x67, 0x63, 0x44, 0xbb, 0x3d, 0x1e, 0xeb, 0xc2, 0x54, 0x8c, 0x0f, 0xb8, 0x99, 0x6d, 0xef, 0x81, 0xe7, 0x8c, 0x3c, 0xaf  0x4a}) return false;
                BigInteger amountS = 0;
                BigInteger amountB = 0;
                byte[] toS = null;
                byte[] toB = null;
                byte[] fromS = null;
                byte[] fromB = null;
                if (tokenSR == itx.Script.Range(52, LENGTH_OF_SCRIPTHASH) && tokenBR == itx.Script.Range(124, LENGTH_OF_SCRIPTHASH))
                {
                    amountS = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();
                    toS = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    fromS = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
                    
                    amountB = itx.Script.Range(73, LENGTH_OF_AMOUNT).AsBigInteger();
                    toB = itx.Script.Range(82, LENGTH_OF_SCRIPTHASH);
                    fromB = itx.Script.Range(103, LENGTH_OF_SCRIPTHASH);
                }
                else if (tokenBR == itx.Script.Range(52, LENGTH_OF_SCRIPTHASH) && tokenSR == itx.Script.Range(124, LENGTH_OF_SCRIPTHASH))
                {
                    amountB = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();
                    toB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    fromB = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
                    
                    amountS = itx.Script.Range(73, LENGTH_OF_AMOUNT).AsBigInteger();
                    toS = itx.Script.Range(82, LENGTH_OF_SCRIPTHASH);
                    fromS = itx.Script.Range(103, LENGTH_OF_SCRIPTHASH);
                }
                else
                {
                    return false;
                }
                if (amountS <= 0 || amountB <= 0) return false;
                if (toS == null || toB == null || fromS == null || fromB == null) return false;
                if (owner != toB || ExecutionEngine.ExecutingScriptHash.AsBigInteger() != fromS.AsBigInteger()) return false;
                if (amountS * 100000000 > amountB * price.AsBigInteger()) return false;
                
                var balanceArgs = new object[] { fromS };
                var balanceContract = (NEP5Contract)tokenSR.ToDelegate();
                BigInteger balanceResult = (BigInteger)balanceContract("balanceOf", balanceArgs);
                if (amountS > balanceResult) return false;
                
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
