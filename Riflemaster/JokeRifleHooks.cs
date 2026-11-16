using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;

namespace Riflemaster
{
    public static class JokeRifleHooks
    {

        public static void OnEnable()
        {
            On.JokeRifle.ctor += JokeRifle_ctor;
            On.JokeRifle.AmmoTypeFromObject += JokeRifle_AmmoTypeFromObject;
            On.JokeRifle.ApplyPalette += JokeRifle_ApplyPalette;
            On.JokeRifle.DrawSprites += JokeRifle_DrawSprites;
            On.JokeRifle.IsValidAmmo += JokeRifle_IsValidAmmo;
            On.JokeRifle.ReloadRifle += JokeRifle_ReloadRifle;
            On.JokeRifle.Update += JokeRifle_Update;
            On.MoreSlugcats.AmmoMeter.AmmoSprite += AmmoMeter_AmmoSprite;

            IL.JokeRifle.Use += JokeRifle_Use_IL;
            
        }

        //initializing dictionaries
        private static void JokeRifle_ctor(On.JokeRifle.orig_ctor orig, JokeRifle self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            orig(self, abstractPhysicalObject, world);
            Dictionaries.jokeRifleCustom.Add(self, new Dictionaries.JokeRifleCustom());
        }

        //Attached spearmaster needle ammo type
        private static JokeRifle.AbstractRifle.AmmoType JokeRifle_AmmoTypeFromObject(On.JokeRifle.orig_AmmoTypeFromObject orig, PhysicalObject obj)
        {
            if (RiflemasterCustom.IsAttachedSpearmasterSpear(obj))
            {
                return AbstractRifleAmmoType.SpearmasterNeedle;
            }
            return orig(obj);
        }

        //challenge rifle coloring
        private static void JokeRifle_ApplyPalette(On.JokeRifle.orig_ApplyPalette orig, JokeRifle self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (Dictionaries.jokeRifleCustom[self].challengeRifleColor != null)
            {
                float disintegrateProg = Dictionaries.jokeRifleCustom[self].disintegrateCounter / 200f;
                sLeaser.sprites[0].color = Color.Lerp(Dictionaries.jokeRifleCustom[self].challengeRifleColor.Value, palette.blackColor, disintegrateProg);
            }
        }

        //challenge rifle disintegrate visual
        private static void JokeRifle_DrawSprites(On.JokeRifle.orig_DrawSprites orig, JokeRifle self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (Dictionaries.jokeRifleCustom[self].challengeRifleColor != null)
            {
                float disintegrateProg = Dictionaries.jokeRifleCustom[self].disintegrateCounter / 200f;
                sLeaser.sprites[0].y -= 20 * disintegrateProg;
                sLeaser.sprites[0].scaleX = 1 - disintegrateProg;
                sLeaser.sprites[0].scaleY = (1 - (disintegrateProg / 2f)) * (sLeaser.sprites[0].rotation < 0f ? -1 : 1);
                self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }
        }

        //needle spear ammo validity
        private static bool JokeRifle_IsValidAmmo(On.JokeRifle.orig_IsValidAmmo orig, PhysicalObject obj)
        {
            if (RiflemasterCustom.IsAttachedSpearmasterSpear(obj))
            {
                return true;
            }
            return orig(obj);
        }

        //loading rifle with needles
        private static void JokeRifle_ReloadRifle(On.JokeRifle.orig_ReloadRifle orig, JokeRifle self, PhysicalObject obj)
        {
            if (RiflemasterCustom.IsAttachedSpearmasterSpear(obj))
            {
                self.abstractRifle.ammoStyle = JokeRifle.AmmoTypeFromObject(obj);
                self.abstractRifle.setCurrentAmmo(self.abstractRifle.currentAmmo() + RemixSettings.bulletsPerNeedle.Value);
                self.lastShotTime = 7;
            }
            else
            {
                orig(self, obj);
            }
        }

        //challenge rifle update
        private static void JokeRifle_Update(On.JokeRifle.orig_Update orig, JokeRifle self, bool eu)
        {
            Dictionaries.JokeRifleCustom rifleCustom = Dictionaries.jokeRifleCustom[self];

            //if challenge rifle not being held by its owner, disintegrate
            if (rifleCustom.challengeOwner != null && rifleCustom.disintegrateCounter == 0)
            {
                bool heldByOwner = false;
                for (int i = 0; i < self.grabbedBy.Count; i++)
                {
                    if (self.grabbedBy[i] != null && self.grabbedBy[i].grabber != null && self.grabbedBy[i].grabber != null && self.grabbedBy[i].grabber == rifleCustom.challengeOwner)
                    {
                        heldByOwner = true;
                        break;
                    }
                }
                if (heldByOwner == false)
                {
                    rifleCustom.disintegrateCounter = 1;
                }
            }
            //prevent pickup while disintegrating and destroy when animation is complete
            if (rifleCustom.disintegrateCounter > 0)
            {
                self.AllGraspsLetGoOfThisObject(true);
                rifleCustom.disintegrateCounter++;
                if (rifleCustom.disintegrateCounter >= 200)
                {
                    self.Destroy();
                    return;
                }
            }
            Dictionaries.jokeRifleCustom[self] = rifleCustom;
            orig(self, eu);
        }

        //needle spear ammo icon
        private static string AmmoMeter_AmmoSprite(On.MoreSlugcats.AmmoMeter.orig_AmmoSprite orig, MoreSlugcats.AmmoMeter self)
        {
            JokeRifle jokeRifle = self.rifleRef();
            if (jokeRifle != null && jokeRifle.abstractRifle.ammoStyle == AbstractRifleAmmoType.SpearmasterNeedle)
            {
                return "Symbol_NeedleSpear";
            }
            return orig(self);
        }

        //IL hooks

        //Joke rifle shooting needle bullet
        public static void JokeRifle_Use_IL(ILContext il)
        {
            try
            {

                //Extra firing behaviors if we're using needle ammo

                //match the part right after it checks for ammo amount
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.After,
                   x => x.MatchCall(out _),
                   x => x.MatchCallvirt<JokeRifle.AbstractRifle>(nameof(JokeRifle.AbstractRifle.currentAmmo)),
                   x => x.MatchLdcI4(0),
                   x => x.MatchBle(out _)
                );
                c.MoveAfterLabels();

                //set counter
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<JokeRifle>>((JokeRifle self) =>
                {
                    if (self.abstractRifle.ammoStyle == AbstractRifleAmmoType.SpearmasterNeedle)
                    {
                        self.counter = RemixSettings.ticksPerShot.Value;
                    }
                });
            
                //match bullet lifetime setting right after the timeToLive value is initialized
                c.GotoNext(MoveType.After,
                    x => x.MatchStloc(19)
                    );
                c.MoveAfterLabels();
            
                //set bullet lifetime
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldloc, 19);
                c.EmitDelegate<Func<JokeRifle, int, int>>((JokeRifle self, int origValue) =>
                {
                    if (self.abstractRifle.ammoStyle == AbstractRifleAmmoType.SpearmasterNeedle)
                    {
                        return Mathf.RoundToInt(Mathf.Lerp(550, 650, Mathf.Pow(UnityEngine.Random.value, 2)));
                    }
                    return origValue;
                });
                c.Emit(OpCodes.Stloc, 19);

                //match bullet spawning
                c.GotoNext(MoveType.After,
                    x => x.MatchLdloc(20),
                    x => x.MatchCallvirt<AbstractPhysicalObject>(nameof(AbstractPhysicalObject.RealizeInRoom))
                    );
                c.MoveAfterLabels();

                //Initialize umbilical and jolly color for needle bullets (and set destroyOnAbstraction)
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldloc, 20);
                c.EmitDelegate<Action<JokeRifle, AbstractBullet>>((JokeRifle self, AbstractBullet absBullet) =>
                {
                    if (absBullet.bulletType == AbstractRifleAmmoType.SpearmasterNeedle) 
                    {
                        absBullet.destroyOnAbstraction = true;

                        if (self.grabbedBy[0].grabber is Player)
                        {
                            Player player = self.grabbedBy[0].grabber as Player;
                            if ((player.graphicsModule as PlayerGraphics).useJollyColor)
                            {
                                Dictionaries.BulletCustom bCustom = Dictionaries.needleBulletCustom[absBullet.realizedObject as Bullet];
                                bCustom.jollyCustomColor = new Color?(PlayerGraphics.JollyColor(player.playerState.playerNumber, 2));
                                Dictionaries.needleBulletCustom[absBullet.realizedObject as Bullet] = bCustom;
                            }
                        }

                        if (RemixSettings.spawnUmbilicals.Value)
                        {
                            self.room.AddObject(new Umbilical(self.room, absBullet.realizedObject as Bullet, self, self.aimDir));
                        }
                    }
                });

            }
            catch (Exception e)
            {
                Riflemaster.MyLogger.LogError(e);
            }
        }
    }
}
