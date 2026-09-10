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
    static Pose Body(Pose p,float hipY,float lean,float head,float scarf)
    {
        Move(p,"Hip",0,hipY); Turn(p,"Torso",lean); Turn(p,"Head",head);
        // Independent phase offsets keep the two coat tails and hair from moving as one sheet.
        Turn(p,"ScarfLongBend",scarf); Turn(p,"ScarfLongTip",scarf*.62f + Mathf.Sin(p.time*13.1f)*2.8f);
        Turn(p,"ScarfShortBend",scarf*.48f + Mathf.Sin(p.time*10.7f-1.1f)*4);
        Turn(p,"ScarfShortTip",scarf*.34f + Mathf.Sin(p.time*15.3f-1.8f)*3.6f);
        Turn(p,"HairBend",-scarf*.23f + Mathf.Sin(p.time*9.7f-.6f)*1.8f);
        Turn(p,"HairTip",-scarf*.15f + Mathf.Sin(p.time*12.7f-1.5f)*2.4f);
        return p;
    }
    static Pose View(Pose p, string view) { p.view = view; return p; }
    static Pose Strike(Pose p) { p.linearIncoming = true; p.face = "action"; return p; }
    static Pose Guard(float time) => PlantedSword(Hand(Body(P(time),1.035f,-4,3,2),-.39f,1.23f,40),0,-.38f,.39f,.65f,1.04f,-8);
    static Pose Fight(float time, float hipY, float lean, float handX, float handY, float bladeAngle, float cloth, float hipX = 0)
        => PlantedSword(Hand(Body(P(time),hipY,lean,-lean*.52f,cloth),handX,handY,bladeAngle),hipX,-.46f,.47f,.60f+hipX,hipY+.12f,-lean*.35f);
    static Pose Hand(Pose p,float x,float y,float angle)
    {
        Move(p,"NearArmTarget",x,y); Turn(p,"NearArmTarget",angle); return p;
    }
    static Pose PlantedSword(Pose p,float hipX,float nearFootX,float farFootX,float guardX,float guardY,float guardAngle)
    {
        // Feet stay on the same floor while the hips load the back leg, then drive forward.
        // The hand targets move the actual arm IK chains; the sword remains attached to its grip.
        var hip = p.positions["Hip"]; Move(p,"Hip",hipX,hip.y);
        Move(p,"NearFootTarget",nearFootX,.238f); Move(p,"FarFootTarget",farFootX,.214f);
        Move(p,"FarArmTarget",guardX,guardY); Turn(p,"FarArmTarget",guardAngle);
        return p;
    }
    static AnimationClip[] BuildClips()
    {
        var idle = new[] { Guard(0), Guard(.6f), Guard(1.2f), Guard(1.8f), Guard(1.97f), Guard(2.055f), Guard(2.4f) };
        Move(idle[1], "Hip", -.005f, 1.048f); Turn(idle[1], "Torso", -4.8f);
        Move(idle[2], "Hip", 0, 1.057f); Turn(idle[2], "Torso", -5.2f); Turn(idle[2], "Head", 3.5f);
        Move(idle[3], "Hip", .004f, 1.047f); Turn(idle[3], "Torso", -4.5f);
        // Loop endpoints use exactly the same independently authored secondary pose.
        idle[4].face = "blink";
        idle[idle.Length-1].positions = new Dictionary<string, Vector3>(idle[0].positions);
        idle[idle.Length-1].rotations = new Dictionary<string, float>(idle[0].rotations);
        var run = new List<Pose>();
        for (var i = 0; i <= 8; i++)
        {
            var phase = i * Mathf.PI / 4;
            var p = Body(P(i * .08f), 1.005f + Mathf.Abs(Mathf.Sin(phase)) * .065f, -12, 7, -15 + Mathf.Sin(phase-.5f)*8);
            Move(p,"NearFootTarget",-.17f+Mathf.Sin(phase)*.36f,.238f+Mathf.Max(0,Mathf.Cos(phase))*.24f);
            Move(p,"FarFootTarget",.17f-Mathf.Sin(phase)*.36f,.214f+Mathf.Max(0,-Mathf.Cos(phase))*.24f);
            Hand(p,-.39f-Mathf.Sin(phase)*.13f,1.28f,46+Mathf.Sin(phase)*12);
            Move(p,"FarArmTarget",.64f+Mathf.Sin(phase)*.14f,1.04f+Mathf.Sin(phase)*.07f);
            run.Add(p);
        }
        run[8].positions = new Dictionary<string, Vector3>(run[0].positions);
        run[8].rotations = new Dictionary<string, float>(run[0].rotations);

        // One basic ability, three physical choreographies. Contacts are authored once
        // and named explicitly; gameplay resolves their shared damage budget.
        var cross = new[]
        {
            Guard(0),
            Fight(.08f,.97f,13,-.67f,1.51f,140,-12,-.06f),
            Fight(.16f,.94f,19,-.50f,1.91f,132,-25,-.07f),
            Strike(Fight(.24f,.97f,-23,.57f,1.34f,-2,30,.12f)),
            Fight(.29f,.93f,-29,.65f,1.11f,-37,38,.15f),
            // Coil below the target, then reverse through a rising diagonal.
            Fight(.37f,.91f,-15,.42f,1.04f,-73,19,.08f),
            Fight(.45f,.94f,10,-.12f,1.08f,-131,-18,-.03f),
            Strike(Fight(.52f,1.04f,17,.26f,1.98f,111,-39,-.045f)),
            Fight(.59f,1.06f,10,.10f,2.09f,142,-25,-.03f),
            Fight(.70f,1.01f,-3,-.31f,1.51f,80,11),
            Guard(.84f)
        };
        var spin = new[]
        {
            Guard(0),
            Fight(.08f,.96f,9,-.62f,1.32f,95,-11,-.06f),
            View(Fight(.16f,.91f,15,-.38f,1.19f,163,-26,-.075f),"side"),
            View(Fight(.22f,.88f,10,.10f,1.15f,212,-37,-.035f),"rear"),
            View(Fight(.30f,.87f,0,.43f,1.12f,252,-42,.045f),"rear"),
            View(Strike(Fight(.36f,.88f,-15,.64f,1.12f,290,25,.12f)),"side"),
            Strike(Fight(.42f,.90f,-27,.67f,1.24f,351,43,.16f)),
            Fight(.50f,.87f,-30,.49f,1.10f,389,36,.14f),
            Fight(.64f,.98f,-13,-.06f,1.19f,415,-16,.045f),
            Guard(.84f)
        };
        // Maintain winding through the full turn; it is a multi-view body turn and
        // shoulder-driven horizontal cut, not a 360 degree spin of a flat body card.
        Turn(spin[9],"NearArmTarget",400);
        // The blade turns into the depth plane during the low coil. It retains its
        // full length at each actual contact; its tip and VFX anchors share the same
        // authored perspective scale because they are children of the Sword bone.
        cross[4].swordPerspective = .78f;
        cross[5].swordPerspective = .50f; cross[5].view = "side";
        cross[6].swordPerspective = .54f; cross[6].view = "side";
        spin[3].swordPerspective = .80f;
        spin[4].swordPerspective = spin[5].swordPerspective = .50f;
        var jump = new[]
        {
            Guard(0),
            Fight(.09f,.88f,14,-.57f,1.35f,116,-16,-.07f),
            Fight(.17f,.86f,19,-.47f,1.70f,137,-27,-.055f),
            View(Fight(.27f,1.38f,4,-.23f,2.02f,120,-40,.005f),"side"),
            Fight(.37f,1.52f,-5,.07f,2.28f,93,-34,.10f),
            Strike(Fight(.46f,1.34f,-27,.70f,1.72f,7,39,.21f)),
            Fight(.53f,1.04f,-34,.66f,1.15f,-30,46,.20f),
            Fight(.59f,.87f,-20,.43f,1.05f,-22,26,.13f),
            Fight(.69f,.97f,-11,-.05f,1.16f,15,-19,.04f),
            Guard(.84f)
        };
        Move(jump[3],"NearFootTarget",-.34f,.80f); Move(jump[3],"FarFootTarget",.40f,.57f);
        Move(jump[4],"NearFootTarget",-.25f,1.05f); Move(jump[4],"FarFootTarget",.54f,.76f);
        Move(jump[5],"NearFootTarget",-.08f,.59f); Move(jump[5],"FarFootTarget",.56f,.42f);
        Turn(jump[3],"NearFootTarget",-21); Turn(jump[4],"NearFootTarget",-24);
        Turn(jump[3],"FarFootTarget",27); Turn(jump[4],"FarFootTarget",20);
        jump[6].swordPerspective = .86f; jump[7].swordPerspective = .86f;

        // Distinct ultimate: low draw -> rear turn -> rising contact -> airborne coil
        // -> dominant descending finish. The focus clock may dwell on the low draw.
        var skill = new[]
        {
            Guard(0),
            Fight(.08f,.88f,20,-.59f,1.12f,83,-14,-.095f),
            View(Fight(.18f,.85f,23,-.35f,1.05f,164,-29,-.10f),"side"),
            View(Fight(.28f,.85f,15,.17f,1.12f,218,-38,-.065f),"rear"),
            View(Strike(Fight(.35f,.93f,-3,.58f,1.19f,282,24,.085f)),"side"),
            Strike(Fight(.40f,1.08f,-15,.62f,1.89f,65,40,.16f)),
            Fight(.47f,1.26f,-7,.21f,2.34f,115,26,.13f),
            View(Fight(.58f,1.40f,11,-.32f,2.24f,169,-26,.065f),"side"),
            View(Fight(.69f,1.46f,17,-.22f,2.16f,224,-39,.035f),"rear"),
            View(Fight(.79f,1.37f,4,.12f,2.27f,131,-45,.065f),"side"),
            Fight(.865f,1.27f,-3,.34f,2.28f,78,-30,.12f),
            Strike(Fight(.92f,.96f,-32,.75f,1.32f,-10,49,.23f)),
            Fight(.98f,.85f,-35,.67f,1.05f,-36,40,.22f),
            Fight(1.075f,.88f,-29,.55f,1.08f,-29,11,.17f),
            Fight(1.19f,1.00f,-12,-.04f,1.15f,16,-21,.055f),
            Guard(1.32f)
        };
        Move(skill[6],"NearFootTarget",-.33f,.43f); Move(skill[6],"FarFootTarget",.49f,.34f);
        Move(skill[7],"NearFootTarget",-.39f,.72f); Move(skill[7],"FarFootTarget",.42f,.58f);
        Move(skill[8],"NearFootTarget",-.28f,.95f); Move(skill[8],"FarFootTarget",.44f,.65f);
        Move(skill[9],"NearFootTarget",-.19f,.73f); Move(skill[9],"FarFootTarget",.53f,.52f);
        Move(skill[10],"NearFootTarget",-.10f,.53f); Move(skill[10],"FarFootTarget",.55f,.38f);
        Turn(skill[7],"NearFootTarget",-19); Turn(skill[8],"NearFootTarget",-26);
        Turn(skill[8],"FarFootTarget",25);
        // Consistent continuous blade winding on the early rear turn, then a reversal
        // into the rising cut. Extra key below avoids interpolation through the body.
        Turn(skill[5],"NearArmTarget",425); Turn(skill[6],"NearArmTarget",475);
        Turn(skill[7],"NearArmTarget",529); Turn(skill[8],"NearArmTarget",584);
        Turn(skill[9],"NearArmTarget",491); Turn(skill[10],"NearArmTarget",438);
        Turn(skill[11],"NearArmTarget",350); Turn(skill[12],"NearArmTarget",324);
        Turn(skill[13],"NearArmTarget",331); Turn(skill[14],"NearArmTarget",376); Turn(skill[15],"NearArmTarget",400);
        skill[1].face = skill[6].face = skill[10].face = skill[12].face = "action";
        skill[3].swordPerspective = .68f; skill[4].swordPerspective = .54f;
        skill[12].swordPerspective = skill[13].swordPerspective = .66f;

        // Saved pre-v2 server events retain their one-contact timings. These aliases
        // use the new drawing/pose language, with no invented second contact or trail.
        var legacyAttack = new[] { Guard(0), Fight(.075f,.96f,15,-.62f,1.60f,135,-20,-.06f),
            Fight(.14f,.94f,19,-.50f,1.91f,132,-25,-.07f),
            Strike(Fight(.20f,.97f,-23,.57f,1.34f,-2,30,.12f)),
            Fight(.27f,.93f,-29,.65f,1.11f,-37,38,.15f),
            Fight(.43f,1.01f,-8,-.15f,1.30f,22,-13,.035f), Guard(.60f) };
        var legacySkill = new[] { Guard(0), Fight(.08f,.88f,20,-.59f,1.12f,83,-14,-.095f),
            View(Fight(.18f,.85f,23,-.35f,1.05f,164,-29,-.10f),"side"),
            View(Fight(.28f,.85f,15,.17f,1.12f,218,-38,-.065f),"rear"),
            View(Strike(Fight(.35f,.93f,-3,.58f,1.19f,282,24,.085f)),"side"),
            Strike(Fight(.40f,1.08f,-15,.62f,1.89f,425,40,.16f)),
            Fight(.51f,1.09f,-7,.21f,2.20f,475,26,.13f),
            Fight(.70f,1.01f,-11,-.15f,1.42f,426,-19,.04f), Guard(1f) };
        Turn(legacySkill[8],"NearArmTarget",400);
        legacyAttack[4].swordPerspective = .78f;
        legacySkill[3].swordPerspective = .68f; legacySkill[4].swordPerspective = .54f;

        var hit = new[] { Guard(0), Fight(.05f,1.00f,8,-.51f,1.27f,58,-16,-.025f), Fight(.12f,1.015f,2,-.44f,1.24f,49,11), Guard(.2f) };
        var death = new[] { Guard(0), Body(P(.18f),.98f,8,-8,-14), Body(P(.4f),1,5,8,20), Body(P(.66f),1,-5,5,8), Body(P(.9f),1,-5,5,0) };
        Move(death[1],"Root",.08f,.04f); Turn(death[1],"Root",10);
        Move(death[2],"Root",.5f,.22f); Turn(death[2],"Root",45);
        Move(death[3],"Root",.98f,.48f); Turn(death[3],"Root",85);
        Move(death[4],"Root",.98f,.45f); Turn(death[4],"Root",82);
        foreach (var p in death)
        {
            p.ikWeight=0; var t=p.time/.9f;
            Turn(p,"ThighNear",t*15); Turn(p,"ShinNear",-t*20); Turn(p,"ThighFar",-t*10); Turn(p,"ShinFar",t*18);
            Turn(p,"UpperArmNear",t*15); Turn(p,"ForearmNear",-t*20); Turn(p,"Head",5*t);
        }
        return new[]
        {
            Clip("idle",idle,true), Clip("run",run.ToArray(),true),
            Clip("attack",legacyAttack,false,.20f), Clip("attack_cross",cross,false,.24f,.52f),
            Clip("attack_spin",spin,false,.42f), Clip("attack_jump",jump,false,.46f),
            Clip("skill",skill,false,.40f,.92f), Clip("skill_legacy",legacySkill,false,.40f),
            Clip("hit",hit,false), Clip("death",death,false)
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
        Smooth(curve, poses);
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
            Smooth(rotation,poses); clip.SetCurve(binding,typeof(Transform),"localEulerAnglesRaw.z",rotation);
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
    static void Smooth(AnimationCurve curve,Pose[] poses)
    {
        for(var i=0;i<curve.length;i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        }
        for(var i=1;i<poses.Length;i++)
        {
            if(!poses[i].linearIncoming) continue;
            AnimationUtility.SetKeyRightTangentMode(curve,i-1,AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
        }
    }
}
