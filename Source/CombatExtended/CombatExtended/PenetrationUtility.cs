using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public enum ImpactOutcome
    {
        Penetrate,
        Stop
    }

    public static class PenetrationUtility
    {

        public static float ComputeEnergyRemainingAfterPenetration(Thing hitThing, BulletCE bullet, AmmoDef ammo)
        {
            float inertiaPenalty;

            var itemHitPointsPercent = hitThing.HitPoints / (float)hitThing.MaxHitPoints; // we think of this as a multiplier on the velocity required to penetrate an object (as an object gets weaker, it becomes easier to fully go through)
            var itemLength = hitThing.def.fillPercent;

            var finalPenetration = hitThing.HitPoints * itemHitPointsPercent * itemLength;
            var projProps = bullet.def.projectile as ProjectilePropertiesCE;

            var projectileMass = ammo.BaseMass;
            var projectileInerta = bullet.inertia / projectileMass;
            var percentOfPenetration = finalPenetration / projectileInerta;

            inertiaPenalty = bullet.inertia - (float)Math.Pow(projProps.speed * percentOfPenetration, 2);
            if (bullet.ammoDef.ammoClass.bulletClass == BulletClass.LargeCaliber) inertiaPenalty *= 0.85f;

            Log.Message($"Inertia penalty {projProps.speed * percentOfPenetration} new inertia {inertiaPenalty}, final penetration {finalPenetration}, projectile inerta {projectileInerta} percent {percentOfPenetration}");

            return inertiaPenalty;
        }

        public static ImpactOutcome DetermineImpactOutcome(Thing hitThing, BulletCE bullet, Thing launcher, AmmoDef ammo)
        {

            var bulletClass = ammo.ammoClass.bulletClass;

            // If the bullet fragments, or explodes, it should *not* do anything other than stop on impact
            if (bullet.inertia == 0 || bulletClass == BulletClass.Fragmenting) return ImpactOutcome.Stop;

            bullet.inertia = ComputeEnergyRemainingAfterPenetration(hitThing, bullet, ammo);
            if (bullet.inertia <= 0) return ImpactOutcome.Stop;


            return ImpactOutcome.Penetrate;
        }
    }
}
