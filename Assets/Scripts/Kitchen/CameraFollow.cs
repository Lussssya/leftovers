using UnityEngine;

namespace Leftovers
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 7f, -8f);
        [SerializeField] float followSpeed = 6f;
        [SerializeField] float lookHeight = 0.5f;

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            float followAmount = Mathf.Clamp01(followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followAmount);

            Vector3 lookPosition = target.position + Vector3.up * lookHeight;
            transform.LookAt(lookPosition);
        }
    }
}
