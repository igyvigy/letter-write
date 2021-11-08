using UnityEngine;

/// <summary>
///  Line creator with mesh extended by a collider. Used for drawing zone control
/// </summary>
public class LineDrawZoneCreator : LineCreator
{
    public MeshCollider meshCollider;

    public override void AssignMeshComponents()
    {
        if (meshHolder == null)
        {
            meshHolder = new GameObject(meshHolderName);
        }

        meshHolder.transform.rotation = Quaternion.identity;
        meshHolder.transform.position = Vector3.zero;
        meshHolder.transform.localScale = Vector3.one;

        if (!meshHolder.gameObject.GetComponent<MeshFilter>())
        {
            meshHolder.gameObject.AddComponent<MeshFilter>();
        }
        if (!meshHolder.GetComponent<MeshRenderer>())
        {
            meshHolder.gameObject.AddComponent<MeshRenderer>();
        }
        if (!meshHolder.GetComponent<MeshCollider>())
        {
            meshHolder.gameObject.AddComponent<MeshCollider>();
        }

        meshRenderer = meshHolder.GetComponent<MeshRenderer>();
        meshFilter = meshHolder.GetComponent<MeshFilter>();
        meshCollider = meshHolder.GetComponent<MeshCollider>();

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public override void AssignMaterials()
    {
        if (lineMaterial != null && undersideMaterial != null)
        {
            meshRenderer.sharedMaterials =
                new Material[] {
                    lineMaterial,
                    undersideMaterial,
                    undersideMaterial
                };

            meshRenderer.sharedMaterials[0].mainTextureScale =
                new Vector3(1, textureTiling);
        }
    }
}
