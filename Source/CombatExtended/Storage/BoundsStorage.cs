using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended.Storage
{
    public class BoundsStorage
    {
        private Map map;
        private List<Thing>[] pawnArray;

        public BoundsStorage(Map map)
        {
            this.map = map;
            this.pawnArray = new List<Thing>[map.cellIndices.NumGridCells];
        }

        public void RegisterAt(Thing t, int index)
        {
            if (pawnArray[index] == null)
            {
                pawnArray[index] = new List<Thing>(4);
            }

            pawnArray[index].Add(t);
        }

        public void DeRegisterAt(Thing t, int index)
        {
            if (pawnArray[index] != null)
            {
                pawnArray[index].RemoveAll(thing => thing.thingIDNumber == t.thingIDNumber);
            }
        }

        public void RegisterAt(Thing t, IntVec3 position)
        {
            if (position.InBounds(map))
            {
                RegisterAt(t, map.cellIndices.CellToIndex(position));
            }
        }

        public void DeRegisterAt(Thing t, IntVec3 position)
        {
            if (position.InBounds(map))
            {
                DeRegisterAt(t, map.cellIndices.CellToIndex(position));
            }
        }

        public Thing ThingOrPawnAt(IntVec3 position, out bool isPawn, bool noNoneFillage = true)
        {
            var index = map.cellIndices.CellToIndex(position);
            if (pawnArray[index] == null || pawnArray[index].Count == 0)
            {
                isPawn = false;
                var thing = map.coverGrid[index];
                if (thing?.def?.Fillage != FillCategory.None)
                    return thing;
                return null;
            }
            else
            {
                isPawn = true;
                return pawnArray[index].First();
            }
        }
    }
}
