using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour for controlling main game logic.
/// </summary>
public class LetterManager : LetterSceneTool
{
    public static readonly float Z_POSITION_FOR_DRAW = -0.01f;
    public static readonly float Z_POSITION_LINE_FILLER = -0.02f;
    public static readonly float Z_POSITION_NOT_COMPLETED_CONTROL_POINTS = -0.05f;
    public static readonly float Z_POSITION_COMPLETED_CONTROL_POINTS = -0.07f;

    [SerializeField]
    private Drawer drawer;

    [SerializeField]
    private GameObject pfControlPoint;

    [SerializeField]
    private GameObject pfLineFiller;

    [SerializeField]
    private RectTransform completeUI;

    [SerializeField]
    private RectTransform failUI;

    [SerializeField]
    private List<ControlPoint> controlPoints = new List<ControlPoint>();


    private List<Vector3> pointPositions = new List<Vector3>();

    private Dictionary<int, int[]>
        lineIndexForPointNumbers = new Dictionary<int, int[]>();

    private Dictionary<int, Transform>
        lineFillers = new Dictionary<int, Transform>();

    private Dictionary<int, float>
        lineMaxProgresses = new Dictionary<int, float>();

    private int lastSuccesfulPoint = 0;
    private int currentLineIndex = 0;
    private int drawStartPoint = 0;
    private int lastVisitedControlPointIndex = -1;
    private bool isInControlPoint = false;

    private bool IsUIShown
    {
        get
        {
            return failUI.gameObject.activeInHierarchy ||
            completeUI.gameObject.activeInHierarchy;
        }
    }

    private int GoalNumber
    {
        get
        {
            int goal = 0;
            foreach (var cp in controlPoints)
            {
                goal = Mathf.Max(goal, cp.number);
            }
            return goal;
        }
    }

    public void Retry()
    {
        ResetProgress();
    }

    private void Start()
    {
        drawer.onDrawStart += OnDrawStart;
        drawer.onDrawUpdate += OnDrawUpdate;
        drawer.onDrawEnd += OnDrawEnd;
        ReCalculatePointsToUse();
    }

    protected override void PathsUpdated()
    {
        ReCalculatePointsToUse();
        CreateOrUpdateControlPoints();
    }

    protected override void PathsCleared()
    {
        foreach (var cp in controlPoints)
        {
            DestroyImmediate(cp.gameObject);
        }
        controlPoints.Clear();
        pointPositions.Clear();
    }

    private void LetterComplete()
    {
        completeUI.gameObject.SetActive(true);
    }

    private void ShowFail()
    {
        failUI.gameObject.SetActive(true);
    }

    private void OnDrawStart(Vector3 point)
    {
        if (IsUIShown)
        {
            return;
        }
        var controlPointIndexForPoint = IsInControlPointWithIndex(point);
        if (controlPointIndexForPoint == -1)
        {
            // started not on a control point. Ignore
            drawStartPoint = 0;
            return;
        }
        else
        {
            var num = controlPoints[controlPointIndexForPoint].number;
            drawStartPoint = num;
            DidEnterControlPointWithNumber(num);
        }
    }

    private void OnDrawUpdate(Vector3 point)
    {
        if (IsUIShown)
        {
            return;
        }

        // explore control points for draw point
        var controlPointIndexForPoint = IsInControlPointWithIndex(point);
        if (controlPointIndexForPoint == -1)
        {
            if (isInControlPoint)
            {
                isInControlPoint = false;
                DidExitControlPointWithNumber(controlPoints[lastVisitedControlPointIndex]
                    .number);
            }
        }
        else
        {
            lastVisitedControlPointIndex = controlPointIndexForPoint;
            if (!isInControlPoint)
            {
                isInControlPoint = true;
                DidEnterControlPointWithNumber(controlPoints[controlPointIndexForPoint]
                    .number);
            }
        }

        // explore lines for draw point
        var lineIndicesAtPoint = CheckIfLinesWithIndexCollideWithPoint(point);
        if (lineIndicesAtPoint.Length == 0)
        {
            // not in a line. Check if control point
            if (!isInControlPoint)
            {
                // draw did exit permitted area. Fail.
                ShowFail();
            }
        }
        else
        {
            if (!Contains(lineIndicesAtPoint, currentLineIndex))
            {
                // not in a line. Check if control point
                if (!isInControlPoint)
                {
                    // drawing wrong line now. Fail.
                    ShowFail();
                }
            }
            else
            {
                // check if control point passed for the line
                var pointsForTheLine =
                    lineIndexForPointNumbers[currentLineIndex];
                if (!Contains(pointsForTheLine, lastSuccesfulPoint))
                {
                    if (!isInControlPoint)
                    {
                        // line started without previously completing control point. Fail.
                        ShowFail();
                    }
                }

                // handle case for starting to draw outside a control point or from a control point which is far from current
                if (drawStartPoint == 0 || drawStartPoint > lastSuccesfulPoint)
                {
                    if (!isInControlPoint)
                    {
                        // drawing didn't start from a control point. Or draw started from a further point on line. Fail
                        ShowFail();
                    }
                }

                // drawing now on correct line. Explore it's progress and fill.
                var path = lineCreators[currentLineIndex].pathCreator.path;
                var distance = path.GetClosestDistanceAlongPath(point);
                var progress = path.GetClosestTimeOnPath(point);

                if (!lineMaxProgresses.ContainsKey(currentLineIndex))
                {
                    lineMaxProgresses[currentLineIndex] = progress;
                }
                else
                {
                    if (lineMaxProgresses[currentLineIndex] > progress)
                    {
                        // user is drawing backwards. Ignore
                        return;
                    }
                    else
                    {
                        lineMaxProgresses[currentLineIndex] = progress;
                    }
                }

                // move line filller
                var pos =
                    path
                        .GetPointAtDistance(distance,
                        PathCreation.EndOfPathInstruction.Stop);
                var positionWithZ =
                    new Vector3(pos.x, pos.y, Z_POSITION_LINE_FILLER);
                var rot =
                    path
                        .GetRotationAtDistance(distance,
                        PathCreation.EndOfPathInstruction.Stop);
                if (!lineFillers.ContainsKey(currentLineIndex))
                {
                    var lineFiller =
                        Instantiate(pfLineFiller, positionWithZ, rot).transform;
                    lineFillers[currentLineIndex] = lineFiller;
                }
                else
                {
                    lineFillers[currentLineIndex].position = positionWithZ;
                    lineFillers[currentLineIndex].rotation = rot;
                }
            }
        }
    }

    private void OnDrawEnd(Vector3 point)
    {
        if (IsUIShown)
        {
            return;
        }
        var controlPointIndexForPoint = IsInControlPointWithIndex(point);
        if (controlPointIndexForPoint == -1)
        {
            // end draw not inside a control point. Fail
            ShowFail();
        }
        else
        {
            if (isInControlPoint)
            {
                isInControlPoint = false;
            }
            var num = controlPoints[controlPointIndexForPoint].number;
            if (num == lastSuccesfulPoint + 1)
            {
                CompletePointWithNumber(num);
            }
            else
            {
                // end draw in a wrong control point. Fail
                ShowFail();
            }
        }
    }

    private void DidExitControlPointWithNumber(int num)
    {
        if (num == lastSuccesfulPoint + 1)
        {
            CompletePointWithNumber(num);
        }
        else if (num == lastSuccesfulPoint && num != drawStartPoint)
        {
            // passed previously completed control point. Fail
            ShowFail();
        }
    }

    private void CompletePointWithNumber(int num)
    {
        lastSuccesfulPoint = num;
        if (controlPoints.Find(cp => cp.number == num) != null)
        {
            var controlPoint = controlPoints.Find(cp => cp.number == num);
            controlPoint.SetCompleted(true);
            controlPoint.transform.position =
                new Vector3(controlPoint.transform.position.x,
                    controlPoint.transform.position.y,
                    Z_POSITION_COMPLETED_CONTROL_POINTS);
        }
        if (num == GoalNumber)
        {
            LetterComplete();
        }
        else
        {
            // set current line index
            var maxLineIndex = -1;
            foreach (KeyValuePair<int, int[]> entry in lineIndexForPointNumbers)
            {
                var lineIndex = entry.Key;
                var pointNumbers = entry.Value;
                var maxPointNumber = 0;
                foreach (var pointNumber in pointNumbers)
                {
                    maxPointNumber = Mathf.Max(maxPointNumber, pointNumber);
                }
                if (num == maxPointNumber)
                {
                    maxLineIndex = Mathf.Max(maxLineIndex, lineIndex + 1);
                }
            }
            if (maxLineIndex > 0)
            {
                currentLineIndex = maxLineIndex;
            }
        }
    }

    private void DidEnterControlPointWithNumber(int num)
    {
    }

    private void ResetProgress()
    {
        drawer.RemoveAllLines();
        lastSuccesfulPoint = 0;
        lastVisitedControlPointIndex = -1;
        drawStartPoint = 0;
        isInControlPoint = false;
        currentLineIndex = 0;
        foreach (var cp in controlPoints)
        {
            cp.SetCompleted(false);
            cp.transform.position =
                new Vector3(cp.transform.position.x,
                    cp.transform.position.y,
                    Z_POSITION_NOT_COMPLETED_CONTROL_POINTS);
        }
        foreach (KeyValuePair<int, Transform> entry in lineFillers)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value.gameObject);
            }
        }
        lineFillers.Clear();
        lineMaxProgresses.Clear();
        if (completeUI.gameObject.activeInHierarchy)
        {
            completeUI.gameObject.SetActive(false);
        }
        if (failUI.gameObject.activeInHierarchy)
        {
            failUI.gameObject.SetActive(false);
        }
    }

    private void CreateOrUpdateControlPoints()
    {
        for (var index = 0; index < pointPositions.Count; index++)
        {
            var pos = pointPositions[index];
            int number = index + 1;
            var cpPos =
                new Vector3(pos.x,
                    pos.y,
                    Z_POSITION_NOT_COMPLETED_CONTROL_POINTS);

            if (controlPoints.Find(cp => cp.number == number) != null)
            {
                var controlPoint =
                    controlPoints.Find(cp => cp.number == number);
                controlPoint.transform.position = cpPos;
            }
            else
            {
                var controlPoint =
                    Instantiate(pfControlPoint,
                    cpPos,
                    Quaternion.identity,
                    this.transform).GetComponent<ControlPoint>();
                controlPoint.SetNumber(number);
                controlPoint.SetCompleted(false);
                controlPoints.Add(controlPoint);
            }
        }
    }

    private void ReCalculatePointsToUse()
    {
        pointPositions.Clear();

        foreach (var line in linesFromPaths)
        {
            foreach (var p in line.points)
            {
                var pos = GetPositionForPoint(p);
                if (!pointPositions.Contains(pos))
                {
                    pointPositions.Add(pos);
                }
            }
        }

        RecalculatePointNumbersForLines();
    }

    private void RecalculatePointNumbersForLines()
    {
        lineIndexForPointNumbers.Clear();

        for (var lineIndex = 0; lineIndex < linesFromPaths.Length; lineIndex++)
        {
            var line = linesFromPaths[lineIndex];
            var pointNumbers = new List<int>();
            foreach (var point in line.points)
            {
                var pointPosition = GetPositionForPoint(point);
                for (
                    var pointPositionIndex = 0;
                    pointPositionIndex < pointPositions.Count;
                    pointPositionIndex++
                )
                {
                    var pos = pointPositions[pointPositionIndex];
                    int number = pointPositionIndex + 1;

                    if (pointPosition == pos)
                    {
                        pointNumbers.Add(number);
                    }
                }
            }

            lineIndexForPointNumbers[lineIndex] = pointNumbers.ToArray();
        }
    }

    private MeshCollider[] Getcolliders()
    {
        List<MeshCollider> list = new List<MeshCollider>();
        foreach (var creator in drawZoneCreators)
        {
            list.Add(creator.meshCollider.GetComponent<MeshCollider>());
        }
        return list.ToArray();
    }

    private SphereCollider[] GetControlPointColliders()
    {
        List<SphereCollider> list = new List<SphereCollider>();
        foreach (var cp in controlPoints)
        {
            list.Add(cp.GetComponent<SphereCollider>());
        }
        return list.ToArray();
    }

    private int[] CheckIfLinesWithIndexCollideWithPoint(Vector3 point)
    {
        List<int> indices = new List<int>();
        for (var index = 0; index < Getcolliders().Length; index++)
        {
            if (IsInCollider(Getcolliders()[index], point))
            {
                indices.Add(index);
            }
        }
        return indices.ToArray();
    }

    private int IsInControlPointWithIndex(Vector3 point)
    {
        int res = -1;
        for (var index = 0; index < GetControlPointColliders().Length; index++)
        {
            if (IsInCollider(GetControlPointColliders()[index], point))
            {
                res = index;
            }
        }
        return res;
    }

    private bool IsInCollider(Collider other, Vector3 point)
    {
        bool result = false;
        Vector3 from = new Vector3(point.x, point.y, point.z - 1);
        Vector3 to = new Vector3(point.x, point.y, point.z + 1);
        Vector3 dir = (to - from);

        Debug.DrawLine(from, from + dir);

        RaycastHit[] hits = Physics.RaycastAll(from, dir);

        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    if (hit.collider == other)
                    {
                        result = true;
                    }
                }
            }
        }

        return result;
    }

    private bool Contains(int[] arr, int index)
    {
        var contains = false;
        foreach (var i in arr)
        {
            if (i == index)
            {
                contains = true;
            }
        }
        return contains;
    }
}
