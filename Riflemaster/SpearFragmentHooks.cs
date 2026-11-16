using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riflemaster
{
    public static class SpearFragmentHooks
    {

        public static void OnEnable()
        {
            On.ExplosiveSpear.SpearFragment.ApplyPalette += SpearFragment_ApplyPalette;
        }

        private static void SpearFragment_ApplyPalette(On.ExplosiveSpear.SpearFragment.orig_ApplyPalette orig, ExplosiveSpear.SpearFragment self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (Dictionaries.needleSpearFragments.ContainsKey(self))
            {
                sLeaser.sprites[0].color = Dictionaries.needleSpearFragments[self];
            }
        }
    }
}
