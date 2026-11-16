using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;

namespace Riflemaster
{
    //spearmaster's spear umbilical was ripped from spitter spiders... and now the needle bullet umbilicals are ripped from spearmaster's spear umbilical
    //these ones decay twice as fast and will immediately break when the bullet exits its thrown state
    public class Umbilical : CosmeticSprite
    {
        public Vector2[,] points;
        public Bullet bullet;
        public JokeRifle rifle;
        public Color fogColor;
        public Color threadCol;
        public Color blackColor;

        public Umbilical(Room room, Bullet bullet, JokeRifle rifle, Vector2 shootVel)
        {
            this.room = room;
            this.bullet = bullet;
            this.rifle = rifle;
            this.points = new Vector2[UnityEngine.Random.Range(10, 20), 4];
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                //pos, lastPos, vel, (life left, lifespan)
                this.points[i, 0] = rifle.firePos + Custom.RNV();
                this.points[i, 1] = rifle.firePos;
                this.points[i, 2] = shootVel * (0.3f * UnityEngine.Random.value) + Custom.RNV() * (UnityEngine.Random.value * 1.5f);
                this.points[i, 3] = new Vector2(2f, Mathf.Lerp(75f, 100f, Mathf.Pow(UnityEngine.Random.value, 0.3f)));
            }
        }

        public float LifeOfSegment(int i)
        {
            if (i > 0)
            {
                return Mathf.Min(this.points[i, 3].x, this.points[i - 1, 3].x);
            }
            return this.points[i, 3].x;
        }

        public bool RifleEnteringShortcut()
        {
            return rifle.room == null || (rifle.grabbedBy.Count > 0 && rifle.grabbedBy[0] != null && rifle.grabbedBy[0].grabber != null && rifle.grabbedBy[0].grabber.enteringShortCut != null);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            bool flag = true;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                this.points[i, 1] = this.points[i, 0];
                this.points[i, 0] += this.points[i, 2];
                this.points[i, 2] *= Custom.LerpMap(this.points[i, 2].magnitude, 1f, 30f, 0.99f, 0.8f);
                this.points[i, 2].y -= Mathf.Lerp(0.1f, 0.6f, this.LifeOfSegment(i));
                if (this.LifeOfSegment(i) > 0f)
                {
                    if (this.room.GetTile(this.points[i, 0]).Solid)
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(this.points[i, 0], this.points[i, 1], this.points[i, 2], 1f, default(IntVector2), true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                        this.points[i, 0] = terrainCollisionData.pos;
                        this.points[i, 2] = terrainCollisionData.vel;
                    }
                    if (i > 0)
                    {
                        if (!Custom.DistLess(this.points[i, 0], this.points[i - 1, 0], 6f))
                        {
                            Vector2 a = Custom.DirVec(this.points[i, 0], this.points[i - 1, 0]) * (Vector2.Distance(this.points[i, 0], this.points[i - 1, 0]) - 6f);
                            this.points[i, 0] += a * 0.15f;
                            this.points[i, 2] += a * 0.25f;
                            this.points[i - 1, 0] -= a * 0.15f;
                            this.points[i - 1, 2] -= a * 0.25f;
                        }
                        if (!this.room.VisualContact(this.points[i, 0], this.points[i - 1, 0]))
                        {
                            this.points[i, 3].x -= 0.2f;
                        }
                    }
                    if (i > 1 && this.LifeOfSegment(i - 1) > 0f)
                    {
                        this.points[i, 2] += Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                        this.points[i - 2, 2] -= Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                    }
                    this.points[i, 3].x -= 1f / this.points[i, 3].y;
                    if (this.points[i, 3].x > 0f)
                    {
                        flag = false;
                    }
                }
            }
            if (this.LifeOfSegment(0) > 0f)
            {
                this.points[0, 0] = rifle.firePos;
                this.points[0, 2] *= 0f;
                this.points[1, 2] += Custom.DirVec(rifle.firePos, rifle.firePos) * 3f;
                this.points[3, 2] += Custom.DirVec(rifle.firePos, rifle.firePos) * 1.5f;
                if (this.LifeOfSegment(1) <= 0f || RifleEnteringShortcut() || this.rifle.room != this.room || this.bullet.mode != Weapon.Mode.Thrown)
                {
                    this.points[0, 3].x -= 1f;
                }
            }
            if (this.LifeOfSegment(this.points.GetLength(0) - 1) > 0f)
            {
                Vector2 rotation = this.bullet.rotation * Dictionaries.needleBulletCustom[bullet].size.y;
                rotation.Scale(new Vector2(25f, 25f));
                this.points[this.points.GetLength(0) - 1, 0] = this.bullet.bodyChunks[0].pos - rotation;
                this.points[this.points.GetLength(0) - 1, 2] *= 0f;
                this.points[this.points.GetLength(0) - 2, 2] += Custom.DirVec(this.bullet.bodyChunks[0].pos, this.bullet.bodyChunks[0].pos - rotation) * 6f;
                this.points[this.points.GetLength(0) - 3, 2] += Custom.DirVec(this.bullet.bodyChunks[0].pos, this.bullet.bodyChunks[0].pos - rotation) * 1.5f;
                if (this.bullet.slatedForDeletetion || this.bullet.room != this.room)
                {
                    this.points[this.points.GetLength(0) - 1, 3].x -= 1f;
                }
            }
            if (flag)
            {
                this.Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(this.points.GetLength(0), false, true);
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector;
            if (this.LifeOfSegment(0) > 0f && this.rifle != null)
            {
                vector = rifle.firePos;
            }
            else
            {
                vector = Vector2.Lerp(this.points[0, 1], this.points[0, 0], timeStacker);
            }
            float num = 0f;
            float b = 1f;
            bool flag = rCam.room.Darkness(this.pos) > 0.2f;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                float f = Mathf.InverseLerp(0f, (float)(this.points.GetLength(0) - 1), (float)i);
                float num2 = this.LifeOfSegment(i);
                if (i < this.points.GetLength(0) - 1)
                {
                    num2 = Mathf.Min(num2, this.LifeOfSegment(i + 1));
                }
                float num3 = 0.5f * Mathf.InverseLerp(0f, 0.3f, num2);
                Vector2 vector2 = Vector2.Lerp(this.points[i, 1], this.points[i, 0], timeStacker);
                if (i == 0 && this.LifeOfSegment(0) > 0f)
                {
                    vector2 = vector;
                }
                else if (i == this.points.GetLength(0) - 1 && this.LifeOfSegment(i) > 0f)
                {
                    Vector2 rotation = this.bullet.rotation;
                    rotation.Scale(new Vector2(25f, 25f));
                    vector2 = Vector2.Lerp(this.bullet.bodyChunks[0].pos - rotation, this.bullet.bodyChunks[0].pos - rotation, timeStacker);
                }
                Vector2 a = Custom.PerpendicularVector(vector2, vector);
                Vector2 b2 = a * ((num3 + num) * 0.5f);
                Vector2 b3 = a * num3;
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, (vector + vector2) / 2f - b2 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, (vector + vector2) / 2f + b2 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - b3 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + b3 - camPos);
                Color color = Color.Lerp(this.fogColor, Color.Lerp(new Color(1f, 0f, 0f), Color.Lerp(this.threadCol, new Color(1f, 1f, 1f), rCam.room.WaterShinyness(vector2, timeStacker)), 0.1f + 0.9f * Mathf.Pow(f, 0.25f + num2)), Mathf.Min(num2, b));
                if (flag && num2 > 0f)
                {
                    color = Color.Lerp(color, this.blackColor, rCam.room.DarknessOfPoint(rCam, vector2));
                }
                for (int j = 0; j < 4; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[i * 4 + j] = color;
                }
                vector = vector2;
                num = num3;
                b = num2;
            }
            for (int k = 0; k < 4; k++)
            {
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[k] = Color.Lerp(this.fogColor, this.blackColor, this.LifeOfSegment(0));
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        // Token: 0x06002BF6 RID: 11254 RVA: 0x0030204C File Offset: 0x0030024C
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.blackColor = palette.blackColor;
            this.fogColor = palette.fogColor;
            this.threadCol = Color.Lerp(new Color(0.95f, 0.8f, 0.55f), palette.fogColor, 0.2f);
        }


    }
}
