using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypePathFollower : MonoBehaviour
    {
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float startDistance;
        [SerializeField] private bool loopPath = true;
        [SerializeField] private float waypointReachDistance = 0.02f;

        private int segmentIndex;
        private Animator animator;
        private PrototypeEnemyCastleAttacker castleAttacker;

        public void Configure(Transform[] pathWaypoints, float movementSpeed, float startDistanceAlongPath, bool shouldLoop)
        {
            waypoints = pathWaypoints;
            speed = movementSpeed;
            startDistance = startDistanceAlongPath;
            loopPath = shouldLoop;
            CacheComponents();
            SetAttacking(false);
            PlaceAtDistance(startDistance);
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            SetAttacking(false);
            PlaceAtDistance(startDistance);
        }

        private void Update()
        {
            MoveAlongPath(speed * Time.deltaTime);
        }

        private void CacheComponents()
        {
            animator = GetComponentInChildren<Animator>(true);
            castleAttacker = GetComponent<PrototypeEnemyCastleAttacker>();
        }

        private void PlaceAtDistance(float distance)
        {
            if (!HasPath())
            {
                return;
            }

            var totalLength = GetTotalPathLength();
            if (totalLength <= 0f)
            {
                transform.position = waypoints[0].position;
                segmentIndex = 0;
                return;
            }

            if (loopPath)
            {
                distance %= totalLength;
                if (distance < 0f)
                {
                    distance += totalLength;
                }
            }
            else
            {
                distance = Mathf.Clamp(distance, 0f, totalLength);
            }

            for (var i = 0; i < waypoints.Length - 1; i++)
            {
                var start = waypoints[i].position;
                var end = waypoints[i + 1].position;
                var segmentLength = Vector3.Distance(start, end);
                if (distance > segmentLength && i < waypoints.Length - 2)
                {
                    distance -= segmentLength;
                    continue;
                }

                segmentIndex = i;
                var t = segmentLength > 0f ? distance / segmentLength : 0f;
                transform.position = Vector3.Lerp(start, end, t);
                FaceTowards(end - start);
                return;
            }

            segmentIndex = waypoints.Length - 2;
            transform.position = waypoints[^1].position;
        }

        private void MoveAlongPath(float distance)
        {
            if (!HasPath() || distance <= 0f)
            {
                return;
            }

            while (distance > 0f)
            {
                var targetIndex = segmentIndex + 1;
                if (targetIndex >= waypoints.Length)
                {
                    if (!loopPath)
                    {
                        ReachEndOfPath();
                        return;
                    }

                    segmentIndex = 0;
                    transform.position = waypoints[0].position;
                    targetIndex = 1;
                }

                var target = waypoints[targetIndex].position;
                var toTarget = target - transform.position;
                var remaining = toTarget.magnitude;
                if (remaining <= waypointReachDistance)
                {
                    segmentIndex++;
                    continue;
                }

                var step = Mathf.Min(distance, remaining);
                transform.position += toTarget.normalized * step;
                FaceTowards(toTarget);
                distance -= step;

                if (step >= remaining)
                {
                    segmentIndex++;
                }
            }
        }

        private void ReachEndOfPath()
        {
            transform.position = waypoints[^1].position;
            SetAttacking(true);
            if (castleAttacker != null)
            {
                castleAttacker.BeginAttacking();
            }

            enabled = false;
        }

        private void SetAttacking(bool isAttacking)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsAttackingHash, isAttacking);
        }

        private bool HasPath()
        {
            return waypoints != null && waypoints.Length >= 2 && waypoints[0] != null && waypoints[1] != null;
        }

        private float GetTotalPathLength()
        {
            var length = 0f;
            for (var i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    length += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
                }
            }

            return length;
        }

        private void FaceTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
