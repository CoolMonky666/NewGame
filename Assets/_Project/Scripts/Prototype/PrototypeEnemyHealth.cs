using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeEnemyHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private Transform hitPoint;
        [SerializeField] private Vector3 healthBarOffset = new(0f, 1.65f, 0f);

        private int currentHealth;
        private GameObject[] healthPips;

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
            CreateHealthPips();
            UpdateHealthPips();
        }

        public void ApplyDamage(int damage)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth -= Mathf.Max(1, damage);
            UpdateHealthPips();
            if (currentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void CreateHealthPips()
        {
            healthPips = new GameObject[maxHealth];
            var root = new GameObject("Health Pips");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = healthBarOffset;

            var totalWidth = (maxHealth - 1) * 0.18f;
            for (var i = 0; i < healthPips.Length; i++)
            {
                var pip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pip.name = $"Health Pip {i + 1}";
                pip.transform.SetParent(root.transform, false);
                pip.transform.localPosition = new Vector3(i * 0.18f - totalWidth * 0.5f, 0f, 0f);
                pip.transform.localScale = new Vector3(0.13f, 0.08f, 0.04f);

                var collider = pip.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var renderer = pip.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.85f, 0.12f, 0.10f, 1f);
                }

                healthPips[i] = pip;
            }
        }

        private void UpdateHealthPips()
        {
            if (healthPips == null)
            {
                return;
            }

            for (var i = 0; i < healthPips.Length; i++)
            {
                if (healthPips[i] != null)
                {
                    healthPips[i].SetActive(i < currentHealth);
                }
            }
        }
    }
}
