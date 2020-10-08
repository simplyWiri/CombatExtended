using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.HarmonyCE;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    public class Controller : Mod
    {
        public static Settings settings;
        // This is here because I've had troubles directly referencing the additional enum type using visual studio
        // it functions as a 'compile friendly' version of the same code, for those that
        // run into the same issues.
        public static int Ammo_ThingRequestGroupInteger = typeof(ThingRequestGroup)
            .GetFields()
            .Where(t => t.Name == "Ammo")
            .FirstOrDefault()
            .GetRawConstantValue()
            .ChangeType<int>();


        public Controller(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();

            // Apply Harmony patches
            HarmonyBase.InitPatches();

            // Initialize loadout generator
            LongEventHandler.QueueLongEvent(LoadoutPropertiesExtension.Reset, "CE_LongEvent_LoadoutProperties", false, null);

            // Inject ammo
            LongEventHandler.QueueLongEvent(AmmoInjector.Inject, "CE_LongEvent_AmmoInjector", false, null);

            // Inject pawn and plant bounds
            LongEventHandler.QueueLongEvent(BoundsInjector.Inject, "CE_LongEvent_BoundingBoxes", false, null);

            Log.Message("Combat Extended :: initialized");

            // Tutorial popup
            if (settings.ShowTutorialPopup && !Prefs.AdaptiveTrainingEnabled)
                LongEventHandler.QueueLongEvent(DoTutorialPopup, "CE_LongEvent_TutorialPopup", false, null);

            LongEventHandler.QueueLongEvent(Patches.Init, "CE_LongEvent_CompatibilityPatches", false, null);
        }

        private static void DoTutorialPopup()
        {
            var enableAction = new Action(() =>
            {
                Prefs.AdaptiveTrainingEnabled = true;
                settings.ShowTutorialPopup = false;
                settings.Write();
            });
            var disableAction = new Action(() =>
            {
                settings.ShowTutorialPopup = false;
                settings.Write();
            });

            var dialog = new Dialog_MessageBox("CE_EnableTutorText".Translate(), "CE_EnableTutorDisable".Translate(), disableAction, "CE_EnableTutorEnable".Translate(),
                enableAction, null, true);
            Find.WindowStack.Add(dialog);
        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
