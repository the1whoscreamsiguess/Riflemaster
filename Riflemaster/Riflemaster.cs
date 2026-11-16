using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using System.Security.Permissions;
using MoreSlugcats;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Riflemaster
{
    [BepInPlugin("t1ws.riflemaster", "Riflemaster", "1.0")]
   [BepInDependency("d596de70-746a-480d-88d8-18028645190c.EighthFoodPoint", BepInDependency.DependencyFlags.HardDependency)]
    public class Riflemaster : BaseUnityPlugin
    {
        static bool initiated;

        public static bool DETSCompat;

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public void Awake()
        {
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(Riflemaster.Init);
        }

        static void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (!initiated)
            {
                initiated = true;
                AbstractRifleAmmoType.RegisterValues();
                Futile.atlasManager.LoadAtlas("atlases/NeedleSpearAtlas");
                MachineConnector.SetRegisteredOI("t1ws.riflemaster", new RemixSettings());
            }
            DETSCompat = false;
            UnityEngine.Debug.Log("Riflemaster checking for companions");
            for (int i = 0; i < ModManager.ActiveMods.Count; i++)
            {
                if (ModManager.ActiveMods[i].id == "donteatthespiders")
                {
                    UnityEngine.Debug.Log("Riflemaster found DontEatTheSpiders");
                    DETSCompat = true;
                }
            }
        }

        public void OnEnable()
        {
            MyLogger = Logger;
            JokeRifleHooks.OnEnable();
            BulletHooks.OnEnable();
            PlayerHooks.OnEnable();
            SpearHooks.OnEnable();
            SpearFragmentHooks.OnEnable();
            Dictionaries.OnEnable();
            MiscHooks.OnEnable();
        }

    }
}