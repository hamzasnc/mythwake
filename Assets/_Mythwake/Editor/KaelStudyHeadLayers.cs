using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

/// <summary>Study-only geometric separation of the rigid head drawing; the atlas is never edited.</summary>
public static class KaelStudyHeadLayers
{
    static readonly Vector2[] Cut = { new Vector2(0,156), new Vector2(68,156), new Vector2(82,171),
        new Vector2(114,194), new Vector2(144,207), new Vector2(243,207) };
    [Serializable] sealed class Fingerprint { public string path, sha256; }
    [Serializable] sealed class Manifest
    {
        public string sourceSprite, sourceGuid;
        public long sourceLocalId;
        public Rect rect;
        public Vector2 pivotPixels;
        public float pixelsPerUnit;
        public Vector2[] cutTopLeftPixels;
        public int verticesPerPart, indicesPerPart;
        public bool savedGeometryAndUvsVerified;
        public Fingerprint[] files;
    }

    public static void Prepare(GameObject actor, string assetFolder)
    {
        if (actor == null || EditorUtility.IsPersistent(actor)) throw new ArgumentException("Head layers require a temporary study actor instance.");
        var rig = actor.GetComponent<KaelRig>();
        if (rig == null) throw new InvalidOperationException("Study source is missing KaelRig.");
        var head = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "Head Art");
        var original = head.sprite;
        if (head.transform.parent.name != "Head" || head.transform.parent.Find("Face Art") != null)
            throw new InvalidOperationException("Prepare the original Head Art once, before constructing study controls.");
        if (original == null || original.name != "Head" || original.packed ||
            original.rect.width != 243 || original.rect.height != 230 || head.drawMode != SpriteDrawMode.Simple)
            throw new InvalidOperationException("The authored cut requires the original unpacked 243x230 Head sprite.");
        var skin = head.GetComponent<SpriteSkin>();
        if (skin != null && (skin.boneTransforms.Length != 1 || skin.boneTransforms[0] != head.transform.parent))
            throw new InvalidOperationException("Only the rigid single-bone head can be separated without rebaking skin weights.");

        var folder = Path.GetFullPath(Path.Combine(assetFolder, "HeadLayers"));
        var studyRoot = Path.GetFullPath("Assets/_Mythwake/ArtStudies") + Path.DirectorySeparatorChar;
        if (!folder.StartsWith(studyRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Derived head geometry must stay under Assets/_Mythwake/ArtStudies.");
        var assetsRoot = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
        folder = "Assets/" + folder.Substring(assetsRoot.Length).Replace('\\','/');
        var neckPath = folder + "/Head_Neck.asset";
        var facePath = folder + "/Head_Face.asset";
        var manifestPath = folder + "/head-layer-fingerprints.json";
        ProtectExisting(manifestPath, neckPath, facePath);
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var neck = SaveSprite(original, false, neckPath);
        var face = SaveSprite(original, true, facePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long localId);
        var manifest = new Manifest { sourceSprite = AssetDatabase.GetAssetPath(original), sourceGuid = guid, sourceLocalId = localId,
            rect = original.rect, pivotPixels = original.pivot, pixelsPerUnit = original.pixelsPerUnit, cutTopLeftPixels = Cut,
            files = new[] { neckPath, facePath }.Select(path => new Fingerprint { path = path, sha256 = Hash(path) }).ToArray() };
        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true) + Environment.NewLine);
        // Verify the saved native asset rather than trusting the in-memory
        // geometry or a successful process exit. OverrideGeometry can log an
        // Editor error without throwing and leave a full rectangle behind.
        Resources.UnloadAsset(neck); Resources.UnloadAsset(face);
        neck = AssetDatabase.LoadAssetAtPath<Sprite>(neckPath);
        face = AssetDatabase.LoadAssetAtPath<Sprite>(facePath);
        RequireGeometry(neck, original, false, "reloaded");
        RequireGeometry(face, original, true, "reloaded");
        manifest.verticesPerPart = neck.vertices.Length;
        manifest.indicesPerPart = neck.triangles.Length;
        manifest.savedGeometryAndUvsVerified = true;
        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true) + Environment.NewLine);
        AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceSynchronousImport);

        // Both rigid renderers remain directly below the same animated Head.
        // Removing its one-bone skin also avoids stale deformation buffers from
        // the original sprite being applied to the newly cut vertex geometry.
        if (skin != null) UnityEngine.Object.DestroyImmediate(skin);
        var faceObject = new GameObject("Face Art", typeof(SpriteRenderer));
        faceObject.layer = head.gameObject.layer;
        faceObject.transform.SetParent(head.transform.parent, false);
        faceObject.transform.localPosition = head.transform.localPosition;
        faceObject.transform.localRotation = head.transform.localRotation;
        faceObject.transform.localScale = head.transform.localScale;
        var faceRenderer = faceObject.GetComponent<SpriteRenderer>();
        faceRenderer.sharedMaterials = head.sharedMaterials;
        faceRenderer.color = head.color;
        faceRenderer.flipX = head.flipX; faceRenderer.flipY = head.flipY;
        faceRenderer.maskInteraction = head.maskInteraction;
        faceRenderer.spriteSortPoint = head.spriteSortPoint;
        faceRenderer.sortingLayerID = head.sortingLayerID;
        faceRenderer.sortingOrder = 60;
        faceRenderer.enabled = head.enabled;
        faceRenderer.sprite = face;
        head.sprite = neck; // Original drawing keeps its existing neck depth (29).
        rig.bodySockets = rig.bodySockets.Select(socket => new KaelRig.BodySocket {
            drawing = socket.drawing, childBone = socket.childBone,
            views = socket.views.Select(view => new KaelRig.DrawingSocket {
                sprite = view.sprite == original ? neck : view.sprite, position = view.position }).ToArray()
        }).ToArray();
    }

    static Sprite SaveSprite(Sprite source, bool upper, string path)
    {
        var rect = source.rect;
        var pivot = new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height);
        var sprite = Sprite.Create(source.texture, rect, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border, false);
        sprite.name = upper ? "Head_Face" : "Head_Neck";
        try
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) EditorUtility.CopySerialized(sprite, existing);
            var target = existing != null ? existing : sprite;
            Geometry(source, upper, out var vertices, out var uvs, out var triangles);
            // Use the same mesh-data API as the installed Unity 2D importer.
            // Unlike runtime OverrideGeometry, these setters work during editor
            // asset production; positions use local units and UVs use the atlas.
            using (var positions = new NativeArray<Vector3>(vertices, Allocator.Temp))
            using (var textureCoordinates = new NativeArray<Vector2>(uvs, Allocator.Temp))
            using (var indices = new NativeArray<ushort>(triangles, Allocator.Temp))
            {
                target.SetVertexCount(vertices.Length);
                target.SetVertexAttribute(VertexAttribute.Position, positions);
                target.SetVertexAttribute(VertexAttribute.TexCoord0, textureCoordinates);
                target.SetIndices(indices);
            }
            RequireGeometry(target, source, upper, "before-save");
            if (existing == null) AssetDatabase.CreateAsset(target, path);
            EditorUtility.SetDirty(target);
            return target;
        }
        finally
        {
            if (!EditorUtility.IsPersistent(sprite)) UnityEngine.Object.DestroyImmediate(sprite);
        }
    }

    static void Geometry(Sprite source, bool upper, out Vector3[] positions, out Vector2[] uvs, out ushort[] indices)
    {
        var rect = source.rect;
        var vertices = new List<Vector3>(); var textureCoordinates = new List<Vector2>(); var triangles = new List<ushort>();
        for (var strip = 0; strip < Cut.Length - 1; strip++)
        {
            var left = Cut[strip]; var right = Cut[strip + 1];
            // Both halves and adjacent strips share identical cut endpoints.
            var topLeft = upper ? new Vector2(left.x, 0) : left;
            var topRight = upper ? new Vector2(right.x, 0) : right;
            var bottomLeft = upper ? left : new Vector2(left.x, rect.height);
            var bottomRight = upper ? right : new Vector2(right.x, rect.height);
            var first = vertices.Count;
            foreach (var pixel in new[] { topLeft, topRight, bottomRight, bottomLeft })
            {
                var bottomOrigin = new Vector2(pixel.x, rect.height - pixel.y);
                vertices.Add((bottomOrigin - source.pivot) / source.pixelsPerUnit);
                textureCoordinates.Add(new Vector2((rect.x + bottomOrigin.x) / source.texture.width,
                    (rect.y + bottomOrigin.y) / source.texture.height));
            }
            foreach (var offset in new[] { 0, 1, 2, 0, 2, 3 }) triangles.Add((ushort)(first + offset));
        }
        positions = vertices.ToArray(); uvs = textureCoordinates.ToArray(); indices = triangles.ToArray();
    }

    static void RequireGeometry(Sprite sprite, Sprite source, bool upper, string stage)
    {
        if (sprite == null) throw new InvalidOperationException("Head geometry is missing after " + stage + ".");
        Geometry(source, upper, out var positions, out var expectedUvs, out var indices);
        var vertices = sprite.vertices; var uvs = sprite.uv; var triangles = sprite.triangles;
        if (sprite.texture != source.texture || sprite.rect != source.rect || sprite.pixelsPerUnit != source.pixelsPerUnit ||
            Vector2.Distance(sprite.pivot, source.pivot) > .0001f || vertices.Length != positions.Length ||
            uvs.Length != expectedUvs.Length || triangles.Length != indices.Length)
            throw new InvalidOperationException("Head geometry/atlas metadata differs after " + stage + ": " + sprite.name);
        for (var i = 0; i < vertices.Length; i++)
            if (Vector3.Distance(vertices[i], positions[i]) > .000001f || Vector2.Distance(uvs[i], expectedUvs[i]) > .000001f)
                throw new InvalidOperationException("Head vertex/UV differs after " + stage + ": " + sprite.name + " vertex " + i);
        for (var i = 0; i < triangles.Length; i++)
            if (triangles[i] != indices[i]) throw new InvalidOperationException("Head topology differs after " + stage + ": " + sprite.name);
    }

    static void ProtectExisting(string manifestPath, params string[] paths)
    {
        var prior = File.Exists(manifestPath) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath)) : null;
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            var known = prior?.files?.FirstOrDefault(file => file.path == path);
            if (known == null || known.sha256 != Hash(path))
                throw new InvalidOperationException("Head layer export stopped: " + path + " has no matching generated fingerprint or was edited. Preserve the edited asset before regenerating.");
        }
    }
    static string Hash(string path)
    {
        using (var sha = SHA256.Create()) using (var input = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }
}
