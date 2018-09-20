using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    public class EXA: Framework.SmartContract
    {
        private const int LENGTH_OF_SCRIPTHASH = 20;
        private const int LENGTH_OF_AMOUNT = 8;
        public delegate object NEP5Contract(string method, object[] args);
        
        public static bool Main(byte[] tokenAR, byte[] fromA, byte[] toA, byte[] amountA, byte[] tokenBR, byte[] fromB, byte[] toB, byte[] amountB)
        {
            var magicstr = "2018-09-20 21:58";

            if (tokenAR.Length != LENGTH_OF_SCRIPTHASH || tokenBR.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (fromA.Length != LENGTH_OF_SCRIPTHASH || fromB.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (toA.Length != LENGTH_OF_SCRIPTHASH || toB.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (amountA.Length != LENGTH_OF_AMOUNT || amountA.AsBigInteger() <= 0) return false;
            if (amountB.Length != LENGTH_OF_AMOUNT || amountB.AsBigInteger() <= 0) return false;
            //if (tokenAR == tokenBR) return false;
            //if (fromA == fromB) return false;
            //if (fromA == toA) return false;
            //if (fromB == toB) return false;
            
            if (!Runtime.CheckWitness(fromA)) return false;
            if (!Runtime.CheckWitness(fromB)) return false;
            
            var balanceArgsA = new object[] { fromA };
            var contractA = (NEP5Contract)tokenAR.ToDelegate();
            BigInteger balanceResultA = (BigInteger)contractA("balanceOf", balanceArgsA);
            if (amountA.AsBigInteger() > balanceResultA) return false;

            var balanceArgsB = new object[] { fromB };
            var contractB = (NEP5Contract)tokenBR.ToDelegate();
            BigInteger balanceResultB = (BigInteger)contractB("balanceOf", balanceArgsB);
            if (amountB.AsBigInteger() > balanceResultB) return false;
            
            var argsA = new object[] { fromA, toA, amountA };
            var contractC = (NEP5Contract)tokenAR.ToDelegate();
            bool resultA = (bool)contractC("transfer", argsA);
            if (!resultA) return false;
            
            var argsB = new object[] { fromB, toB, amountB };
            var contractD = (NEP5Contract)tokenBR.ToDelegate();
            bool resultB = (bool)contractD("transfer", argsB);
            if (!resultB) return false;
            
            return true;
        }
    }
}
