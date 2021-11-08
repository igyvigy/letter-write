using System.Collections.Generic;
using PathCreation;
using UnityEngine;

/// <summary>
///  Behaviour for controlling line creators in editor
/// </summary>
[ExecuteInEditMode]
public abstract class LetterSceneTool : MonoBehaviour
{
    public event System.Action onDestroyed;

    public bool autoUpdate = true;

    public List<LineCreator> lineCreators;

    [SerializeField]
    public string json;

    [SerializeField]
    public string generatedJson;

    public Letter letter
    {
        get
        {
            return JsonUtility.FromJson<Letter>(json);
        }
    }

    public Line[] linesFromPaths
    {
        get
        {
            List<Line> list = new List<Line>();
            foreach (var creator in lineCreators)
            {
                var numOfPointsInLine =
                    creator.pathCreator.bezierPath.NumAnchorPoints;
                var line = creator.line;
                var points = new List<Point>();
                for (
                    var pointIndex = 0;
                    pointIndex < numOfPointsInLine;
                    pointIndex++
                )
                {
                    if (pointIndex == 0)
                    {
                        var point = creator.pathCreator.bezierPath.GetPoint(0);
                        points.Add(new Point { x = point.x, y = point.y });
                    }
                    else
                    {
                        var point =
                            creator
                                .pathCreator
                                .bezierPath
                                .GetPoint(pointIndex * 3);
                        points.Add(new Point { x = point.x, y = point.y });
                    }
                }
                line.points = points.ToArray();
                list.Add(line);
            }
            return list.ToArray();
        }
    }

    public LineDrawZoneCreator[] drawZoneCreators
    {
        get
        {
            List<LineDrawZoneCreator> list = new List<LineDrawZoneCreator>();
            foreach (var creator in lineCreators)
            {
                list.Add(creator.GetComponent<LineDrawZoneCreator>());
            }
            return list.ToArray();
        }
    }

    [SerializeField]
    private GameObject pfLineCreator;

    public void Clear()
    {
        foreach (var creator in lineCreators)
        {
            creator.GetComponent<LineDrawZoneCreator>().Clear();
            creator.Clear();
            DestroyImmediate(creator.gameObject);
        }
        lineCreators.Clear();
        PathsCleared();
    }

    public void TriggerUpdate()
    {
        foreach (var line in letter.lines)
        {
            CreateOrUpdateLineCreatorForLine(line);
        }
        PathsUpdated();
    }

    public void JsonFromPaths(string name, bool curved = false)
    {
        if (curved)
        {
        }
        else
        {
            var letter = new Letter();
            letter.name = name;
            letter.lines = linesFromPaths;
            generatedJson = JsonUtility.ToJson(letter);
        }
    }

    public Vector3 GetPositionForPoint(Point point)
    {
        return point.ToVector();
    }

    protected virtual void OnDestroy()
    {
        if (onDestroyed != null)
        {
            onDestroyed();
        }
    }

    protected abstract void PathsUpdated();

    protected abstract void PathsCleared();

    private void CreateOrUpdateLineCreatorForLine(Line line)
    {
        if (lineCreators.Find(lc => lc.line.num == line.num) != null)
        {
            var lineCreator = lineCreators.Find(lc => lc.line.num == line.num);
            lineCreator.PathUpdated();
            lineCreator.GetComponent<LineDrawZoneCreator>().PathUpdated();
        }
        else
        {
            var lineCreator =
                Instantiate(pfLineCreator,
                this.transform.position,
                Quaternion.identity).GetComponent<LineCreator>();
            lineCreator.line = line;
            lineCreator.pathCreator = lineCreator.GetComponent<PathCreator>();
            lineCreator.GetComponent<LineDrawZoneCreator>().pathCreator =
                lineCreator
                    .GetComponent<LineDrawZoneCreator>()
                    .GetComponent<PathCreator>();

            float controlPOintOffsetPercent = 0.1f;

            if (
                !line.isCurved // this logic only for strait lines since we place control points aligned with the line it self
            )
            {
                for (
                    var pointIndex = 0;
                    pointIndex < line.points.Length;
                    pointIndex++
                )
                {
                    if (
                        pointIndex == 0 // for the first anchor control point will be towards next point
                    )
                    {
                        var point = line.points[pointIndex];
                        var currentPointPosition = GetPositionForPoint(point);
                        var nextPointPosition =
                            GetPositionForPoint(line.points[pointIndex + 1]);
                        var dir = (nextPointPosition - currentPointPosition).normalized;
                        var distance =
                            Vector3.Distance(currentPointPosition, nextPointPosition);
                        lineCreator
                            .pathCreator
                            .bezierPath
                            .SetPoint(0, currentPointPosition);
                        lineCreator
                            .pathCreator
                            .bezierPath
                            .SetPoint(1, currentPointPosition + dir * distance * controlPOintOffsetPercent);
                    }
                    else
                    {
                        // for other points there will be a control point in direction of previous anchor

                        var point = line.points[pointIndex];
                        var currentPointPosition = GetPositionForPoint(point);
                        var previousPointPosition =
                            GetPositionForPoint(line.points[pointIndex - 1]);
                        var dirToPrevious = (previousPointPosition - currentPointPosition).normalized;
                        var distanceToPrevious =
                            Vector3.Distance(currentPointPosition, previousPointPosition);
                        lineCreator
                            .pathCreator
                            .bezierPath
                            .SetPoint((pointIndex * 3) - 1,
                            currentPointPosition + dirToPrevious * distanceToPrevious * controlPOintOffsetPercent);
                        lineCreator
                            .pathCreator
                            .bezierPath
                            .SetPoint(pointIndex * 3, currentPointPosition);

                        // and if there will be one more point next, another control point in that direction
                        if (pointIndex + 1 < line.points.Length)
                        {
                            var nextPoint = line.points[pointIndex + 1];
                            var nextPointPosition = GetPositionForPoint(nextPoint);
                            lineCreator.pathCreator.bezierPath.AddSegmentToEnd(nextPointPosition);
                            var dirToNext = (nextPointPosition - currentPointPosition).normalized;
                            var distanceToNext =
                            Vector3.Distance(currentPointPosition, nextPointPosition);
                            lineCreator
                            .pathCreator
                            .bezierPath
                            .SetPoint((pointIndex * 3) + 1,
                            currentPointPosition + dirToNext * distanceToNext * controlPOintOffsetPercent);
                        }
                    }
                }
            }
            else
            {
                // just don't care about curved lines for now
            }

            if (lineCreators.Find(lc => lc.line.num == line.num) == null)
            {
                lineCreators.Add(lineCreator);
                lineCreator.PathUpdated();
                lineCreator.GetComponent<LineDrawZoneCreator>().PathUpdated();
            }
        }
    }
}
