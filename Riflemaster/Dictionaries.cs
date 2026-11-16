using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;

namespace Riflemaster
{
    //what we have to do to mimic adding new values to an object
    public static class Dictionaries
    {
        public static Dictionary<Player, float> playerBulletFeedProg;
        public static Dictionary<Bullet, BulletCustom> needleBulletCustom;
        public static Dictionary<JokeRifle, JokeRifleCustom> jokeRifleCustom;
        public static Dictionary<ExplosiveSpear.SpearFragment, Color> needleSpearFragments;


        public static void OnEnable()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
        }

        //Initialize dictionaries
        public static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            Dictionaries.needleBulletCustom = new Dictionary<Bullet, BulletCustom>();
            Dictionaries.jokeRifleCustom = new Dictionary<JokeRifle, JokeRifleCustom>();
            Dictionaries.playerBulletFeedProg = new Dictionary<Player, float>();
            Dictionaries.needleSpearFragments = new Dictionary<ExplosiveSpear.SpearFragment, Color>();
        }

        //Struct containing all custom needle values so that we don't have to juggle 13 dictionaries
        //This requires getting a reference to the struct and then reassigning the bullet's dictionary entry every time you change a value
        public struct BulletCustom
        {
            public PhysicalObject stuckInObject;
            public int stuckInChunkIndex;
            public float stuckRotation;
            public int stuckBodyPart;
            public PhysicalObject.Appendage.Pos stuckInAppendage;

            public int fadeCounter;
            public int type;
            public Color? jollyCustomColor;
            public bool[] pivotAtTip;

            public Vector2 size;
            public Vector2 rotationOffset;
        }

        //Custom values for Spearmaster's special rifle
        public struct JokeRifleCustom
        {
            public Color? challengeRifleColor; //color will be different if this is set
            public Creature challengeOwner; //marks this as a challenge rifle if not null + starts disintegration if ever not being held by this creature
            public int disintegrateCounter; //disintegrate animation plays if this is above 0, also prevents pickup
        }

    }
}
