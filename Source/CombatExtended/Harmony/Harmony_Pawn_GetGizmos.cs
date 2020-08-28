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


                var distance = Vector2.Distance(drawPos, selPos);
                if (distance < 24)
                    continue;

                var midPoint = (drawPos + selPos) / 2;
                var rect = new Rect(midPoint - offset, offset * 2);
                var realDistance = Mathf.RoundToInt(thing.Position.DistanceTo(__instance.Position));
                if (realDistance <= 10)
                {
                    var color = new Color(0.1f, 0.5f, 0.1f);
                    Widgets.DrawLine(drawPos, selPos, color, 1);
                    Widgets.DrawWindowBackgroundTutor(rect);
                    Widgets.Label(rect, "" + Mathf.RoundToInt(thing.Position.DistanceTo(__instance.Position)));
                }
                else
                {
                    var color = new Color(1f, 0.1f, 0.1f);
                    Widgets.DrawLine(drawPos, selPos, color, 1);
                    Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f));
                    Widgets.Label(rect, "" + Mathf.RoundToInt(thing.Position.DistanceTo(__instance.Position)));
                }
            }

            Text.Font = textSize;
            Text.Anchor = anchor;
        }


    }
}
