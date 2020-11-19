﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Storage
{
    public static class StorageUtils
    {
        public static IEnumerable<Pawn> HostilesOfInRange(this IntVec3 cell, Faction faction, Map map, int radius)
        {
            return PawnsInRange(cell, radius, map).Where(p => !p.RaceProps.Animal && (p.Faction?.HostileTo(faction) ?? false));
        }

        public static IEnumerable<Pawn> HostilesInRange(this Pawn pawn, int radius)
        {
            return PawnsInRange(pawn.positionInt, radius, pawn.Map).Where(p => !p.RaceProps.Animal && (p.Faction?.HostileTo(pawn.factionInt) ?? false));
        }

        public static IEnumerable<Pawn> PawnsAt(this IntVec3 cell, Map map)
        {
            return cell.GetThingList(map).Where(p => p.CEIsPawn && p.CEInnerPawn != null && !p.Destroyed && !p.CEInnerPawn.Dead).Select(p => p.CEInnerPawn);
        }

        public static IEnumerable<Pawn> PawnsInRange(this IntVec3 cell, int radius, Map map)
        {
            if (map == null || !cell.InBounds(map)) yield return null;
            else if (map.CERangeStore != null)
            {
                Thing other;
                RangeStorage dataStore = map.CERangeStore;

                var x = RangeStorage.BinaryFindIndex(dataStore.locationCacheX, cell.x, map);

                int index = x;
                while (index + 1 < dataStore.locationCacheX.Count)
                {
                    other = dataStore.locationCacheX[index + 1].value; index++;
                    if (!other.Spawned || other.Destroyed) continue; // valid thing?

                    if (other.positionInt.DistanceTo(cell) < radius / 2f) yield return other.CEInnerPawn; // in radius?
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius / 2f) break; // are we still in bounds?
                }

                index = x;
                while (index - 1 >= 0)
                {
                    other = dataStore.locationCacheX[index - 1].value; index--;
                    if (!other.Spawned || other.Destroyed) continue;

                    if (other.positionInt.DistanceTo(cell) < radius / 2f) yield return other.CEInnerPawn;
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius / 2f) break;
                }
            }
        }

        public static IEnumerable<Pawn> PawnsInRange(this Thing thing, float radius)
        {
            if (thing?.Map == null) yield return null;
            else if (thing.Spawned && !thing.Destroyed)
            {
                Thing other;
                RangeStorage dataStore = thing.Map.CERangeStore;
                if (thing.CEIndexValid)
                {
                    var x = thing.CEPositionIndex_x;
                    var z = thing.CEPositionIndex_z;

                    if (x >= dataStore.locationCacheX.Count)
                    {
                        Log.Message("CE: Tied to get Pawns in range for thing with no valid index");
                    }
                    else
                    {
                        int index = x;
                        while (index + 1 < dataStore.locationCacheX.Count)
                        {
                            other = dataStore.locationCacheX[index + 1].value; index++;
                            if (!other.Spawned || other.Destroyed) continue; // valid thing?

                            if (other.positionInt.DistanceTo(thing.positionInt) < radius) yield return other.CEInnerPawn; // in radius?
                            else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > radius) break; // are we still in bounds?
                        }

                        index = x;
                        while (index - 1 >= 0)
                        {
                            other = dataStore.locationCacheX[index - 1].value; index--;
                            if (!other.Spawned || other.Destroyed) continue;

                            if (other.positionInt.DistanceTo(thing.positionInt) < radius) yield return other.CEInnerPawn;
                            else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > radius) break;
                        }
                    }
                }
            }
        }
    }
}