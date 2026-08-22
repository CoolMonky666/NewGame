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
            return CellToWorld(WorldToCellClamped(worldPosition));
        }

        public bool TrySnapToFreeCell(Vector3 worldPosition, PrototypeTowerDraggable ignoredTower, out Vector3 snappedPosition)
        {
            var targetCell = WorldToCellClamped(worldPosition);
            if (IsCellOccupied(targetCell, ignoredTower))
            {
                snappedPosition = default;
                return false;
            }

            snappedPosition = CellToWorld(targetCell);
            return true;
        }

        public bool HasFreeCell()
        {
            return TryGetRandomFreeCell(out _);
        }

        public bool TryGetRandomFreeCell(out Vector3 position)
        {
            var occupiedCells = GetOccupiedCells(null);
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

            position = CellToWorld(freeCells[Random.Range(0, freeCells.Count)]);
            return true;
        }

        private bool IsCellOccupied(Vector2Int cell, PrototypeTowerDraggable ignoredTower)
        {
            return GetOccupiedCells(ignoredTower).Contains(cell);
        }

        private HashSet<Vector2Int> GetOccupiedCells(PrototypeTowerDraggable ignoredTower)
        {
            var occupiedCells = new HashSet<Vector2Int>();
            var towers = UnityEngine.Object.FindObjectsByType<PrototypeTowerDraggable>(FindObjectsInactive.Exclude);
            foreach (var tower in towers)
            {
                if (tower == null || tower == ignoredTower)
                {
                    continue;
                }

                if (TryWorldToCell(tower.transform.position, out var cell))
                {
                    occupiedCells.Add(cell);
                }
            }

            return occupiedCells;
        }

        private Vector2Int WorldToCellClamped(Vector3 worldPosition)
        {
            var half = (size - 1) * 0.5f;
            var x = Mathf.RoundToInt(worldPosition.x / cellSize + half);
            var z = Mathf.RoundToInt(worldPosition.z / cellSize + half);
            x = Mathf.Clamp(x, 0, size - 1);
            z = Mathf.Clamp(z, 0, size - 1);
            return new Vector2Int(x, z);
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            var half = (size - 1) * 0.5f;
            return new Vector3((cell.x - half) * cellSize, towerHeightOffset, (cell.y - half) * cellSize);
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
