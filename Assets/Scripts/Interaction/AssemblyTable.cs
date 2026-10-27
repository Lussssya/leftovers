using UnityEngine;

namespace Leftovers
{
    public class AssemblyTable : MonoBehaviour
    {
        [SerializeField] Transform[] placementPoints;
        [SerializeField] float placementReach = 4f;

        ProductInteractable[] occupants;

        public bool HasSpace
        {
            get
            {
                return FindNextFreePoint() >= 0;
            }
        }

        void Awake()
        {
            EnsureOccupants();
        }

        public bool IsInRange(Vector3 playerPosition)
        {
            Vector3 tablePosition = transform.position;
            playerPosition.y = tablePosition.y;
            return Vector3.Distance(playerPosition, tablePosition) <= placementReach;
        }

        public bool TryPlace(ProductInteractable product)
        {
            if (product == null || !product.IsHeld)
            {
                return false;
            }

            int pointIndex = FindNextFreePoint();
            if (pointIndex < 0)
            {
                return false;
            }

            occupants[pointIndex] = product;
            product.PlaceOnTable(this, placementPoints[pointIndex]);
            return true;
        }

        public void ReleaseProduct(ProductInteractable product)
        {
            EnsureOccupants();
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] == product)
                {
                    occupants[i] = null;
                    return;
                }
            }
        }

        int FindNextFreePoint()
        {
            EnsureOccupants();
            if (placementPoints == null)
            {
                return -1;
            }

            for (int i = 0; i < placementPoints.Length; i++)
            {
                if (placementPoints[i] != null && occupants[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        void EnsureOccupants()
        {
            int pointCount = placementPoints == null ? 0 : placementPoints.Length;
            if (occupants == null || occupants.Length != pointCount)
            {
                occupants = new ProductInteractable[pointCount];
            }
        }
    }
}
