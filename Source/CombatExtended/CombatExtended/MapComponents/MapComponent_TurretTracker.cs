using System.Collections.Generic;
using System.Text;
using Verse;

namespace CombatExtended
{
    public class MapComponent_TurretTracker : MapComponent
    {
        public HashSet<Building_TurretGunCE> Turrets = new HashSet<Building_TurretGunCE>();
        public HashSet<Building_TurretGunCE> TurretsRequiringReArming = new HashSet<Building_TurretGunCE>();
        
        public MapComponent_TurretTracker(Map map) : base(map)
        {
        }
        
        public void Register(Building_TurretGunCE t)
        {
            if (!Turrets.Contains(t)) {
                Turrets.Add(t);
            }
        }

        public void Unregister(Building_TurretGunCE t)
        {
            if (Turrets.Contains(t)) {
                Turrets.Remove(t);
            }
        }
    }
}
