using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace Riflemaster
{
    public static class BulletHooks
    {

        public static int needleBulletFadeCounterMax = 400;

        public static void OnEnable()
        {
            On.MoreSlugcats.Bullet.ctor += Bullet_ctor;
            On.MoreSlugcats.Bullet.InitiateSprites += Bullet_InitiateSprites;
            On.MoreSlugcats.Bullet.DrawSprites += Bullet_DrawSprites;
            On.MoreSlugcats.Bullet.HitSomething += Bullet_HitSomething;
            On.MoreSlugcats.Bullet.HitWall += Bullet_HitWall;
            On.MoreSlugcats.Bullet.Update += Bullet_Update;

            On.Weapon.ChangeMode += Weapon_ChangeMode;
            On.Weapon.HitSomethingWithoutStopping += Weapon_HitSomethingWithoutStopping;

            try { IL.MoreSlugcats.Bullet.HitSomething += Bullet_HitSomething_IL; }
            catch (Exception e)
            {
                Riflemaster.MyLogger.LogMessage(e);
            }
        }

        //initialize needle bullet dictionary values
        private static void Bullet_ctor(On.MoreSlugcats.Bullet.orig_ctor orig, Bullet self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            orig(self, abstractPhysicalObject, world);
            if (RiflemasterCustom.IsNeedleBullet(self) && !Dictionaries.needleBulletCustom.ContainsKey(self))
            {
                Dictionaries.BulletCustom bCustom = new Dictionaries.BulletCustom();
                bCustom.fadeCounter = needleBulletFadeCounterMax;
                bCustom.pivotAtTip = new bool[2];
                bCustom.stuckBodyPart = -1;
                bCustom.stuckInChunkIndex = -1;


                //random value assignment based on id
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractPhysicalObject.ID.RandomSeed);

                bCustom.type = UnityEngine.Random.Range(0, 3);
                bCustom.size = new Vector2(UnityEngine.Random.Range(0.4f, 0.6f), UnityEngine.Random.Range(0.4f, 0.6f));
                bCustom.rotationOffset = Custom.RNV() * 0.35f;

                UnityEngine.Random.state = state;


                Dictionaries.needleBulletCustom.Add(self, bCustom);


            }
        }

       //Needle bullet draw
        private static void Bullet_DrawSprites(On.MoreSlugcats.Bullet.orig_DrawSprites orig, Bullet self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            //the following code pretty much ripped directly from spearmaster's spears with the variable names changed
            if (RiflemasterCustom.IsNeedleBullet(self))
            {
                Dictionaries.BulletCustom bCustom = Dictionaries.needleBulletCustom[self];
                float num2 = (float)bCustom.fadeCounter / (float)needleBulletFadeCounterMax;
                if (num2 < 0.01f)
                {
                    num2 = 0.01f;
                }
                //if i'm reading the jolly needle code right it would mean that needle spears never lose their color??? this is ripped directly from msc code so i'm keeping it in for parity
                if (ModManager.CoopAvailable && bCustom.jollyCustomColor != null)
                {
                    sLeaser.sprites[0].color = bCustom.jollyCustomColor.Value;
                }
                else if (PlayerGraphics.CustomColorsEnabled())
                {
                    sLeaser.sprites[0].color = Color.Lerp(PlayerGraphics.CustomColorSafety(2), self.color, 1f - num2);
                }
                else
                {
                    sLeaser.sprites[0].color = Color.Lerp(new Color(1f, 1f, 1f, 1f), self.color, 1f - num2);
                }
                sLeaser.sprites[1].color = sLeaser.sprites[0].color;

                //rotation

                Vector3 v = Vector3.Slerp(self.lastRotation, self.rotation, timeStacker);
                sLeaser.sprites[0].anchorY = Mathf.Lerp(bCustom.pivotAtTip[1] ? 0.85f : 0.5f, bCustom.pivotAtTip[0] ? 0.85f : 0.5f, timeStacker);
                    sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), v);

                //slowly sink into the ground and disappear at end of lifetime
                if (self.abstractBullet.timeToLive <= 400)
                {
                    float decay = Mathf.Pow(1 - (self.abstractBullet.timeToLive / 400f), 2);
                    sLeaser.sprites[0].y -= 20 * decay;
                    sLeaser.sprites[0].scaleX = Dictionaries.needleBulletCustom[self].size.x * (1 - (decay / 2));
                    sLeaser.sprites[0].scaleY = Dictionaries.needleBulletCustom[self].size.y * (1 - decay);
                }
            }
        }

        //needle bullet jellyfish, seedcob, and pomegranate handling
        private static bool Bullet_HitSomething(On.MoreSlugcats.Bullet.orig_HitSomething orig, Bullet self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool origResult = orig(self, result, eu);
            bool stick = false;
            if (RiflemasterCustom.IsNeedleBullet(self) && result.obj != null)
            {
                if (result.obj is JellyFish)
                {
                    if (!(result.obj as JellyFish).dead)
                    {
                        Player player = null;
                        if (self.thrownBy != null && self.thrownBy is Player)
                        {
                            player = self.thrownBy as Player;
                        }
                        if (player != null)
                        {
                            player.AddQuarterFood();
                            player.AddQuarterFood();
                        }
                        if (player.AI == null && self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
                        {
                            self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(result.obj);
                        }
                    }
                    (result.obj as JellyFish).dead = true;
                    stick = true;
                }
                else if (result.obj is SeedCob || (result.obj is Pomegranate && (result.obj as Pomegranate).smashed))
                {
                    if (self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
                    {
                        self.room.game.GetStorySession.playerSessionRecords[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(result.obj);
                    }
                    (self.thrownBy as Player).AddFood(5);
                    if (result.obj is SeedCob)
                    {
                        (result.obj as SeedCob).Open();
                    }
                    else if (result.obj is Pomegranate)
                    {
                        (result.obj as Pomegranate).spearmasterStabbed = true;
                    }

                    stick = true;
                }

                if (stick)
                {
                    self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk.pos, 0.9f, 1.05f);
                    RiflemasterCustom.NeedleBulletStickInObject(self, result.obj, result, eu);
                }
            }
            return origResult || stick;
        }

        //needle bullet hitting wall
        private static void Bullet_HitWall(On.MoreSlugcats.Bullet.orig_HitWall orig, Bullet self)
        {
            if (RiflemasterCustom.IsNeedleBullet(self))
            {
                if (self.room.BeingViewed)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        self.room.AddObject(new Spark(self.firstChunk.pos + self.throwDir.ToVector2() * (self.firstChunk.rad - 1f), Custom.DegToVec(UnityEngine.Random.value * 360f) * (10f * UnityEngine.Random.value) + -self.throwDir.ToVector2() * 10f, new Color(1f, 1f, 1f), null, 2, 4));
                    }
                }
                self.room.ScreenMovement(new Vector2?(self.firstChunk.pos), self.throwDir.ToVector2() * 1.5f, 0f);
                self.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.firstChunk.pos, 0.9f, 1.05f);
                self.SetRandomSpin();
                self.ChangeMode(Weapon.Mode.Free);
            }
            else
            {
                orig(self);
            }
        }

        //needle bullet visual, looks like a tiny spearmaster spear with random size variation
        private static void Bullet_InitiateSprites(On.MoreSlugcats.Bullet.orig_InitiateSprites orig, Bullet self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (RiflemasterCustom.IsNeedleBullet(self))
            {
                sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("BioSpear" + (Dictionaries.needleBulletCustom[self].type % 3 + 1).ToString());
                sLeaser.sprites[0].scaleX = Dictionaries.needleBulletCustom[self].size.x;
                sLeaser.sprites[0].scaleY = Dictionaries.needleBulletCustom[self].size.y;
            }
        }

        //Needle bullet color fading and sticking in objects
        private static void Bullet_Update(On.MoreSlugcats.Bullet.orig_Update orig, MoreSlugcats.Bullet self, bool eu)
        {
            orig(self, eu);
            if (RiflemasterCustom.IsNeedleBullet(self))
            {
                Dictionaries.BulletCustom bCustom = Dictionaries.needleBulletCustom[self];
                //decrease fade counter when not in thrown state
                if (bCustom.fadeCounter > 0 && self.mode != Weapon.Mode.Thrown)
                {
                    bCustom.fadeCounter--;
                }

                //dislodge
                if (self.mode == Weapon.Mode.StuckInCreature && self.abstractBullet.timeToLive <= 400)
                {
                    self.ChangeMode(Weapon.Mode.Free);
                }

                //as i continuously rip more and more code from spears to replicate their behavior in needle bullets, i realize with increasing horror that it would have been
                //way easier and better for mod compatibility to do this the other way around and have the needle bullet be a spear with bullet code glued to it instead...
                //oh well! it's too late to change now
                bCustom.pivotAtTip[1] = bCustom.pivotAtTip[0];
                bCustom.pivotAtTip[0] = (self.mode == Weapon.Mode.Thrown || self.mode == Weapon.Mode.StuckInCreature);
                if (self.mode == Weapon.Mode.StuckInCreature)
                {
                    if (bCustom.stuckInAppendage != null)
                    {
                        self.setRotation = new Vector2?(Custom.DegToVec(bCustom.stuckRotation + Custom.VecToDeg(bCustom.stuckInAppendage.appendage.OnAppendageDirection(bCustom.stuckInAppendage))));
                        self.firstChunk.pos = bCustom.stuckInAppendage.appendage.OnAppendagePosition(bCustom.stuckInAppendage);
                    }
                    else
                    {
                        self.firstChunk.vel = RiflemasterCustom.NeedleBulletStuckInChunk(self).vel;
                        if (bCustom.stuckBodyPart == -1 || !self.room.BeingViewed || (RiflemasterCustom.NeedleBulletStuckInChunk(self).owner as Creature).BodyPartByIndex(bCustom.stuckBodyPart) == null)
                        {
                            self.setRotation = new Vector2?(Custom.DegToVec(bCustom.stuckRotation + Custom.VecToDeg(RiflemasterCustom.NeedleBulletStuckInChunk(self).Rotation)));
                            self.firstChunk.MoveWithOtherObject(eu, RiflemasterCustom.NeedleBulletStuckInChunk(self), new Vector2(0f, 0f));
                        }
                        else
                        {
                            self.setRotation = new Vector2?(Custom.DegToVec(bCustom.stuckRotation + Custom.AimFromOneVectorToAnother(RiflemasterCustom.NeedleBulletStuckInChunk(self).pos, (RiflemasterCustom.NeedleBulletStuckInChunk(self).owner as Creature).BodyPartByIndex(bCustom.stuckBodyPart).pos)));
                            self.firstChunk.MoveWithOtherObject(eu, RiflemasterCustom.NeedleBulletStuckInChunk(self), Vector2.Lerp(RiflemasterCustom.NeedleBulletStuckInChunk(self).pos, (RiflemasterCustom.NeedleBulletStuckInChunk(self).owner as Creature).BodyPartByIndex(bCustom.stuckBodyPart).pos, 0.5f) - RiflemasterCustom.NeedleBulletStuckInChunk(self).pos);
                        }
                        self.setRotation = (self.setRotation.Value + bCustom.rotationOffset).normalized;
                    }

                    if (RiflemasterCustom.NeedleBulletStuckInChunk(self) != null && RiflemasterCustom.NeedleBulletStuckInChunk(self).owner.slatedForDeletetion)
                    {
                        self.ChangeMode(Weapon.Mode.Free);
                    }
                }

                Dictionaries.needleBulletCustom[self] = bCustom;
            }
        }

        //needle bullet mode changing behaviors
        private static void Weapon_ChangeMode(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            if (self is Bullet && RiflemasterCustom.IsNeedleBullet(self as Bullet))
            {
                if (newMode != Weapon.Mode.Thrown)
                {
                    self.firstChunk.rad = 1f; //shrink the bullet's chunk so that it doesn't appear to float above the ground when resting
                }
                //if sticking in a creature, increase lifetime
                //it's really long because turning things into pincushions is funny
                if (newMode == Weapon.Mode.StuckInCreature)
                {
                    (self as Bullet).abstractBullet.timeToLive = Mathf.RoundToInt(Mathf.Lerp(2500, 3500, Mathf.Pow(UnityEngine.Random.value, 2)));
                }

                //render behind stuck in object
                if (self.mode == Weapon.Mode.StuckInCreature)
                {
                    RiflemasterCustom.NeedleBulletPulledOutOfStuckObject(self as Bullet);
                    self.ChangeOverlap(true);
                }
                else if (newMode == Weapon.Mode.StuckInCreature)
                {
                    self.ChangeOverlap(false);
                }
            }
            orig(self, newMode);
        }

        //needle bullet hitting small object
        private static void Weapon_HitSomethingWithoutStopping(On.Weapon.orig_HitSomethingWithoutStopping orig, Weapon self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            if (self is Bullet && RiflemasterCustom.IsNeedleBullet(self as Bullet))
            {
                if (obj is Creature)
                {
                    RiflemasterCustom.NeedleBulletHitSmallCreature(self as Bullet, obj as Creature);
                }

                if (obj is Mushroom)
                {
                    (self.thrownBy as Player).mushroomCounter += 320;
                    obj.Destroy();
                }
                if (obj is KarmaFlower)
                {
                    if ((self.thrownBy as Player).room.game.session is StoryGameSession && !((self.thrownBy as Player).room.game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma)
                    {
                        ((self.thrownBy as Player).room.game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = true;
                        int i = 0;
                        while (i < (self.thrownBy as Player).room.game.cameras.Length)
                        {
                            if ((self.thrownBy as Player).room.game.cameras[i].followAbstractCreature == (self.thrownBy as Player).abstractCreature || ModManager.CoopAvailable)
                            {
                                if ((self.thrownBy as Player).room.game.cameras[i].hud != null)
                                {
                                    (self.thrownBy as Player).room.game.cameras[i].hud.karmaMeter.reinforceAnimation = 0;
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                    obj.Destroy();
                }
                if (obj is OracleSwarmer)
                {
                    self.room.PlaySound(SoundID.Centipede_Shock, obj.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value);
                    if (self.room.game.IsStorySession)
                    {
                        if (self.room.game.GetStorySession.playerSessionRecords != null)
                        {
                            self.room.game.GetStorySession.playerSessionRecords[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(obj);
                        }
                        (self.thrownBy as Player).AddQuarterFood();
                        (self.thrownBy as Player).AddQuarterFood();
                    }
                    else
                    {
                        (self.thrownBy as Player).AddFood(1);
                    }
                    (self.thrownBy as Player).glowing = true;
                    if (self.room.game.session is StoryGameSession)
                    {
                        (self.room.game.session as StoryGameSession).saveState.theGlow = true;
                    }
                    Color color = Color.white;
                    if (obj is SSOracleSwarmer)
                    {
                        color = Custom.HSL2RGB(((obj as SSOracleSwarmer).color.x > 0.5f) ? Custom.LerpMap((obj as SSOracleSwarmer).color.x, 0.5f, 1f, 0.6666667f, 0.997222245f) : 0.6666667f, 1f, Mathf.Lerp(0.75f, 0.9f, (obj as SSOracleSwarmer).color.y));
                    }
                    self.room.AddObject(new Spark(obj.firstChunk.pos, Custom.RNV() * (60f * UnityEngine.Random.value), color, null, 20, 50));
                    obj.Destroy();
                    foreach (AbstractCreature abstractCreature in self.room.abstractRoom.creatures)
                    {
                        if (ModManager.DLCShared && abstractCreature != null && abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.Inspector && abstractCreature.realizedCreature != null && self.thrownBy != null && (abstractCreature.realizedCreature as Inspector).AI.VisualContact(self.thrownBy.firstChunk) && (abstractCreature.realizedCreature as Inspector).AI.VisualContact(self.firstChunk))
                        {
                            Custom.Log(new string[]
                            {
                        "Inspector saw neuron eaten!"
                            });
                            (abstractCreature.realizedCreature as Inspector).AI.preyTracker.AddPrey((abstractCreature.realizedCreature as Inspector).AI.tracker.RepresentationForCreature(self.thrownBy.abstractCreature, true));
                        }
                    }
                }
            }
            orig(self, obj, chunk, appendage);
        }

        //IL hooks (it's ok i forgive them)
    
        //Needle bullet hit behaviors
        public static void Bullet_HitSomething_IL(ILContext il)
        {
            try
            {

                //REPLACE VIOLENCE CALL

                ILLabel beforeViolenceLabel = null;
                //match part right before creature violence is triggered
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.Before,
                   x => x.MatchLdarg(1),
               x => x.MatchLdfld<SharedPhysics.CollisionResult>(nameof(SharedPhysics.CollisionResult.obj)),
               x => x.MatchIsinst(nameof(Creature)),
               x => x.MatchLdloc(3),
               x => x.MatchLdarg(0),
               x => x.MatchCall(out _),
               x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.vel))
                );

                beforeViolenceLabel = c.MarkLabel();

                //match part after creature violence trigger (this is where we're branching to)
                c.GotoNext(MoveType.After,
                    x => x.MatchCallvirt(nameof(Creature), nameof(Creature.Violence))//,
                    //x => x.MatchBr(out _)
                    );
                ILLabel afterViolenceLabel = c.MarkLabel();

                c.MoveAfterLabels();

                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldarg, 1);
                c.Emit(OpCodes.Ldarg, 2);
                //Insert needle bullet hit function after original violence
                c.EmitDelegate<Action<Bullet, SharedPhysics.CollisionResult, bool>>(RiflemasterCustom.NeedleBulletViolenceCheck);


                //now go back to before and insert the code
                c.GotoLabel(beforeViolenceLabel);
                c.MoveAfterLabels();

                c.Emit(OpCodes.Ldarg, 0); //Push self
                //Branch around the original violence call if self is a needle bullet
               
                c.EmitDelegate<Func<Bullet, bool>>((Bullet self) =>
                {
                    return RiflemasterCustom.IsNeedleBullet(self);
                });
                c.Emit(OpCodes.Brtrue, afterViolenceLabel);

                //DIFFERENT HIT SOUND

                //match BEFORE part where it makes a sound
                c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdsfld<SoundID>(nameof(SoundID.Rock_Hit_Creature))
     );
                c.MoveAfterLabels();
                ILLabel beforeSoundLabel = c.MarkLabel();

                //match AFTER part wehre it makes a sound
                c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall(out _),
                x => x.MatchCallvirt(out _),
                x => x.MatchPop()
                );
                c.MoveAfterLabels();
                ILLabel afterSoundLabel = c.MarkLabel();

                //now go back to before
                c.GotoLabel(beforeSoundLabel);

                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldarg, 1);
                //If this is a needle bullet, branch around the orignal sound code
                c.EmitDelegate<Func<Bullet, SharedPhysics.CollisionResult, bool>>((Bullet self, SharedPhysics.CollisionResult result) =>
                {
                    if (RiflemasterCustom.IsNeedleBullet(self))
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, afterSoundLabel);


            }
            catch (Exception e)
            {
                Riflemaster.MyLogger.LogError(e);
            }
        }
    
    }
}
