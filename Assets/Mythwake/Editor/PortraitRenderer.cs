using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Mythwake.Editor
{
    /// <summary>
    /// Renders UI art for every hero from its own Spine rig (first frame of "idle"), so cards always match the
    /// in-game character: a head-and-shoulders portrait and a full-body image. Needs a graphics device (not -nographics).
    /// </summary>
    public static class PortraitRenderer
    {
        private const int Layer = 31;

        [MenuItem("Mythwake/Render Hero Portraits")]
        public static void RenderAll()
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { HeroAssetPostprocessor.HeroesRoot.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                var dir = Path.GetDirectoryName(path).Replace('\\', '/');
                var id = Path.GetFileName(dir);
                if (Render(data, $"{dir}/{id}_portrait.png", 320, 400, Framing.Portrait) &&
                    Render(data, $"{dir}/{id}_full.png", 512, 768, Framing.FullBody))
                    count++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Portraits] rendered {count}");
        }

        private enum Framing { Portrait, FullBody }

        private static bool Render(SkeletonDataAsset data, string output, int width, int height, Framing framing)
        {
            var skeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(data, true);
            var go = skeleton.gameObject;
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = Layer;
            go.transform.position = new Vector3(1000f, 1000f, 0f);
            var camGo = new GameObject("PortraitCamera") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            try
            {
                skeleton.Initialize(true);
                if (skeleton.Skeleton.Data.FindAnimation("idle") != null)
                    skeleton.AnimationState.SetAnimation(0, "idle", true);
                skeleton.Update(0f);
                skeleton.LateUpdate();

                // frame from the rendered bounds, so rigs with unusual proportions (monsters, own rigs) still fit
                var bounds = go.GetComponent<MeshRenderer>().bounds;
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.cullingMask = 1 << Layer;
                cam.targetTexture = rt;
                cam.enabled = false;
                var aspect = (float)width / height;
                if (framing == Framing.FullBody)
                {
                    var size = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * 1.06f;
                    cam.orthographicSize = size;
                    cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
                }
                else
                {
                    var head = skeleton.Skeleton.FindBone("head");
                    var bodyHeight = bounds.size.y;
                    var focus = head != null
                        ? head.GetWorldPosition(go.transform)
                        : new Vector3(bounds.center.x, bounds.max.y - bodyHeight * 0.18f, 0f);
                    cam.orthographicSize = Mathf.Max(0.2f, bodyHeight * 0.2f);
                    cam.transform.position = focus + new Vector3(0f, -bodyHeight * 0.04f, -10f);
                }
                cam.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = previous;

                // the Spine shader outputs premultiplied colour; PNGs are straight alpha
                var pixels = tex.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.a > 0 && p.a < 255)
                    {
                        p.r = (byte)Mathf.Min(255, p.r * 255 / p.a);
                        p.g = (byte)Mathf.Min(255, p.g * 255 / p.a);
                        p.b = (byte)Mathf.Min(255, p.b * 255 / p.a);
                        pixels[i] = p;
                    }
                }
                tex.SetPixels32(pixels);
                File.WriteAllBytes(output, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Portraits] {data.name}: {e.Message}");
                return false;
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(camGo);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
