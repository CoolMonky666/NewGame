using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeEnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private PrototypeCastleHealth targetCastle;
        [SerializeField] private int enemyCount = 10;
        [SerializeField] private float spawnInterval = 1.35f;
        [SerializeField] private float enemySpeed = 0.85f;
        [SerializeField] private int enemyHealth = 3;
        [SerializeField] private int castleDamage = 1;
        [SerializeField] private float castleAttackInterval = 1f;
        [SerializeField] private bool loopPath = true;

        private int spawnedCount;
        private float nextSpawnTime;

        public void Configure(GameObject prefab, Transform root, Transform[] pathWaypoints, PrototypeCastleHealth castleHealth, int count, float interval, float speed, int health, int damageToCastle, float attackInterval, bool shouldLoop)
        {
            enemyPrefab = prefab;
            enemyRoot = root;
            waypoints = pathWaypoints;
            targetCastle = castleHealth;
            enemyCount = Mathf.Max(0, count);
            spawnInterval = Mathf.Max(0.1f, interval);
            enemySpeed = Mathf.Max(0.01f, speed);
            enemyHealth = Mathf.Max(1, health);
            castleDamage = Mathf.Max(1, damageToCastle);
            castleAttackInterval = Mathf.Max(0.1f, attackInterval);
            loopPath = shouldLoop;
        }

        private void Awake()
        {
            nextSpawnTime = Time.time;
            ResolveCastleTarget();
        }

        private void Update()
        {
            if (spawnedCount >= enemyCount || Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || waypoints == null || waypoints.Length < 2 || waypoints[0] == null)
            {
                return;
            }

            ResolveCastleTarget();

            spawnedCount++;
            var enemy = Instantiate(enemyPrefab, waypoints[0].position, Quaternion.identity, enemyRoot);
            enemy.name = $"base_enemy_1_{spawnedCount:00}";

            var health = enemy.GetComponent<PrototypeEnemyHealth>() ?? enemy.AddComponent<PrototypeEnemyHealth>();
            health.Configure(enemyHealth, FindChild(enemy.transform, "HitPoint"));

            var attacker = enemy.GetComponent<PrototypeEnemyCastleAttacker>() ?? enemy.AddComponent<PrototypeEnemyCastleAttacker>();
            attacker.Configure(targetCastle, castleDamage, castleAttackInterval);

            var follower = enemy.GetComponent<PrototypePathFollower>() ?? enemy.AddComponent<PrototypePathFollower>();
            follower.Configure(waypoints, enemySpeed, 0f, loopPath);
        }

        private void ResolveCastleTarget()
        {
            if (targetCastle != null)
            {
                return;
            }

            targetCastle = FindAnyObjectByType<PrototypeCastleHealth>();
            if (targetCastle != null)
            {
                return;
            }

            var castleObject = GameObject.Find("Castle");
            if (castleObject != null)
            {
                targetCastle = castleObject.GetComponent<PrototypeCastleHealth>() ?? castleObject.AddComponent<PrototypeCastleHealth>();
                targetCastle.Configure(10, null);
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return root;
        }
    }
}

