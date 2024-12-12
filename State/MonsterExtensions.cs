using System;

namespace ReAgent.State;

public static class MonsterExtensions
{
    [Api]
    public static double DistanceTo(this MonsterInfo monster1, MonsterInfo monster2)
    {
        var dx = monster1.Position.X - monster2.Position.X;
        var dy = monster1.Position.Y - monster2.Position.Y;
        var dz = monster1.Position.Z - monster2.Position.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
} 