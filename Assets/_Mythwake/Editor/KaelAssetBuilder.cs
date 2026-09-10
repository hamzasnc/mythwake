using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D.IK;

/// <summary>Reproducible original Unity-native Kael authoring pipeline. Does not read other characters.</summary>
public static class KaelAssetBuilder
{
    const string Root = "Assets/_Mythwake/Resources/Characters/Kael";
    const string Generated = Root + "/Animations/GeneratedV2";
    const string Authored = Root + "/Animations/Authored";
    const string Fingerprints = Generated + "/authoring-fingerprints.json";
    [Serializable] sealed class Layout
    {
        public float pixelsPerUnit;
        public float weaponLength = 1.75f;
        public Joint[] joints;
        public ViewJoint[] viewJoints;
        public Part[] parts;
    }
    [Serializable] sealed class Joint { public string name; public float x, y; }
    [Serializable] sealed class ViewJoint { public string view, name; public float x, y; }
    [Serializable] sealed class Landmark { public string name; public float x, y; }
    [Serializable] sealed class Part
    {
        public string name, bone, variant;
        public int x,y,width,height;
        public float pivotX,pivotY;
        public float softX, softY;
        public Landmark[] landmarks;
    }
    [Serializable] sealed class FingerprintManifest { public Fingerprint[] clips; }
    [Serializable] sealed class Fingerprint { public string path, sha256; }
    sealed class Attachment
    {
        public string name;
        public SpriteRenderer renderer;
        public Dictionary<string, Sprite> views = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        public int order;
    }
    sealed class Pose
    {
        public float time;
        public Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
        public Dictionary<string, float> rotations = new Dictionary<string, float>();
        public float ikWeight = 1;
        public bool linearIncoming;
        public string view = "front";
        public string face = "front";
        public float swordPerspective = 1;
    }
    static readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
    static readonly Dictionary<string, Vector3> restPositions = new Dictionary<string, Vector3>();
    static readonly Dictionary<string, float> restRotations = new Dictionary<string, float>();
    static readonly Dictionary<string, Vector3> canonicalControls = new Dictionary<string, Vector3>
    {
        { "Hip", new Vector3(0, 1.06f, 0) },
        { "NearArmTarget", new Vector3(-.46f, 1, 0) },
        { "FarArmTarget", new Vector3(.58f, 1.08f, 0) },
        { "NearFootTarget", new Vector3(-.34f, .238f, 0) },
        { "FarFootTarget", new Vector3(.34f, .214f, 0) }
    };
    static readonly List<Attachment> attachments = new List<Attachment>();
    static readonly HashSet<string> generatedThisBuild = new HashSet<string>(StringComparer.Ordinal);
    static Layout activeLayout;
    static GameObject root;

    [MenuItem("Mythwake/Kael/Rebuild Original Rig")]
    public static void Build()
    {
        try
        {
            ValidateGeneratedFingerprints();
            generatedThisBuild.Clear();
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory(Authored);
            AssetDatabase.Refresh();
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Root + "/KaelAtlasLayout.json"));
            ValidateLayout(layout);
            activeLayout = layout;
            ImportAtlas(layout);
            ImportPortrait("Portrait", 512);
            ImportPortrait("SkillIcon", 256);
            ImportPortrait("ActionPortrait", 2048);
            ImportPortrait("VfxAtlas", 2048);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/KaelAtlas.png").OfType<Sprite>().ToDictionary(s => s.name);
            bones.Clear(); restPositions.Clear(); restRotations.Clear(); attachments.Clear();
            root = new GameObject("Kael");
            root.AddComponent<Animator>();
            var rig = root.AddComponent<KaelRig>();
            MakeBone("Root", root.transform, 0, 0);
            WorldBone("Hip", "Root", 0, 1.06f);
            // WorldBone takes measured anatomical joints from the atlas layout.
            // The literal coordinates are the old authoring-space fallback only.
            WorldBone("Torso", "Hip", 0, 1.12f);
            WorldBone("Neck", "Torso", .06f, 1.94f);
            WorldBone("Head", "Neck", .04f, 1.99f);
            WorldBone("UpperArmFar", "Torso", .34f, 1.76f);
            WorldBone("ForearmFar", "UpperArmFar", .48f, 1.37f);
            WorldBone("HandFar", "ForearmFar", .58f, 1.08f);
            WorldBone("UpperArmNear", "Torso", -.36f, 1.75f);
            WorldBone("ForearmNear", "UpperArmNear", -.51f, 1.34f);
            WorldBone("HandNear", "ForearmNear", -.46f, 1.00f);
            rig.shoulderArmor = MakeBone("PauldronNear", bones["UpperArmNear"], 0, 0);
            WorldBone("ThighFar", "Hip", .16f, 1.01f);
            WorldBone("ShinFar", "ThighFar", .29f, .61f);
            WorldBone("FootFar", "ShinFar", .34f, .214f);
            WorldBone("ThighNear", "Hip", -.16f, 1.00f);
            WorldBone("ShinNear", "ThighNear", -.27f, .60f);
            WorldBone("FootNear", "ShinNear", -.34f, .238f);
            // Legacy bone identifiers remain stable; these are the new split coat tails.
            WorldBone("ScarfLong", "Hip", -.14f, 1.12f);
            WorldBone("ScarfShort", "Hip", .12f, 1.12f);
            WorldBone("Hair", "Head", .35f, 2.72f);
            var grip = PartLandmark("HandNear", "front", "grip", new Vector2(.02f, -.13f));
            MakeBone("Sword", bones["HandNear"], grip.x, grip.y);
            bones["Sword"].localRotation = Quaternion.Euler(0, 0, -100);
            rig.gripAnchor = MakeBone("GripAnchor", bones["Sword"], 0, 0);
            rig.weaponTip = MakeBone("WeaponTip", bones["Sword"], 0, layout.weaponLength);
            rig.vfxAnchor = MakeBone("VfxAnchor", bones["Sword"], 0, layout.weaponLength * .58f);
            var order = new Dictionary<string,int>
            {
                {"ScarfShort",0},{"ScarfLong",1},{"UpperArmFar",5},{"ForearmFar",6},{"HandFar",7},
                {"ThighFar",10},{"ShinFar",11},{"FootFar",12},{"ThighNear",20},{"ShinNear",21},{"FootNear",22},
                {"TorsoBack",27},{"Hair",28},{"Head",29},{"Torso",30},{"Pelvis",32},{"Collar",40},
                {"UpperArmNear",50},{"ForearmNear",51},{"PauldronNear",52},{"Sword",55},{"HandNear",56}
            };
            var material = SaveMaterial("KaelSprite", Shader.Find("Sprites/Default"));
            foreach (var group in layout.parts.GroupBy(PartBone))
            {
                // The collar is split around the head: TorsoBack behind it and
                // Torso in front. Both drawings use the same torso transform.
                // The old standalone collar remains source-only.
                if (group.Key == "Collar") continue;
                var part = group.FirstOrDefault(p => ViewName(p) == "front");
                if (part == null) throw new InvalidOperationException("Kael v2 needs a front attachment for " + group.Key);
                var boneName = group.Key == "Pelvis" ? "Hip" : group.Key == "TorsoBack" ? "Torso" : group.Key;
                var attachment = bones[boneName];
                var piece = new GameObject(group.Key + " Art", typeof(SpriteRenderer));
                piece.transform.SetParent(attachment, false);
                var renderer = piece.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites[part.name];
                renderer.sharedMaterial = material;
                renderer.sortingOrder = order[group.Key];
                attachments.Add(new Attachment { name = group.Key, renderer = renderer, order = order[group.Key],
                    views = group.ToDictionary(ViewName, p => sprites[p.name], StringComparer.Ordinal) });
                var transforms = new List<Transform> { attachment };
                if (Soft(part))
                {
                    var step = SoftStep(part, layout.pixelsPerUnit);
                    var bend = MakeBone(group.Key + "Bend", attachment, step.x, step.y);
                    var tip = MakeBone(group.Key + "Tip", bend, step.x, step.y);
                    transforms.Add(bend); transforms.Add(tip);
                }
                var skin = piece.AddComponent<SpriteSkin>();
                skin.autoRebind = false;
                skin.SetRootBone(attachment);
                skin.SetBoneTransforms(transforms.ToArray());
                skin.alwaysUpdate = true;
            }
            BindBodySockets(rig, layout, sprites);
            MakeBone("Targets", root.transform, 0, 0);
            MakeBone("IK", root.transform, 0, 0);
            rig.ik = root.AddComponent<IKManager2D>();
            rig.ik.weight = 1;
            rig.ik.alwaysUpdate = true;
            AddIK(rig.ik, "NearArm", "HandNear", true);
            AddIK(rig.ik, "FarArm", "HandFar", false);
            AddIK(rig.ik, "NearFoot", "FootNear", true);
            AddIK(rig.ik, "FarFoot", "FootFar", false);
            foreach (var bone in bones)
            {
                restPositions[bone.Key] = bone.Value.localPosition;
                restRotations[bone.Key] = Mathf.DeltaAngle(0, bone.Value.localEulerAngles.z);
            }
            rig.clips = BuildClips();
            // Death handover uses the actual exported target curves to select a
            // continuous weapon route independently of the first rendered delta.
            var deathClip = rig.clips.Single(clip => clip.name == "death");
            var handPath = AnimationUtility.CalculateTransformPath(bones["NearArmTarget"], root.transform);
            rig.deathWristAngle = AnimationUtility.GetEditorCurve(deathClip,
                EditorCurveBinding.FloatCurve(handPath, typeof(Transform), "localEulerAnglesRaw.z"));
            rig.deathHandHeight = AnimationUtility.GetEditorCurve(deathClip,
                EditorCurveBinding.FloatCurve(handPath, typeof(Transform), "m_LocalPosition.y"));
            rig.deathHandToTip = bones["HandNear"].InverseTransformPoint(rig.weaponTip.position);
            if (rig.deathWristAngle == null || rig.deathHandHeight == null)
                throw new InvalidOperationException("Kael death requires authored hand target rotation and height curves.");
            var trailObject = new GameObject("WeaponTrail",typeof(LineRenderer));
            trailObject.transform.SetParent(root.transform,false);
            rig.weaponTrail = trailObject.GetComponent<LineRenderer>();
            rig.weaponTrail.useWorldSpace = false;
            rig.weaponTrail.positionCount = 0;
            rig.weaponTrail.widthMultiplier = .085f;
            rig.weaponTrail.numCornerVertices = 3;
            rig.weaponTrail.numCapVertices = 3;
            rig.weaponTrail.sortingOrder = 60;
            rig.weaponTrail.sharedMaterial = material;
            rig.weaponTrail.startColor = new Color(1,.6f,.1f,0);
            rig.weaponTrail.endColor = new Color(1,.94f,.66f,.9f);
            var controllerPath = Root + "/Kael.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            foreach (var clip in rig.clips)
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                state.writeDefaultValues = false;
                if (clip.name == "idle") machine.defaultState = state;
            }
            root.GetComponent<Animator>().runtimeAnimatorController = controller;
            SaveMaterial("KaelCanvas", Shader.Find("Mythwake/Kael Canvas Premultiplied"));
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Kael.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            WriteGeneratedFingerprints();
            AssetDatabase.Refresh();
            Debug.Log("KAEL_ASSETS_OK: original v2 atlas with " + layout.parts.Length + " views, weighted hair/coat tails, 4 limb IK constraints, 3 basic choreographies, two-contact ultimate, preserved authoring overrides.");
        }
        catch (Exception ex)
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    static Material SaveMaterial(string name, Shader shader)
    {
        if (shader == null) throw new InvalidOperationException("Kael shader missing: " + name);
        var path = Root + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        else material.shader = shader;
        EditorUtility.SetDirty(material);
        return material;
    }
    static void ImportPortrait(string name, int max)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/" + name + ".png");
        if (importer == null) throw new InvalidOperationException("Kael v2 source texture is missing: " + name + ".png. Run both V2 preparation scripts first.");
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = max;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.ClearPlatformTextureSettings("Android");
        importer.SaveAndReimport();
    }
    static string PartBone(Part p)
    {
        var value = string.IsNullOrEmpty(p.bone) ? p.name.Split('_')[0] : p.bone;
        return value == "Hip" ? "Pelvis" : value == "Neck" ? "Collar" : value;
    }
    static string ViewName(Part p) => string.IsNullOrEmpty(p.variant) ? "front" : p.variant.ToLowerInvariant();
    static Vector2 PartLandmark(string bone, string view, string name, Vector2 fallback)
    {
        var part = Array.Find(activeLayout.parts, value => PartBone(value) == bone && ViewName(value) == view);
        var point = part?.landmarks == null ? null : Array.Find(part.landmarks, value => value.name == name);
        return point == null ? fallback : new Vector2(point.x, point.y);
    }
    static bool Soft(Part p) => PartBone(p).StartsWith("Scarf", StringComparison.Ordinal) || PartBone(p) == "Hair";
    static Vector2 SoftStep(Part p, float ppu)
    {
        if (Mathf.Abs(p.softX) + Mathf.Abs(p.softY) > .001f) return new Vector2(p.softX, p.softY) / ppu;
        return new Vector2(0, -p.height / ppu * .42f);
    }
    static void ImportAtlas(Layout layout)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/KaelAtlas.png");
        if (importer == null) throw new InvalidOperationException("Kael v2 atlas is missing. Run V2/prepare_assets.py first.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = layout.pixelsPerUnit;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.ClearPlatformTextureSettings("Android");
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = layout.parts.Select(p => new SpriteRect
        {
            name = p.name, rect = new Rect(p.x,p.y,p.width,p.height), pivot = new Vector2(p.pivotX,p.pivotY),
            alignment = SpriteAlignment.Custom, spriteID = previous.TryGetValue(p.name, out var id) ? id : GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
        var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
        for (var n = 0; n < layout.parts.Length; n++)
        {
            var p = layout.parts[n]; var id = rects[n].spriteID;
            var soft = Soft(p); var step = SoftStep(p, 1); var boneName = PartBone(p);
            if (boneName == "TorsoBack") boneName = "Torso";
            var spriteBones = new List<SpriteBone> { new SpriteBone { name=boneName, parentId=-1, position=new Vector3(p.width*p.pivotX,p.height*p.pivotY), rotation=Quaternion.identity, length=soft ? step.magnitude : p.height } };
            if (soft)
            {
                spriteBones.Add(new SpriteBone { name=boneName+"Bend", parentId=0,position=step,rotation=Quaternion.identity,length=step.magnitude });
                spriteBones.Add(new SpriteBone { name=boneName+"Tip", parentId=1,position=step,rotation=Quaternion.identity,length=step.magnitude*.25f });
            }
            boneProvider.SetBones(id, spriteBones);
            var cols = soft ? 7 : 1; var rows = soft ? 5 : 1;
            var vertices = new List<Vertex2DMetaData>(); var indices = new List<int>();
            for (var y = 0; y <= rows; y++) for (var x = 0; x <= cols; x++)
            {
                var position = new Vector2(p.width*x/(float)cols,p.height*y/(float)rows);
                var weight = new BoneWeight { boneIndex0=0, weight0=1 };
                if (soft)
                {
                    var pivot = new Vector2(p.width*p.pivotX,p.height*p.pivotY);
                    var distance = Vector2.Dot(position - pivot, step) / step.sqrMagnitude;
                    var scaled = Mathf.Clamp(distance,0,2); var left = Mathf.Min(1,Mathf.FloorToInt(scaled));
                    weight = new BoneWeight { boneIndex0=left,boneIndex1=left+1,weight0=1-(scaled-left),weight1=scaled-left };
                }
                vertices.Add(new Vertex2DMetaData { position=position,boneWeight=weight });
            }
            for (var y = 0; y < rows; y++) for (var x = 0; x < cols; x++)
            {
                var a=y*(cols+1)+x; var b=a+cols+1;
                indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            meshProvider.SetVertices(id,vertices.ToArray()); meshProvider.SetIndices(id,indices.ToArray());
            var edges = new List<Vector2Int>();
            for (var x=0;x<cols;x++)
            {
                edges.Add(new Vector2Int(x,x+1));
                edges.Add(new Vector2Int(rows*(cols+1)+x,rows*(cols+1)+x+1));
            }
            for(var y=0;y<rows;y++)
            {
                edges.Add(new Vector2Int(y*(cols+1),(y+1)*(cols+1)));
                edges.Add(new Vector2Int(y*(cols+1)+cols,(y+1)*(cols+1)+cols));
            }
            meshProvider.SetEdges(id,edges.ToArray());
        }
        provider.Apply(); importer.SaveAndReimport();
    }

    static void BindBodySockets(KaelRig rig, Layout layout, Dictionary<string, Sprite> sprites)
    {
        var result = new List<KaelRig.BodySocket>();
        void Bind(string drawing, string landmark, string child)
        {
            var renderer = attachments.Single(attachment => attachment.name == drawing).renderer;
            var views = layout.parts.Where(part => PartBone(part) == drawing).Select(part =>
            {
                var point = part.landmarks?.SingleOrDefault(value => value.name == landmark);
                if (point == null) throw new InvalidOperationException("Kael missing anatomical " + landmark + " on " + part.name);
                return new KaelRig.DrawingSocket { sprite = sprites[part.name], position = new Vector2(point.x,point.y) };
            }).ToArray();
            result.Add(new KaelRig.BodySocket { drawing = renderer, childBone = bones[child], views = views });
        }
        // Parent-before-child order is essential: move each socket after its parent
        // has been registered, then let IK solve the now coherent limb lengths.
        Bind("Torso","neck","Neck");
        foreach (var side in new[] { "Near", "Far" })
        {
            Bind("Torso","shoulder"+side,"UpperArm"+side);
            Bind("UpperArm"+side,"end","Forearm"+side);
            Bind("Forearm"+side,"end","Hand"+side);
            Bind("Pelvis","hip"+side,"Thigh"+side);
            Bind("Thigh"+side,"end","Shin"+side);
            Bind("Shin"+side,"end","Foot"+side);
        }
        Bind("Head","hair","Hair");
        Bind("HandNear","grip","Sword");
        rig.bodySockets = result.ToArray();
    }

    static void ValidateLayout(Layout layout)
    {
        if (layout == null || layout.pixelsPerUnit <= 0 || layout.weaponLength <= 0 || layout.parts == null)
            throw new InvalidOperationException("Kael v2 layout requires positive pixelsPerUnit/weaponLength and its authored parts.");
        var required = new[] { "Head", "Torso", "TorsoBack", "Pelvis", "Collar", "Hair", "ScarfLong", "ScarfShort", "Sword", "PauldronNear",
            "UpperArmNear", "ForearmNear", "HandNear", "UpperArmFar", "ForearmFar", "HandFar",
            "ThighNear", "ShinNear", "FootNear", "ThighFar", "ShinFar", "FootFar" };
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in layout.parts)
        {
            if (string.IsNullOrEmpty(part.name) || !names.Add(part.name) || part.width <= 0 || part.height <= 0 || part.x < 0 || part.y < 0)
                throw new InvalidOperationException("Kael v2 has an invalid or duplicate sprite rectangle.");
            if (!required.Contains(PartBone(part))) throw new InvalidOperationException("Kael v2 unknown attachment group: " + PartBone(part));
            if (part.pivotX < 0 || part.pivotX > 1 || part.pivotY < 0 || part.pivotY > 1)
                throw new InvalidOperationException("Kael v2 sprite pivot must be normalized inside its padded rectangle: " + part.name);
        }
        foreach (var requiredPart in required)
        {
            var parts = layout.parts.Where(part => PartBone(part) == requiredPart).ToArray();
            if (parts.Length == 0 || !parts.Any(part => ViewName(part) == "front") || parts.Select(ViewName).Distinct().Count() != parts.Length)
                throw new InvalidOperationException("Kael v2 needs a unique front drawing and unique view names for " + requiredPart);
            var lowerBody = requiredPart.StartsWith("Thigh",StringComparison.Ordinal) ||
                requiredPart.StartsWith("Shin",StringComparison.Ordinal) || requiredPart.StartsWith("Foot",StringComparison.Ordinal);
            var requiredViews = requiredPart == "Head" ? new[] { "rear", "side", "action", "blink" } :
                requiredPart == "Torso" || requiredPart == "TorsoBack" || requiredPart == "Pelvis" ? new[] { "rear", "side" } :
                lowerBody ? new[] { "rear" } : Array.Empty<string>();
            foreach (var neededView in requiredViews)
                if (!parts.Any(part => ViewName(part) == neededView)) throw new InvalidOperationException("Kael v2 missing " + neededView + " drawing for " + requiredPart);
            if (Soft(parts[0]))
            {
                var step = SoftStep(parts[0], layout.pixelsPerUnit);
                if (parts.Any(part => Vector2.Distance(step,SoftStep(part,layout.pixelsPerUnit)) > .005f))
                    throw new InvalidOperationException("Kael v2 soft exchange drawings must share softX/softY bind lengths: " + requiredPart);
            }
        }
    }
    static Transform MakeBone(string name, Transform parent, float x, float y)
    {
        var bone = new GameObject(name).transform;
        bone.SetParent(parent, false); bone.localPosition = new Vector3(x,y,0); bones[name]=bone;
        return bone;
    }
    static Transform WorldBone(string name,string parent,float x,float y)
    {
        var authored = activeLayout.joints == null ? null : Array.Find(activeLayout.joints, joint => joint.name == name);
        if (authored != null) { x = authored.x; y = authored.y; }
        var local = bones[parent].InverseTransformPoint(new Vector3(x,y,0));
        return MakeBone(name,bones[parent],local.x,local.y);
    }
    static void AddIK(IKManager2D manager,string name,string effector,bool flip)
    {
        var target = MakeBone(name+"Target",bones["Targets"],bones[effector].position.x,bones[effector].position.y);
        var solverObject = new GameObject(name); solverObject.transform.SetParent(bones["IK"],false);
        var solver = solverObject.AddComponent<LimbSolver2D>();
        solver.GetChain(0).effector = bones[effector]; solver.GetChain(0).target = target;
        solver.flip = flip; solver.constrainRotation = true; solver.solveFromDefaultPose = true; solver.weight = 1;
        solver.Initialize(); manager.AddSolver(solver);
    }
    static Pose P(float time) => new Pose { time=time };
    static Pose Move(Pose p,string bone,float x,float y) { p.positions[bone]=new Vector3(x,y,0); return p; }
    static Pose Turn(Pose p,string bone,float z) { p.rotations[bone]=z; return p; }
    static Pose Body(Pose p,float hipY,float lean,float head,float cloth)
    {
        Move(p,"Hip",0,hipY); Turn(p,"Hip",Mathf.Clamp(lean*.25f,-8,6));
        Turn(p,"Torso",lean); Turn(p,"Head",head);
        // Authored drag impulses are baked into damped secondary curves below.
        // No time-based decorative oscillation is added to combat poses.
        Turn(p,"ScarfLongBend",cloth); Turn(p,"ScarfLongTip",cloth*.60f);
        Turn(p,"ScarfShortBend",cloth*.48f); Turn(p,"ScarfShortTip",cloth*.32f);
        Turn(p,"HairBend",-cloth*.12f); Turn(p,"HairTip",-cloth*.08f);
        return p;
    }
    static Pose View(Pose p,string view) { p.view=view; return p; }
    static Pose Strike(Pose p) { p.linearIncoming=true; p.face="action"; return p; }
    static Pose Feet(Pose p,float nearX,float nearY,float farX,float farY,float nearAngle=0,float farAngle=0)
    {
        Move(p,"NearFootTarget",nearX,nearY); Move(p,"FarFootTarget",farX,farY);
        Turn(p,"NearFootTarget",nearAngle); Turn(p,"FarFootTarget",farAngle); return p;
    }
    // Hand positions are authored relative to the shoulder in rig space, with an
    // intentional elbow bend. The earlier world targets often exceeded the real
    // arm's reach, flattening different key poses into the same locked arm.
    static Pose Reach(Pose p,string target,string shoulder,float x,float y,float angle)
    {
        var hip=PosePosition(p,"Hip");
        var hipAngle=p.rotations.TryGetValue("Hip",out var h)?h:0;
        var torsoAngle=p.rotations.TryGetValue("Torso",out var t)?t:0;
        var shoulderOffset=bones[shoulder].position-bones["Torso"].position;
        var shoulderPosition=hip + Quaternion.Euler(0,0,hipAngle)*restPositions["Torso"]
            + Quaternion.Euler(0,0,hipAngle+torsoAngle)*shoulderOffset;
        var position=shoulderPosition+new Vector3(x,y,0);
        position-=restPositions[target]-canonicalControls[target];
        Move(p,target,position.x,position.y); Turn(p,target,angle); return p;
    }
    static Pose Fight(float time,float hipY,float lean,float reachX,float reachY,float wrist,float cloth,float hipX=0)
    {
        var p=Body(P(time),hipY,lean,-lean*.42f,cloth); Move(p,"Hip",hipX,hipY);
        Feet(p,-.50f,.238f,.50f,.214f);
        Reach(p,"NearArmTarget","UpperArmNear",reachX,reachY,wrist);
        Reach(p,"FarArmTarget","UpperArmFar",-.10f-lean*.002f,-.40f,-lean*.6f);
        return p;
    }
    static Pose Guard(float time) => Fight(time,.965f,-5,.17f,-.40f,35,0,-.025f);
    static Pose CopyPose(Pose from,float time)
    {
        return new Pose {time=time,positions=new Dictionary<string,Vector3>(from.positions),
            rotations=new Dictionary<string,float>(from.rotations),view=from.view,face=from.face,
            swordPerspective=from.swordPerspective,ikWeight=from.ikWeight};
    }
    static AnimationClip[] BuildClips()
    {
        // Quiet asymmetric guard: planted ankles, breathing through the ribcage,
        // slight pelvis shift and delayed head/wrist response, no whole-body bob.
        var idle=new[] {
            Guard(0), Fight(.48f,.969f,-5.8f,.168f,-.398f,35.5f,0,-.028f),
            Fight(1.03f,.975f,-6.1f,.174f,-.391f,36.5f,0,-.020f),
            Fight(1.52f,.970f,-5.4f,.180f,-.389f,36.1f,0,-.014f),
            Fight(1.94f,.966f,-4.8f,.175f,-.398f,35.2f,0,-.020f),
            Guard(2.03f),Guard(2.11f),Guard(2.4f)
        };
        Turn(idle[2],"Head",3.0f); Turn(idle[3],"Head",3.2f);
        idle[5].face="blink";

        // A support leg travels backwards during stance; the opposite knee folds
        // on recovery. Both contacts use zero foot roll, with toe-off after release.
        var run=new List<Pose>();
        for(var i=0;i<=16;i++)
        {
            var time=i*.04f; var phase=(float)i/16;
            var p=Fight(time,.915f+Mathf.Cos(phase*Mathf.PI*4)*.025f,-11,
                .08f+Mathf.Sin(phase*Mathf.PI*2)*.04f,-.34f,39+Mathf.Sin(phase*Mathf.PI*2)*5,
                -8,-.02f);
            for(var leg=0;leg<2;leg++)
            {
                var u=Mathf.Repeat(phase+leg*.5f,1);
                var stance=u<.6f;
                var x=stance?Mathf.Lerp(.30f,-.30f,u/.6f):Mathf.Lerp(-.30f,.30f,(u-.6f)/.4f);
                var lift=stance?0:Mathf.Sin((u-.6f)/.4f*Mathf.PI)*.26f;
                Move(p,leg==0?"NearFootTarget":"FarFootTarget",(leg==0?-.22f:.22f)+x,(leg==0?.238f:.214f)+lift);
                Turn(p,leg==0?"NearFootTarget":"FarFootTarget",stance?0:-Mathf.Sin((u-.6f)/.4f*Mathf.PI)*18);
            }
            Reach(p,"FarArmTarget","UpperArmFar",-.08f-Mathf.Sin(phase*Mathf.PI*2)*.15f,-.34f,-10);
            run.Add(p);
        }
        run[16]=CopyPose(run[0],.64f);

        // Hip loads first, shoulder follows, then the wrist releases. The feet
        // keep their common guard anchors through both contacts and recovery.
        var cross=new[] {
            Guard(0),Fight(.055f,.920f,3,.11f,-.35f,61,-4,-.07f),
            Fight(.115f,.885f,14,-.12f,.09f,112,-10,-.11f),
            Fight(.178f,.900f,9,-.13f,.38f,139,-17,-.075f),
            Fight(.21f,.930f,-7,.26f,.03f,56,-8,.015f),
            Strike(Fight(.24f,.925f,-20,.49f,-.19f,-4,17,.105f)),
            Fight(.285f,.885f,-25,.46f,-.31f,-33,23,.135f),
            Fight(.34f,.875f,-15,.31f,-.35f,-46,17,.095f),
            Fight(.408f,.890f,-2,.04f,-.42f,-43,2,.005f),
            Fight(.468f,.920f,5,.09f,-.20f,12,-12,-.035f),
            Strike(Fight(.52f,.990f,8,.32f,.34f,100,-24,-.015f)),
            Fight(.568f,1.005f,3,.17f,.43f,122,-18,.01f),
            Fight(.642f,.963f,-8,.10f,.09f,79,10,.035f),
            Fight(.728f,.943f,-7,.18f,-.31f,40,5,-.01f),Guard(.84f)
        };
        // Low second-cut chamber stays in the same drawing. The previous side
        // swap here changed the entire silhouette without a motivated body turn.
        cross[7].swordPerspective=.72f; cross[8].swordPerspective=.72f;

        var spin=new[] {
            Guard(0),Fight(.065f,.915f,3,.08f,-.38f,65,-5,-.075f),
            Fight(.13f,.860f,13,-.24f,-.25f,119,-13,-.11f),
            View(Fight(.21f,.825f,12,-.12f,-.29f,174,-22,-.075f),"side"),
            View(Fight(.28f,.840f,5,.21f,-.33f,225,-26,-.01f),"rear"),
            View(Fight(.335f,.800f,-5,.39f,-.175f,278,-12,.065f),"rear"),
            View(Fight(.375f,.825f,-14,.47f,-.115f,317,12,.12f),"side"),
            Strike(Fight(.42f,.915f,-23,.51f,-.18f,355,26,.16f)),
            Fight(.475f,.865f,-27,.42f,-.33f,385,22,.17f),
            Fight(.555f,.870f,-19,.20f,-.38f,409,5,.105f),
            Fight(.66f,.930f,-9,.10f,-.36f,409,-10,.015f),
            Fight(.75f,.953f,-6,.15f,-.40f,398,-3,-.015f),Guard(.84f)
        };
        Turn(spin[12],"NearArmTarget",395);
        // The turning leg clears the ground before relocating; the supporting
        // leg holds the floor. It plants before the .42 s cut, then steps home.
        Feet(spin[2],-.50f,.238f,.50f,.244f,0,-6);
        Feet(spin[3],-.50f,.238f,.34f,.37f,0,-18);
        Feet(spin[4],-.50f,.238f,.43f,.39f,0,-12);
        Feet(spin[5],-.50f,.238f,.58f,.29f);
        Feet(spin[6],-.50f,.238f,.62f,.214f);
        Feet(spin[7],-.50f,.238f,.62f,.214f);
        Feet(spin[8],-.50f,.238f,.62f,.214f);
        Feet(spin[9],-.50f,.238f,.60f,.32f,0,-9);
        Feet(spin[10],-.50f,.238f,.53f,.29f);
        spin[4].swordPerspective=.68f; spin[5].swordPerspective=.68f;

        var jump=new[] {
            Guard(0),Fight(.065f,.875f,3,.08f,-.33f,65,-5,-.06f),
            Fight(.135f,.805f,13,-.16f,.13f,121,-12,-.07f),
            Fight(.195f,.960f,4,-.06f,.38f,132,-23,-.04f),
            Fight(.27f,1.235f,-1,.02f,.41f,117,-25,.015f),
            Fight(.355f,1.325f,-6,.13f,.36f,104,-12,.075f),
            Fight(.405f,1.285f,-12,.27f,.22f,74,0,.13f),
            Strike(Fight(.46f,1.135f,-25,.47f,-.17f,1,22,.19f)),
            Fight(.515f,.905f,-27,.44f,-.28f,-20,27,.19f),
            Fight(.58f,.790f,-17,.29f,-.36f,-20,16,.14f),
            Fight(.668f,.865f,-9,.13f,-.39f,16,-9,.06f),
            Fight(.754f,.945f,-6,.15f,-.40f,33,-4,-.01f),Guard(.84f)
        };
        Feet(jump[3],-.46f,.34f,.48f,.29f,-14,-10);
        Feet(jump[4],-.49f,.90f,.35f,.68f,-26,18);
        Feet(jump[5],-.37f,1.04f,.47f,.89f,-22,22);
        Feet(jump[6],-.30f,.90f,.51f,.64f,-14,11);
        Feet(jump[7],-.30f,.56f,.56f,.36f,-5,0);
        Feet(jump[8],-.39f,.29f,.56f,.214f);
        Feet(jump[9],-.43f,.238f,.56f,.214f);
        Feet(jump[10],-.43f,.238f,.56f,.214f);
        Feet(jump[11],-.48f,.275f,.52f,.25f);
        Turn(jump[1],"ScarfLong",-3);
        Turn(jump[2],"ScarfLong",-4);
        Turn(jump[3],"ScarfLong",-2);
        jump[8].swordPerspective=.80f; jump[9].swordPerspective=.80f;

        var skill=new[] {
            Guard(0),Fight(.07f,.87f,5,.05f,-.40f,60,-5,-.075f),
            Fight(.14f,.79f,15,-.22f,-.25f,117,-15,-.12f),
            View(Fight(.215f,.79f,13,-.14f,-.32f,173,-23,-.09f),"side"),
            View(Fight(.285f,.825f,6,.16f,-.34f,228,-28,-.01f),"rear"),
            View(Fight(.345f,.89f,-8,.40f,-.26f,295,-10,.09f),"side"),
            Strike(Fight(.40f,1.045f,-15,.38f,.23f,425,24,.15f)),
            Fight(.475f,1.24f,-6,.14f,.43f,477,13,.12f),
            View(Fight(.565f,1.33f,4,-.10f,.36f,518,-13,.075f),"side"),
            View(Fight(.66f,1.35f,9,-.19f,.25f,563,-22,.04f),"rear"),
            View(Fight(.755f,1.32f,4,-.01f,.40f,498,-20,.06f),"side"),
            Fight(.84f,1.275f,-4,.19f,.37f,455,-12,.105f),
            Fight(.885f,1.17f,-16,.36f,.02f,403,4,.16f),
            Strike(Fight(.92f,.95f,-29,.51f,-.22f,350,28,.23f)),
            Fight(.98f,.775f,-25,.41f,-.32f,335,25,.22f),
            Fight(1.06f,.795f,-17,.25f,-.39f,342,9,.17f),
            Fight(1.16f,.865f,-11,.09f,-.40f,378,-11,.07f),
            Fight(1.245f,.949f,-7,.15f,-.40f,394,-4,-.005f),Guard(1.32f)
        };
        Turn(skill[18],"NearArmTarget",395);
        Feet(skill[3],-.50f,.238f,.34f,.37f,0,-18);
        Feet(skill[4],-.50f,.238f,.48f,.34f,0,-9);
        Feet(skill[5],-.50f,.238f,.56f,.214f);
        Feet(skill[6],-.46f,.36f,.52f,.29f,-14,-5);
        Feet(skill[7],-.45f,.79f,.34f,.59f,-23,15);
        Feet(skill[8],-.48f,.97f,.32f,.78f,-26,22);
        Feet(skill[9],-.34f,1.04f,.42f,.86f,-24,19);
        Feet(skill[10],-.31f,.94f,.48f,.75f,-20,16);
        Feet(skill[11],-.24f,.83f,.53f,.60f,-15,9);
        Feet(skill[12],-.23f,.58f,.58f,.32f,-8,0);
        Feet(skill[13],-.32f,.30f,.60f,.214f);
        Feet(skill[14],-.42f,.238f,.60f,.214f);
        Feet(skill[15],-.42f,.238f,.60f,.214f);
        Feet(skill[16],-.44f,.30f,.58f,.214f,-8,0);
        Feet(skill[17],-.50f,.238f,.52f,.26f);
        // The long panel moves away from the floor while the pelvis compresses.
        Turn(skill[1],"ScarfLong",-4);
        Turn(skill[2],"ScarfLong",-6);
        Turn(skill[3],"ScarfLong",-5);
        Turn(skill[4],"ScarfLong",-2);
        skill[4].swordPerspective=.70f; skill[5].swordPerspective=.70f;
        skill[14].swordPerspective=.76f; skill[15].swordPerspective=.76f;

        var hit=new[] {Guard(0),Fight(.035f,.935f,6,.03f,-.29f,53,-9,-.065f),
            Fight(.075f,.90f,10,-.02f,-.30f,61,-5,-.085f),
            Fight(.135f,.931f,-2,.13f,-.36f,43,6,-.04f),Guard(.20f)};

        // Loss of support -> knee collapse -> attempted brace -> shoulder/head
        // follow -> floor settle. The root never tips the whole standing puppet.
        var death=new[] {
            Guard(0),Fight(.09f,.895f,8,.02f,-.26f,66,-8,-.06f),
            Fight(.205f,.68f,-3,.17f,-.31f,45,10,-.01f),
            Fight(.34f,.415f,-20,.31f,-.35f,22,17,.10f),
            Fight(.465f,.21f,-31,.37f,-.29f,9,22,.24f),
            Fight(.57f,.085f,-37,.43f,-.15f,10,16,.37f),
            Fight(.655f,.035f,-31,.48f,-.04f,10,-9,.43f),
            Fight(.735f,.065f,-28,.45f,-.05f,11,4,.44f),
            Fight(.82f,.038f,-29,.46f,-.04f,10,1,.44f),
            Fight(.90f,.038f,-29,.46f,-.04f,10,0,.44f)
        };
        var hipRoll=new[]{-1.25f,0,-8,-20,-32,-43,-49,-47,-49,-49};
        for(var i=0;i<death.Length;i++)
        {
            var p=death[i]; Turn(p,"Hip",hipRoll[i]);
            if(i==0) continue;
            // The pelvis lands on its painted lower edge; its bone origin is
            // above that edge. Keep the hands and ankles on their own floor goals.
            var settleLift=.055f*Mathf.Clamp01((i-4)/2f);
            var hip=p.positions["Hip"]; Move(p,"Hip",hip.x,hip.y+settleLift);
            Turn(p,"Head",i<4?5:Mathf.Lerp(-5,-13,(i-4)/5f));
            if(i>=5) p.face="blink";
            var nearX=i<3?-.50f:Mathf.Lerp(-.50f,-.26f,Mathf.Clamp01((i-2)/3f));
            // Once the hip rolls onto its side the unloaded rear ankle folds up.
            // Pinning both ankles to the floor here forces the far IK knee through
            // the floor as the pelvis passes below it.
            var fold=Mathf.Clamp01((i-2)/3f);
            var farX=Mathf.Lerp(.50f,.78f,fold);
            Feet(p,nearX,.238f,farX,Mathf.Lerp(.214f,.84f,fold),0,-24*fold);
            // The short coat panel folds across the floor as the pelvis rolls;
            // retaining its upright attachment angle stabbed its tip below Y=0.
            Turn(p,"ScarfShort",i==1?-8:i==2?-28:-48);
            Turn(p,"ScarfLong",i==2?-5:i==3 || i==4?-14:i==5?-7:0);
            // Recompute hand anchors after the pelvis loses tension.
            // Release the blade downwards immediately; do not lift it back into
            // the guard while the knees are already collapsing.
            Reach(p,"NearArmTarget","UpperArmNear",i==1?.20f:i==2?.24f:.30f,
                (i==1?-.42f:i==2?-.43f:i==3?-.40f:-.36f)-settleLift,i==1?20:i==2?10:6);
            Reach(p,"FarArmTarget","UpperArmFar",i<4?.22f:.33f,(i<4?-.44f:-.02f)-settleLift,i<4?-10:62);
        }
        // The final impact is held; no autonomous sine wiggle continues after death.
        death[death.Length-1]=CopyPose(death[death.Length-2],.9f);

        var legacyAttack=new[] {Guard(0),CopyPose(cross[2],.075f),CopyPose(cross[3],.14f),
            Strike(CopyPose(cross[5],.20f)),CopyPose(cross[6],.27f),CopyPose(cross[13],.43f),Guard(.60f)};
        var legacySkill=new[] {Guard(0),CopyPose(skill[2],.08f),CopyPose(skill[3],.18f),
            CopyPose(skill[4],.28f),CopyPose(skill[5],.35f),Strike(CopyPose(skill[6],.40f)),
            CopyPose(skill[7],.51f),CopyPose(skill[17],.70f),Guard(1f)};
        Feet(legacySkill[6],-.50f,.238f,.50f,.214f);
        Turn(legacySkill[8],"NearArmTarget",395);
        return new[] {
            Clip("idle",idle,true),Clip("run",run.ToArray(),true),
            Clip("attack",legacyAttack,false,.20f),Clip("attack_cross",cross,false,.24f,.52f),
            Clip("attack_spin",spin,false,.42f),Clip("attack_jump",jump,false,.46f),
            Clip("skill",skill,false,.40f,.92f),Clip("skill_legacy",legacySkill,false,.40f),
            Clip("hit",hit,false),Clip("death",death,false)
        };
    }

    static Vector3 PosePosition(Pose pose, string key)
    {
        if (!pose.positions.TryGetValue(key, out var value)) return restPositions[key];
        // Keep the choreography's offsets when art authoring changes standing proportions.
        if (canonicalControls.TryGetValue(key, out var canonical)) value += restPositions[key] - canonical;
        return value;
    }

    static float DrawingTime(Pose[] poses, int index, float frameRate)
        => index == poses.Length - 1 ? Mathf.Max(0, poses[index].time - 1f / frameRate) : poses[index].time;

    static Vector3 DrawingRestOffset(string key, string view)
    {
        if (view == "front" || activeLayout.viewJoints == null) return Vector3.zero;
        var effector = RegisteredTargetEffector(key);
        if (effector != null)
        {
            var registered = Array.Find(activeLayout.viewJoints, value => value.name == effector && value.view == view);
            return registered == null ? Vector3.zero : new Vector3(registered.x,registered.y,0)-bones[effector].position;
        }
        var joint = Array.Find(activeLayout.viewJoints, value => value.name == key && value.view == view);
        if (joint == null) return Vector3.zero;
        var parent = bones[key].parent;
        var parentJoint = Array.Find(activeLayout.viewJoints, value => value.name == parent.name && value.view == view);
        var parentPosition = parentJoint == null ? parent.position : new Vector3(parentJoint.x, parentJoint.y, 0);
        return new Vector3(joint.x, joint.y, 0) - parentPosition - restPositions[key];
    }

    static string RegisteredTargetEffector(string key)
        => key == "NearFootTarget" ? "FootNear" : key == "FarFootTarget" ? "FootFar" : key == "FarArmTarget" ? "HandFar" : null;

    static AnimationCurve PositionCurve(Pose[] poses, string key, int axis, float frameRate)
    {
        var curve = new AnimationCurve(poses.Select(p => new Keyframe(p.time, PosePosition(p, key)[axis])).ToArray());
        Smooth(curve, poses, key);
        // A planted ankle is a floor anchor, not a socket on the torso drawing.
        // Mirroring it when the pelvis swaps to its rear drawing teleported the
        // supporting foot by .71 world units. Horizontal steps are authored by
        // Feet(); only the drawing-specific sole height needs Y registration.
        if(axis==0 && (key=="NearFootTarget" || key=="FarFootTarget")) return curve;
        var registration = RegisteredTargetEffector(key) ?? key;
        if (activeLayout.viewJoints == null || !activeLayout.viewJoints.Any(value => value.name == registration)) return curve;

        // Perspective drawings have their own anatomical sockets. Switch these
        // registrations on exactly the same frame as the drawing, while preserving
        // the existing action-clock position curve between switches. Interpolating
        // front shoulder sockets into a rear drawing puts the arm on the wrong side.
        var times = new SortedSet<float>(poses.Select(p => p.time));
        for (var frame = 0; frame <= Mathf.CeilToInt(poses.Last().time * frameRate); frame++)
            times.Add(Mathf.Min(poses.Last().time, frame / frameRate));
        for (var i = 1; i < poses.Length; i++)
        {
            if (poses[i].view == poses[i - 1].view) continue;
            var time = DrawingTime(poses, i, frameRate);
            times.Add(Mathf.Max(0, time - .00001f)); times.Add(time);
        }
        var registered = new AnimationCurve(times.Select(time =>
        {
            var view = poses[0].view;
            for (var i = 1; i < poses.Length && DrawingTime(poses, i, frameRate) <= time; i++) view = poses[i].view;
            var value = curve.Evaluate(time);
            var position = value + DrawingRestOffset(key, view)[axis];
            // A turned anatomical target mirrors its displacement from the rest
            // socket as well as moving the socket itself. Translation alone puts
            // both feet under the centre of the rear-view pelvis.
            if (axis == 0 && view == "rear" && RegisteredTargetEffector(key) != null)
                position -= 2 * (value - restPositions[key].x);
            return new Keyframe(time, position);
        }).ToArray());
        for (var i = 0; i < registered.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(registered, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(registered, i, AnimationUtility.TangentMode.Linear);
        }
        return registered;
    }

    static AnimationClip Clip(string name,Pose[] poses,bool loop,params float[] impacts)
    {
        var handAuthored = AssetDatabase.LoadAssetAtPath<AnimationClip>(Authored + "/" + name + ".anim");
        if (handAuthored != null)
        {
            if (handAuthored.name != name) throw new InvalidOperationException("Kael authored override must be named " + name);
            Debug.Log("KAEL_AUTHORED_PRESERVED: " + AssetDatabase.GetAssetPath(handAuthored));
            return handAuthored;
        }
        var path=Generated+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null) {clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
        generatedThisBuild.Add(path);
        clip.ClearCurves(); clip.name=name; clip.frameRate=60;
        foreach(var item in bones)
        {
            var key=item.Key; var binding=AnimationUtility.CalculateTransformPath(item.Value,root.transform);
            for(var axis=0;axis<3;axis++)
            {
                clip.SetCurve(binding,typeof(Transform),"m_LocalPosition."+"xyz"[axis],PositionCurve(poses,key,axis,clip.frameRate));
            }
            var rotation=new AnimationCurve(poses.Select(p=>new Keyframe(p.time,p.rotations.TryGetValue(key,out var value)?value:restRotations[key])).ToArray());
            Smooth(rotation,poses,key); clip.SetCurve(binding,typeof(Transform),"localEulerAnglesRaw.z",rotation);
            if (key == "Sword")
            {
                var perspective = new AnimationCurve(poses.Select(p => new Keyframe(p.time,p.swordPerspective)).ToArray());
                Smooth(perspective,poses);
                clip.SetCurve(binding,typeof(Transform),"m_LocalScale.y",perspective);
            }
        }
        foreach(var solver in root.GetComponentsInChildren<LimbSolver2D>())
        {
            clip.SetCurve(AnimationUtility.CalculateTransformPath(solver.transform,root.transform),typeof(LimbSolver2D),"m_Weight",new AnimationCurve(poses.Select(p=>new Keyframe(p.time,p.ikWeight)).ToArray()));
            if (solver.name.EndsWith("Foot",StringComparison.Ordinal))
            {
                var pole = poses.Select((p,i) => new Keyframe(DrawingTime(poses,i,clip.frameRate),
                    (p.view == "rear" ? !solver.flip : solver.flip) ? 1 : 0,
                    float.PositiveInfinity,float.PositiveInfinity)).ToArray();
                clip.SetCurve(AnimationUtility.CalculateTransformPath(solver.transform,root.transform),typeof(LimbSolver2D),"m_Flip",new AnimationCurve(pole));
            }
        }
        foreach (var attachment in attachments)
        {
            var binding = AnimationUtility.CalculateTransformPath(attachment.renderer.transform, root.transform);
            var swaps = new ObjectReferenceKeyframe[poses.Length];
            var sort = new Keyframe[poses.Length];
            for (var i = 0; i < poses.Length; i++)
            {
                var view = poses[i].view;
                if (attachment.name == "Head" && view == "front" && poses[i].face != "front") view = poses[i].face;
                // Only perspective-neutral attachments may reuse front art. Torso,
                // head, pelvis and rear legs are required by layout validation.
                if (!attachment.views.TryGetValue(view, out var sprite)) sprite = attachment.views["front"];
                // Unity's discrete PPtr tracks hold their last sample for one frame.
                // Put that final sample one frame before the float-curve endpoint so
                // sprite exchange keys do not extend the authoritative action duration.
                var swapTime = DrawingTime(poses,i,clip.frameRate);
                swaps[i] = new ObjectReferenceKeyframe { time = swapTime, value = sprite };
                var order = attachment.order;
                if (view == "rear")
                {
                    // Swap limb depth within its own anatomical group. The former
                    // +/-45 rule brought a rear leg in front of the waist and coat.
                    if (attachment.name.StartsWith("Thigh", StringComparison.Ordinal) ||
                        attachment.name.StartsWith("Shin", StringComparison.Ordinal) ||
                        attachment.name.StartsWith("Foot", StringComparison.Ordinal))
                        order += attachment.name.EndsWith("Near", StringComparison.Ordinal) ? -10 : 10;
                    // The authored rear drawing places the armored shoulder in
                    // the foreground on the right. Cover its torso socket with
                    // that arm, rather than exposing the painted construction port.
                    if (attachment.name == "ForearmFar") order = 6;
                    if (attachment.name == "HandFar") order = 7;
                    if (attachment.name == "Sword") order = 4;
                    if (attachment.name.StartsWith("Scarf", StringComparison.Ordinal)) order = 31;
                }
                else
                {
                    // The free hand crosses the chest in front/side guard poses.
                    // Keep it above torso/belt so an intact arm cannot read as an
                    // empty cuff. The rear view retains its original 51/52 orders.
                    if (attachment.name == "ForearmFar") order = 43;
                    if (attachment.name == "HandFar") order = 44;
                }
                sort[i] = new Keyframe(swapTime, order, float.PositiveInfinity, float.PositiveInfinity);
            }
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve(binding, typeof(SpriteRenderer), "m_Sprite"), swaps);
            clip.SetCurve(binding, typeof(SpriteRenderer), "m_SortingOrder", new AnimationCurve(sort));
        }
        BakeSecondaryMotion(clip,poses,loop);
        var events=new List<AnimationEvent>();
        for (var i = 0; i < impacts.Length; i++)
        {
            var impact = impacts[i];
            events.Add(new AnimationEvent {time=Mathf.Max(0,impact-.09f),functionName="OnKaelAnimationMarker",stringParameter="weapon_trail"});
            events.Add(new AnimationEvent {time=impact,functionName="OnKaelAnimationMarker",stringParameter="impact:"+i});
            events.Add(new AnimationEvent {time=impact+.105f,functionName="OnKaelAnimationMarker",stringParameter="weapon_trail_end"});
        }
        AnimationUtility.SetAnimationEvents(clip,events.OrderBy(marker => marker.time).ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=loop; settings.stopTime=poses.Last().time;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip); return clip;
    }

    static string Sha256(string path)
    {
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>Preflight occurs before asset mutation, so edited animation sources cannot be lost.</summary>
    public static void ValidateGeneratedFingerprints()
    {
        var known = File.Exists(Fingerprints)
            ? JsonUtility.FromJson<FingerprintManifest>(File.ReadAllText(Fingerprints)).clips
            : Array.Empty<Fingerprint>();
        var map = (known ?? Array.Empty<Fingerprint>()).ToDictionary(value => value.path, value => value.sha256, StringComparer.Ordinal);
        if (!Directory.Exists(Generated)) return;
        foreach (var path in Directory.GetFiles(Generated,"*.anim").Select(path => path.Replace('\\','/')))
        {
            if (!map.TryGetValue(path, out var expected) || expected != Sha256(path))
                throw new InvalidOperationException("Kael regeneration stopped: " + path +
                    " was edited or has no generator fingerprint. Preserve it as Animations/Authored/" + Path.GetFileName(path) +
                    " and restore the generated file from version control before rebuilding. Authored overrides are never overwritten.");
        }
    }

    static void WriteGeneratedFingerprints()
    {
        // Include existing generated clips when an authored override currently takes precedence.
        var manifest = new FingerprintManifest
        {
            clips = Directory.GetFiles(Generated,"*.anim").Select(path => path.Replace('\\','/')).OrderBy(path => path,StringComparer.Ordinal)
                .Select(path => new Fingerprint { path = path, sha256 = Sha256(path) }).ToArray()
        };
        File.WriteAllText(Fingerprints, JsonUtility.ToJson(manifest, true) + Environment.NewLine);
    }
    static void BakeSecondaryMotion(AnimationClip clip,Pose[] poses,bool loop)
    {
        var channels=new[]{"ScarfLongBend","ScarfLongTip","ScarfShortBend","ScarfShortTip","HairBend","HairTip"};
        var rates=new[]{19f,15f,24f,18f,32f,25f};
        var damping=new[]{.70f,.68f,.76f,.72f,.82f,.78f};
        var duration=poses.Last().time;
        var frames=Mathf.CeilToInt(duration*60);
        for(var channel=0;channel<channels.Length;channel++)
        {
            var key=channels[channel];
            var drive=new AnimationCurve(poses.Select(p=>new Keyframe(p.time,p.rotations[key])).ToArray());
            Smooth(drive,poses);
            var output=new List<Keyframe>();
            var angle=drive.Evaluate(0); var velocity=0f;
            // Warm a cyclic clip to its periodic response; action curves start from
            // the compatible quiet guard. This cost exists only in the exporter.
            var cycles=loop?4:1;
            for(var cycle=0;cycle<cycles;cycle++)
            {
                for(var frame=0;frame<=frames;frame++)
                {
                    var time=Mathf.Min(duration,frame/60f);
                    if(frame>0) for(var substep=0;substep<4;substep++)
                    {
                        var dt=(time-Mathf.Min(duration,(frame-1)/60f))/4;
                        var target=drive.Evaluate(time-dt*(3-substep));
                        var rate=rates[channel];
                        velocity+=((target-angle)*rate*rate-2*damping[channel]*rate*velocity)*dt;
                        angle+=velocity*dt;
                    }
                    if(cycle!=cycles-1) continue;
                    var recovery=loop?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration*.82f,duration,time));
                    output.Add(new Keyframe(time,angle*recovery));
                }
            }
            if(loop) { var end=output[output.Count-1]; end.value=output[0].value; output[output.Count-1]=end; }
            var curve=new AnimationCurve(output.ToArray());
            for(var i=0;i<curve.length;i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
            }
            clip.SetCurve(AnimationUtility.CalculateTransformPath(bones[key],root.transform),typeof(Transform),"localEulerAnglesRaw.z",curve);
        }
    }

    static void Smooth(AnimationCurve curve,Pose[] poses,string channel=null)
    {
        for(var i=0;i<curve.length;i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        }
        for(var i=1;i<poses.Length;i++)
        {
            // Fast contact spacing belongs to the weapon hand. Applying this to
            // every body channel made hips, neck and both feet change speed at once.
            if(!poses[i].linearIncoming || channel!="NearArmTarget") continue;
            AnimationUtility.SetKeyRightTangentMode(curve,i-1,AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
        }
    }
}
