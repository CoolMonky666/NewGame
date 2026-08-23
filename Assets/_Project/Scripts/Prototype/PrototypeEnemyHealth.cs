using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeEnemyHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private Transform hitPoint;

        private int currentHealth;

        public bool IsAlive => currentHealth > 0;
        public Transform HitPoint => hitPoint != null ? hitPoint : transform;

        public void Configure(int health, Transform targetPoint)
        {
            maxHealth = Mathf.Max(1, health);
            hitPoint = targetPoint;
            currentHealth = maxHealth;
        }

        private void Awake()
        {
            currentHealth = Mathf.Max(1, maxHealth);
        }

        public void ApplyDamage(int damage)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth -= Mathf.Max(1, damage);
            if (currentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}

