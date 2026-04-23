using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using UnityEngine;

public static class ConverterExtension
{
    public static Vector3 rosMsg2Unity(this Vector3Msg position)
    {
        return new Vector3((float)position.x, (float)position.y, (float)position.z);
    }

    public static Vector3 rosMsg2Unity(this PointMsg point)
    {
        return new Vector3((float)point.x, (float)point.y, (float)point.z);
    }

    public static Quaternion rosMsg2Unity(this QuaternionMsg quaternion)
    {
        return new Quaternion((float)quaternion.x, (float)quaternion.y, (float)quaternion.z, (float)quaternion.w);
    }

    public static Color rosMsg2Unity(this ColorRGBAMsg colour)
    {
        return new Color(colour.r, colour.g, colour.b, colour.a);
    }

    public static PointMsg unity2RosPointMsg(this Vector3 point)
    {
        return new PointMsg(point.x, point.y, point.z);
    }

    public static Vector3Msg unity2RosVector3Msg(this Vector3 vector3)
    {
        return new Vector3Msg(vector3.x, vector3.y, vector3.z);
    }

    public static QuaternionMsg unity2RosQuaternionMsg(this Quaternion quaternion)
    {
        return new QuaternionMsg(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}
