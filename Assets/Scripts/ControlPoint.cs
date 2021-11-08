using TMPro;
using UnityEngine;

/// <summary>
/// Behavior for a control point
/// </summary>
public class ControlPoint : MonoBehaviour
{
    bool isCompleted = false;
    public int number = 0;

    [SerializeField]
    Material incompleteMaterial;

    [SerializeField]
    Material completeMaterial;

    public void SetNumber(int number)
    {
        this.number = number;
        transform
            .Find("Text")
            .GetComponent<TextMeshPro>()
            .SetText(number.ToString());
    }

    public void SetCompleted(bool isCompleted)
    {
        this.isCompleted = isCompleted;
        SetMaterial();
    }

    private void SetMaterial()
    {
        if (isCompleted)
        {
            transform.Find("Sprite").GetComponent<SpriteRenderer>().material =
                completeMaterial;
        }
        else
        {
            transform.Find("Sprite").GetComponent<SpriteRenderer>().material =
                incompleteMaterial;
        }
    }
}
