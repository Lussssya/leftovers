using UnityEngine;

namespace Leftovers
{
    public class ProductInteractable : MonoBehaviour
    {
        [SerializeField] string displayName = "Product";
        [SerializeField] float hoverPadding = 0.15f;
        [SerializeField] BoxCollider hoverArea;

        Collider[] productColliders;
        bool[] colliderEnabledStates;
        Rigidbody productRigidbody;
        Transform previousParent;
        bool rigidbodyWasKinematic;
        bool rigidbodyUsedGravity;
        bool rigidbodyDetectedCollisions;
        bool hasSavedRigidbodyState;
        AssemblyTable placedTable;

        public bool IsHeld { get; private set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    return gameObject.name;
                }

                return displayName;
            }
        }

        void Awake()
        {
            EnsureHoverArea();
            CacheProductComponents();
            UpdateHoverArea();
        }

        void Reset()
        {
            EnsureHoverArea();
            UpdateHoverArea();
        }

        void OnValidate()
        {
            FindHoverArea();
            UpdateHoverArea();
        }

        public Vector3 GetTargetPoint()
        {
            if (productColliders == null)
            {
                productColliders = GetComponentsInChildren<Collider>();
            }

            bool foundCollider = false;
            Bounds productBounds = new Bounds(transform.position, Vector3.zero);

            for (int i = 0; i < productColliders.Length; i++)
            {
                Collider productCollider = productColliders[i];
                if (productCollider == null || !productCollider.enabled || productCollider.isTrigger)
                {
                    continue;
                }

                if (!foundCollider)
                {
                    productBounds = productCollider.bounds;
                    foundCollider = true;
                }
                else
                {
                    productBounds.Encapsulate(productCollider.bounds);
                }
            }

            if (foundCollider)
            {
                return productBounds.center;
            }

            return transform.position;
        }

        public bool IsHoverArea(Collider candidate)
        {
            return hoverArea != null && candidate == hoverArea;
        }

        public void PickUp(Transform holdPoint)
        {
            if (IsHeld || holdPoint == null)
            {
                return;
            }

            bool wasPlaced = placedTable != null;
            if (wasPlaced)
            {
                placedTable.ReleaseProduct(this);
                placedTable = null;
            }
            else
            {
                previousParent = transform.parent;
            }

            CacheProductComponents();
            SaveAndDisableColliders();
            DisableRigidbody(!wasPlaced);
            IsHeld = true;
            transform.SetParent(holdPoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Drop(Vector3 position, Quaternion rotation)
        {
            if (!IsHeld)
            {
                return;
            }

            transform.SetParent(previousParent, true);
            transform.SetPositionAndRotation(position, rotation);
            RestoreColliders();
            RestoreRigidbody();
            hasSavedRigidbodyState = false;
            IsHeld = false;
        }

        public void PlaceOnTable(AssemblyTable table, Transform placementPoint)
        {
            if (!IsHeld || table == null || placementPoint == null)
            {
                return;
            }

            transform.SetParent(placementPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            RestoreColliders();
            KeepRigidbodyFixed();
            placedTable = table;
            IsHeld = false;
        }

        void CacheProductComponents()
        {
            productColliders = GetComponentsInChildren<Collider>(true);
            colliderEnabledStates = new bool[productColliders.Length];
            productRigidbody = GetComponent<Rigidbody>();
        }

        void SaveAndDisableColliders()
        {
            for (int i = 0; i < productColliders.Length; i++)
            {
                Collider productCollider = productColliders[i];
                if (productCollider == null)
                {
                    continue;
                }

                colliderEnabledStates[i] = productCollider.enabled;
                productCollider.enabled = false;
            }
        }

        void RestoreColliders()
        {
            for (int i = 0; i < productColliders.Length; i++)
            {
                Collider productCollider = productColliders[i];
                if (productCollider != null)
                {
                    productCollider.enabled = colliderEnabledStates[i];
                }
            }
        }

        void DisableRigidbody(bool saveState)
        {
            if (productRigidbody == null)
            {
                return;
            }

            if (saveState || !hasSavedRigidbodyState)
            {
                rigidbodyWasKinematic = productRigidbody.isKinematic;
                rigidbodyUsedGravity = productRigidbody.useGravity;
                rigidbodyDetectedCollisions = productRigidbody.detectCollisions;
                hasSavedRigidbodyState = true;
            }

            if (!productRigidbody.isKinematic)
            {
                productRigidbody.velocity = Vector3.zero;
                productRigidbody.angularVelocity = Vector3.zero;
            }

            productRigidbody.useGravity = false;
            productRigidbody.detectCollisions = false;
            productRigidbody.isKinematic = true;
        }

        void RestoreRigidbody()
        {
            if (productRigidbody == null)
            {
                return;
            }

            productRigidbody.isKinematic = rigidbodyWasKinematic;
            productRigidbody.useGravity = rigidbodyUsedGravity;
            productRigidbody.detectCollisions = rigidbodyDetectedCollisions;
            if (!productRigidbody.isKinematic)
            {
                productRigidbody.velocity = Vector3.zero;
                productRigidbody.angularVelocity = Vector3.zero;
            }
        }

        void KeepRigidbodyFixed()
        {
            if (productRigidbody == null)
            {
                return;
            }

            productRigidbody.isKinematic = true;
            productRigidbody.useGravity = false;
            productRigidbody.detectCollisions = rigidbodyDetectedCollisions;
        }

        void EnsureHoverArea()
        {
            FindHoverArea();
            if (hoverArea != null)
            {
                return;
            }

            GameObject hoverObject = new GameObject("HoverArea");
            hoverObject.transform.SetParent(transform, false);
            hoverArea = hoverObject.AddComponent<BoxCollider>();
            hoverArea.isTrigger = true;
        }

        void FindHoverArea()
        {
            if (hoverArea != null)
            {
                return;
            }

            Transform hoverTransform = transform.Find("HoverArea");
            if (hoverTransform != null)
            {
                hoverArea = hoverTransform.GetComponent<BoxCollider>();
            }
        }

        void UpdateHoverArea()
        {
            if (hoverArea == null)
            {
                return;
            }

            Bounds localBounds;
            if (!TryGetLocalProductBounds(out localBounds))
            {
                localBounds = new Bounds(Vector3.zero, Vector3.one);
            }

            Vector3 scale = transform.lossyScale;
            Vector3 localPadding = new Vector3(
                GetLocalPadding(scale.x),
                GetLocalPadding(scale.y),
                GetLocalPadding(scale.z));

            hoverArea.transform.localPosition = Vector3.zero;
            hoverArea.transform.localRotation = Quaternion.identity;
            hoverArea.transform.localScale = Vector3.one;
            hoverArea.center = localBounds.center;
            hoverArea.size = localBounds.size + localPadding * 2f;
            hoverArea.isTrigger = true;
        }

        bool TryGetLocalProductBounds(out Bounds localBounds)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            localBounds = new Bounds();
            bool foundCollider = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider productCollider = colliders[i];
                if (productCollider == null || !productCollider.enabled || productCollider.isTrigger)
                {
                    continue;
                }

                AddColliderBounds(productCollider.bounds, ref localBounds, ref foundCollider);
            }

            return foundCollider;
        }

        void AddColliderBounds(Bounds worldBounds, ref Bounds localBounds, ref bool foundPoint)
        {
            Vector3 minimum = worldBounds.min;
            Vector3 maximum = worldBounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 worldPoint = new Vector3(
                            x == 0 ? minimum.x : maximum.x,
                            y == 0 ? minimum.y : maximum.y,
                            z == 0 ? minimum.z : maximum.z);
                        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

                        if (!foundPoint)
                        {
                            localBounds = new Bounds(localPoint, Vector3.zero);
                            foundPoint = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localPoint);
                        }
                    }
                }
            }
        }

        float GetLocalPadding(float scale)
        {
            return hoverPadding / Mathf.Max(Mathf.Abs(scale), 0.0001f);
        }

        void OnDrawGizmosSelected()
        {
            FindHoverArea();
            if (hoverArea == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.8f);
            Gizmos.matrix = hoverArea.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(hoverArea.center, hoverArea.size);
        }
    }
}
