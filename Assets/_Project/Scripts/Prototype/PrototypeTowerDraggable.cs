using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerDraggable : MonoBehaviour
    {
        [SerializeField] private PrototypeBattleBoard board;
        [SerializeField] private float dragPlaneHeight = 0.08f;

        private Vector3 lastValidPosition;

        public float DragPlaneHeight => dragPlaneHeight;

        public void Configure(PrototypeBattleBoard battleBoard)
        {
            board = battleBoard;
            if (board != null)
            {
                dragPlaneHeight = board.TowerHeightOffset;
                lastValidPosition = board.SnapToCell(transform.position);
                transform.position = lastValidPosition;
            }
            else
            {
                lastValidPosition = transform.position;
            }
        }

        private void Awake()
        {
            if (board == null)
            {
                Configure(UnityEngine.Object.FindAnyObjectByType<PrototypeBattleBoard>());
            }
        }

        public void DragTo(Vector3 worldPosition)
        {
            worldPosition.y = dragPlaneHeight;
            transform.position = worldPosition;
        }

        public void SnapToBoard()
        {
            if (board == null)
            {
                lastValidPosition = transform.position;
                return;
            }

            if (board.TrySnapToFreeCell(transform.position, this, out var snappedPosition))
            {
                transform.position = snappedPosition;
                lastValidPosition = snappedPosition;
                return;
            }

            transform.position = lastValidPosition;
        }
    }
}
