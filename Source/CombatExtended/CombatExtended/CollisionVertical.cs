using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public struct CollisionVertical
    {
    	public const float ThickRoofThicknessMultiplier = 2f;
    	public const float NaturalRoofThicknessMultiplier = 2f;
        public const float MeterPerCellHeight = 1.75f;
    	public const float WallCollisionHeight = 2f;       // Walls are this tall
        public const float BodyRegionBottomHeight = 0.45f;  // Hits below this percentage will impact the corresponding body region
        public const float BodyRegionMiddleHeight = 0.85f;  // This also sets the altitude at which pawns hold their guns

        private readonly FloatRange heightRange;
        public readonly float shotHeight;

        public FloatRange HeightRange => new FloatRange(heightRange.min, heightRange.max);
        public float Min => heightRange.min;
        public float Max => heightRange.max;
        public float BottomHeight => Max * BodyRegionBottomHeight;
        public float MiddleHeight => Max * BodyRegionMiddleHeight;

        public static Dictionary<Thing, CollisionVertical> cachedCollisions = new Dictionary<Thing, CollisionVertical>();

        public CollisionVertical(float shotHeight, FloatRange heightRange) { this.shotHeight = shotHeight; this.heightRange = heightRange; }
        public CollisionVertical(Thing thing) => CalculateHeightRange(thing, out heightRange, out shotHeight);
        public CollisionVertical(Pawn pawn) => CalculatePawnHeightRange(pawn, out heightRange, out shotHeight);

        private static void CalculatePawnHeightRange(Pawn pawn, out FloatRange heightRange, out float shotHeight)
        {            
            float collisionHeight = 0f;
            float shotHeightOffset = 0;

            collisionHeight = CE_Utility.GetCollisionBodyFactors(pawn).y;
            	
            shotHeightOffset = collisionHeight * (1 - BodyRegionMiddleHeight);
				
            // Humanlikes in combat crouch to reduce their profile
            if (pawn.IsCrouching())
            {
                float crouchHeight = BodyRegionBottomHeight * collisionHeight;  // Minimum height we can crouch down to
                    
                // Find the highest adjacent cover
                Map map = pawn.Map;
                foreach(IntVec3 curCell in GenAdjFast.AdjacentCells8Way(pawn.Position))
                {
                    if (curCell.InBounds(map))
                    {
                        Thing cover = map.coverGrid[curCell];
                        if (cover != null && cover.def.Fillage == FillCategory.Partial && !cover.IsPlant())
                        {
                            var coverHeight = new CollisionVertical(cover).Max;
                            if (coverHeight > crouchHeight) crouchHeight = coverHeight;
                        }
                    }
                }
                collisionHeight = Mathf.Min(collisionHeight, crouchHeight + 0.01f + shotHeightOffset);  // We crouch down only so far that we can still shoot over our own cover and never beyond our own body size
            }

            var edificeHeight = 0f;
            if (pawn.Map != null)
            {
                var edifice = pawn.Map.coverGrid[pawn.Position];
                if (edifice != null && !edifice.IsPlant())
                {
                    edificeHeight = new CollisionVertical(edifice).heightRange.max;
                }

            }
            heightRange = new FloatRange(Mathf.Min(edificeHeight, edificeHeight + collisionHeight), Mathf.Max(edificeHeight, edificeHeight + collisionHeight));
            shotHeight = heightRange.max - shotHeightOffset;
        }

        private static void CalculateHeightRange(Thing thing, out FloatRange heightRange, out float shotHeight)
        {
            // In this case, I would rather see an error for the thing being null. 
            // This method is only called from within CE, and it should (logically) never
            // be called for something which is null. (Check in the code which calls it)
            shotHeight = 0;

            if(thing.CEIsPawn) // prefer to use bools instead of an `isinst`
            {
                CalculatePawnHeightRange(thing.CEInnerPawn, out heightRange, out shotHeight);
                return;
            }

            if(thing.IsPlant()) // prefer to use bools instead of an `isinst`
            { 
                //Height matches up exactly with visual size
                heightRange = new FloatRange(0f, BoundsInjector.ForPlant(thing as Plant).y);
                return;
            }
            if(thing is Building building)
            {
                if(thing.def.IsDoor && ((thing as Building_Door)?.Open ?? false))
                {
                    heightRange = new FloatRange(0,0);
                    return; //returns heightRange = (0,0) & shotHeight = 0. If not open, doors have FillCategory.Full so returns (0, WallCollisionHeight)
                }

                if(cachedCollisions.TryGetValue(thing, out CollisionVertical value)) // TODO: Maybe try to find a faster way to get the entry (thing doesn't really matter, just fillPercent)
                {
                    shotHeight = value.shotHeight;
                    heightRange = value.heightRange;
                    return;
                } 

                if (thing.def.Fillage == FillCategory.Full)
                {
                    heightRange = new FloatRange(0, WallCollisionHeight);
                    shotHeight = WallCollisionHeight;
                    return;
                }
                float fillPercent = thing.def.fillPercent;
                heightRange = new FloatRange(Mathf.Min(0f, fillPercent), Mathf.Max(0f, fillPercent));
                shotHeight = fillPercent;

                cachedCollisions.Add(thing, new CollisionVertical(shotHeight, heightRange));
                return;
            }
            
            // sneaking suspicision that this code doesn't need to be run if the `thing` is not a pawn - TODO

            float collisionHeight = thing.def.fillPercent;
            float shotHeightOffset = 0;

            var edificeHeight = 0f;
            if (thing.Map != null)
            {
                var edifice = thing.Map.coverGrid[thing.Position];
                if (edifice != null && edifice.GetHashCode() != thing.GetHashCode() && !edifice.IsPlant())
                {
                    edificeHeight = new CollisionVertical(edifice).heightRange.max;
                }
            }

            float fillPercent2 = collisionHeight;
            heightRange = new FloatRange(Mathf.Min(edificeHeight, edificeHeight + fillPercent2), Mathf.Max(edificeHeight, edificeHeight + fillPercent2));
            shotHeight = heightRange.max - shotHeightOffset;
        }

        /// <summary>
        /// Calculates the BodyPartHeight based on how high a projectile impacted in relation to overall pawn height.
        /// </summary>
        /// <param name="projectileHeight">The height of the projectile at time of impact.</param>
        /// <returns>BodyPartHeight between Bottom and Top.</returns>
        public BodyPartHeight GetCollisionBodyHeight(float projectileHeight)
        {
            if (projectileHeight < BottomHeight) return BodyPartHeight.Bottom;
            else if (projectileHeight < MiddleHeight) return BodyPartHeight.Middle;
            return BodyPartHeight.Top;
        }

        public BodyPartHeight GetRandWeightedBodyHeightBelow(float threshold)
        {
            return GetCollisionBodyHeight(Rand.Range(Min, threshold));
        }
    }
}
