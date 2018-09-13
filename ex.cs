using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class CT: Framework.SmartContract
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
            else if (166 == itx.Script.Length)
            {
                if (price.AsBigInteger() <= 0) return false;
                if (itx.Script[0] != 0x08) return false;
                if (itx.Script[9] != 0x14) return false;
                if (itx.Script[30] != 0x14) return false;
                if (itx.Script.Range(51, 12) != new byte[] { 0x53, 0xc1, 0x08, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x66, 0x65, 0x72, 0x67 }) return false;
                if (itx.Script[83] != 0x08) return false;
                if (itx.Script[92] != 0x14) return false;
                if (itx.Script[113] != 0x14) return false;
                if (itx.Script.Range(134, 12) != new byte[] { 0x53, 0xc1, 0x08, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x66, 0x65, 0x72, 0x67 }) return false;
                BigInteger amountS = 0;
                BigInteger amountB = 0;
                byte[] toS = null;
                byte[] toB = null;
                byte[] fromS = null;
                byte[] fromB = null;
                if (tokenSR == itx.Script.Range(63,20) && tokenBR == itx.Script.Range(146,20))
                {
                    amountS = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();
                    toS = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    fromS = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
                    
                    amountB = itx.Script.Range(84, LENGTH_OF_AMOUNT).AsBigInteger();
                    toB = itx.Script.Range(93, LENGTH_OF_SCRIPTHASH);
                    fromB = itx.Script.Range(114, LENGTH_OF_SCRIPTHASH);
                }
                else if (tokenBR == itx.Script.Range(63,20) && tokenSR == itx.Script.Range(146,20))
                {
                    amountB = itx.Script.Range(1, LENGTH_OF_AMOUNT).AsBigInteger();
                    toB = itx.Script.Range(10, LENGTH_OF_SCRIPTHASH);
                    fromB = itx.Script.Range(31, LENGTH_OF_SCRIPTHASH);
                    
                    amountS = itx.Script.Range(84, LENGTH_OF_AMOUNT).AsBigInteger();
                    toS = itx.Script.Range(93, LENGTH_OF_SCRIPTHASH);
                    fromS = itx.Script.Range(114, LENGTH_OF_SCRIPTHASH);
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
