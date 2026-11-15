using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class IntroPlayScene
{
    static IntroPlayScene()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Intro.unity");
    }
}
