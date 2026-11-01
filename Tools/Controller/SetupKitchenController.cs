using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class SetupKitchenController
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Environment.unity");
        Mover mover = Object.FindObjectOfType<Mover>();
        GameObject player = mover.gameObject;
        Camera camera = player.GetComponentInChildren<Camera>();
        Object.DestroyImmediate(mover);
        Object.DestroyImmediate(camera.GetComponent<CameraFollow>());
        player.GetComponent<BoxCollider>().enabled = false;
        player.transform.position = new Vector3(0f,.03f,6.8f);
        player.transform.localScale = Vector3.one;
        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = player.AddComponent<CapsuleCollider>();
        capsule.radius = .3f;
        capsule.height = 2.9f;
        capsule.center = new Vector3(0f,1.45f,0f);
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.mass = 70f;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        camera.transform.parent.localPosition = new Vector3(0f,2.65f,0f);
        camera.transform.parent.localRotation = Quaternion.identity;
        camera.transform.localRotation = Quaternion.Euler(7f,0f,0f);
        FirstPersonController controller = player.GetComponent<FirstPersonController>();
        if (controller == null) controller = player.AddComponent<FirstPersonController>();
        controller.playerCamera = camera;
        controller.joint = camera.transform.parent;
        camera.GetComponent<MouseFoodInteractor>().reach = 4.5f;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("FPS_SETUP_PASS");
    }
}

