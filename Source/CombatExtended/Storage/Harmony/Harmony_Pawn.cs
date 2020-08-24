using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.Storage.Harmony
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Harmony_Pawn_GetGizmo
    {
        public static void Postfix(Pawn __instance)
        {
            if (!Prefs.DevMode)
                return;

            List<Thing> others = __instance.ThingsInRange(10).ToList();

            if (others == null)
                return;

            GameFont textSize = Text.Font;
            Text.Font = GameFont.Tiny;
            foreach (Thing thing in others)
            {
                if (thing == null)
                    continue;
                var drawPos = thing.positionInt.ToUIPosition();
                var label = thing != __instance ? "other" : "me";
                Widgets.Label(new Rect(drawPos, Text.CalcSize(label)), label);
            }

            Text.Font = textSize;
        }

    }
}
