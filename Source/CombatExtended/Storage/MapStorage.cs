using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Storage
{
    public class MapStorage
    {
        private Map map;
        private readonly int dim;

        struct CacheUnit<T>
        {
            public int tick;
            public T value;
        }

        private const int MAX_CACHE_AGE = 5;

        private const int X_GRID = 5;
        private const int Y_GRID = 5;

        struct CacheKey
        {
            public IntVec3 a;
            public IntVec3 b;

            public int dim;

            public override int GetHashCode()
            {
                return ((int)(a.x * dim + a.z)).GetHashCode() ^ (((int)((b.x / X_GRID) * dim + (b.z / Y_GRID))).GetHashCode() << 1);
            }

            public override bool Equals(object obj)
            {
                return GetHashCode() == obj.GetHashCode();
            }
        }

        private Dictionary<CacheKey, CacheUnit<Pair<float, bool>>> canHitCache = new Dictionary<CacheKey, CacheUnit<Pair<float, bool>>>(5000);
        private Dictionary<CacheKey, CacheUnit<bool>> canHitIgnoringRangeCache = new Dictionary<CacheKey, CacheUnit<bool>>(5000);

        public int ticksGame = 0;

        public MapStorage(Map map)
        {
            this.map = map;
            this.dim = map.Size.x / X_GRID;
        }

        public bool TryGetCanHitFromIgnoringRange(IntVec3 a, IntVec3 b, out bool result)
        {
            var key = new CacheKey() { a = a, b = b, dim = this.dim };
            if (canHitIgnoringRangeCache.TryGetValue(key, out CacheUnit<bool> unit))
            {
                if (unit.tick + MAX_CACHE_AGE > ticksGame)
                {
                    result = unit.value;
                    return true;
                }
            }
            result = false;
            return false;
        }

        public bool TryGetCanHit(IntVec3 a, IntVec3 b, float minRange, out bool result)
        {
            var key = new CacheKey() { a = a, b = b, dim = this.dim };
            if (canHitCache.TryGetValue(key, out CacheUnit<Pair<float, bool>> unit))
            {
                if (unit.tick + MAX_CACHE_AGE > ticksGame)
                {
                    result = unit.value.second;
                    if (unit.value.first < minRange)
                        return true;
                    return false;
                }
            }
            result = false;
            return false;
        }

        public void RegisterCanHit(IntVec3 a, IntVec3 b, float range, bool result)
        {
            var key = new CacheKey() { a = a, b = b, dim = this.dim };
            var value = new CacheUnit<Pair<float, bool>>()
            {
                tick = ticksGame,
                value = new Pair<float, bool>() { first = range, second = result }
            };

            canHitCache[key] = value;
        }

        public void RegisterCanHiIgnoringRange(IntVec3 a, IntVec3 b, bool result)
        {
            var key = new CacheKey() { a = a, b = b, dim = this.dim };
            var value = new CacheUnit<bool>()
            {
                tick = ticksGame,
                value = result
            };

            canHitIgnoringRangeCache[key] = value;
        }

        public void Reset()
        {
            canHitCache.Clear();
            canHitIgnoringRangeCache.Clear();
        }
    }
}
