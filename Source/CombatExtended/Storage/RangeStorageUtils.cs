using System;
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
            return cell.GetThingList(map).Where(p => p.isPawn && p.innerPawn != null && !p.Destroyed && !p.innerPawn.Dead).Select(p => p.innerPawn);
        }

        public static IEnumerable<Pawn> PawnsInRange(this IntVec3 cell, int radius, Map map)
        {
            if (map == null || !cell.InBounds(map)) yield return null;
            else if (map.rangeStore != null)
            {
                Thing other;
                RangeStorage dataStore = map.rangeStore;

                var x = RangeStorage.BinaryFindIndex(dataStore.locationCacheX, cell.x, map);

                int index = x;
                while (index + 1 < dataStore.locationCacheX.Count)
                {
                    other = dataStore.locationCacheX[index + 1].value; index++;
                    if (!other.Spawned || other.Destroyed) continue; // valid thing?

                    if (other.positionInt.DistanceTo(cell) < radius) yield return other.innerPawn; // in radius?
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius) break; // are we still in bounds?
                }

                index = x;
                while (index - 1 >= 0)
                {
                    other = dataStore.locationCacheX[index - 1].value; index--;
                    if (!other.Spawned || other.Destroyed) continue;

                    if (other.positionInt.DistanceTo(cell) < radius) yield return other.innerPawn;
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius) break;
                }
            }
        }

        public static IEnumerable<Pawn> PawnsInRange(this Thing thing, float radius)
        {
            if (thing?.Map == null) yield return null;
            else if (thing.Spawned && !thing.Destroyed)
            {
                Thing other;
                RangeStorage dataStore = thing.Map.rangeStore;
                if (thing.indexValid)
                {
                    var x = thing.positionIndex_x;
                    var z = thing.positionIndex_z;

                    int index = x;
                    while (index + 1 < dataStore.locationCacheX.Count)
                    {
                        other = dataStore.locationCacheX[index + 1].value; index++;
                        if (!other.Spawned || other.Destroyed) continue; // valid thing?

                        if (other.positionInt.DistanceTo(thing.positionInt) < radius) yield return other.innerPawn; // in radius?
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > radius) break; // are we still in bounds?
                    }

                    index = x;
                    while (index - 1 >= 0)
                    {
                        other = dataStore.locationCacheX[index - 1].value; index--;
                        if (!other.Spawned || other.Destroyed) continue;

                        if (other.positionInt.DistanceTo(thing.positionInt) < radius) yield return other.innerPawn;
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > radius) break;
                    }
                }
            }
        }
    }
}