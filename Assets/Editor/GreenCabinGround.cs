using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class GreenCabinGround {
 [MenuItem("Tools/Add Grass Around Cabin")]
 public static void Run(){
 var t=Object.FindFirstObjectByType<Terrain>();var house=GameObject.Find("cabin in the forest");
 if(!t||!house)throw new System.Exception("Terrain or cabin missing");
 System.IO.File.WriteAllLines("Assets/LandscapeFinish/CabinSurfaces.txt", System.Array.ConvertAll(house.GetComponentsInChildren<Renderer>(true), r=>r.name+" | "+r.bounds+" | "+string.Join(",",System.Array.ConvertAll(r.sharedMaterials,m=>m?m.name:"none"))));
 var d=t.terrainData;var layers=d.terrainLayers;int grass=System.Array.FindIndex(layers,l=>l&&l.name=="Meadow");if(grass<0)throw new System.Exception("Meadow layer missing");
 Undo.RegisterCompleteObjectUndo(d,"Paint grass around cabin");
 int n=d.alphamapResolution;var a=d.GetAlphamaps(0,0,n,n);var c=house.transform.position;
 var lake=GameObject.Find("Lake");float water=lake?lake.transform.position.y:12.4f;int painted=0;
 for(int y=0;y<n;y++)for(int x=0;x<n;x++){
 float u=(float)x/(n-1),v=(float)y/(n-1),wx=t.transform.position.x+u*d.size.x,wz=t.transform.position.z+v*d.size.z;
 float distance=Vector2.Distance(new Vector2(wx,wz),new Vector2(c.x,c.z));if(distance>65)continue;
 float h=d.GetInterpolatedHeight(u,v)+t.transform.position.y;
 float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(water+.15f,water+1.4f,h));
 float noise=Mathf.PerlinNoise(wx*.11f,wz*.11f);
 float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(24,60+noise*5,distance)))*edge*Mathf.Lerp(.74f,.94f,noise);
 blend*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(32,52,d.GetSteepness(u,v)));
 if(blend<=.001f)continue;
 for(int l=0;l<layers.Length;l++)a[y,x,l]=Mathf.Lerp(a[y,x,l],l==grass?1:0,blend);
 painted++;
 }
 d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);t.Flush();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(t.gameObject.scene);SceneView.RepaintAll();Debug.Log("CABIN_GRASS_OK: "+painted+" terrain samples painted around "+c);
 }
}
