using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public class CheckMicrowave
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");
        new GameObject("Microwave runtime checks").AddComponent<MicrowaveCheckRunner>();
        EditorApplication.isPlaying = true;
    }
}
