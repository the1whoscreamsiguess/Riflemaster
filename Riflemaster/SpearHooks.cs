using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace Riflemaster
{
    public static class SpearHooks
    {
        //technically the easier and more logical thing would be to just make SM unable to throw spears at all but their regions are built around climbing walls with spears so...

        public static void OnEnable()
        {
            On.Spear.HitSomething += Spear_HitSomething;
            On.Spear.HitSomethingWithoutStopping += Spear_HitSomethingWithoutStopping;
        }

        public static void BreakNeedle(Spear self)
        {
            Player player = self.thrownBy as Player;
            Color color = Color.white;
            if ((player.graphicsModule as PlayerGraphics).useJollyColor)
            {
                color = PlayerGraphics.JollyColor(player.playerState.playerNumber, 2);
            }
            else if (PlayerGraphics.CustomColorsEnabled())
            {
                color = PlayerGraphics.CustomColorSafety(2);
            }
            else
            {
                color = new Color(1f, 1f, 1f, 1f);
            }
            for (int k = 0; k < 2; k++)
            {
                ExplosiveSpear.SpearFragment fragment = new ExplosiveSpear.SpearFragment(self.firstChunk.pos, Custom.RNV() * Mathf.Lerp(1f, 4f, UnityEngine.Random.value), self);
                Dictionaries.needleSpearFragments.Add(fragment, color);
                self.room.AddObject(fragment);
            }
            for (int n = 17; n > 0; n--)
            {
                self.room.AddObject(new Spark(self.firstChunk.pos, Custom.RNV() * Mathf.Lerp(0f, 4f, UnityEngine.Random.value), n % 2 == 0 ? color : Color.white, null, 10, 20));
            }
            self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk.pos);
            self.room.PlaySound(SoundID.Spear_Fragment_Bounce, self.firstChunk.pos);
            self.Destroy();
        }

        //If riflemaster challenge is enabled, spears thrown by Spearmaster bounce off of creatures and deal no damage
        //if it's a fresh needle, they additionally shatter on impact
        public static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (RemixSettings.riflemasterChallenge.Value && self.thrownBy != null && self.thrownBy is Player &&
                (self.thrownBy as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear && result.obj != null)
            {
                if (self.IsNeedle && self.Spear_NeedleCanFeed())
                {
                    BreakNeedle(self);
                }
                else
                {
                    self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
                    self.HitWall();
                    self.firstChunk.vel *= -1;
                    self.vibrate = 10;
                    for (int n = 17; n > 0; n--)
                    {
                        self.room.AddObject(new Spark(self.firstChunk.pos, Custom.RNV(), Color.white, null, 10, 20));
                    }
                }
                return false;
            }
            return orig(self, result, eu);
        }

        //If riflemaster challenge is enabled, spears thrown by Spearmaster pass through objects
        private static void Spear_HitSomethingWithoutStopping(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            if (RemixSettings.riflemasterChallenge.Value && self.thrownBy != null && self.thrownBy is Player &&
                (self.thrownBy as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                return;
            }
            orig(self, obj, chunk, appendage);
        }
    }
}