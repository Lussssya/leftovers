using UnityEngine;

namespace Leftovers
{
    public class ProductHidingSpot : MonoBehaviour
    {
        [SerializeField] Vector3 availableSpace = new Vector3(0.8f, 0.6f, 0.5f);
        [SerializeField] Vector3 productRotation;

        public void PlaceProduct(ProductInteractable product)
        {
            BoxCollider productCollider = product.GetComponent<BoxCollider>();
            product.transform.SetParent(transform, true);
            product.transform.localRotation = Quaternion.Euler(productRotation);

            Vector3 colliderSize = Vector3.Scale(productCollider.size, product.transform.localScale);
            Vector3 productSize = GetRotatedSize(colliderSize, product.transform.localRotation);
            float sizeMultiplier = Mathf.Min(1f, availableSpace.x / productSize.x);
            sizeMultiplier = Mathf.Min(sizeMultiplier, availableSpace.y / productSize.y);
            sizeMultiplier = Mathf.Min(sizeMultiplier, availableSpace.z / productSize.z);
            product.transform.localScale = product.transform.localScale * sizeMultiplier;

            Vector3 center = product.transform.localRotation * Vector3.Scale(productCollider.center, product.transform.localScale);
            float halfHeight = productSize.y * sizeMultiplier * 0.5f;
            product.transform.localPosition = new Vector3(-center.x, halfHeight - center.y, -center.z);
            product.gameObject.SetActive(true);
        }

        Vector3 GetRotatedSize(Vector3 size, Quaternion rotation)
        {
            Vector3 width = rotation * new Vector3(size.x, 0f, 0f);
            Vector3 height = rotation * new Vector3(0f, size.y, 0f);
            Vector3 depth = rotation * new Vector3(0f, 0f, size.z);
            return new Vector3(
                Mathf.Abs(width.x) + Mathf.Abs(height.x) + Mathf.Abs(depth.x),
                Mathf.Abs(width.y) + Mathf.Abs(height.y) + Mathf.Abs(depth.y),
                Mathf.Abs(width.z) + Mathf.Abs(height.z) + Mathf.Abs(depth.z));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * availableSpace.y * 0.5f, availableSpace);
        }
    }
}
