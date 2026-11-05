using UnityEngine;

namespace KuleNobeti
{
    public sealed class Door : MonoBehaviour
    {
        private Vector3 closedPosition;
        private Collider barrier;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            closedPosition = transform.position;
            barrier = GetComponent<Collider>();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            Vector3 target = closedPosition + Vector3.up * 3.2f;
            transform.position = Vector3.MoveTowards(transform.position, target, 4f * Time.deltaTime);
            if ((target - transform.position).sqrMagnitude < .001f)
                barrier.enabled = false;
        }
    }
}
