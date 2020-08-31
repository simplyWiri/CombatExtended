﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using Verse.Grammar;
using UnityEngine;
using JetBrains.Annotations;
using HarmonyLib;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CombatExtended
{
    public class Verb_LaunchProjectileCE : Verb
    {
        #region Constants

        // Cover check constants
        private const float distToCheckForCover = 3f;   // How many cells to raycast on the cover check
        private const float segmentLength = 0.2f;       // How long a single raycast segment is
        //private const float shotHeightFactor = 0.85f;   // The height at which pawns hold their guns

        #endregion

        #region Fields

        // Targeting factors
        private float estimatedTargDist = -1;           // Stores estimate target distance for each burst, so each burst shot uses the same
        private int numShotsFired = 0;                  // Stores how many shots were fired for purposes of recoil

        // Angle in Vector2(degrees, radians)
        private Vector2 newTargetLoc = new Vector2(0, 0);
        private Vector2 sourceLoc = new Vector2(0, 0);

        private float shotAngle = 0f;   // Shot angle off the ground in radians.
        private float shotRotation = 0f;    // Angle rotation towards target.

        protected CompCharges compCharges = null;
        protected CompAmmoUser compAmmo = null;
        protected CompFireModes compFireModes = null;
        protected CompChangeableProjectile compChangeable = null;
        protected CompReloadable compReloadable = null;
        private float shotSpeed = -1;

        private float rotationDegrees = 0f;
        private float angleRadians = 0f;

        // TODO: make defof
        private static StatDef shotSpread = StatDef.Named("ShotSpread");

        public static Dictionary<ThingDef, Bounds> bounds = new Dictionary<ThingDef, Bounds>();
        //private int lastTauntTick;

        #endregion

        #region Properties

        public VerbPropertiesCE VerbPropsCE => verbProps as VerbPropertiesCE;
        public ProjectilePropertiesCE projectilePropsCE => Projectile.projectile as ProjectilePropertiesCE;

        // Returns either the pawn aiming the weapon or in case of turret guns the turret operator or null if neither exists
        public Pawn ShooterPawn => CasterPawn ?? CE_Utility.TryGetTurretOperator(caster);
        public Thing Shooter => ShooterPawn ?? caster;

        protected CompCharges CompCharges
        {
            get
            {
                if (compCharges == null && EquipmentSource != null)
                {
                    compCharges = EquipmentSource.TryGetComp<CompCharges>();
                }
                return compCharges;
            }
        }
        private float ShotSpeed
        {
            get
            {
                if (CompCharges != null)
                {
                    if (CompCharges.GetChargeBracket((currentTarget.Cell - caster.Position).LengthHorizontal, ShotHeight, projectilePropsCE.Gravity, out var bracket))
                    {
                        shotSpeed = bracket.x;
                    }
                }
                else
                {
                    shotSpeed = Projectile.projectile.speed;
                }
                return shotSpeed;
            }
        }
        protected float ShotHeight => (new CollisionVertical(caster)).shotHeight;
        private Vector3 ShotSource
        {
            get
            {
                var casterPos = caster.DrawPos;
                return new Vector3(casterPos.x, ShotHeight, casterPos.z);
            }
        }

        protected float ShootingAccuracy => Mathf.Min(CasterShootingAccuracyValue(caster), 4.5f);
        protected float AimingAccuracy => Mathf.Min(Shooter.GetStatValue(CE_StatDefOf.AimingAccuracy), 1.5f); //equivalent of ShooterPawn?.GetStatValue(CE_StatDefOf.AimingAccuracy) ?? caster.GetStatValue(CE_StatDefOf.AimingAccuracy)
        protected float SightsEfficiency => EquipmentSource?.GetStatValue(CE_StatDefOf.SightsEfficiency) ?? 1f;
        protected virtual float SwayAmplitude => Mathf.Max(0, (4.5f - ShootingAccuracy) * (EquipmentSource?.GetStatValue(StatDef.Named("SwayFactor")) ?? 1f));

        // Ammo variables
        protected CompAmmoUser CompAmmo
        {
            get
            {
                if (compAmmo == null && EquipmentSource != null)
                {
                    compAmmo = EquipmentSource.TryGetComp<CompAmmoUser>();
                }
                return compAmmo;
            }
        }
        public ThingDef Projectile
        {
            get
            {
                if (CompAmmo != null && CompAmmo.CurrentAmmo != null)
                {
                    return CompAmmo.CurAmmoProjectile;
                }
                if (CompChangeable != null && CompChangeable.Loaded)
                {
                    return CompChangeable.Projectile;
                }
                return VerbPropsCE.defaultProjectile;
            }
        }

        protected CompChangeableProjectile CompChangeable
        {
            get
            {
                if (compChangeable == null && EquipmentSource != null)
                {
                    compChangeable = EquipmentSource.TryGetComp<CompChangeableProjectile>();
                }
                return compChangeable;
            }
        }

        public CompFireModes CompFireModes
        {
            get
            {
                if (compFireModes == null && EquipmentSource != null)
                {
                    compFireModes = EquipmentSource.TryGetComp<CompFireModes>();
                }
                return compFireModes;
            }
        }

        public CompReloadable CompReloadable
        {
            get
            {
                if (compReloadable == null && EquipmentSource != null)
                {
                    compReloadable = EquipmentSource.TryGetComp<CompReloadable>();
                }
                return compReloadable;
            }
        }

        private bool IsAttacking => ShooterPawn?.CurJobDef == JobDefOf.AttackStatic || WarmingUp;


        #endregion

        #region Methods

        public override bool Available()
        {
            // This part copied from vanilla Verb_LaunchProjectile
            if (!base.Available())
                return false;

            if (CasterIsPawn)
            {
                if (CasterPawn.Faction != Faction.OfPlayer
                    && CasterPawn.mindState.MeleeThreatStillThreat
                    && CasterPawn.mindState.meleeThreat.AdjacentTo8WayOrInside(CasterPawn))
                    return false;
            }

            // Add check for reload
            if (Projectile == null || (IsAttacking && CompAmmo != null && !CompAmmo.CanBeFiredNow))
            {
                CompAmmo?.TryStartReload();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets caster's weapon handling based on if it's a pawn or a turret
        /// </summary>
        /// <param name="caster">What thing is equipping the projectile launcher</param>
        private float CasterShootingAccuracyValue(Thing caster) => // ShootingAccuracy was split into ShootingAccuracyPawn and ShootingAccuracyTurret
            (caster as Pawn != null) ? caster.GetStatValue(StatDefOf.ShootingAccuracyPawn) : caster.GetStatValue(StatDefOf.ShootingAccuracyTurret);

        /// <summary>
        /// Resets current burst shot count and estimated distance at beginning of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            // attack shooting expression
            if ((ShooterPawn?.Spawned ?? false) && currentTarget.Thing is Pawn && Rand.Chance(0.25f))
            {
                var tauntThrower = (TauntThrower)ShooterPawn.Map.GetComponent(typeof(TauntThrower));
                tauntThrower?.TryThrowTaunt(CE_RulePackDefOf.AttackMote, ShooterPawn);
            }

            numShotsFired = 0;
            base.WarmupComplete();
            Find.BattleLog.Add(
                new BattleLogEntry_RangedFire(
                    Shooter,
                    (!currentTarget.HasThing) ? null : currentTarget.Thing,
                    (EquipmentSource == null) ? null : EquipmentSource.def,
                    Projectile,
                    VerbPropsCE.burstShotCount > 1)
            );
        }

        /// <summary>
        /// Shifts the original target position in accordance with target leading, range estimation and weather/lighting effects
        /// </summary>
        protected virtual void ShiftTarget(ShiftVecReport report, bool calculateMechanicalOnly = false)
        {
            if (!calculateMechanicalOnly)
            {
                Vector3 u = caster.TrueCenter();
                sourceLoc.Set(u.x, u.z);

                if (numShotsFired == 0)
                {
                    // On first shot of burst do a range estimate
                    estimatedTargDist = report.GetRandDist();
                }
                Vector3 v = report.target.Thing?.TrueCenter() ?? report.target.Cell.ToVector3Shifted(); //report.targetPawn != null ? report.targetPawn.DrawPos + report.targetPawn.Drawer.leaner.LeanOffset * 0.5f : report.target.Cell.ToVector3Shifted();
                if (report.targetPawn != null)
                    v += report.targetPawn.Drawer.leaner.LeanOffset * 0.5f;

                newTargetLoc.Set(v.x, v.z);

                // ----------------------------------- STEP 1: Actual location + Shift for visibility

                //FIXME : GetRandCircularVec may be causing recoil to be unnoticeable - each next shot in the burst has a new random circular vector around the target.
                newTargetLoc += report.GetRandCircularVec();

                // ----------------------------------- STEP 2: Estimated shot to hit location

                newTargetLoc = sourceLoc + (newTargetLoc - sourceLoc).normalized * estimatedTargDist;

                // Lead a moving target
                newTargetLoc += report.GetRandLeadVec();

                // ----------------------------------- STEP 3: Recoil, Skewing, Skill checks, Cover calculations

                rotationDegrees = 0f;
                angleRadians = 0f;

                GetSwayVec(ref rotationDegrees, ref angleRadians);
                GetRecoilVec(ref rotationDegrees, ref angleRadians);

                // Height difference calculations for ShotAngle
                float targetHeight = 0f;

                var coverRange = (report.cover == null) ? new FloatRange(0, 0) : new CollisionVertical(report.cover).HeightRange;   //Get " " cover, assume it is the edifice

                // Projectiles with flyOverhead target the surface in front of the target
                if (Projectile.projectile.flyOverhead)
                {
                    targetHeight = coverRange.max;
                }
                else
                {
                    var victimVert = (currentTarget.Thing == null) ? new CollisionVertical(0, new FloatRange(0, 0)) : new CollisionVertical(currentTarget.Thing);
                    var targetRange = victimVert.HeightRange;   //Get lower and upper heights of the target
                    /*if (currentTarget.Thing is Building && CompFireModes?.CurrentAimMode == AimMode.SuppressFire)
                    {
                        targetRange.min = targetRange.max;
                        targetRange.max = targetRange.min + 1f;
                    }*/
                    if (targetRange.min < coverRange.max)   //Some part of the target is hidden behind some cover
                    {
                        // - It is possible for targetRange.max < coverRange.max, technically, in which case the shooter will never hit until the cover is gone.
                        // - This should be checked for in LoS -NIA
                        targetRange.min = coverRange.max;

                        // Target fully hidden, shift aim upwards if we're doing suppressive fire
                        if (targetRange.max <= coverRange.max && CompFireModes?.CurrentAimMode == AimMode.SuppressFire)
                        {
                            targetRange.max = coverRange.max * 2;
                        }
                    }
                    else if (currentTarget.Thing is Pawn)
                    {
                        // Aim for center of mass on an exposed target
                        targetRange.min = victimVert.BottomHeight;
                        targetRange.max = victimVert.MiddleHeight;
                    }
                    targetHeight = VerbPropsCE.ignorePartialLoSBlocker ? 0 : targetRange.Average;
                }
                angleRadians += ProjectileCE.GetShotAngle(ShotSpeed, (newTargetLoc - sourceLoc).magnitude, targetHeight - ShotHeight, Projectile.projectile.flyOverhead, projectilePropsCE.Gravity);
            }

            // ----------------------------------- STEP 4: Mechanical variation

            // Get shotvariation, in angle Vector2 RADIANS.
            Vector2 spreadVec = report.GetRandSpreadVec();

            // ----------------------------------- STEP 5: Finalization

            var w = (newTargetLoc - sourceLoc);
            shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x) + rotationDegrees + spreadVec.x) % 360;
            shotAngle = angleRadians + spreadVec.y * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Calculates the amount of recoil at a given point in a burst, up to a maximum
        /// </summary>
        /// <param name="rotation">The ref float to have horizontal recoil in degrees added to.</param>
        /// <param name="angle">The ref float to have vertical recoil in radians added to.</param>
        private void GetRecoilVec(ref float rotation, ref float angle)
        {
            var recoil = VerbPropsCE.recoilAmount;
            float maxX = recoil * 0.5f;
            float minX = -maxX;
            float maxY = recoil;
            float minY = -recoil / 3;

            float recoilMagnitude = numShotsFired == 0 ? 0 : Mathf.Pow((5 - ShootingAccuracy), (Mathf.Min(10, numShotsFired) / 6.25f));

            rotation += recoilMagnitude * UnityEngine.Random.Range(minX, maxX);
            angle += Mathf.Deg2Rad * recoilMagnitude * UnityEngine.Random.Range(minY, maxY);
        }

        /// <summary>
        /// Calculates current weapon sway based on a parametric function with maximum amplitude depending on shootingAccuracy and scaled by weapon's swayFactor.
        /// </summary>
        /// <param name="rotation">The ref float to have horizontal sway in degrees added to.</param>
        /// <param name="angle">The ref float to have vertical sway in radians added to.</param>
        protected void GetSwayVec(ref float rotation, ref float angle)
        {
            float ticks = (float)(Find.TickManager.TicksAbs + Shooter.thingIDNumber);
            rotation += SwayAmplitude * (float)Mathf.Sin(ticks * 0.022f);
            angle += Mathf.Deg2Rad * 0.25f * SwayAmplitude * (float)Mathf.Sin(ticks * 0.0165f);
        }

        public virtual ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            IntVec3 targetCell = target.Cell;
            ShiftVecReport report = new ShiftVecReport();
            report.target = target;
            report.aimingAccuracy = AimingAccuracy;
            report.sightsEfficiency = SightsEfficiency;
            report.shotDist = (targetCell - caster.Position).LengthHorizontal;
            report.maxRange = verbProps.range;

            report.lightingShift = 1 - caster.Map.glowGrid.GameGlowAt(targetCell);
            if (!caster.Position.Roofed(caster.Map) || !targetCell.Roofed(caster.Map))  //Change to more accurate algorithm?
            {
                report.weatherShift = 1 - caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
            }
            report.shotSpeed = ShotSpeed;
            report.swayDegrees = SwayAmplitude;
            var spreadmult = projectilePropsCE != null ? projectilePropsCE.spreadMult : 0f;
            report.spreadDegrees = (EquipmentSource?.GetStatValue(shotSpread) ?? 0) * spreadmult;
            Thing cover;
            float smokeDensity;
            GetHighestCoverAndSmokeForTarget(target, out cover, out smokeDensity);
            report.cover = cover;
            report.smokeDensity = smokeDensity;

            return report;
        }

        /// <summary>
        /// Checks for cover along the flight path of the bullet, doesn't check for walls or trees, only intended for cover with partial fillPercent
        /// </summary>
        /// <param name="target">The target of which to find cover of</param>
        /// <param name="cover">Output parameter, filled with the highest cover object found</param>
        /// <returns>True if cover was found, false otherwise</returns>
        private bool GetHighestCoverAndSmokeForTarget(LocalTargetInfo target, out Thing cover, out float smokeDensity)
        {
            Map map = caster.Map;
            Thing targetThing = target.Thing;
            Thing highestCover = null;
            float highestCoverHeight = 0f;

            smokeDensity = 0;

            // Iterate through all cells on line of sight and check for cover and smoke
            var cells = GenSight.PointsOnLineOfSight(target.Cell, caster.Position).ToArray();
            if (cells.Length < 3)
            {
                cover = null;
                return false;
            }
            for (int i = 0; i <= cells.Length / 2; i++)
            {
                var cell = cells[i];

                if (cell.AdjacentTo8Way(caster.Position)) continue;

                // Check for smoke
                smokeDensity += cell.GetGas(map)?.def?.gas?.accuracyPenalty ?? 0;

                // Check for cover in the second half of LoS
                if (i <= cells.Length / 2)
                {
                    Pawn pawn = cell.GetFirstPawn(map);
                    Thing newCover = pawn == null ? cell.GetCover(map) : pawn;

                    // Cover check, if cell has cover compare collision height and get the highest piece of cover, ignore if cover is the target (e.g. solar panels, crashed ship, etc)
                    if (newCover == null) continue;

                    float newCoverHeight = new CollisionVertical(newCover).Max;

                    if ((targetThing == null || !newCover.Equals(targetThing))
                        && (highestCover == null || highestCoverHeight < newCoverHeight)
                        && newCover.def.Fillage == FillCategory.Partial
                        && !newCover.IsPlant())
                    {
                        highestCover = newCover;
                        highestCoverHeight = newCoverHeight;
                        if (Controller.settings.DebugDrawTargetCoverChecks) map.debugDrawer.FlashCell(cell, highestCoverHeight, highestCoverHeight.ToString());
                    }
                }
            }
            cover = highestCover;

            //Report success if found cover
            return cover != null;
        }

        /// <summary>
        /// Checks if the shooter can hit the target from a certain position with regards to cover height
        /// </summary>
        /// <param name="root">The position from which to check</param>
        /// <param name="targ">The target to check for line of sight</param>
        /// <returns>True if shooter can hit target from root position, false otherwise</returns>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            string unused;
            return CanHitTargetFrom(root, targ, out unused);
        }

        public bool CanHitTarget(LocalTargetInfo targ, out string report)
        {
            return CanHitTargetFrom(caster.Position, targ, out report);
        }

        public virtual bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ, out string report)
        {
            report = "";
            if (caster?.Map == null || !targ.Cell.InBounds(caster.Map) || !root.InBounds(caster.Map))
            {
                report = "Out of bounds";
                return false;
            }
            // Check target self
            if (targ.Thing != null && targ.Thing == caster)
            {
                if (!verbProps.targetParams.canTargetSelf)
                {
                    report = "Can't target self";
                    return false;
                }
                return true;
            }

            // Check thick roofs
            if (Projectile.projectile.flyOverhead)
            {
                RoofDef roofDef = caster.Map.roofGrid.RoofAt(targ.Cell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    report = "Blocked by roof";
                    return false;
                }
            }

            if (ShooterPawn != null)
            {
                // Check for capable of violence
                if (ShooterPawn.story != null && ShooterPawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    report = "IsIncapableOfViolenceLower".Translate(ShooterPawn.Name.ToStringShort);
                    return false;
                }

                // Check for apparel
                bool isTurretOperator = caster.def.building?.IsTurret ?? false;
                if (ShooterPawn.apparel != null) // Cache which pawns have disabled verbs due to clothing
                {
                    List<Apparel> wornApparel = ShooterPawn.apparel.WornApparel;
                    foreach (Apparel current in wornApparel)
                    {
                        //pawns can use turrets while wearing shield belts, but the shield is disabled for the duration via Harmony patch (see Harmony-ShieldBelt.cs)
                        if (!current.AllowVerbCast(root, caster.Map, targ, this) && !(current is ShieldBelt && isTurretOperator))
                        {
                            report = "Shooting disallowed by " + current.LabelShort;
                            return false;
                        }
                    }
                }
            }
            // Check for line of sight
            ShootLine shootLine;
            if (!TryFindCEShootLineFromTo(root, targ, out shootLine)) // Mafs
            {
                float lengthHorizontalSquared = (root - targ.Cell).LengthHorizontalSquared;
                if (lengthHorizontalSquared > verbProps.range * verbProps.range)
                {
                    report = "Out of range";
                }
                else if (lengthHorizontalSquared < verbProps.minRange * verbProps.minRange)
                {
                    report = "Within minimum range";
                }
                else
                {
                    report = "No line of sight";
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Fires a projectile using the new aiming system
        /// </summary>
        /// <returns>True for successful shot, false otherwise</returns>
        public override bool TryCastShot()
        {
            if (!TryFindCEShootLineFromTo(caster.Position, currentTarget, out var shootLine))
            {
                return false;
            }
            if (projectilePropsCE.pelletCount < 1)
            {
                Log.Error(EquipmentSource.LabelCap + " tried firing with pelletCount less than 1.");
                return false;
            }
            ShiftVecReport report = ShiftVecReportFor(currentTarget);
            bool pelletMechanicsOnly = false;
            for (int i = 0; i < projectilePropsCE.pelletCount; i++)
            {

                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(Projectile, null);
                GenSpawn.Spawn(projectile, shootLine.Source, caster.Map);
                ShiftTarget(report, pelletMechanicsOnly);

                //New aiming algorithm
                projectile.canTargetSelf = false;

                var targDist = (sourceLoc - currentTarget.Cell.ToIntVec2.ToVector2Shifted()).magnitude;
                if (targDist <= 2)
                    targDist *= 2;  // Double to account for divide by 4 in ProjectileCE minimum collision distance calculations
                projectile.minCollisionSqr = Mathf.Pow(targDist, 2);
                projectile.intendedTarget = currentTarget.Thing;
                projectile.mount = caster.Position.GetThingList(caster.Map).FirstOrDefault(t => t is Pawn && t != caster);
                projectile.AccuracyFactor = report.accuracyFactor * report.swayDegrees * ((numShotsFired + 1) * 0.75f);
                projectile.Launch(
                    Shooter,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
                    sourceLoc,
                    shotAngle,
                    shotRotation,
                    ShotHeight,
                    ShotSpeed,
                    EquipmentSource
                );
                pelletMechanicsOnly = true;
            }
            /// Log.Message("Fired from "+caster.ThingID+" at "+ShotHeight); /// 
            pelletMechanicsOnly = false;
            numShotsFired++;
            if (CompAmmo != null && !CompAmmo.CanBeFiredNow)
            {
                CompAmmo?.TryStartReload();
            }
            if (CompReloadable != null)
            {
                CompReloadable.UsedOnce();
            }
            return true;
        }

        /// <summary>
        /// This is a custom CE ticker. Since the vanilla VerbTick() method is non-virtual we need to detour VerbTracker and make it call this method in addition to the vanilla ticker in order to
        /// add custom ticker functionality.
        /// </summary>
        public virtual void VerbTickCE()
        {
        }

        #region Line of Sight Utility

        /* Line of sight calculating methods
         * 
         * Copied from vanilla Verse.Verb class, the only change here is usage of our own validator for partial cover checks. Copy-paste should be kept up to date with vanilla
         * and if possible replaced with a cleaner solution.
         * 
         * -NIA
         */

        private static new List<IntVec3> tempDestList = new List<IntVec3>();
        private static new List<IntVec3> tempLeanShootSources = new List<IntVec3>();

        public bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            if (targ.HasThing && targ.Thing.Map != caster.Map)
            {
                resultingLine = default(ShootLine);
                return false;
            }
            if (verbProps.range <= ShootTuning.MeleeRange) // If this verb has a MAX range up to melee range (NOT a MIN RANGE!)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return ReachabilityImmediate.CanReachImmediate(root, targ, caster.Map, PathEndMode.Touch, null);
            }
            CellRect cellRect = (!targ.HasThing) ? CellRect.SingleCell(targ.Cell) : targ.Thing.OccupiedRect();
            float num = cellRect.ClosestDistSquaredTo(root);
            if (num > verbProps.range * verbProps.range || num < verbProps.minRange * verbProps.minRange)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return false;
            }
            //if (!this.verbProps.NeedsLineOfSight) This method doesn't consider the currently loaded projectile
            if (Projectile.projectile.flyOverhead)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return true;
            }

            // First check current cell for early opt-out
            IntVec3 dest;
            var shotSource = root.ToVector3Shifted();
            shotSource.y = ShotHeight;

            // Adjust for multi-tile turrets
            if (caster.def.building?.IsTurret ?? false)
            {
                shotSource = ShotSource;
            }

            if (CanHitFromCellIgnoringRange(shotSource, root, targ, out dest))
            {
                resultingLine = new ShootLine(root, dest);
                return true;
            }
            // For pawns, calculate possible lean locations
            if (CasterIsPawn)
            {
                // Next check lean sources
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                foreach (var leanLoc in tempLeanShootSources)
                {
                    var leanOffset = (leanLoc - root).ToVector3() * 0.5f;

                    if (CanHitFromCellIgnoringRange(shotSource + leanOffset, root, targ, out dest))
                    {
                        resultingLine = new ShootLine(leanLoc, dest);
                        return true;
                    }
                }
            }

            resultingLine = new ShootLine(root, targ.Cell);
            return false;
        }


        private bool CanHitFromCellIgnoringRange(Vector3 shotSource, IntVec3 root, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            if (verbProps.mustCastOnOpenGround)
                if (!targ.Cell.Standable(caster.Map) || caster.Map.thingGrid.CellContains(targ.Cell, ThingCategory.Pawn))
                {
                    goodDest = IntVec3.Invalid; return false;
                }

            if (verbProps.requireLineOfSight)
                if (!CanHitFromCellIgnoringRange(
                    shotSource,
                    targ.Cell.ToVector3(),
                    root,
                    targ.Cell,
                    targ.Thing,
                    (AimMode)(CompFireModes?.CurrentAimMode),
                    caster.Map))
                {
                    goodDest = IntVec3.Invalid; return false;
                }
            goodDest = targ.Cell; return true;
        }


        private bool CanHitFromCellIgnoringRange(
            UnityEngine.Vector3 root,
            UnityEngine.Vector3 targetPos,
            IntVec3 sourceCell,
            IntVec3 targetCell,
            Thing target,
            AimMode aimMode,
            Map map)
        {

            if (target != null)
            {
                Vector3 targDrawPos = target.DrawPos;
                targetPos = new Vector3(targDrawPos.x, new CollisionVertical(target).Max, targDrawPos.z);
                var targPawn = target.innerPawn;
                if (targPawn != null)
                    targetPos += targPawn.Drawer.leaner.LeanOffset * 0.6f;
            }
            else
            {
                targetPos = targetCell.ToVector3Shifted();
            }

            Ray shootline = new Ray(root, (targetPos - root));

            var cells = SightUtility.GetCellsOnLine(root, targetCell.ToVector3(), map);
            var shotTargDist = sourceCell.DistanceTo(targetCell);
            var shooterFaction = ShooterPawn.Faction;

            foreach (IntVec3 cell in cells)
            {
                if (Controller.settings.DebugDrawPartialLoSChecks)
                    caster.Map.debugDrawer.FlashCell(cell, 0.4f);

                if (sourceCell == cell)
                    continue;

                var index = map.cellIndices.CellToIndex(cell);
                var thing = map.thingGrid.thingGrid[index].Find(t => t.isPawn) ?? map.coverGrid.innerArray[index];

                if (thing == null)
                    continue;

                if (thing?.IsPlant() ?? true)
                    continue;

                if (thing.isPawn && (thing?.innerPawn?.Faction?.HostileTo(shooterFaction) ?? false))
                {
                    if (thing == target)
                        return true;
                    else
                    {
                        continue;
                    }
                }

                var notFullFillage = thing.def.Fillage != FillCategory.Full;

                if ((VerbPropsCE.ignorePartialLoSBlocker || aimMode == AimMode.SuppressFire) && notFullFillage)
                    continue;

                var isCover = sourceCell.AdjacentTo8Way(cell);
                if (isCover && notFullFillage)
                    if (shotTargDist > cell.DistanceTo(targetCell))
                    {
                        if (!thing.isPawn && cell != targetCell && CE_Utility.GetBoundsFor(thing).size.y >= targetPos.y)
                            return false;
                        continue;
                    }

                var bounds = CE_Utility.GetBoundsFor(thing);
                var interset = bounds.IntersectRay(ray: shootline);
                if (Controller.settings.DebugDrawPartialLoSChecks)
                    caster.Map.debugDrawer.FlashCell(cell, 0.7f, bounds.extents.y.ToString());
                if (cell != targetCell && interset)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #endregion
    }
}




