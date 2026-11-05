using UnityEngine;

namespace KuleNobeti
{
    public sealed class CompanionSensor : MonoBehaviour
    {
        private const float ScanRange = 2f;
        private static readonly float[] ScanAngles = { 0f, -25f, 25f, -50f, 50f };
        private Vector3 Eye => Position + Vector3.up * .48f;
        public Vector3 Position { get; private set; }
        public float ObstacleDistance { get; private set; } = float.PositiveInfinity;
        public float ObstacleAngle { get; private set; }
        public float ForwardObstacleDistance { get; private set; } = float.PositiveInfinity;
        public float TargetDistance { get; private set; }
        public float TargetAngle { get; private set; }

        public void Observe(Transform target)
        {
            Position = transform.position;
            Vector3 towardTarget = target.position - Eye;
            TargetDistance = towardTarget.magnitude;
            Vector3 planar = Vector3.ProjectOnPlane(towardTarget, Vector3.up);
            TargetAngle = planar.sqrMagnitude > .001f ? Vector3.Angle(transform.forward, planar) : 0f;
            ObstacleDistance = ForwardObstacleDistance = float.PositiveInfinity;
            ObstacleAngle = 0f;
            foreach (float angle in ScanAngles)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                bool hit = Physics.Raycast(Eye, direction, out RaycastHit obstacle, ScanRange,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (hit && angle == 0f) ForwardObstacleDistance = obstacle.distance;
                if (hit && obstacle.distance < ObstacleDistance)
                {
                    ObstacleDistance = obstacle.distance;
                    ObstacleAngle = angle;
                }
                Debug.DrawRay(Eye, direction * (hit ? obstacle.distance : ScanRange), hit ? Color.red : Color.green);
            }
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
