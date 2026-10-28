using UnityEngine;

namespace Leftovers
{
    public class FridgeDoorHandle : MonoBehaviour
    {
        public FridgeDoor door;
        public Mesh highlightMesh;

        MeshRenderer highlightRenderer;

        public FridgeDoor Door
        {
            get { return door; }
        }

        public Renderer GetHighlightRenderer()
        {
            if (highlightRenderer != null)
            {
                return highlightRenderer;
            }

            if (highlightMesh == null || door == null)
            {
                return null;
            }

            GameObject highlight = new GameObject("Handle highlight");
            highlight.transform.SetParent(transform, false);
            MeshFilter filter = highlight.AddComponent<MeshFilter>();
            filter.sharedMesh = highlightMesh;
            highlightRenderer = highlight.AddComponent<MeshRenderer>();
            highlightRenderer.sharedMaterial = door.GetComponent<Renderer>().sharedMaterial;
            highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            highlightRenderer.receiveShadows = false;
            highlightRenderer.enabled = false;
            return highlightRenderer;
        }
    }
}
