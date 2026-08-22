using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 7f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float hitDistance = 0.12f;
        [SerializeField] private float lifeTime = 3f;

        private PrototypeEnemyHealth target;
        private float elapsed;

        public void Initialize(PrototypeEnemyHealth projectileTarget, int projectileDamage, float projectileSpeed)
        {
            target = projectileTarget;
            damage = Mathf.Max(1, projectileDamage);
            speed = Mathf.Max(0.1f, projectileSpeed);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (target == null || !target.IsAlive || elapsed >= lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            var targetPosition = target.HitPoint.position;
            var toTarget = targetPosition - transform.position;
            var step = speed * Time.deltaTime;
            if (toTarget.magnitude <= Mathf.Max(hitDistance, step))
            {
                target.ApplyDamage(damage);
                Destroy(gameObject);
                return;
            }

            transform.position += toTarget.normalized * step;
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }
    }
}
