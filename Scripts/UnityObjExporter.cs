using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class UnityObjExporter : MonoBehaviour
{
    [Header("Configuration")]
    public int floatingPointPrecision = 4;
    public TransformMode transformMode = TransformMode.ApplyTransform;

    [Header("Editor")]
    [SerializeField] private bool runOnAwake;
    [SerializeField] private string exportPath;
    [SerializeField] private List<MeshFilter> meshFilters;
    [SerializeField] private List<SkinnedMeshRenderer> skinnedMeshRenderers;

    private int startIndex;
    private string floatingPointFormat;
    private List<ExportableObject> exportableObjects = new List<ExportableObject>();

    public enum TransformMode
    {
        DoNotApplyTransform,
        ApplyTransform
    }

    private class ExportableObject
    {
        public string name;
        public Mesh mesh;
        public Transform transform;
    }

    private void Awake()
    {
        if (runOnAwake)
        {
            foreach (var filter in meshFilters)
            {
                exportableObjects.Add(new ExportableObject()
                {
                    name = filter.name,
                    mesh = filter.sharedMesh,
                    transform = filter.transform
                });
            }

            foreach (var renderer in skinnedMeshRenderers)
            {
                exportableObjects.Add(new ExportableObject()
                {
                    name = renderer.name,
                    mesh = renderer.sharedMesh,
                    transform = renderer.transform
                });
            }

            Export(exportPath);
        }
    }

    public void Add (MeshFilter filter)
    {
        var obj = new ExportableObject()
        {
            name = filter.name,
            mesh = filter.sharedMesh,
            transform = filter.transform,
        };
        exportableObjects.Add(obj);
    }

    public void Add (SkinnedMeshRenderer renderer)
    {
        var obj = new ExportableObject()
        {
            name = renderer.name,
            mesh = renderer.sharedMesh,
            transform = renderer.transform,
        };
        exportableObjects.Add(obj);
    }

    public void Clear()
    {
        exportableObjects.Clear();
    }

    public void Export (string exportPath)
    {
        exportPath = CleanPath(exportPath);

        var builder = new StringBuilder();

        AppendHeaderText(builder);

        startIndex = 1;
        floatingPointFormat = EncodeFloatingPointFormat();
        foreach (var obj in exportableObjects)
            AppendObject(obj, builder);

        using (var writer = new StreamWriter(exportPath))
            writer.Write(builder.ToString());

        Debug.Log($".obj successfully exported to {exportPath}");
    }

    private string CleanPath(string exportPath)
    {
        string directory = Path.GetDirectoryName(exportPath);
        if (!Directory.Exists(directory))
            throw new ArgumentException($"Directory \"{directory}\" does not exist");

        if (Path.GetExtension(exportPath) != ".obj")
            exportPath = Path.ChangeExtension(exportPath, ".obj");

        return exportPath;
    }

    private string EncodeFloatingPointFormat()
    {
        var builder = new StringBuilder();
        builder.Append("0.");
        for (int i = 0; i < floatingPointPrecision; i++)
            builder.Append("0");
        return builder.ToString();
    }

    private void AppendHeaderText(StringBuilder builder)
    {
        builder.Append("#.obj created with UnityObjExporter by Michael Ovens\n");
        builder.Append("\n");
    }

    private void AppendObject (ExportableObject obj, StringBuilder builder)
    {
        AppendObjectInfo(obj, builder);
        AppendGeometricVertices(obj, builder);
        AppendTextureVertices(obj, builder);
        AppendVertexNormals(obj, builder);
        AppendFaces(obj, builder);

        builder.Append("\n");
        startIndex += obj.mesh.vertexCount;
    }

    private void AppendObjectInfo (ExportableObject obj, StringBuilder builder)
    {
        builder.Append($"o {obj.name}\n");
    }

    private void AppendGeometricVertices (ExportableObject obj, StringBuilder builder)
    {
        var vertices = obj.mesh.vertices;
        foreach (var vertex in vertices)
            builder.Append($"v {Vector3ToString(TransformVector3(vertex, obj))}\n");
    }

    private void AppendTextureVertices (ExportableObject obj, StringBuilder builder)
    {
        var vertices = obj.mesh.uv;
        foreach (var vertex in vertices)
            builder.Append($"vt {Vector2ToString(vertex)}\n");
    }

    private void AppendVertexNormals (ExportableObject obj, StringBuilder builder)
    {
        var vertices = obj.mesh.normals;
        foreach (var vertex in vertices)
            builder.Append($"vn {Vector3ToString(TransformVector3(vertex, obj))}\n");
    }

    private void AppendFaces (ExportableObject obj, StringBuilder builder)
    {
        var faces = obj.mesh.triangles;
        int faceCount = faces.Length / 3;
        for (int i = 0; i < faceCount; i++)
        {
            int v1 = faces[i * 3] + startIndex;
            int v2 = faces[i * 3 + 1] + startIndex;
            int v3 = faces[i * 3 + 2] + startIndex;

            // Reverse winding order to go from left-hand to right-hand coordinate system
            builder.Append($"f {v3}/{v3}/{v3} {v2}/{v2}/{v2} {v1}/{v1}/{v1}\n");
        }
    }

    private Vector3 TransformVector3 (Vector3 vector, ExportableObject obj)
    {
        if (transformMode == TransformMode.ApplyTransform)
        {
            vector = obj.transform.TransformPoint(vector);
            vector = new Vector3(
                vector.x * obj.transform.localScale.x,
                vector.y * obj.transform.localScale.y,
                vector.z * obj.transform.localScale.z);
        }

        vector.z *= -1f; // Convert to right-hand coordinate system

        return vector;
    }

    private string Vector3ToString (Vector3 vector)
    {
        return
            vector.x.ToString(floatingPointFormat) + " " +
            vector.y.ToString(floatingPointFormat) + " " +
            vector.z.ToString(floatingPointFormat);
    }

    private string Vector2ToString (Vector2 vector)
    {
        return
            vector.x.ToString(floatingPointFormat) + " " +
            vector.y.ToString(floatingPointFormat);
    }
}