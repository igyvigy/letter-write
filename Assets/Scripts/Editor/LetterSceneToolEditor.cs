using UnityEditor;
using UnityEngine;

/// <summary>
///  Editor tool and event listener
/// </summary>
[CustomEditor(typeof(LetterSceneTool), true)]
public class LetterSceneToolEditor : Editor
{
    protected LetterSceneTool letterTool;

    bool isSubscribed;
    private string jsonName = "";
    private bool isJsonCurved = false;

    public override void OnInspectorGUI()
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            DrawDefaultInspector();

            if (check.changed)
            {
                if (!isSubscribed)
                {
                    Subscribe();
                }

                if (letterTool.autoUpdate)
                {
                    TriggerUpdate();
                }
            }
        }

        if (GUILayout.Button("Manual Update"))
        {
            TriggerUpdate();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Clear"))
        {
            Clear();
            SceneView.RepaintAll();
        }

        jsonName = GUILayout.TextField(jsonName);
        isJsonCurved = GUILayout.Toggle(isJsonCurved, "is curved");
        if (GUILayout.Button("JSON from paths"))
        {
            letterTool.JsonFromPaths(jsonName, isJsonCurved);
        }
    }

    void TriggerUpdate()
    {
        letterTool.TriggerUpdate();
    }

    void Clear()
    {
        letterTool.Clear();
    }

    protected virtual void OnPathModified()
    {
        if (letterTool.autoUpdate)
        {
            TriggerUpdate();
        }
    }

    protected virtual void OnEnable()
    {
        letterTool = (LetterSceneTool)target;
        letterTool.onDestroyed += OnToolDestroyed;

        if (!isSubscribed)
        {
            Subscribe();
        }
        TriggerUpdate();
    }

    void OnToolDestroyed()
    {
        if (letterTool != null)
        {
            foreach (var lineCreator in letterTool.lineCreators)
            {
                lineCreator.pathCreator.pathUpdated -= OnPathModified;
            }
        }
    }

    protected virtual void Subscribe()
    {
        if (letterTool.lineCreators.Count > 0)
        {
            isSubscribed = true;
            foreach (var lineCreator in letterTool.lineCreators)
            {
                lineCreator.pathCreator.pathUpdated -= OnPathModified;
                lineCreator.pathCreator.pathUpdated += OnPathModified;
            }
        }
    }
}
