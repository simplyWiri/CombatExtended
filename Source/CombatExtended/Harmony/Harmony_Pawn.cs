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
            Scribe_Values.Look(ref pawn.CEHasApparelShield, "CEHasApparelShieldCE", false);
            Scribe_Values.Look(ref pawn.CEHasShieldBelt, "CEHasShieldBeltCE", false);
            Scribe_Values.Look(ref pawn.CEIsPawn, "CEIsPawnCE", false);
            Scribe_Values.Look(ref pawn.CEIsTurret, "isTurretCE", false);

            Scribe_References.Look(ref pawn.CEApparelShield, "CEApparelShieldCE", false);
            Scribe_References.Look(ref pawn.CEShieldBelt, "shieldBeltCE", false);
        }
    }
}
