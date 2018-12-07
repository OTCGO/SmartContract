using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class BONUS : Framework.SmartContract
    {
        [DisplayName("bonusshare")]
        public static event Action<byte[], BigInteger> BonusShared;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {

                if (operation == "bonus")
                {
                    if (args.Length != 2) return false;
                    byte[] addr = (byte[])args[0];
                    BigInteger value = (BigInteger)args[1];
                    return Bonus(addr, value);
                }
            }
            return false;
        }
        private static bool Bonus(byte[] addr, BigInteger value)
        {
            BonusShared(addr, value);
            return true;
        }
    }
}