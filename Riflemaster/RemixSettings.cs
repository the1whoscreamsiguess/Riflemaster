using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace Riflemaster
{
    //remix menu
    public class RemixSettings : OptionInterface
    {
        public static Configurable<int> needlesPerEighthPip;
        public static Configurable<int> bulletsPerNeedle;
        public static Configurable<float> damagePerNeedle;
        public static Configurable<int> ticksPerShot;
        public static Configurable<bool> spawnUmbilicals;
        public static Configurable<int> killBonusEighths;

        List<UIelement> elements;

        public static Configurable<bool> riflemasterChallenge;
        bool challengeUnlocked = false;

        OpCheckBox challengeBox;

        public RemixSettings()
        {
            needlesPerEighthPip = this.config.Bind<int>("needlesPerEighthPip", 1);
            bulletsPerNeedle = this.config.Bind<int>("bulletsPerNeedle", 20);
            damagePerNeedle = this.config.Bind<float>("damagePerNeedle", 0.1f);
            ticksPerShot = this.config.Bind<int>("ticksPerShot", 6);
            spawnUmbilicals = this.config.Bind<bool>("spawnUmbilicals", true);
            killBonusEighths = this.config.Bind<int>("killBonusEighths", 1);

            riflemasterChallenge = this.config.Bind<bool>("riflemasterChallenge", false);

            elements = new List<UIelement>();
        }

        void Add(UIelement element, string description)
        {
            elements.Add(element);
            if (description != "")
            {
                element.description = description;
            }
            this.Tabs[0]._AddItem(element);
        }

        public override void Initialize()
        {
            base.Initialize();

            challengeUnlocked = false;

            this.Tabs = new OpTab[]
                {
            new OpTab(this, "Options")
                };

            //big title text
            Add(new OpLabel(new UnityEngine.Vector2(0f, 500f), new UnityEngine.Vector2(600f, 100f), "Riflemaster", FLabelAlignment.Center, true), "");

            Vector2 pos = new Vector2(10, 500f);
            Vector2 posIncrement = new Vector2(0, -40f);
            Vector2 labelOffset = new Vector2(110f, 0);

            string text = "Number of hits needed to gain one eighth of a food pip";
            OpSlider slider = new OpSlider(needlesPerEighthPip, pos)
            {
                min = 1,
                max = 4,
                _increment = 1
            };
            Add(slider, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Hits Per Eighth Pip", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Ammunition gained per needle loaded";
            slider = new OpSlider(bulletsPerNeedle, pos)
            {
                min = 1,
                max = 100,
                _increment = 1
            };
            Add(slider, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Shots Per Needle", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Damage dealt per needle shot (more damage = fewer hits = less food)";
            OpFloatSlider floatSlider = new OpFloatSlider(damagePerNeedle, pos, 100)
            {
                min = 0,
                max = 1
            };
            Add(floatSlider, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Damage Per Hit", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Number of frames between shots (lower = faster)";
            slider = new OpSlider(ticksPerShot, pos)
            {
                min = 0,
                max = 40
            };
            Add(slider, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Frames Per Shot", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Toggle small cords connecting shots to your rifle. Disable to reduce visual clutter and increase performance";
            Add(new OpCheckBox(spawnUmbilicals, pos + new Vector2(0, 3)), text);
            Add(new OpLabel(pos + new Vector2(30, 0), new Vector2(400f, 30f), "Spawn Umbilicals", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            text = "Bonus eighth pips gained when directly killing a creature with a needle shot";
            slider = new OpSlider(killBonusEighths, pos)
            {
                min = 0,
                max = 8
            };
            Add(slider, text);
            Add(new OpLabel(pos + labelOffset, new Vector2(400f, 30f), "Food Bonus On Kill", FLabelAlignment.Left, false), text);
            pos += posIncrement;

            challengeBox = new OpCheckBox(riflemasterChallenge, new Vector2(460, 265));
            Add(challengeBox, "");
            challengeBox.Hide();
        }

        public void AddChallengeUI()
        {
            challengeBox.Show();
            Add(new OpLabel(new Vector2(425, 250), new Vector2(100, 200),
                "RIFLEMASTER CHALLENGE\n\n" +
                "-All spears thrown by Spearmaster\n" +
                "bounce off of creatures harmlessly\n" +
                "-Spearmaster will draw a special\n" +
                "rifle instead of a needle if they\n" +
                "are not already holding one", FLabelAlignment.Center, false), "");
        }
        
        public override void Update()
        {
            base.Update();
            if (!challengeUnlocked && (Input.GetKey("t") || riflemasterChallenge.Value))
            {
                if (!riflemasterChallenge.Value)
                {
                    Menu.ModdingMenu.instance.PlaySound(SoundID.MENU_Start_New_Game);
                }
                challengeUnlocked = true;
                AddChallengeUI();
            }
        }

    }
}
