using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour for getting drawing input from user
/// </summary>
public class Drawer : MonoBehaviour
{
    public event System.Action<Vector3> onDrawStart;
    public event System.Action<Vector3> onDrawEnd;
    public event System.Action<Vector3> onDrawTap;
    public event System.Action<Vector3> onDrawUpdate;

    [SerializeField]
    private GameObject trailPrefab;
    private GameObject thisTrail;
    private Vector3 startPos;
    private Plane objPlane;
    private List<GameObject> drawnLines = new List<GameObject>();

    public void RemoveAllLines()
    {
        foreach (var line in drawnLines)
        {
            Destroy(line);
        }
        drawnLines.Clear();
    }

    private void Start()
    {
        objPlane =
            new Plane(Camera.main.transform.forward * -1,
                new Vector3(0, 0, LetterManager.Z_POSITION_FOR_DRAW));
    }

    private void Update()
    {
        if (
            Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began ||
            Input.GetMouseButtonDown(0)
        )
        {
            var point = GetPointOnPlaneForPointer();

            if (point != null)
            {
                startPos = point.Value;
            }

            thisTrail = Instantiate(trailPrefab, startPos, Quaternion.identity);
            thisTrail.transform.SetParent(this.transform);
            drawnLines.Add(thisTrail);
            if (onDrawStart != null)
            {
                onDrawStart(startPos);
            }
        }
        else if (
            Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Moved ||
            Input.GetMouseButton(0)
        )
        {
            var point = GetPointOnPlaneForPointer();

            if (point != null)
            {
                if (point == startPos)
                {
                    return;
                }
                if (thisTrail != null)
                {
                    thisTrail.transform.position = point.Value;
                }

                if (onDrawUpdate != null)
                {
                    onDrawUpdate(point.Value);
                }
            }
        }
        else if (
            Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Ended ||
            Input.GetMouseButtonUp(0)
        )
        {
            var point = GetPointOnPlaneForPointer();

            if (point != null)
            {
                if (thisTrail != null)
                {
                    if (Vector3.Distance(point.Value, startPos) < 0.1)
                    {
                        Destroy(thisTrail);
                        if (onDrawTap != null)
                        {
                            onDrawTap(point.Value);
                        }
                    }
                    else
                    {
                        if (onDrawEnd != null)
                        {
                            onDrawEnd(point.Value);
                        }
                    }
                }
            }
        }
    }

    private Vector3? GetPointOnPlaneForPointer()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (objPlane.Raycast(ray, out float rayDistance))
        {
            return ray.GetPoint(rayDistance);
        }
        return null;
    }
}
