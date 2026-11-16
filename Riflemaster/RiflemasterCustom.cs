using DontEatTheSpiders;
using DontEatTheSpiders;
using EighthFoodPoint;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace Riflemaster
{
    //Custom code related to needle bullet feeding and sticking in creatures
    //A lot of this is copied directly from spear code! This SUCKS!!!! but it's the only way :(
   public static class RiflemasterCustom
    {

        public static BodyChunk NeedleBulletStuckInChunk(Bullet self)
        {
            if (Dictionaries.needleBulletCustom.ContainsKey(self) && Dictionaries.needleBulletCustom[self].stuckInObject != null)
            {
                return Dictionaries.needleBulletCustom[self].stuckInObject.bodyChunks[Dictionaries.needleBulletCustom[self].stuckInChunkIndex];
            }
            return null;
        }

        //don't eat the spiders compatibility
        //for some reason all the DETS classes except its options are private??? didn't make this too hard but why 2025 me...
        public static void DETS_NeedleBulletHitSmallCreature(Bullet self, Creature creature)
        {
            if (DETSConfig.configurables.ContainsKey(creature.abstractCreature.creatureTemplate.type))
            {
                Player player = null;
                if (self.thrownBy != null && self.thrownBy is Player)
                {
                    player = self.thrownBy as Player;
                }
                if (player != null)
                {
                    bool flag = creature.dead;
                    if (!flag)
                    {
                        if (self.room.game.IsStorySession)
                        {
                            if (self.room.game.GetStorySession.playerSessionRecords != null)
                            {
                                self.room.game.GetStorySession.playerSessionRecords[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(creature);
                            }
                        }
                        UnityEngine.Debug.Log(DETSConfig.configurables[creature.abstractCreature.creatureTemplate.type].Value);
                        //add 1/16th of a pip * the DETS configurable food value
                        AddFoodFraction(player, RemixSettings.needlesPerEighthPip.Value * (DETSConfig.configurables[creature.abstractCreature.creatureTemplate.type].Value / 2));
                    }
                }
            }
        }

        //Rifle spawn conditions when riflemaster challenge is active (player not already holding one)
        public static bool ShouldSpawnChallengeRifle(Player player)
        {
            if (RemixSettings.riflemasterChallenge.Value)
            {
                bool rifleFound = false;
                for (int i = 0; i < player.grasps.Length; i++)
                {
                    if (player.grasps[i] != null && player.grasps[i].grabbed != null && player.grasps[i].grabbed is JokeRifle)
                    {
                        rifleFound = true;
                        break;
                    }
                }
                return !rifleFound;
            }
            return false;
        }

        public static void NeedleBulletHitSmallCreature(Bullet self, Creature creature)
        {
            if (Riflemaster.DETSCompat)
            {
                DETS_NeedleBulletHitSmallCreature(self, creature);
            }
            if (!creature.dead && self.thrownBy != null && self.thrownBy is Player)
            {
                Player player = self.thrownBy as Player;
                if (creature is Fly)
                {
                    if (self.room.game.IsStorySession)
                    {
                        if (player.AI == null && self.room.game.GetStorySession.playerSessionRecords != null)
                        {
                            self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(creature);
                        }
                        player.AddQuarterFood();
                    }
                    else
                    {
                        player.AddFood(1);
                    }
                }
            }
            creature.Die();
        }

        //Clear out stick info, taken from spear code
        public static void NeedleBulletPulledOutOfStuckObject(Bullet self)
        {
            for (int i = 0; i < self.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (self.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearStick && (self.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).Spear == self.abstractPhysicalObject)
                {
                    self.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
                if (self.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearAppendageStick && (self.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).Spear == self.abstractPhysicalObject)
                {
                    self.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
            }
            Dictionaries.BulletCustom bCustom = Dictionaries.needleBulletCustom[self];
            bCustom.stuckInObject = null;
            bCustom.stuckInAppendage = null;
            bCustom.stuckInChunkIndex = 0;
            Dictionaries.needleBulletCustom[self] = bCustom;
        }

        //This is taken from spear code
        public static void NeedleBulletStickInObject(Bullet self, PhysicalObject obj, SharedPhysics.CollisionResult result, bool eu)
        {
            Dictionaries.BulletCustom bCustom = Dictionaries.needleBulletCustom[self];
            bCustom.stuckInObject = result.obj;
            self.ChangeMode(Weapon.Mode.StuckInCreature);
            self.firstChunk.collideWithObjects = false;

            if (result.chunk != null)
            {
                bCustom.stuckInChunkIndex = result.chunk.index;
                Dictionaries.needleBulletCustom[self] = bCustom; //set this early so that NeedleBulletStuckInChunk returns the correct result
                if (bCustom.stuckBodyPart == -1)
                {
                    bCustom.stuckRotation = Custom.Angle(self.throwDir.ToVector2(), NeedleBulletStuckInChunk(self).Rotation);
                }
                self.firstChunk.MoveWithOtherObject(eu, NeedleBulletStuckInChunk(self), new Vector2(0f, 0f));
                new AbstractPhysicalObject.AbstractSpearStick(self.abstractPhysicalObject, result.obj.abstractPhysicalObject, bCustom.stuckInChunkIndex, bCustom.stuckBodyPart, bCustom.stuckRotation);
            }
            else if (result.onAppendagePos != null)
            {
                bCustom.stuckInChunkIndex = 0;
                bCustom.stuckInAppendage = result.onAppendagePos;
                bCustom.stuckRotation = Custom.VecToDeg(self.rotation) - Custom.VecToDeg(bCustom.stuckInAppendage.appendage.OnAppendageDirection(bCustom.stuckInAppendage));
                new AbstractPhysicalObject.AbstractSpearAppendageStick(self.abstractPhysicalObject, result.obj.abstractPhysicalObject, result.onAppendagePos.appendage.appIndex, result.onAppendagePos.prevSegment, result.onAppendagePos.distanceToNext, bCustom.stuckRotation);
            }
            if (self.room.BeingViewed)
            {
                for (int i = 0; i < 2; i++)
                {
                    self.room.AddObject(new WaterDrip(result.collisionPoint, -self.firstChunk.vel * (UnityEngine.Random.value * 0.5f) + Custom.DegToVec(360f * UnityEngine.Random.value) * (self.firstChunk.vel.magnitude * UnityEngine.Random.value * 0.5f), false));
                }
            }
            Dictionaries.needleBulletCustom[self] = bCustom;
        }

        public static bool IsAttachedSpearmasterSpear(PhysicalObject spear)
        {
            return spear is Spear && (spear as Spear).spearmasterNeedle && (spear as Spear).spearmasterNeedle_hasConnection;
        }

        public static bool IsNeedleBullet(Bullet self)
        {
            return self.abstractBullet.bulletType == AbstractRifleAmmoType.SpearmasterNeedle;
        }

        //From bullet IL hook: if the passed bullet is a needle bullet, do special damage function and stick in the creature
        //This is only for creatures! Special interactions with objects (jellyfish) are in BulletHooks.Bullet_HitSomething because I didn't feel like wrangling the IL hook
        public static void NeedleBulletViolenceCheck(Bullet self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (RiflemasterCustom.IsNeedleBullet(self))
            {
                if (result.obj != null)
                {
                    Player player = null;
                    if (self.thrownBy != null && self.thrownBy is Player)
                    {
                        player = self.thrownBy as Player;
                    }
                    if (result.obj is Creature)
                    {
                        Creature creature = result.obj as Creature;

                        bool wasEggBug = false;


                        float damage = RemixSettings.damagePerNeedle.Value;

                        bool dontFeed = false;

                        if (creature.State != null && !(creature.State is HealthState))
                        {
                            if (creature.abstractCreature.creatureTemplate.instantDeathDamageLimit <= 1f)
                            {
                                damage = creature.abstractCreature.creatureTemplate.instantDeathDamageLimit;
                            }
                            else
                            {
                                dontFeed = true;
                            }
                        }

                        if (creature.SpearStick(self, damage, result.chunk, result.onAppendagePos, self.firstChunk.vel))
                        {
                            self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk.pos, 0.9f, 1.05f);
                            if (creature is EggBug && !(creature as EggBug).FireBug && !creature.dead)
                            {
                                wasEggBug = true;
                                (creature as EggBug).dropEggs = false;
                                if (player != null)
                                {
                                    player.AddFood(3);
                                    if (player.AI == null && self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
                                    {
                                        self.room.game.GetStorySession.playerSessionRecords[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(creature);
                                    }
                                }
                                damage = 1;
                            }
                            else if (!dontFeed)
                            {
                                if (player != null && FeedingMult(creature, result) > 0)
                                {
                                    FeedFromCreature(player, creature, result);
                                }
                            }
                            NeedleBulletStickInObject(self, creature, result, eu);
                        }
                        else
                        {
                            self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk.pos, 0.9f, 1.05f);
                        }
                        bool wasDead = creature.dead;

                        creature.Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass * 2), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, damage, 5f);

                        //give bonus food if this shot directly killed the creature
                        if (!wasEggBug && player != null && !wasDead && creature.dead && RemixSettings.killBonusEighths.Value > 0)
                        {
                            for (int i = 0; i < RemixSettings.killBonusEighths.Value; i++)
                            {
                                EighthFoodPoints.AddEighthFoodPoint(player);
                            }
                        }
                    }
                }
            }
        }

        public static void AddFoodFraction(Player player, float amount)
        {
            Dictionaries.playerBulletFeedProg[player] += amount;
            //give eighth pip if above food threshold
            if (Dictionaries.playerBulletFeedProg[player] >= RemixSettings.needlesPerEighthPip.Value)
            {
                while (Dictionaries.playerBulletFeedProg[player] >= RemixSettings.needlesPerEighthPip.Value)
                {
                    Dictionaries.playerBulletFeedProg[player] -= RemixSettings.needlesPerEighthPip.Value;
                    EighthFoodPoints.AddEighthFoodPoint(player);
                }
            }
        }
        
        //Determine how much food is obtained from shooting a creature based on its type and result
        //creatures not defined will attempt to estimate a value based on appendage hit, rough body shape, and whether it's considered a small creature
        public static float FeedingMult(Creature creature, SharedPhysics.CollisionResult result)
        {
            if (creature.dead || (creature is GarbageWorm) || (creature is Deer))
            {
                return 0;
            }
            if (creature is TentaclePlant || creature is PoleMimic)
            {
                return 1;
            }
            if (creature is Cicada || creature is TubeWorm || creature is Snail || creature is Leech
            || creature is VultureGrub || creature is SmallNeedleWorm || creature is Hazer)
            {
                return 0.5f;
            }
            if (creature is DaddyLongLegs)
            {
                return result.onAppendagePos != null ? 0.25f : 1f;
            }
            if (creature is BigSpider)
            {
                return ((creature as BigSpider).borrowedTime == -1) ? 1 : 0;
            }

            //If not on the above list, try to approximate what its food value should be

            //Small creatures
            if (creature.abstractCreature.creatureTemplate.smallCreature)
            {
                return 0.5f;
            }
            //Pole plant-like creatures where the whole body is an appendage
            if (creature.appendages != null && creature.appendages.Count == 1 && creature.bodyChunks.Length <= 2)
            {
                return 1f;

            }
            //Anything else
            return result.onAppendagePos != null ? 0.5f : 1f;
        }

        public static void FeedFromCreature(Player player, Creature creature, SharedPhysics.CollisionResult result)
        {
            float amount = 1 * FeedingMult(creature, result);
            AddFoodFraction(player, amount);
            if (player.AI == null && player.room.game.IsStorySession && player.room.game.GetStorySession.playerSessionRecords != null)
            {
                player.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(creature);
            }
        }
    }
}
