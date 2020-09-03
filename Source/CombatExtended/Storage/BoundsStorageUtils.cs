using Verse;

namespace CombatExtended.Storage
{
    public static class BoundsStorageUtils
    {
        public static void RegisterBoundPosition(this Thing pawn, IntVec3 oldPosition, IntVec3 newPosition)
        {
            var map = pawn.Map;

            var newIndex = map.cellIndices.CellToIndex(newPosition);
            var oldIndex = map.cellIndices.CellToIndex(oldPosition); ;

            if (oldPosition.IsValid)
            {
                map.boundsGrid.DeRegisterAt(pawn, oldIndex);
            }

            map.boundsGrid.RegisterAt(pawn, newIndex);
        }

        public static void DeRegisterBoundPosition(this Thing pawn, IntVec3 position)
        {
            var map = pawn.Map;
            var oldIndex = map.cellIndices.CellToIndex(position);

            if (position.IsValid)
            {
                map.boundsGrid.DeRegisterAt(pawn, oldIndex);
            }
        }
    }
}
