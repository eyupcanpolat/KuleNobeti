using UnityEngine;

namespace KuleNobeti
{
    public sealed class CompanionSensor : MonoBehaviour
    {
        private Vector3 Eye => transform.position + Vector3.up * .48f;
        public float ObstacleDistance { get; private set; } = float.PositiveInfinity;
        public float TargetDistance { get; private set; }
        public float TargetAngle { get; private set; }

        public void Observe(Transform target)
        {
            Vector3 towardTarget = target.position - Eye;
            TargetDistance = towardTarget.magnitude;
            Vector3 planar = Vector3.ProjectOnPlane(towardTarget, Vector3.up);
            TargetAngle = planar.sqrMagnitude > .001f ? Vector3.Angle(transform.forward, planar) : 0f;
            ObstacleDistance = Physics.Raycast(Eye, transform.forward, out RaycastHit obstacle, 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? obstacle.distance : float.PositiveInfinity;
            Debug.DrawRay(Eye, transform.forward * (float.IsInfinity(ObstacleDistance) ? 2f : ObstacleDistance),
                float.IsInfinity(ObstacleDistance) ? Color.green : Color.red);
        }

        public bool CanUse(Transform target)
        {
            Vector3 towardTarget = target.position - Eye;
            if (TargetDistance > 1.48f || TargetAngle > 30f) return false;
            bool clear = Physics.Raycast(Eye, towardTarget.normalized, out RaycastHit hit,
                TargetDistance + .05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.transform == target;
            Debug.DrawLine(Eye, target.position, clear ? Color.cyan : Color.red);
            return clear;
        }
    }
}
