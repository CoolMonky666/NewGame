using System.Collections.Generic;
using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeBattleBoard : MonoBehaviour
    {
        [SerializeField] private int size = 5;
        [SerializeField] private float cellSize = 1.2f;
        [SerializeField] private float towerHeightOffset = 0.08f;

        public float TowerHeightOffset => towerHeightOffset;

        public void Configure(int boardSize, float boardCellSize, float heightOffset)
        {
            size = Mathf.Max(1, boardSize);
            cellSize = Mathf.Max(0.1f, boardCellSize);
            towerHeightOffset = heightOffset;
        }

        public Vector3 SnapToCell(Vector3 worldPosition)
        {
            var half = (size - 1) * 0.5f;
            var x = Mathf.Round(worldPosition.x / cellSize + half);
            var z = Mathf.Round(worldPosition.z / cellSize + half);
            x = Mathf.Clamp(x, 0f, size - 1f);
            z = Mathf.Clamp(z, 0f, size - 1f);
            return CellToWorld((int)x, (int)z);
        }

        public bool TryGetRandomFreeCell(out Vector3 position)
        {
            var occupiedCells = new HashSet<Vector2Int>();
            var towers = UnityEngine.Object.FindObjectsByType<PrototypeTowerDraggable>(FindObjectsInactive.Exclude);
            foreach (var tower in towers)
            {
                if (TryWorldToCell(tower.transform.position, out var cell))
                {
                    occupiedCells.Add(cell);
                }
            }

            var freeCells = new List<Vector2Int>();
            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (!occupiedCells.Contains(cell))
                    {
                        freeCells.Add(cell);
                    }
                }
            }

            if (freeCells.Count == 0)
            {
                position = default;
                return false;
            }

            var selectedCell = freeCells[Random.Range(0, freeCells.Count)];
            position = CellToWorld(selectedCell.x, selectedCell.y);
            return true;
        }

        private Vector3 CellToWorld(int x, int z)
        {
            var half = (size - 1) * 0.5f;
            return new Vector3((x - half) * cellSize, towerHeightOffset, (z - half) * cellSize);
        }

        private bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            var half = (size - 1) * 0.5f;
            var x = Mathf.RoundToInt(worldPosition.x / cellSize + half);
            var z = Mathf.RoundToInt(worldPosition.z / cellSize + half);
            if (x < 0 || x >= size || z < 0 || z >= size)
            {
                cell = default;
                return false;
            }

            cell = new Vector2Int(x, z);
            return true;
        }
    }
}

