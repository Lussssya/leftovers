using System;
using UnityEngine;

namespace Leftovers
{
    public class ProductHidingSpots : MonoBehaviour
    {
        [SerializeField] ProductHidingSpot[] hidingSpots = new ProductHidingSpot[3];
        [SerializeField] ProductInteractable[] products = new ProductInteractable[0];

        Transform[] originalParents;
        Vector3[] originalScales;
        bool[] collectedProducts;
        ProductInteractable currentProduct;

        void Awake()
        {
            originalParents = new Transform[products.Length];
            originalScales = new Vector3[products.Length];
            collectedProducts = new bool[products.Length];

            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] != null)
                {
                    originalParents[i] = products[i].transform.parent;
                    originalScales[i] = products[i].transform.localScale;
                }
            }
        }

        public void PlaceRequestedProduct(string requestedName)
        {
            int productIndex = FindAvailableProduct(requestedName);
            if (productIndex < 0 || hidingSpots == null || hidingSpots.Length != 3)
            {
                Debug.LogError("The request needs an available product and three hiding spots.", this);
                return;
            }

            for (int i = 0; i < hidingSpots.Length; i++)
            {
                if (hidingSpots[i] == null)
                {
                    Debug.LogError("A product hiding spot is missing.", this);
                    return;
                }
            }

            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] != null && !collectedProducts[i] && MatchesRequest(products[i], requestedName))
                {
                    products[i].gameObject.SetActive(false);
                }
            }

            int spotIndex = UnityEngine.Random.Range(0, hidingSpots.Length);
            currentProduct = products[productIndex];
            currentProduct.transform.SetParent(originalParents[productIndex], false);
            currentProduct.transform.localScale = originalScales[productIndex];
            hidingSpots[spotIndex].PlaceProduct(currentProduct);
        }

        public bool HasProduct(string requestedName)
        {
            return FindAvailableProduct(requestedName) >= 0;
        }

        public bool CanCollectProduct(ProductInteractable product)
        {
            return !HasProduct(product.DisplayName) || product == currentProduct;
        }

        public void MarkProductCollected(ProductInteractable product)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] == product)
                {
                    collectedProducts[i] = true;
                    if (currentProduct == product)
                    {
                        currentProduct = null;
                    }
                    return;
                }
            }
        }

        public void StoreProduct(ProductInteractable product)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] == product)
                {
                    product.gameObject.SetActive(false);
                    product.transform.SetParent(originalParents[i], true);
                    product.transform.localScale = originalScales[i];
                    collectedProducts[i] = true;
                    if (currentProduct == product)
                    {
                        currentProduct = null;
                    }
                    return;
                }
            }
        }

        int FindAvailableProduct(string requestedName)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] != null && !collectedProducts[i] && MatchesRequest(products[i], requestedName))
                {
                    return i;
                }
            }

            return -1;
        }

        bool MatchesRequest(ProductInteractable product, string requestedName)
        {
            return string.Equals(product.DisplayName.Trim(), (requestedName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
