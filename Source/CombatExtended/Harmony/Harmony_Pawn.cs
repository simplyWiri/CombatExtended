using System;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static class Harmony_Pawn_ExposeData
    {
        public static void Postfix(Pawn __instance)
        {
            try
            {
                ScribeExtras(__instance);
            }
            catch (Exception er)
            {
                Log.Error(er.Message);
                Log.Error(er.Source);
            }
            finally
            {

            }
        }

        private static void ScribeExtras(Pawn pawn)
        {
            Scribe_Values.Look(ref pawn.hasApparelShield, "hasApparelShieldCE", false);
            Scribe_Values.Look(ref pawn.hasShieldBelt, "hasShieldBeltCE", false);
            Scribe_Values.Look(ref pawn.isPawn, "isPawnCE", false);
            Scribe_Values.Look(ref pawn.isTurret, "isTurretCE", false);

            Scribe_References.Look(ref pawn.apparelShield, "apparelShieldCE", false);
            Scribe_References.Look(ref pawn.shieldBelt, "shieldBeltCE", false);
        }
    }
}
