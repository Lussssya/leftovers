using UnityEngine;
using UnityEditor;
using Leftovers;
public class AnchorFoodSteam
{
    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        MicrowaveFood food = root.GetComponent<MicrowaveFood>();
        MeshFilter filter = food.bread.GetComponent<MeshFilter>();
        Bounds bounds = food.bread.bounds;
        Vector3[] vertices = filter.sharedMesh.vertices;
        float height = bounds.center.y;
        for(int bin=0;bin<30;bin++)
        {
            float low = Mathf.Lerp(bounds.min.y,bounds.max.y,bin/30f);
            float high = Mathf.Lerp(bounds.min.y,bounds.max.y,(bin+1)/30f);
            float minX=float.MaxValue, maxX=float.MinValue, minZ=float.MaxValue,maxZ=float.MinValue;
            foreach(Vector3 vertex in vertices)
            {
                Vector3 point=filter.transform.TransformPoint(vertex);
                if(point.y<low || point.y>high) continue;
                minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);
                minZ=Mathf.Min(minZ,point.z);maxZ=Mathf.Max(maxZ,point.z);
            }
            if(maxX-minX>bounds.size.x*.55f && maxZ-minZ>bounds.size.z*.55f) height=high;
        }
        food.steamOrigin=root.transform.InverseTransformPoint(new Vector3(bounds.center.x,height+.002f,bounds.center.z));
        food.steam.transform.localPosition=food.steamOrigin;
        Debug.Log("BREAD_STEAM_SURFACE " + food.steamOrigin + " bounds top " + bounds.max.y);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(root);
    }
}
