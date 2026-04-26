using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class OutlineSkinned : MonoBehaviour
{
    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public Mode OutlineMode
    {
        get { return outlineMode; }
        set { outlineMode = value; needsUpdate = true; }
    }

    public Color OutlineColor
    {
        get { return outlineColor; }
        set { outlineColor = value; needsUpdate = true; }
    }

    public float OutlineWidth
    {
        get { return outlineWidth; }
        set { outlineWidth = value; needsUpdate = true; }
    }

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    [SerializeField]
    private Mode outlineMode;

    [SerializeField]
    private Color outlineColor = Color.white; [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2f; [Header("Optional")]
    [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
    + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
    private bool precomputeOutline; [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>(); [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;
    private bool needsUpdate;

    // References to cleanly restore meshes so we don't permanently corrupt Editor Assets
    private List<Mesh> instancedMeshes = new List<Mesh>();
    private Dictionary<MeshFilter, Mesh> originalMeshFilters = new Dictionary<MeshFilter, Mesh>();
    private Dictionary<SkinnedMeshRenderer, Mesh> originalSkinnedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));
        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        LoadSmoothNormals();
        needsUpdate = true;
    }

    void OnEnable()
    {
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaskMaterial);
            materials.Add(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnValidate()
    {
        needsUpdate = true;

        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
        {
            bakeKeys.Clear();
            bakeValues.Clear();
        }

        if (precomputeOutline && bakeKeys.Count == 0)
        {
            Bake();
        }
    }

    void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    void OnDisable()
    {
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnDestroy()
    {
        Destroy(outlineMaskMaterial);
        Destroy(outlineFillMaterial);

        // Restore original meshes to prevent the models from turning invisible
        foreach (var kvp in originalMeshFilters)
        {
            if (kvp.Key != null) kvp.Key.sharedMesh = kvp.Value;
        }
        foreach (var kvp in originalSkinnedMeshes)
        {
            if (kvp.Key != null) kvp.Key.sharedMesh = kvp.Value;
        }

        // Clean up our cloned meshes from memory
        foreach (var mesh in instancedMeshes)
        {
            Destroy(mesh);
        }

        instancedMeshes.Clear();
        originalMeshFilters.Clear();
        originalSkinnedMeshes.Clear();
    }

    void Bake()
    {
        var bakedMeshes = new HashSet<Mesh>();

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;

            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }

        // FIX: Track SkinnedMeshRenderers too so we don't get trapped in an endless Precompute loop
        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!bakedMeshes.Add(skinnedMeshRenderer.sharedMesh)) continue;

            bakeKeys.Add(skinnedMeshRenderer.sharedMesh);
            bakeValues.Add(new ListVector3() { data = new List<Vector3>() });
        }
    }

    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null) continue;

            // Clone the mesh so we don't modify the actual project asset (.fbx)
            Mesh originalMesh = meshFilter.sharedMesh;
            originalMeshFilters[meshFilter] = originalMesh;

            Mesh clone = Instantiate(originalMesh);
            instancedMeshes.Add(clone);
            meshFilter.sharedMesh = clone;

            var index = bakeKeys.IndexOf(originalMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(clone);

            clone.SetUVs(3, smoothNormals);

            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null)
            {
                CombineSubmeshes(clone, renderer.sharedMaterials);
            }
        }

        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (skinnedMeshRenderer.sharedMesh == null) continue;

            Mesh originalMesh = skinnedMeshRenderer.sharedMesh;
            originalSkinnedMeshes[skinnedMeshRenderer] = originalMesh;

            Mesh clone = Instantiate(originalMesh);
            instancedMeshes.Add(clone);
            skinnedMeshRenderer.sharedMesh = clone;

            // Clear UV4 to prevent the animated outline from snapping to a bind-pose direction.
            // The shader will fall back to using standard animated normals.
            clone.uv4 = new Vector2[clone.vertexCount];

            CombineSubmeshes(clone, skinnedMeshRenderer.sharedMaterials);
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        var smoothNormals = new List<Vector3>(mesh.normals);

        foreach (var group in groups)
        {
            if (group.Count() == 1) continue;

            var smoothNormal = Vector3.zero;
            foreach (var pair in group) smoothNormal += smoothNormals[pair.Value];
            smoothNormal.Normalize();

            foreach (var pair in group) smoothNormals[pair.Value] = smoothNormal;
        }
        return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials)
    {

        // Skip meshes with a single submesh
        if (mesh.subMeshCount == 1)
        {
            return;
        }

        // Skip if submesh count exceeds material count
        if (mesh.subMeshCount > materials.Length)
        {
            return;
        }

        // Append combined submesh
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    void UpdateMaterialProperties()
    {

        // Apply properties according to mode
        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

        switch (outlineMode)
        {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }
}
