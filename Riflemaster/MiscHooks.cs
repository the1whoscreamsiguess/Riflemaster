using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using MoreSlugcats;

namespace Riflemaster
{
    public static class MiscHooks
    {

        public static void OnEnable()
        {
            On.Pomegranate.Update += Pomegranate_Update;
            On.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
        }

        //destroy challenge rifles in shelter before saving
        private static void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
        {
            for (int i = self.room.abstractRoom.entities.Count - 1; i >= 0; i--)
            {
                if (self.room.abstractRoom.entities[i] is AbstractPhysicalObject)
                {
                    AbstractPhysicalObject obj = self.room.abstractRoom.entities[i] as AbstractPhysicalObject;

                    if (obj.realizedObject != null && obj.realizedObject is JokeRifle && Dictionaries.jokeRifleCustom[obj.realizedObject as JokeRifle].challengeOwner != null)
                    {
                        obj.realizedObject.Destroy();
                        self.room.RemoveObject(obj.realizedObject);
                        self.room.abstractRoom.RemoveEntity(obj);
                    }

                }
               
            }
            orig(self);
        }

        //needle bullet lodge in smashed pomegranate
        private static void Pomegranate_Update(On.Pomegranate.orig_Update orig, Pomegranate self, bool eu)
        {
            orig(self, eu);
            if (self.smashed && ModManager.MSC && !self.spearmasterStabbed)
            {
                for (int j = 0; j < self.room.physicalObjects[0].Count; j++)
                {
                    if (self.room.physicalObjects[0][j] is Bullet)
                    {
                        Bullet bullet = self.room.physicalObjects[0][j] as Bullet;
                        if (RiflemasterCustom.IsNeedleBullet(bullet) && bullet.mode == Weapon.Mode.Thrown)
                        {
                            for (int k = 0; k < self.bodyChunks.Length; k++)
                            {
                                BodyChunk bodyChunk = self.bodyChunks[k];
                                float num2 = Custom.CirclesCollisionTime(bullet.firstChunk.lastPos.x, bullet.firstChunk.lastPos.y, bodyChunk.pos.x, bodyChunk.pos.y, bullet.firstChunk.pos.x - bullet.firstChunk.lastPos.x, bullet.firstChunk.pos.y - bullet.firstChunk.lastPos.y, bullet.firstChunk.rad + ((bullet.thrownBy != null && bullet.thrownBy is Player) ? 5f : 0f), bodyChunk.rad);
                                if (num2 > 0f && num2 < 1f)
                                {
                                    bullet.meleeHitChunk = self.firstChunk;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
//so, funny story
//i initially tried to do deleting challenge rifles from shelters directly in AdaptRegionStateToWorld
//somehow i fucked it up so bad that it a) crashed the game and b) DELETED THE ENTIRE SAVE SLOT I WAS TESTING IT ON
//so umm... needless to say i am not risking something like that getting left in