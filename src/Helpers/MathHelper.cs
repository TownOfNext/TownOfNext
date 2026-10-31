using UnityEngine;

namespace TONX;

public class MathHelper
{
    public const float PI = (float)System.Math.PI;
    public const float Deg2Rad = PI / 180f;
    public const float Rad2Deg = 180f / PI;

    public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        var x = target.x - current.x;
        var y = target.y - current.y;
        var z = target.z - current.z;
        var squaredDistance = x * x + y * y + z * z;
        if (squaredDistance == 0f ||
            (maxDistanceDelta >= 0f && squaredDistance <= maxDistanceDelta * maxDistanceDelta))
            return target;
        var scale = maxDistanceDelta / (float)System.Math.Sqrt(squaredDistance);
        return new Vector3(current.x + x * scale, current.y + y * scale, current.z + z * scale);
    }
}