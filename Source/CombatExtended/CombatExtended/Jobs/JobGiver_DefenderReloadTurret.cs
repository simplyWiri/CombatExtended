using CombatExtended.CombatExtended.Jobs.Utils;
using CombatExtended.CombatExtended.LoggerUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_DefenderReloadTurret : ThinkNode_JobGiver
    {
        /// <summary>
        /// How low can ammo get before we want to reload the turret?
        /// Set arbitrarily, balance if needed.
        /// </summary>
        public const float ammoReloadThreshold = .5f;
        public override Job TryGiveJob(Pawn pawn)
        {
            var turret = TryFindTurretWhichNeedsReloading(pawn);
            if (turret == null)
            {
                return null; // signals ThinkResult.NoJob.
            }
            return JobGiverUtils_Reload.MakeReloadJob(pawn, turret);
        }

        private Building_TurretGunCE TryFindTurretWhichNeedsReloading(Pawn pawn)
        {
            Predicate<Building_TurretGunCE> turretCanBeReloaded = (Building_TurretGunCE t) =>
            {
                if (!JobGiverUtils_Reload.CanReload(pawn, t, forced: false, emergency: true)) { return false; }
                
                return t.CompAmmo.CurMagCount <= t.CompAmmo.Props.magazineSize / ammoReloadThreshold;
            };
            var reloadableTurrets = pawn.Map.GetComponent<MapComponent_TurretTracker>().TurretsRequiringReArming.Where(t => turretCanBeReloaded(t));

            var turret = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup((ThingRequestGroup)Controller.Ammo_ThingRequestGroupInteger), PathEndMode.Touch, TraverseParms.For(pawn), 100f, null, reloadableTurrets);
            // get the closest one to the pawn
            if(turret == null) return null;

            return turret as Building_TurretGunCE;
        }

    
    }
}
