using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerDraggable : MonoBehaviour
    {
        [SerializeField] private PrototypeBattleBoard board;
        [SerializeField] private float dragPlaneHeight = 0.08f;

        public float DragPlaneHeight => dragPlaneHeight;

        public void Configure(PrototypeBattleBoard battleBoard)
        {
            board = battleBoard;
            if (board != null)
            {
                dragPlaneHeight = board.TowerHeightOffset;
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
            if (board != null)
            {
                transform.position = board.SnapToCell(transform.position);
            }
        }
    }
}
