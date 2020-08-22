﻿using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace CombatExtended
{
    public class JobGiver_ManTurretsNearSelfCE : JobGiver_ManTurretsNearSelf
    {
        /// <inheritdoc cref="JobGiver_ManTurrets.TryGiveJob" />
        /// <remarks>Overriden to avoid invalid type cast exception.</remarks>
        public override Job TryGiveJob(Pawn pawn)
        {
            var turrets = pawn.Map.GetComponent<MapComponent_TurretTracker>().Turrets.Where(t => 
                t.def.hasInteractionCell && 
                t.def.HasComp(typeof(CompMannable)) && 
                pawn.CanReserve(t) && 
                FindAmmoForTurret(pawn, t) != null);

            var thing = GenClosest.ClosestThingReachable(
                GetRoot(pawn),
                pawn.Map,
                ThingRequest.ForUndefined(), PathEndMode.InteractionCell, TraverseParms.For(pawn),
                maxDistFromPoint,
                null, turrets);

            if (thing == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.ManTurret, thing);
            job.expiryInterval = 2000;
            job.checkOverrideOnExpire = true;
            return job;
        }

        /// <remarks>Copied from <see cref="JobDriver_ManTurret.FindAmmoForTurret" />.</remarks>
        private static Thing FindAmmoForTurret(Pawn pawn, Thing turret)
        {
            var compAmmo = (turret as Building_TurretGunCE)?.CompAmmo;
            if (compAmmo == null || !compAmmo.UseAmmo)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                turret.Position,
                turret.Map,
                ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                40f,
                t => !t.IsForbidden(pawn) &&
                     pawn.CanReserve(t, 10, 1) &&
                     compAmmo.Props.ammoSet.ammoTypes.Any(l => l.ammo == t.def));
        }
    }
}