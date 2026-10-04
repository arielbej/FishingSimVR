using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;
public static class RefineMountains {
 const string Root="Assets/LandscapeFinish";
 [MenuItem("Tools/Refine Mountain Textures")]
 public static void Run(){
  var t=UnityEngine.Object.FindFirstObjectByType<Terrain>();
  var d=t.terrainData;
  string rockPath=Root+"/Textures/AlpineRock.png";
  if(!File.Exists(rockPath))throw new Exception("AlpineRock texture missing");
  AssetDatabase.ImportAsset(rockPath,ImportAssetOptions.ForceSynchronousImport);
  var imp=(TextureImporter)AssetImporter.GetAtPath(rockPath);
  imp.wrapMode=TextureWrapMode.Repeat;imp.anisoLevel=16;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
  string normalPath=Root+"/Textures/AlpineRock_Normal.png";
  File.Copy(rockPath,normalPath,true);AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);
  var ni=(TextureImporter)AssetImporter.GetAtPath(normalPath);
  ni.textureType=TextureImporterType.NormalMap;ni.convertToNormalmap=true;ni.heightmapScale=.045f;ni.wrapMode=TextureWrapMode.Repeat;ni.anisoLevel=16;ni.maxTextureSize=2048;ni.SaveAndReimport();
  Undo.RegisterCompleteObjectUndo(d,"Refine mountain painting");
  var layers=d.terrainLayers;
  var rock=layers[2];Undo.RecordObject(rock,"Detailed rock");
  rock.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(rockPath);rock.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);rock.normalScale=.8f;rock.tileSize=new Vector2(34,34);rock.smoothness=.16f;EditorUtility.SetDirty(rock);
  var broad=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Root+"/CliffRock.terrainlayer");
  if(!broad){broad=UnityEngine.Object.Instantiate(rock);broad.name="CliffRock";AssetDatabase.CreateAsset(broad,Root+"/CliffRock.terrainlayer");}
  broad.tileSize=new Vector2(91,91);broad.tileOffset=new Vector2(17,39);broad.normalScale=.65f;EditorUtility.SetDirty(broad);
  d.terrainLayers=new[]{layers[0],layers[1],rock,layers[3],broad};
  d.baseMapResolution=2048;d.alphamapResolution=1024;
  int n=d.alphamapResolution;var w=new float[n,n,5];
  var lake=GameObject.Find("Lake");float water=lake?lake.transform.position.y:12.4f;
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float u=(float)x/(n-1),v=(float)y/(n-1),h=d.GetInterpolatedHeight(u,v)+t.transform.position.y,s=d.GetSteepness(u,v);
   float macro=Mathf.PerlinNoise(u*13+42,v*13+7), detail=Mathf.PerlinNoise(u*65+8,v*65+19);
   float shore=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(water+1,water+8,h+detail*2));
   float cliffs=Mathf.SmoothStep(0,1,Mathf.InverseLerp(29+macro*12,58+detail*5,s));
   float high=Mathf.SmoothStep(0,1,Mathf.InverseLerp(water+95,water+190,h));
   float stone=Mathf.Clamp01(Mathf.Max(cliffs,high*.7f)+(macro-.5f)*.16f);
   float soil=(1-stone)*(1-shore)*Mathf.Lerp(.08f,.30f,detail);
   float grassy=(1-stone)*(1-shore)-soil;
   float broadMix=Mathf.Lerp(.3f,.85f,macro);
   w[y,x,0]=grassy;w[y,x,1]=soil;w[y,x,2]=stone*(1-shore)*(1-broadMix);w[y,x,3]=shore;w[y,x,4]=stone*(1-shore)*broadMix;
  }
  d.SetAlphamaps(0,0,w);t.basemapDistance=2000;EditorUtility.SetDirty(d);EditorUtility.SetDirty(t);
  foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})){
   var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
   string s=m.name.ToLower();
   bool glass=s.StartsWith("glass"),cloth=s.Contains("cloth")||s.Contains("welcome_mat"),rubber=s.Contains("rubber")||s.Contains("tire"),plastic=s.Contains("plastic"),rope=s.Contains("rope"),emitter=s.Contains("emitter");
   if(glass||cloth||rubber||plastic||rope||emitter){
    m.SetTexture("_BaseColorMap",null);
    var c=glass?new Color(.48f,.62f,.68f,.22f):cloth?new Color(.72f,.67f,.52f):rubber?new Color(.035f,.04f,.035f):rope?new Color(.36f,.28f,.16f):new Color(.72f,.70f,.63f);
    if(s.Contains("black"))c=new Color(.025f,.025f,.025f);if(s.Contains("grey"))c=new Color(.24f,.24f,.24f);
    m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",glass?.92f:cloth?.08f:.3f);
    if(glass){m.SetFloat("_SurfaceType",1);m.SetFloat("_BlendMode",0);m.SetFloat("_ZWrite",0);}
   }
   HDShaderUtils.ResetMaterialKeywords(m);EditorUtility.SetDirty(m);
  }
  t.Flush();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(t.gameObject.scene);EditorSceneManager.SaveScene(t.gameObject.scene);SceneView.RepaintAll();
  File.AppendAllText(Root+"/FinishReport.txt","\nREFINED: detailed alpine rock, normal mapping, two texture scales, irregular slope/height transitions. Glass, fabric, rubber and plastics corrected.\n");
  Debug.Log("MOUNTAIN_REFINEMENT_OK");
 }
}
