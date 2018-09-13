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
        
        public static bool Main(byte[] tokenA, byte[] fromA, byte[] toA, byte[] amountA, byte[] tokenB, byte[] fromB, byte[] toB, byte[] amountB)
        {
            if (tokenA.Length != LENGTH_OF_SCRIPTHASH || tokenB.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (fromA.Length != LENGTH_OF_SCRIPTHASH || fromB.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (toA.Length != LENGTH_OF_SCRIPTHASH || toB.Length != LENGTH_OF_SCRIPTHASH) return false;
            if (amountA.Length != LENGTH_OF_AMOUNT || amountA.AsBigInteger() <= 0) return false;
            if (amountB.Length != LENGTH_OF_AMOUNT || amountB.AsBigInteger() <= 0) return false;
            
            if (!Runtime.CheckWitness(fromA)) return false;
            if (!Runtime.CheckWitness(fromB)) return false;
            
            var balanceArgsA = new object[] { fromA };
            var contractA = (NEP5Contract)tokenA.ToDelegate();
            BigInteger balanceResultA = (BigInteger)contractA("balanceOf", balanceArgsA);
            if (amountA.AsBigInteger() > balanceResultA) return false;

            var balanceArgsB = new object[] { fromB };
            var contractB = (NEP5Contract)tokenB.ToDelegate();
            BigInteger balanceResultB = (BigInteger)contractB("balanceOf", balanceArgsB);
            if (amountB.AsBigInteger() > balanceResultB) return false;
            
            var argsA = new object[] { fromA, toA, amountA };
            bool resultA = (bool)contractA("transfer", argsA);
            if (!resultA) return false;
            
            var argsB = new object[] { fromB, toB, amountB };
            bool resultB = (bool)contractB("transfer", argsB);
            if (!resultB) return false;
            
            return true;
        }
    }
}