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
            if (!Prefs.DevMode || !Controller.settings.DebugDrawRangeLines)
                return;

            List<Thing> others = __instance.ThingsInRange(10).ToList();

            if (others == null)
                return;

            var selPos = __instance.DrawPos.MapToUIPosition();
            var color = new Color(Rand.Range(0.1f, 1.0f), Rand.Range(0.1f, 1.0f), Rand.Range(0.1f, 1.0f));
            var offset = new Vector2(12, 12);
            GameFont textSize = Text.Font;
            TextAnchor anchor = Text.Anchor;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            foreach (Thing thing in others)
            {
                if (thing == null || thing == __instance)
                    continue;
                var drawPos = thing.DrawPos.MapToUIPosition();

                Widgets.DrawLine(drawPos, selPos, color, 1);
                var distance = Vector2.Distance(drawPos, selPos);
                if (distance < 24)
                    continue;

                var midPoint = (drawPos + selPos) / 2;
                var rect = new Rect(midPoint - offset, offset * 2);

                Widgets.DrawWindowBackgroundTutor(rect);
                Widgets.Label(rect, "" + Mathf.RoundToInt(thing.Position.DistanceTo(__instance.Position)));
            }

            Text.Font = textSize;
            Text.Anchor = anchor;
        }


    }
}
