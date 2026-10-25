using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FinishLandscape
{
    const string Root = "Assets/LandscapeFinish";
    [MenuItem("Tools/Finish Lakeside Landscape")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (scene.name != "OutdoorsScene" || terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("Open OutdoorsScene with a valid Terrain first.");
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(Root + "/PaintedTerrain.asset") != null)
            throw new InvalidOperationException("Landscape already generated; existing work preserved.");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Backup");
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene, Root + "/Backup/OutdoorsScene_Before.unity", true);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Texture lakeside landscape");
        var data = UnityEngine.Object.Instantiate(terrain.terrainData);
        data.name = "PaintedTerrain";
        AssetDatabase.CreateAsset(data, Root + "/PaintedTerrain.asset");
        Undo.RecordObject(terrain, "Assign painted terrain");
        terrain.terrainData = data;
        var collider = terrain.GetComponent<TerrainCollider>();
        if (collider) { Undo.RecordObject(collider, "Assign painted collider"); collider.terrainData = data; }
        var lake = GameObject.Find("Lake");
        float water = lake ? lake.transform.position.y : 12.4f;
        int n = data.heightmapResolution;
        var old = data.GetHeights(0, 0, n, n);
        var heights = (float[,])old.Clone();
        float baseHeight = water - terrain.transform.position.y;
        // Preserve shore and cabin elevation; soften and vary only high mountain relief.
        for (int y = 1; y < n - 1; y++) for (int x = 1; x < n - 1; x++)
        {
            float h = old[y,x] * data.size.y;
            float influence = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(baseHeight + 12, baseHeight + 65, h));
            float average = (old[y,x] * 4 + old[y-1,x] + old[y+1,x] + old[y,x-1] + old[y,x+1]) / 8;
            float variation = Mathf.Lerp(.9f, 1.10f, (float)x / (n-1));
            float target = baseHeight + (Mathf.Lerp(old[y,x], average, .5f) * data.size.y - baseHeight) * variation;
            heights[y,x] = Mathf.Clamp01(Mathf.Lerp(h, target, influence) / data.size.y);
        }
        data.SetHeights(0,0,heights);
        var trees = data.treeInstances;
        for (int i=0; i<trees.Length; i++) { var p = trees[i].position; p.y = data.GetInterpolatedHeight(p.x,p.z)/data.size.y; trees[i].position=p; }
        data.SetTreeInstances(trees, false);
        var layers = new [] {
            MakeLayer("Meadow", new Color(.19f,.25f,.075f), new Color(.39f,.40f,.17f), 6, 0),
            MakeLayer("ForestSoil", new Color(.17f,.105f,.065f), new Color(.36f,.27f,.15f), 5, 1),
            MakeLayer("MountainRock", new Color(.24f,.255f,.25f), new Color(.52f,.50f,.43f), 10, 2),
            MakeLayer("ShoreGravel", new Color(.32f,.285f,.21f), new Color(.64f,.57f,.41f), 5, 3)
        };
        data.terrainLayers = layers;
        int a = data.alphamapResolution;
        var weights = new float[a,a,4];
        for(int y=0;y<a;y++) for(int x=0;x<a;x++)
        {
            float u=(float)x/(a-1), v=(float)y/(a-1);
            float h=data.GetInterpolatedHeight(u,v)+terrain.transform.position.y;
            float slope=data.GetSteepness(u,v);
            float noise=Mathf.PerlinNoise(u*23+9,v*23+3);
            float shore=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(water+1,water+9,h));
            float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,51,slope));
            rock=Mathf.Max(rock,Mathf.SmoothStep(0,1,Mathf.InverseLerp(water+75,water+155,h))*.65f);
            float soil=Mathf.Lerp(.12f,.50f,noise)* (1-rock)*(1-shore);
            float grass=(1-rock)*(1-shore)-soil;
            weights[y,x,0]=grass;weights[y,x,1]=soil;weights[y,x,2]=rock*(1-shore);weights[y,x,3]=shore;
        }
        data.SetAlphamaps(0,0,weights);
        var terrainShader=Shader.Find("HDRP/TerrainLit");
        if(terrainShader) { var mat=new Material(terrainShader); mat.name="Lakeside Terrain"; AssetDatabase.CreateAsset(mat,Root+"/Materials/Terrain.mat"); terrain.materialTemplate=mat; }
        var cabin=GameObject.Find("cabin in the forest");
        var report=new List<string>();
        if(cabin)
        {
            var cache=new Dictionary<Material,Material>();
            foreach(var r in cabin.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(r,"Restore cabin textures");
                var mats=r.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    var source=mats[i]; if(!source) continue;
                    if(!cache.TryGetValue(source,out var replacement))
                    {
                        string texName=TextureName(source.name);
                        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Materials  and Textures/Textures/Cabin/"+texName+".jpeg");
                        if(!texture) texture=source.mainTexture as Texture2D;
                        replacement=new Material(Shader.Find("HDRP/Lit"));
                        replacement.name=source.name+"_Textured";
                        replacement.SetColor("_BaseColor",texture?Color.white:new Color(.45f,.38f,.28f));
                        if(texture) replacement.SetTexture("_BaseColorMap",texture);
                        replacement.SetFloat("_Smoothness",source.name.ToLower().Contains("metal")?.55f:.22f);
                        replacement.SetFloat("_Metallic",source.name.ToLower().Contains("metal")?.7f:0);
                        replacement.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                        string path=AssetDatabase.GenerateUniqueAssetPath(Root+"/Materials/"+Safe(source.name)+".mat");
                        AssetDatabase.CreateAsset(replacement,path);
                        cache[source]=replacement;
                        report.Add(source.name+" -> "+(texture?AssetDatabase.GetAssetPath(texture):"warm fallback color"));
                    }
                    mats[i]=replacement;
                }
                r.sharedMaterials=mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        }
        EditorUtility.SetDirty(data); EditorUtility.SetDirty(terrain);
        terrain.Flush();
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Root+"/FinishReport.txt",new[]{"Finished scene: "+scene.path,"Original terrain retained; scene backup in Backup/.","Layers: meadow, soil, rock, shore. Mountain elevation varied gently above shoreline."}.Concat(report));
        AssetDatabase.Refresh();
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
        Debug.Log("LANDSCAPE_FINISH_OK: " + report.Count + " cabin materials restored; four terrain layers painted.");
    }
    static string Safe(string s) { foreach(char c in Path.GetInvalidFileNameChars()) s=s.Replace(c,'_'); return s; }
    static string TextureName(string name)
    {
        string s=name.ToLowerInvariant();
        if(s.Contains("siding")) { for(int i=1;i<=5;i++) if(s.Contains("siding"+i)) return "house_siding"+i; }
        if(s.Contains("roof"))return "roof";
        if(s.Contains("ceiling"))return "house_ceiling";
        if(s.Contains("interior")&&s.Contains("floor"))return "interior floor";
        if(s.Contains("interior")&&s.Contains("wall"))return "interior wall";
        if(s.Contains("door"))return "doors";
        if(s.Contains("vertical"))return "wood_verticalboard";
        if(s.Contains("porch")&&s.Contains("plank"))return "porch plank";
        if(s.Contains("dock")||s.Contains("water_support"))return "wood dock";
        if(s.Contains("boat"))return "boat";
        if(s.Contains("metal"))return "metal";
        if(s.Contains("window")||s.Contains("lightwood")||s.Contains("painted"))return "wood3";
        if(s.Contains("misc2"))return "misc2";
        if(s.Contains("misc"))return "misc";
        return "wood2";
    }
    static float Noise(float u,float v,float scale,float seed)
    {
        float x=u*scale,y=v*scale;
        float a=Mathf.PerlinNoise(x+seed,y+seed),b=Mathf.PerlinNoise(x-scale+seed,y+seed);
        float c=Mathf.PerlinNoise(x+seed,y-scale+seed),d=Mathf.PerlinNoise(x-scale+seed,y-scale+seed);
        return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
    }
    static TerrainLayer MakeLayer(string name,Color dark,Color light,float size,int seed)
    {
        const int n=512;
        var tex=new Texture2D(n,n,TextureFormat.RGB24,true);
        var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float u=(float)x/(n-1),v=(float)y/(n-1);
            float value=Noise(u,v,8,seed*13+4)*.45f+Noise(u,v,32,seed*7+31)*.32f+Noise(u,v,128,seed*11+53)*.23f;
            float grain=Noise(u,v,240,seed*5+71);
            value=Mathf.Clamp01((value-.24f)*1.9f+(grain-.5f)*.22f);
            pixels[y*n+x]=Color.Lerp(dark,light,value);
        }
        tex.SetPixels(pixels);tex.Apply();
        string path=Root+"/Textures/"+name+".png";
        File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.SaveAndReimport();
        var layer=new TerrainLayer{diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path),tileSize=new Vector2(size,size),smoothness=.12f,metallic=0};
        layer.name=name;AssetDatabase.CreateAsset(layer,Root+"/"+name+".terrainlayer");
        return layer;
    }
}
