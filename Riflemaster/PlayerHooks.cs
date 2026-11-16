using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil.Cil;

namespace Riflemaster
{
    public static class PlayerHooks
    {

        public static void OnEnable()
        {
            On.Player.ctor += Player_ctor; 
            On.Player.Grabability += Player_Grabability;
            On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpeckles_DrawSprites;
            On.PlayerGraphics.TailSpeckles.setSpearProgress += TailSpeckles_setSpearProgress;

            IL.Player.GrabUpdate += Player_GrabUpdate;
        }



        //Initialize needle bullet feeding value
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!Dictionaries.playerBulletFeedProg.ContainsKey(self))
            {
                Dictionaries.playerBulletFeedProg.Add(self, 0);
            }
        }

        //disintegrating challenge rifles cannot be picked up
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is JokeRifle && Dictionaries.jokeRifleCustom[obj as JokeRifle].disintegrateCounter > 0)
            {
                return Player.ObjectGrabability.CantGrab;
            }
            return orig(self, obj);
        }

        //Challenge mode rifle from tail visual
        private static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (RiflemasterCustom.ShouldSpawnChallengeRifle(self.pGraphics.player))
            {
                FSprite sprite = sLeaser.sprites[self.startSprite + self.lines * self.rows];
                sprite.element = Futile.atlasManager.GetElementWithName("JokeRifle");
                //the rifle sprite is at a different rotation so swap the vectors around
                sprite.scaleX = sprite.scaleY;
                sprite.scaleY = 1;
                sprite.rotation -= 90f;
                sprite.anchorX = 0f;
                sprite.anchorY = 0.5f;
            }
            else
            {
                //undo changes made when drawing rifle
                FSprite sprite = sLeaser.sprites[self.startSprite + self.lines * self.rows];
                sprite.scaleX = 1;
                sprite.anchorX = 0.5f;
                sprite.anchorY = 0f;
            }
        }

        //Riflemaster challenge rifle pulling
        private static void TailSpeckles_setSpearProgress(On.PlayerGraphics.TailSpeckles.orig_setSpearProgress orig, PlayerGraphics.TailSpeckles self, float p)
        {
            //p being 1 indicates that Spearmaster is about to pull a spear, so do special checks instead
            if (p == 1 && RemixSettings.riflemasterChallenge.Value)
            {
                Player player = self.pGraphics.player;

                //Intercept the value and spawn a rifle instead of a spear if player meets the spawn condition
                if (RiflemasterCustom.ShouldSpawnChallengeRifle(player))
                {
                    //visual and audio effects
                    player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab, 0f, 1f, 0.5f + UnityEngine.Random.value * 1.5f);
                    player.smSpearSoundReady = false;
                    Vector2 pos = self.pGraphics.tail[(int)((float)self.pGraphics.tail.Length / 2f)].pos;
                    for (int j = 0; j < 4; j++)
                    {
                        Vector2 a = Custom.DirVec(pos, player.bodyChunks[1].pos);
                        player.room.AddObject(new WaterDrip(pos + Custom.RNV() * (UnityEngine.Random.value * 1.5f), Custom.RNV() * (3f * UnityEngine.Random.value) + a * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), false));
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        Vector2 a2 = Custom.RNV();
                        player.room.AddObject(new Spark(pos + a2 * (UnityEngine.Random.value * 40f), a2 * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                    }

                    //spawn the new rifle
                    JokeRifle.AbstractRifle abstractRifle = new JokeRifle.AbstractRifle(player.room.world, null, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.room.game.GetNewID(), AbstractRifleAmmoType.SpearmasterNeedle);
                    player.room.abstractRoom.AddEntity(abstractRifle);
                    abstractRifle.pos = player.abstractCreature.pos;
                    abstractRifle.RealizeInRoom();
                    Vector2 vector = player.bodyChunks[0].pos;
                    Vector2 a3 = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                    if (Mathf.Abs(player.bodyChunks[0].pos.y - player.bodyChunks[1].pos.y) > Mathf.Abs(player.bodyChunks[0].pos.x - player.bodyChunks[1].pos.x) && player.bodyChunks[0].pos.y > player.bodyChunks[1].pos.y)
                    {
                        vector += Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos) * 5f;
                        a3 *= -1f;
                        a3.x += 0.4f * (float)player.flipDirection;
                        a3.Normalize();
                    }
                    abstractRifle.realizedObject.firstChunk.HardSetPosition(vector);
                    abstractRifle.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((a3 * 2f + Custom.RNV() * UnityEngine.Random.value) / abstractRifle.realizedObject.firstChunk.mass, 6f);
                    if (player.FreeHand() > -1)
                    {
                        player.SlugcatGrab(abstractRifle.realizedObject, player.FreeHand());
                    }

                    //Set flags to make this a challenge rifle
                    JokeRifle rifle = abstractRifle.realizedObject as JokeRifle;
                    Dictionaries.JokeRifleCustom rifleCustom = Dictionaries.jokeRifleCustom[rifle];
                    Color color = new Color(1, 1, 1);
                    rifle.lastShotTime = 10; //prevent light shooting out on spawn
                    abstractRifle.destroyOnAbstraction = true;
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

                    rifleCustom.challengeRifleColor = color;
                    rifleCustom.challengeOwner = player;
                    Dictionaries.jokeRifleCustom[rifle] = rifleCustom;
                    player.wantToThrow = 0;

                    p = 0; //set new tailspeckles value to 0 as though a spear was successfully pulled
                }
            }
            orig(self, p);
        }

        //IL hooks

        private static void Player_GrabUpdate(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);

                //challenge rifles only accept needles as ammo
                //also prevent SM's special pearl from being used as ammo
                c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(Creature).GetProperty(nameof(Creature.grasps)).GetGetMethod()),
                x => x.MatchLdloc(12),
                x => x.MatchLdelemRef(),
                x => x.MatchLdfld<Creature.Grasp>(nameof(Creature.Grasp.grabbed)),
                x => x.MatchCall<JokeRifle>(nameof(JokeRifle.IsValidAmmo))
                );

                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<bool, Player, bool>>((bool originalResult, Player self) =>
                {
                    JokeRifle rifle = null;
                    PhysicalObject otherItem = null;
                    for (int i = 0; i < self.grasps.Length; i++)
                    {
                        if (self.grasps[i] != null)
                        {
                            if (self.grasps[i].grabbed is JokeRifle)
                            {
                                rifle = self.grasps[i].grabbed as JokeRifle;
                            }
                            else
                            {
                                otherItem = self.grasps[i].grabbed;
                            }
                        }
                    }
                    if (otherItem != null && otherItem is SpearMasterPearl)
                    {
                        return false;
                    }
                    if (rifle != null && Dictionaries.jokeRifleCustom[rifle].challengeOwner != null)
                    {
                        return otherItem != null && RiflemasterCustom.IsAttachedSpearmasterSpear(otherItem);
                    }
                    return originalResult;
                });

            }
            catch (Exception e)
            {
                Riflemaster.MyLogger.LogError(e);
            }
        }

    }
}
