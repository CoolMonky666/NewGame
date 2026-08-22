using UnityEngine;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerDraggable : MonoBehaviour
    {
        [SerializeField] private PrototypeBattleBoard board;
        [SerializeField] private float dragPlaneHeight = 0.08f;

        private Camera activeCamera;
        private Vector3 dragOffset;
        private bool isDragging;

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
            activeCamera = Camera.main;
            if (board == null)
            {
                board = UnityEngine.Object.FindAnyObjectByType<PrototypeBattleBoard>();
            }
        }

        private void OnMouseDown()
        {
            activeCamera = Camera.main;
            if (activeCamera == null)
            {
                return;
            }

            if (TryGetPointerWorldPosition(out var worldPosition))
            {
                isDragging = true;
                dragOffset = transform.position - worldPosition;
            }
        }

        private void OnMouseDrag()
        {
            if (!isDragging || activeCamera == null || !TryGetPointerWorldPosition(out var worldPosition))
            {
                return;
            }

            var targetPosition = worldPosition + dragOffset;
            targetPosition.y = dragPlaneHeight;
            transform.position = targetPosition;
        }

        private void OnMouseUp()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            if (board != null)
            {
                transform.position = board.SnapToCell(transform.position);
            }
        }

        private bool TryGetPointerWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = default;
            var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, dragPlaneHeight, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            worldPosition = ray.GetPoint(enter);
            return true;
        }
    }
}


