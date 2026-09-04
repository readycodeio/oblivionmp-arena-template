using System.Numerics;

namespace ArenaMod.Common
{
    public static class RArenaCommon
    {
        public static class Data
        {
            public static readonly Vector3 ArenaMiddle = new Vector3(0, 0, 75);
            public static readonly Vector3 ArenaOuter = new Vector3(0, 1320, 75);
            public static readonly Vector3 ArenaSpectatorStand = new Vector3(0,1835,580);
            public static readonly float MercyGiveUpMargin = 100;
            public static readonly float ArenaInnerRingRadius = 400;
            public static readonly float ArenaRadius = Math.Abs(ArenaMiddle.Y - ArenaOuter.Y);
            public static readonly float ArenaMajorityStartTimer = 30f;
            public static readonly string ArenaCellId = "L_PersistentDungeon-1C60F";
        }
    }
}
