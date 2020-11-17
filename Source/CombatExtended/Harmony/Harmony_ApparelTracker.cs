using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "ExposeData")]
    internal static class Harmony_ApparelTracker_ExposeData
    {
        internal static void Postfix(Pawn_ApparelTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn != null)
            {
                Scribe_Values.Look(ref pawn.hasApparelHeadwear, "CEHasApparelHeadwear", defaultValue: false);
                Scribe_Values.Look(ref pawn.hasApparelShield, "CEHasApparelShield", defaultValue: false);
                Scribe_References.Look(ref pawn.apparelShield, "CEApparelShield", saveDestroyedThings: false);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded")]
    internal static class Harmony_ApparelTracker_Notify_ApparelAdded
    {
        internal static void Prefix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var pawn = __instance.pawn;
            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef != null)
            {
                pawn.health.AddHediff(hediffDef);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "ApparelChanged")]
    internal static class Harmony_ApparelTracker_ApparelChanged
    {
        internal static void Prefix(Pawn_ApparelTracker __instance)
        {
            var pawn = __instance.pawn;
            pawn.hasApparelHeadwear = false;
            pawn.hasShieldBelt = false;
            pawn.apparelShield = null;
            foreach (var apparel in pawn.apparel.wornApparel)
            {
                if (apparel.def.apparel.layers.Any(layer => layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false))
                {
                    pawn.hasApparelHeadwear = true;
                }
                if (!pawn.hasApparelShield && pawn.apparelShield is Apparel_Shield)
                {
                    pawn.hasShieldBelt = true;
                    pawn.apparelShield = apparel as Apparel_Shield;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    internal static class Harmony_ApparelTracker_Notify_ApparelRemoved
    {
        internal static void Prefix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var pawn = __instance.pawn;
            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef != null)
            {
                var hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef);
                if (hediff == null)
                {
                    Log.Warning($"Combat Extended :: Apparel {apparel} tried removing hediff {hediffDef} from {pawn} but could not find any");
                }
                else
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}