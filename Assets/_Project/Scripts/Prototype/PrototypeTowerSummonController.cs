using UnityEngine;
using UnityEngine.UI;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerSummonController : MonoBehaviour
    {
        [SerializeField] private PrototypeBattleBoard board;
        [SerializeField] private Transform towerRoot;
        [SerializeField] private GameObject[] towerPrefabs;
        [SerializeField] private Material projectileMaterial;
        [SerializeField] private Text coinText;
        [SerializeField] private Button summonButton;
        [SerializeField] private int summonCost = 2;
        [SerializeField] private int startingCoins;
        [SerializeField] private int passiveCoinsPerTick = 1;
        [SerializeField] private float passiveCoinInterval = 1f;

        private int coins;
        private float nextCoinTime;
        private int spawnedTowerCount;

        public void Configure(PrototypeBattleBoard battleBoard, Transform towersParent, GameObject[] availableTowerPrefabs, Material shotMaterial, Text coinsLabel, Button button)
        {
            board = battleBoard;
            towerRoot = towersParent;
            towerPrefabs = availableTowerPrefabs;
            projectileMaterial = shotMaterial;
            coinText = coinsLabel;
            summonButton = button;
        }

        private void Awake()
        {
            coins = Mathf.Max(0, startingCoins);
            nextCoinTime = Time.time + passiveCoinInterval;
            if (summonButton != null)
            {
                summonButton.onClick.AddListener(TrySummonTower);
            }

            RefreshUi();
        }

        private void OnDestroy()
        {
            if (summonButton != null)
            {
                summonButton.onClick.RemoveListener(TrySummonTower);
            }
        }

        private void Update()
        {
            if (Time.time < nextCoinTime)
            {
                return;
            }

            coins += Mathf.Max(0, passiveCoinsPerTick);
            nextCoinTime = Time.time + Mathf.Max(0.1f, passiveCoinInterval);
            RefreshUi();
        }

        private void TrySummonTower()
        {
            if (coins < summonCost || board == null || towerRoot == null || towerPrefabs == null || towerPrefabs.Length == 0)
            {
                RefreshUi();
                return;
            }

            if (!board.TryGetRandomFreeCell(out var spawnPosition))
            {
                RefreshUi();
                return;
            }

            var prefab = towerPrefabs[Random.Range(0, towerPrefabs.Length)];
            if (prefab == null)
            {
                RefreshUi();
                return;
            }

            coins -= summonCost;
            spawnedTowerCount++;

            var tower = Instantiate(prefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), towerRoot);
            tower.name = $"summoned_{prefab.name}_{spawnedTowerCount:00}";

            var attack = tower.GetComponent<PrototypeTowerAttack>() ?? tower.AddComponent<PrototypeTowerAttack>();
            attack.Configure(FindChild(tower.transform, "FirePoint"), 6f, 1f, 1, 7f, projectileMaterial);

            var draggable = tower.GetComponent<PrototypeTowerDraggable>() ?? tower.AddComponent<PrototypeTowerDraggable>();
            draggable.Configure(board);

            RefreshUi();
        }

        private void RefreshUi()
        {
            if (coinText != null)
            {
                coinText.text = $"Coins: {coins}";
            }

            if (summonButton != null)
            {
                summonButton.interactable = coins >= summonCost && board != null && towerPrefabs != null && towerPrefabs.Length > 0;
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
