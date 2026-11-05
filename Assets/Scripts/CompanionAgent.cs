using UnityEngine;
using UnityEngine.AI;

namespace KuleNobeti
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CompanionSensor))]
    public sealed class CompanionAgent : MonoBehaviour
    {
        public enum Phase { Following, FindingRoute, GoingToSwitch, Aligning, Succeeded, Failed }
        public Phase State { get; private set; } = Phase.Following;
        public float Elapsed { get; private set; }
        public string Status { get; private set; } = "Takip ediyor";
        public CompanionSensor Sensor { get; private set; }

        private NavMeshAgent agent;
        private Transform player;
        private Transform lever;
        private Vector3 usePoint;
        private Door gate;
        private float started, nextFollow, nextRepath;
        private int repaths;
        private bool measuring, awaitingTestOrder;
        private Vector3 previousDestination, activeGoal, orderStart;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Sensor = GetComponent<CompanionSensor>();
        }

        public void Configure(Transform hero, Transform switchBody, Vector3 switchUsePoint, Door physicalGate)
        {
            player = hero;
            lever = switchBody;
            usePoint = switchUsePoint;
            gate = physicalGate;
        }

        public bool Relocate(Vector3 start)
        {
            if (State != Phase.Following && State != Phase.Failed) return false;
            if (!agent.isOnNavMesh || !NavMesh.SamplePosition(start, out NavMeshHit point, 2f, agent.areaMask))
                return false;
            if (!agent.Warp(point.position)) return false;
            agent.ResetPath();
            measuring = false;
            State = Phase.Following;
            Status = "Başlangıç değiştirildi";
            Elapsed = 0;
            awaitingTestOrder = true;
            return true;
        }

        public void Order()
        {
            if (State != Phase.Following && State != Phase.Failed) return;
            started = Time.time;
            Elapsed = 0;
            measuring = true;
            awaitingTestOrder = false;
            orderStart = transform.position;
            repaths = 0;
            nextRepath = Time.time + 1f;
            PlanRoute();
        }

        private void PlanRoute()
        {
            State = Phase.FindingRoute;
            Status = "NavMesh rota hesaplıyor";
            if (!agent.isOnNavMesh)
            {
                Fail("Ajan NavMesh üzerinde değil.");
                return;
            }
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(usePoint, out NavMeshHit goal, .8f, agent.areaMask) ||
                !agent.CalculatePath(goal.position, path) || path.status != NavMeshPathStatus.PathComplete ||
                !agent.SetPath(path))
            {
                Fail("Şaltere ulaşan geçerli rota bulunamadı.");
                return;
            }
            activeGoal = goal.position;
            State = Phase.GoingToSwitch;
            Status = "Servis tünelinden şaltere gidiyor";
        }

        private void Update()
        {
            if (player == null) return;
            Sensor.Observe(lever);
            if (measuring) Elapsed = Time.time - started;
            if (measuring && Elapsed > 40f)
            {
                Fail("Şaltere ulaşılamadı. Yeniden emir verebilirsin.");
                return;
            }

            switch (State)
            {
                case Phase.Following:
                case Phase.Succeeded:
                    FollowPlayer();
                    break;
                case Phase.GoingToSwitch:
                    if (!agent.isOnNavMesh) { Fail("Ajan rotadan çıktı."); break; }
                    if (!agent.pathPending && (transform.position - activeGoal).sqrMagnitude < .25f)
                    {
                        agent.ResetPath();
                        State = Phase.Aligning;
                        Status = "Sensör şalteri doğruluyor";
                    }
                    else if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
                    {
                        Fail("Rota artık erişilebilir değil.");
                    }
                    else if (Sensor.ForwardObstacleDistance < .65f && agent.velocity.sqrMagnitude < .06f &&
                             repaths < 2 && Time.time >= nextRepath)
                    {
                        repaths++;
                        nextRepath = Time.time + 1.5f;
                        PlanRoute();
                    }
                    break;
                case Phase.Aligning:
                    Vector3 direction = Vector3.ProjectOnPlane(lever.position - transform.position, Vector3.up);
                    if (direction.sqrMagnitude > .001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                            Quaternion.LookRotation(direction), 480f * Time.deltaTime);
                    if (Sensor.CanUse(lever))
                    {
                        gate.Open();
                        measuring = false;
                        State = Phase.Succeeded;
                        Status = "Şalter etkin: fiziksel kapı açık";
                        Debug.Log($"K-07 başlangıç {orderStart:F2}: şalter doğrulandı, {Elapsed:0.00} sn.");
                    }
                    break;
            }
        }

        private void FollowPlayer()
        {
            if (awaitingTestOrder || !agent.isOnNavMesh || Time.time < nextFollow) return;
            nextFollow = Time.time + .35f;
            if ((player.position - transform.position).sqrMagnitude < 3.2f) return;
            if (!NavMesh.SamplePosition(player.position, out NavMeshHit hit, 2.4f, agent.areaMask)) return;
            if (agent.hasPath && (hit.position - previousDestination).sqrMagnitude < .25f) return;
            previousDestination = hit.position;
            agent.SetDestination(hit.position);
        }

        private void Fail(string reason)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            measuring = false;
            State = Phase.Failed;
            Status = reason;
            Debug.LogWarning($"K-07 başlangıç {orderStart:F2}: {reason} ({Elapsed:0.00} sn).");
        }
    }
}
