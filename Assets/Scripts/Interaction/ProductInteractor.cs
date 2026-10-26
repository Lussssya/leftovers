using UnityEngine;

namespace Leftovers
{
    public class ProductInteractor : MonoBehaviour
    {
        [SerializeField] Camera interactionCamera;
        [SerializeField] Transform interactionOrigin;
        [SerializeField] float baseHorizontalReach = 3f;
        [SerializeField] float lowProductReachBonus = 1f;
        [SerializeField] float lowProductHeightThreshold = 0.25f;
        [SerializeField] float raycastMaxDistance = 10f;
        [SerializeField] float maxTargetAngle = 12f;
        [SerializeField] float targetGracePeriod = 0.35f;
        [SerializeField] float switchScoreMargin = 1.25f;
        [SerializeField] float horizontalDistanceScoreWeight = 0.15f;
        [SerializeField] LayerMask interactionLayers = ~0;

        public ProductInteractable TargetedProduct { get; private set; }

        float lastTargetedTime;

        void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponent<Camera>();
            }
        }

        void Update()
        {
            if (interactionCamera == null || Time.timeScale == 0f)
            {
                ClearTarget();
                return;
            }

            Physics.SyncTransforms();
            UpdateTargetSelection();
            if (TargetedProduct != null && Input.GetKeyDown(KeyCode.E))
            {
                TargetedProduct.Interact();
            }
        }

        void UpdateTargetSelection()
        {
            ProductInteractable mouseProduct = FindMouseHoverTarget();
            ChooseTarget(mouseProduct);
        }

        void ChooseTarget(ProductInteractable mouseProduct)
        {
            if (mouseProduct != null)
            {
                SetTarget(mouseProduct);
                return;
            }

            ProductInteractable exactProduct = FindExactCenterTarget();
            if (exactProduct != null)
            {
                SetTarget(exactProduct);
                return;
            }

            if (TargetedProduct != null && !IsImmediatelyValid(TargetedProduct))
            {
                ClearTarget();
            }

            ProductInteractable bestProduct;
            float bestScore;
            bool currentInsideTolerance;
            float currentScore;
            FindBestProduct(
                out bestProduct,
                out bestScore,
                out currentInsideTolerance,
                out currentScore);

            if (TargetedProduct == null)
            {
                if (bestProduct != null)
                {
                    SetTarget(bestProduct);
                }

                return;
            }

            if (currentInsideTolerance)
            {
                lastTargetedTime = Time.time;
            }

            if (bestProduct == TargetedProduct)
            {
                return;
            }

            if (bestProduct != null)
            {
                bool clearlyBetter = bestScore + switchScoreMargin < currentScore;
                bool graceExpired = !currentInsideTolerance
                    && Time.time - lastTargetedTime > targetGracePeriod;

                if (clearlyBetter || graceExpired)
                {
                    SetTarget(bestProduct);
                }

                return;
            }

            if (!currentInsideTolerance && Time.time - lastTargetedTime > targetGracePeriod)
            {
                ClearTarget();
            }
        }

        ProductInteractable FindMouseHoverTarget()
        {
            return FindMouseHoverTargetAt(Input.mousePosition);
        }

        ProductInteractable FindMouseHoverTargetAt(Vector2 pointer)
        {
            Ray mouseRay = interactionCamera.ScreenPointToRay(pointer);
            RaycastHit[] hits = Physics.RaycastAll(
                mouseRay,
                raycastMaxDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide);

            float closestSolidDistance = FindClosestSolidDistance(hits);
            ProductInteractable bestProduct = null;
            float bestScreenDistance = float.MaxValue;
            float bestHorizontalDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (!hit.collider.isTrigger || hit.distance > closestSolidDistance)
                {
                    continue;
                }

                ProductInteractable product = hit.collider.GetComponentInParent<ProductInteractable>();
                if (!IsValidMouseProduct(product, hit.collider))
                {
                    continue;
                }

                float screenDistance = ScreenDistanceToProduct(product, pointer);
                float horizontalDistance = HorizontalDistanceToProduct(product);
                if (IsBetterMouseTarget(
                    product,
                    screenDistance,
                    horizontalDistance,
                    bestProduct,
                    bestScreenDistance,
                    bestHorizontalDistance))
                {
                    bestProduct = product;
                    bestScreenDistance = screenDistance;
                    bestHorizontalDistance = horizontalDistance;
                }
            }

            return bestProduct;
        }

        float FindClosestSolidDistance(RaycastHit[] hits)
        {
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].collider.isTrigger && hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                }
            }

            return closestDistance;
        }

        bool IsValidMouseProduct(ProductInteractable product, Collider hitCollider)
        {
            if (product == null || !product.isActiveAndEnabled || !product.IsHoverArea(hitCollider))
            {
                return false;
            }

            if (HorizontalDistanceToProduct(product) > GetHorizontalReach(product))
            {
                return false;
            }

            return HasLineOfSight(product);
        }

        float ScreenDistanceToProduct(ProductInteractable product, Vector2 pointer)
        {
            Vector3 screenPoint = interactionCamera.WorldToScreenPoint(product.GetTargetPoint());
            if (screenPoint.z <= 0f)
            {
                return float.MaxValue;
            }

            Vector2 difference = new Vector2(screenPoint.x, screenPoint.y) - pointer;
            return difference.sqrMagnitude;
        }

        bool IsBetterMouseTarget(
            ProductInteractable product,
            float screenDistance,
            float horizontalDistance,
            ProductInteractable bestProduct,
            float bestScreenDistance,
            float bestHorizontalDistance)
        {
            if (bestProduct == null || screenDistance < bestScreenDistance - 0.01f)
            {
                return true;
            }

            if (Mathf.Abs(screenDistance - bestScreenDistance) > 0.01f)
            {
                return false;
            }

            if (horizontalDistance < bestHorizontalDistance - 0.001f)
            {
                return true;
            }

            if (Mathf.Abs(horizontalDistance - bestHorizontalDistance) > 0.001f)
            {
                return false;
            }

            return product.GetInstanceID() < bestProduct.GetInstanceID();
        }

        ProductInteractable FindExactCenterTarget()
        {
            Ray centerRay = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return FindDirectTarget(centerRay);
        }

        ProductInteractable FindDirectTarget(Ray ray)
        {
            RaycastHit hit;
            bool foundHit = Physics.Raycast(
                ray,
                out hit,
                raycastMaxDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            if (!foundHit)
            {
                return null;
            }

            ProductInteractable product = hit.collider.GetComponentInParent<ProductInteractable>();
            if (product == null || !product.isActiveAndEnabled)
            {
                return null;
            }

            if (HorizontalDistanceToProduct(product) > GetHorizontalReach(product))
            {
                return null;
            }

            return product;
        }

        void FindBestProduct(
            out ProductInteractable bestProduct,
            out float bestScore,
            out bool currentInsideTolerance,
            out float currentScore)
        {
            bestProduct = null;
            bestScore = float.MaxValue;
            currentInsideTolerance = false;
            currentScore = float.MaxValue;

            if (TargetedProduct != null)
            {
                currentScore = GetTargetScore(TargetedProduct);
            }

            ProductInteractable[] products = FindObjectsOfType<ProductInteractable>();
            for (int i = 0; i < products.Length; i++)
            {
                ProductInteractable product = products[i];
                if (!IsCandidate(product))
                {
                    continue;
                }

                float score = GetTargetScore(product);
                if (product == TargetedProduct)
                {
                    currentInsideTolerance = true;
                    currentScore = score;
                }

                if (score < bestScore)
                {
                    bestProduct = product;
                    bestScore = score;
                }
            }
        }

        bool IsCandidate(ProductInteractable product)
        {
            if (product == null || !product.isActiveAndEnabled)
            {
                return false;
            }

            if (HorizontalDistanceToProduct(product) > GetHorizontalReach(product))
            {
                return false;
            }

            Vector3 targetDirection = product.GetTargetPoint() - interactionCamera.transform.position;
            if (targetDirection.magnitude > raycastMaxDistance)
            {
                return false;
            }

            float targetAngle = Vector3.Angle(interactionCamera.transform.forward, targetDirection);
            if (targetAngle > maxTargetAngle)
            {
                return false;
            }

            return HasLineOfSight(product);
        }

        bool IsImmediatelyValid(ProductInteractable product)
        {
            if (product == null || !product.isActiveAndEnabled)
            {
                return false;
            }

            if (HorizontalDistanceToProduct(product) > GetHorizontalReach(product))
            {
                return false;
            }

            return HasLineOfSight(product);
        }

        bool HasLineOfSight(ProductInteractable product)
        {
            Vector3 targetPoint = product.GetTargetPoint();
            Vector3 targetDirection = targetPoint - interactionCamera.transform.position;
            float targetDistance = targetDirection.magnitude;
            if (targetDistance <= 0f || targetDistance > raycastMaxDistance)
            {
                return false;
            }

            RaycastHit hit;
            float lineOfSightDistance = Mathf.Min(targetDistance + 0.1f, raycastMaxDistance);
            bool foundHit = Physics.Raycast(
                interactionCamera.transform.position,
                targetDirection.normalized,
                out hit,
                lineOfSightDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            if (!foundHit)
            {
                return false;
            }

            ProductInteractable hitProduct = hit.collider.GetComponentInParent<ProductInteractable>();
            return hitProduct == product;
        }

        float GetTargetScore(ProductInteractable product)
        {
            Vector3 targetDirection = product.GetTargetPoint() - interactionCamera.transform.position;
            float targetAngle = Vector3.Angle(interactionCamera.transform.forward, targetDirection);
            float horizontalDistance = HorizontalDistanceToProduct(product);
            return targetAngle + horizontalDistance * horizontalDistanceScoreWeight;
        }

        float HorizontalDistanceToProduct(ProductInteractable product)
        {
            Vector3 origin = GetInteractionOriginPosition();

            Vector3 productPosition = product.transform.position;
            origin.y = productPosition.y;
            return Vector3.Distance(origin, productPosition);
        }

        float GetHorizontalReach(ProductInteractable product)
        {
            float reach = baseHorizontalReach;
            float relativeHeight = product.GetTargetPoint().y - GetInteractionOriginPosition().y;
            if (relativeHeight < lowProductHeightThreshold)
            {
                reach += lowProductReachBonus;
            }

            return reach;
        }

        Vector3 GetInteractionOriginPosition()
        {
            if (interactionOrigin != null)
            {
                return interactionOrigin.position;
            }

            return interactionCamera.transform.position;
        }

        void SetTarget(ProductInteractable product)
        {
            TargetedProduct = product;
            lastTargetedTime = Time.time;
        }

        void ClearTarget()
        {
            TargetedProduct = null;
            lastTargetedTime = 0f;
        }

        void OnDisable()
        {
            ClearTarget();
        }
    }
}
