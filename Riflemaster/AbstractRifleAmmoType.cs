using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riflemaster
{
    public static class AbstractRifleAmmoType
    {
        public static JokeRifle.AbstractRifle.AmmoType SpearmasterNeedle;

        public static void RegisterValues()
        {
            SpearmasterNeedle = new JokeRifle.AbstractRifle.AmmoType("SpearmasterNeedle", true);
        }

        public static void UnregisterValues()
        {
            if (SpearmasterNeedle != null)
            {
                SpearmasterNeedle.Unregister();
                SpearmasterNeedle = null;
            }
        }
    }
}
