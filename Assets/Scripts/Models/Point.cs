using UnityEngine;

[System.Serializable]
public struct Point
{
    public float x;
    public float y;
    public Vector3 ToVector()
    {
        return new Vector3(x, y, 0);
    }
}
