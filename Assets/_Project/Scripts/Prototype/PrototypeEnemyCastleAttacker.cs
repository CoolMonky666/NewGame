using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeEnemyCastleAttacker : MonoBehaviour
    {
        [SerializeField] private PrototypeCastleHealth targetCastle;
        [SerializeField] private int damagePerHit = 1;
        [SerializeField] private float attackInterval = 1f;

        private bool isAttacking;
        private float nextAttackTime;

        public void Configure(PrototypeCastleHealth castleHealth, int damage, float interval)
        {
            targetCastle = castleHealth;
            damagePerHit = Mathf.Max(1, damage);
            attackInterval = Mathf.Max(0.1f, interval);
        }

        public void BeginAttacking()
        {
            if (targetCastle == null)
            {
                return;
            }

            isAttacking = true;
            nextAttackTime = Time.time;
        }

        private void Update()
        {
            if (!isAttacking || targetCastle == null || targetCastle.IsDestroyed || Time.time < nextAttackTime)
            {
                return;
            }

            targetCastle.ApplyDamage(damagePerHit);
            nextAttackTime = Time.time + attackInterval;
        }
    }
}
