using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Builds every object the intro video needs inside Assets/Video/IntroVideo.unity.
// Safe to run again: it removes what it created before and rebuilds it.
public static class IntroVideoSetup
{
    const string Root = "Assets/Video";
    const string ScenePath = Root + "/IntroVideo.unity";
    const string BoatModel = "Assets/Models/Props/Orca/Orca.fbx";
    const float RodLength = 2.4f;

    [MenuItem("Tools/Intro Video/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Models");
        AssetDatabase.Refresh();

        var camGo = GameObject.Find("Main Camera");
        var cabin = GameObject.Find("cabin in the forest");
        var lake = GameObject.Find("Lake");
        if (!camGo || !cabin || !lake) throw new System.InvalidOperationException("Scene is missing the camera, the cabin or the lake.");
        var door = cabin.GetComponentsInChildren<Transform>(true).First(t => t.name == "SW Door");
        var cam = camGo.GetComponent<Camera>();
        cam.nearClipPlane = 0.1f;

        foreach (string old in new[] { "IntroVideo", "Orca" })
        {
            var go = GameObject.Find(old);
            if (go) Object.DestroyImmediate(go);
        }
        var oldHand = camGo.transform.Find("RodHand");
        if (oldHand) Object.DestroyImmediate(oldHand.gameObject);

        var root = new GameObject("IntroVideo");
        var director = root.AddComponent<IntroDirector>();

        // Boat: root pivot at the stern on the waterline, +Z towards the bow
        var boat = new GameObject("Orca");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoatModel);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, boat.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * prefab.transform.localRotation;

        var meshes = BuildMeshes();
        var rodProp = BuildRod("RodProp", meshes, out _);
        rodProp.transform.SetParent(boat.transform, false);
        rodProp.transform.localPosition = new Vector3(0.38f, 1.12f, 1.35f);
        rodProp.transform.localRotation = Quaternion.Euler(-76f, 25f, 0f);

        var rodHand = BuildRod("RodHand", meshes, out Transform tip);
        rodHand.transform.SetParent(camGo.transform, false);

        var bobber = BuildBobber(meshes);
        bobber.transform.SetParent(root.transform, false);

        var lineGo = new GameObject("Line");
        lineGo.transform.SetParent(root.transform, false);
        var line = lineGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 16;
        line.widthMultiplier = 0.004f;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.sharedMaterial = Unlit("Line", new Color(0.85f, 0.88f, 0.9f, 1f), false);

        var rippleMaterial = Unlit("Ripple", new Color(1f, 1f, 1f, 0.5f), true);
        var ripples = new List<Renderer>();
        for (int i = 0; i < 3; i++)
        {
            var ring = new GameObject("Ripple" + i);
            ring.transform.SetParent(root.transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = meshes["Ring"];
            var r = ring.AddComponent<MeshRenderer>();
            r.sharedMaterial = rippleMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ripples.Add(r);
        }

        var wakeMaterial = Unlit("Foam", Color.white, true);
        wakeMaterial.SetTexture("_UnlitColorMap", FoamTexture());
        var wakeRoot = new GameObject("Wake");
        wakeRoot.transform.SetParent(root.transform, false);
        var wake = new List<Renderer>();
        var random = new System.Random(7);
        for (int i = 0; i < 38 * 3; i++)
        {
            var patch = new GameObject("Foam" + i);
            patch.transform.SetParent(wakeRoot.transform, false);
            patch.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            patch.AddComponent<MeshFilter>().sharedMesh = meshes["Patch"];
            var r = patch.AddComponent<MeshRenderer>();
            r.sharedMaterial = wakeMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.enabled = false;
            wake.Add(r);
        }

        var fadeGo = new GameObject("Fade");
        fadeGo.transform.SetParent(root.transform, false);
        var fade = fadeGo.AddComponent<Volume>();
        fade.isGlobal = true;
        fade.priority = 1000f;
        fade.weight = 0f;
        fade.sharedProfile = FadeProfile();

        director.cam = cam;
        director.cabin = cabin.transform;
        director.door = door;
        director.boat = boat.transform;
        director.water = lake.GetComponent<WaterSurface>();
        director.fade = fade;
        director.rodProp = rodProp;
        director.rodHand = rodHand.transform;
        director.rodTip = tip;
        director.bobber = bobber.transform;
        director.line = line;
        director.ripples = ripples.ToArray();
        director.wake = wake.ToArray();
        director.fishingSpot = FindFishingSpot(cabin.transform, door.position, lake.transform.position.y);
        director.Evaluate(0f);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Intro video scene built. Fishing spot: " + director.fishingSpot);
    }

    // Marches from the dock into the lake until the far shore, and stops well before it.
    static Vector3 FindFishingSpot(Transform cabin, Vector3 origin, float waterLevel)
    {
        var terrain = Object.FindFirstObjectByType<Terrain>();
        float far = 30f;
        for (; far < 900f; far += 5f)
        {
            Vector3 p = origin + cabin.forward * far;
            if (!terrain.terrainData.bounds.Contains(terrain.transform.InverseTransformPoint(new Vector3(p.x, terrain.transform.position.y + 1f, p.z)))) break;
            if (terrain.SampleHeight(p) + terrain.transform.position.y > waterLevel - 1.5f) break;
        }
        Vector3 spot = origin + cabin.forward * Mathf.Min(far * 0.45f, 240f);
        spot.y = waterLevel;
        Debug.Log("Far shore at " + far + " m from the dock.");
        return spot;
    }

    // ---------- Assets

    static Material Lit(string name, Color color, float metallic, float smoothness)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        HDShaderUtils.ResetMaterialKeywords(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Unlit(string name, Color color, bool transparent)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_UnlitColor", color);
        m.SetFloat("_SurfaceType", transparent ? 1f : 0f);
        m.SetFloat("_DoubleSidedEnable", 1f);
        HDShaderUtils.ResetMaterialKeywords(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Soft-edged white blob with a broken-up outline, used for the boat's wake.
    static Texture2D FoamTexture()
    {
        string path = Root + "/Textures/Foam.png";
        Directory.CreateDirectory(Root + "/Textures");
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var pixels = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f) * 2f - 1f, v = y / (n - 1f) * 2f - 1f;
                float noise = Mathf.PerlinNoise(x * 0.07f + 3f, y * 0.07f + 9f) * 0.6f + Mathf.PerlinNoise(x * 0.19f + 21f, y * 0.19f + 5f) * 0.4f;
                float edge = 1f - Mathf.SmoothStep(0.35f, 1f, Mathf.Sqrt(u * u + v * v) + (noise - 0.5f) * 0.6f);
                pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * Mathf.Lerp(0.45f, 1f, noise)));
            }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static VolumeProfile FadeProfile()
    {
        string path = Root + "/FadeProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile) return profile;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var color = profile.Add<ColorAdjustments>(true);
        color.colorFilter.Override(Color.black);
        color.name = "ColorAdjustments";
        AssetDatabase.AddObjectToAsset(color, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static Dictionary<string, Mesh> BuildMeshes()
    {
        string path = Root + "/Models/IntroMeshes.asset";
        AssetDatabase.DeleteAsset(path);
        var blank = Frustum("Blank", 0.0075f, 0.0016f, RodLength - 0.3f, 12);
        AssetDatabase.CreateAsset(blank, path);
        var all = new Dictionary<string, Mesh> { ["Blank"] = blank };
        foreach (var m in new[] { Frustum("Cylinder", 1f, 1f, 1f, 20), Torus("Guide", 1f, 0.12f, 16, 6), Annulus("Ring", 0.86f, 1f, 48), Annulus("Patch", 0f, 0.5f, 24), Sphere("Sphere", 12, 16) })
        {
            AssetDatabase.AddObjectToAsset(m, path);
            all[m.name] = m;
        }
        AssetDatabase.SaveAssets();
        return all;
    }

    static GameObject Part(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 euler, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Rod along +Z with the butt at the origin; guides and reel hang below (-Y).
    static GameObject BuildRod(string name, Dictionary<string, Mesh> meshes, out Transform tip)
    {
        var carbon = Lit("RodCarbon", new Color(0.03f, 0.035f, 0.045f), 0.2f, 0.75f);
        var cork = Lit("RodCork", new Color(0.62f, 0.44f, 0.27f), 0f, 0.2f);
        var metal = Lit("RodMetal", new Color(0.72f, 0.73f, 0.75f), 1f, 0.8f);
        var accent = Lit("RodAccent", new Color(0.65f, 0.06f, 0.05f), 0.1f, 0.7f);
        var cyl = meshes["Cylinder"];

        var rod = new GameObject(name);
        var t = rod.transform;
        Part("ButtCap", t, cyl, carbon, Vector3.zero, Vector3.zero, new Vector3(0.017f, 0.017f, 0.02f));
        Part("Grip", t, cyl, cork, new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.015f, 0.015f, 0.16f));
        Part("ReelSeat", t, cyl, carbon, new Vector3(0f, 0f, 0.18f), Vector3.zero, new Vector3(0.012f, 0.012f, 0.10f));
        Part("ForeGrip", t, cyl, cork, new Vector3(0f, 0f, 0.28f), Vector3.zero, new Vector3(0.014f, 0.014f, 0.09f));
        Part("Winding", t, cyl, accent, new Vector3(0f, 0f, 0.37f), Vector3.zero, new Vector3(0.0085f, 0.0085f, 0.04f));
        Part("Blank", t, meshes["Blank"], carbon, new Vector3(0f, 0f, 0.3f), Vector3.zero, Vector3.one);

        // Spinning reel under the seat
        Part("ReelFoot", t, cyl, metal, new Vector3(0f, -0.012f, 0.23f), new Vector3(90f, 0f, 0f), new Vector3(0.006f, 0.006f, 0.05f));
        Part("ReelBody", t, cyl, carbon, new Vector3(-0.02f, -0.075f, 0.23f), new Vector3(0f, 90f, 0f), new Vector3(0.028f, 0.028f, 0.04f));
        Part("ReelSpool", t, cyl, metal, new Vector3(0f, -0.075f, 0.25f), Vector3.zero, new Vector3(0.024f, 0.024f, 0.045f));
        Part("ReelSpoolLip", t, cyl, accent, new Vector3(0f, -0.075f, 0.295f), Vector3.zero, new Vector3(0.027f, 0.027f, 0.006f));
        Part("ReelHandle", t, cyl, metal, new Vector3(0.02f, -0.075f, 0.23f), new Vector3(0f, 90f, 0f), new Vector3(0.004f, 0.004f, 0.05f));
        Part("ReelKnob", t, cyl, cork, new Vector3(0.07f, -0.075f, 0.215f), Vector3.zero, new Vector3(0.008f, 0.008f, 0.03f));

        float[] at = { 0.75f, 1.15f, 1.5f, 1.8f, 2.05f, 2.25f, RodLength };
        for (int i = 0; i < at.Length; i++)
        {
            float k = i / (float)(at.Length - 1);
            float radius = Mathf.Lerp(0.02f, 0.005f, k);
            float blankRadius = Mathf.Lerp(0.0075f, 0.0016f, (at[i] - 0.3f) / (RodLength - 0.3f));
            Part("Guide" + i, t, meshes["Guide"], metal, new Vector3(0f, -(blankRadius + radius), at[i]), Vector3.zero, Vector3.one * radius);
        }
        tip = new GameObject("Tip").transform;
        tip.SetParent(t, false);
        tip.localPosition = new Vector3(0f, -0.007f, RodLength);
        foreach (var r in rod.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
        return rod;
    }

    static GameObject BuildBobber(Dictionary<string, Mesh> meshes)
    {
        var red = Lit("BobberRed", new Color(0.85f, 0.07f, 0.05f), 0f, 0.8f);
        var white = Lit("BobberWhite", new Color(0.93f, 0.93f, 0.9f), 0f, 0.8f);
        var bobber = new GameObject("Bobber");
        Part("Top", bobber.transform, meshes["Sphere"], red, new Vector3(0f, 0.009f, 0f), Vector3.zero, new Vector3(0.036f, 0.036f, 0.036f));
        Part("Bottom", bobber.transform, meshes["Sphere"], white, new Vector3(0f, -0.009f, 0f), Vector3.zero, new Vector3(0.036f, 0.036f, 0.036f));
        Part("Stem", bobber.transform, meshes["Cylinder"], white, new Vector3(0f, 0.03f, 0f), new Vector3(-90f, 0f, 0f), new Vector3(0.004f, 0.004f, 0.05f));
        return bobber;
    }

    // ---------- Procedural meshes

    // Open-ended cone section along +Z, closed with caps.
    static Mesh Frustum(string name, float r0, float r1, float length, int sides)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i <= sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v.Add(dir * r0); n.Add(dir);
            v.Add(dir * r1 + Vector3.forward * length); n.Add(dir);
        }
        for (int i = 0; i < sides; i++)
        {
            int a = i * 2;
            tris.AddRange(new[] { a, a + 2, a + 1, a + 2, a + 3, a + 1 });
        }
        foreach (bool end in new[] { false, true })
        {
            int center = v.Count;
            float z = end ? length : 0f, r = end ? r1 : r0;
            Vector3 normal = end ? Vector3.forward : Vector3.back;
            v.Add(new Vector3(0f, 0f, z)); n.Add(normal);
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z)); n.Add(normal);
            }
            for (int i = 0; i < sides; i++)
                tris.AddRange(end ? new[] { center, center + 1 + i, center + 2 + i } : new[] { center, center + 2 + i, center + 1 + i });
        }
        return Finish(name, v, n, tris);
    }

    // Ring in the XY plane (the rod passes through along Z).
    static Mesh Torus(string name, float radius, float tube, int segments, int sides)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            var center = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            for (int j = 0; j <= sides; j++)
            {
                float b = j / (float)sides * Mathf.PI * 2f;
                Vector3 normal = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                v.Add(center + normal * tube); n.Add(normal);
            }
        }
        int row = sides + 1;
        for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * row + j;
                tris.AddRange(new[] { a, a + row, a + 1, a + 1, a + row, a + row + 1 });
            }
        return Finish(name, v, n, tris);
    }

    // Flat ring lying on the XZ plane, facing up.
    static Mesh Annulus(string name, float inner, float outer, int segments)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(dir * inner); n.Add(Vector3.up);
            v.Add(dir * outer); n.Add(Vector3.up);
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
        }
        var mesh = Finish(name, v, n, tris);
        mesh.SetUVs(0, v.Select(p => new Vector2(p.x / (2f * outer) + 0.5f, p.z / (2f * outer) + 0.5f)).ToList());
        return mesh;
    }

    static Mesh Sphere(string name, int rings, int segments)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float phi = i / (float)rings * Mathf.PI;
            for (int j = 0; j <= segments; j++)
            {
                float theta = j / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                v.Add(dir * 0.5f); n.Add(dir);
            }
        }
        int row = segments + 1;
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < segments; j++)
            {
                int a = i * row + j;
                tris.AddRange(new[] { a, a + 1, a + row, a + 1, a + row + 1, a + row });
            }
        return Finish(name, v, n, tris);
    }

    static Mesh Finish(string name, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
