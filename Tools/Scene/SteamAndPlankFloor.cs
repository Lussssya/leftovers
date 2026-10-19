using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class SteamAndPlankFloor
{
    public static void Run()
    {
        foreach(string path in new string[]{"Assets/Prefabs/Microwave_Interactive.prefab","Assets/Prefabs/Sandwich_Test.prefab"})
        {
            GameObject root=PrefabUtility.LoadPrefabContents(path);
            foreach(ParticleSystem steam in root.GetComponentsInChildren<ParticleSystem>())
            {
                if (!steam.name.ToLower().Contains("steam")) continue;
                ParticleSystem.MainModule main=steam.main;
                main.startLifetime=new ParticleSystem.MinMaxCurve(2.1f,2.7f);
                main.gravityModifier=0;
                main.startSpeed=0;
                main.maxParticles=300;
                ParticleSystem.VelocityOverLifetimeModule velocity=steam.velocityOverLifetime;
                velocity.enabled=true;
                velocity.space=ParticleSystemSimulationSpace.World;
                velocity.y=new ParticleSystem.MinMaxCurve(.45f,.58f);
                velocity.x=new ParticleSystem.MinMaxCurve(-.018f,.018f);
                velocity.z=new ParticleSystem.MinMaxCurve(-.018f,.018f);
                ParticleSystem.LimitVelocityOverLifetimeModule limit=steam.limitVelocityOverLifetime;
                limit.enabled=false;
                ParticleSystem.ForceOverLifetimeModule force=steam.forceOverLifetime;
                force.enabled=false;
                ParticleSystem.InheritVelocityModule inherit=steam.inheritVelocity;
                inherit.enabled=false;
                ParticleSystem.NoiseModule noise=steam.noise;
                noise.strength=.025f;
                ParticleSystem.CollisionModule collision=steam.collision;
                collision.radiusScale=.035f;
                collision.dampen=0;
                collision.lifetimeLoss=0;
                collision.minKillSpeed=0;
                collision.maxKillSpeed=1000;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        GameObject floor=GameObject.Find("Floor (1)");
        float surface=floor.GetComponent<Renderer>().bounds.max.y;
        floor.transform.position-=Vector3.up*.025f;
        GameObject boards=new GameObject("Beveled wood floor");
        boards.transform.SetParent(GameObject.Find("Flow test room").transform);
        System.IO.Directory.CreateDirectory("Assets/Models/FlowRoom");
        Material[] materials=new Material[6];
        for(int i=0;i<6;i++)
        {
            materials[i]=new Material(Shader.Find("Standard"));
            materials[i].color=new Color(.235f+i*.009f,.175f+i*.007f,.115f+i*.005f);
            materials[i].SetFloat("_Glossiness",.27f);
            AssetDatabase.CreateAsset(materials[i],"Assets/Materials/FlowRoom/Oak "+i+".mat");
        }
        int row=0;
        for(float z=-1.1f;z<7.4f;z+=.34f)
        {
            float offset=(row%3)*.6f;
            int column=0;
            for(float x=-6.35f-offset;x<6.35f;x+=1.8f)
            {
                float left=Mathf.Max(x,-6.35f);
                float right=Mathf.Min(x+1.8f,6.35f);
                float width=right-left-.008f;
                if(width<.02f)continue;
                string meshPath="Assets/Models/FlowRoom/Board "+Mathf.RoundToInt(width*1000)+".asset";
                Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(mesh==null)
                {
                    mesh=Board(width,.332f);
                    AssetDatabase.CreateAsset(mesh,meshPath);
                }
                GameObject board=new GameObject("Oak plank "+row+" "+column);
                board.transform.SetParent(boards.transform);
                board.transform.position=new Vector3((left+right)*.5f,surface,z+.166f);
                board.AddComponent<MeshFilter>().sharedMesh=mesh;
                board.AddComponent<MeshRenderer>().sharedMaterial=materials[(row*7+column*11)%6];
                BoxCollider solid=board.AddComponent<BoxCollider>();
                solid.center=new Vector3(0,-.014f,0);
                solid.size=new Vector3(width,.028f,.332f);
                board.isStatic=true;
                column++;
            }
            row++;
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        InspectAppliances.Capture(Camera.main,"plank-room-preview.png");
        Debug.Log("PLANK_FLOOR_SAVED");
    }
    static Mesh Board(float width,float depth)
    {
        float x=width*.5f;
        float z=depth*.5f;
        Vector3[] ring={new Vector3(-x,-.008f,-z),new Vector3(-x,-.008f,z),new Vector3(x,-.008f,z),new Vector3(x,-.008f,-z),new Vector3(-x+.007f,0,-z+.007f),new Vector3(-x+.007f,0,z-.007f),new Vector3(x-.007f,0,z-.007f),new Vector3(x-.007f,0,-z+.007f),new Vector3(-x,-.028f,-z),new Vector3(-x,-.028f,z),new Vector3(x,-.028f,z),new Vector3(x,-.028f,-z)};
        int[] faces={4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7,8,9,1,8,1,0,9,10,2,9,2,1,10,11,3,10,3,2,11,8,0,11,0,3,8,11,10,8,10,9};
        Vector3[] vertices=new Vector3[faces.Length];
        int[] triangles=new int[faces.Length];
        for(int i=0;i<faces.Length;i++){vertices[i]=ring[faces[i]];triangles[i]=i;}
        Mesh mesh=new Mesh();
        mesh.vertices=vertices;
        mesh.triangles=triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
