using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerAttack : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private float range = 4f;
        [SerializeField] private float fireInterval = 1f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float projectileSpeed = 7f;
        [SerializeField] private Material projectileMaterial;

        private float nextFireTime;

        public void Configure(Transform towerFirePoint, float attackRange, float attackInterval, int attackDamage, float shotSpeed, Material shotMaterial)
        {
            firePoint = towerFirePoint;
            range = Mathf.Max(0.1f, attackRange);
            fireInterval = Mathf.Max(0.1f, attackInterval);
            damage = Mathf.Max(1, attackDamage);
            projectileSpeed = Mathf.Max(0.1f, shotSpeed);
            projectileMaterial = shotMaterial;
        }

        private void Update()
        {
            if (Time.time < nextFireTime)
            {
                return;
            }

            var target = FindTarget();
            if (target == null)
            {
                return;
            }

            FireAt(target);
            nextFireTime = Time.time + fireInterval;
        }

        private PrototypeEnemyHealth FindTarget()
        {
            var enemies = UnityEngine.Object.FindObjectsByType<PrototypeEnemyHealth>(FindObjectsInactive.Exclude);
            PrototypeEnemyHealth bestTarget = null;
            var bestDistance = float.MaxValue;
            var origin = transform.position;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(origin, enemy.transform.position);
                if (distance > range || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = enemy;
            }

            return bestTarget;
        }

        private void FireAt(PrototypeEnemyHealth target)
        {
            var origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.2f;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Prototype Projectile";
            projectile.transform.position = origin;
            projectile.transform.localScale = Vector3.one * 0.16f;

            var collider = projectile.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null && projectileMaterial != null)
            {
                renderer.sharedMaterial = projectileMaterial;
            }

            projectile.AddComponent<PrototypeProjectile>().Initialize(target, damage, projectileSpeed);
        }
    }
}


