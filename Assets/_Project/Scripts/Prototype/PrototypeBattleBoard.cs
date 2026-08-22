using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeBattleBoard : MonoBehaviour
    {
        [SerializeField] private int size = 5;
        [SerializeField] private float cellSize = 1.6f;
        [SerializeField] private float towerHeightOffset = 0.08f;

        public float TowerHeightOffset => towerHeightOffset;

        public Vector3 SnapToCell(Vector3 worldPosition)
        {
            var half = (size - 1) * 0.5f;
            var x = Mathf.Round(worldPosition.x / cellSize + half);
            var z = Mathf.Round(worldPosition.z / cellSize + half);
            x = Mathf.Clamp(x, 0f, size - 1f);
            z = Mathf.Clamp(z, 0f, size - 1f);
            return new Vector3((x - half) * cellSize, towerHeightOffset, (z - half) * cellSize);
        }
    }
}
