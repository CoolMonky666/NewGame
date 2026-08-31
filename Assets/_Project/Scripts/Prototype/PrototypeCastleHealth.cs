using UnityEngine;
using UnityEngine.UI;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeCastleHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private Text healthText;

        private int currentHealth;

        public bool IsDestroyed => currentHealth <= 0;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        public void Configure(int health, Text label)
        {
            maxHealth = Mathf.Max(1, health);
            healthText = label;
            currentHealth = maxHealth;
            RefreshUi();
        }

        private void Awake()
        {
            if (currentHealth <= 0)
            {
                currentHealth = Mathf.Max(1, maxHealth);
            }

            RefreshUi();
        }

        public void ApplyDamage(int damage)
        {
            if (IsDestroyed)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (healthText != null)
            {
                healthText.text = $"Castle HP: {currentHealth}/{maxHealth}";
            }
        }
    }
}
