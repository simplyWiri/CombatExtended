using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded")]
    internal static class Harmony_ApparelTracker_Notify_ApparelAdded
    {
        internal static void Prefix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef == null)
                return;

            var pawn = __instance.pawn;

            pawn.health.AddHediff(hediffDef);
            if (apparel is ShieldBelt belt)
            {
                pawn.hasShieldBelt = true;
                if (pawn.hasShieldBelt)
                    pawn.shieldBelt = belt;
            }

            if (apparel is Apparel_Shield shield)
            {
                pawn.hasApparelShield = true;
                if (pawn.hasShieldBelt)
                    pawn.apparelShield = shield;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    internal static class Harmony_ApparelTracker_Notify_ApparelRemoved
    {
        internal static void Prefix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef == null)
                return;

            var pawn = __instance.pawn;
            var hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef);
            if (hediff == null)
            {
                Log.Warning($"Combat Extended :: Apparel {apparel} tried removing hediff {hediffDef} from {pawn} but could not find any");
                return;
            }
            pawn.health.RemoveHediff(hediff);
            if (pawn.hasShieldBelt && apparel is ShieldBelt)
            {
                pawn.hasShieldBelt = false;
                pawn.shieldBelt = null;
            }

            if (pawn.hasApparelShield && pawn.apparelShield is Apparel_Shield)
            {
                pawn.hasShieldBelt = false;
                pawn.apparelShield = null;
            }
        }
    }
}