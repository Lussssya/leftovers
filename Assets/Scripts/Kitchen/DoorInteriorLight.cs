using UnityEngine;

namespace Leftovers
{
    public class DoorInteriorLight : MonoBehaviour
    {
        public FridgeDoor door;
        public Light insideLight;
        public float delay = 0.38f;
        float waiting;

        void LateUpdate()
        {
            Step(Time.deltaTime);
        }

        public void Step(float seconds)
        {
            if (door == null || insideLight == null)
            {
                return;
            }
            if (door.Angle <= 2f)
            {
                waiting = 0f;
                insideLight.enabled = false;
                return;
            }
            waiting += seconds;
            insideLight.enabled = waiting >= delay;
        }

        void OnDisable()
        {
            waiting = 0f;
            if (insideLight != null)
            {
                insideLight.enabled = false;
            }
        }
    }
}
