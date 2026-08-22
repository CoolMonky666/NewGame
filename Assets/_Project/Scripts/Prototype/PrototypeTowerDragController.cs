using UnityEngine;
using UnityEngine.InputSystem;

namespace MergeDefense.Prototype
{
    public sealed class PrototypeTowerDragController : MonoBehaviour
    {
        [SerializeField] private Camera battleCamera;
        [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

        private PrototypeTowerDraggable selectedTower;
        private Vector3 dragOffset;
        private float dragPlaneHeight;

        public void Configure(Camera camera)
        {
            battleCamera = camera;
        }

        private void Awake()
        {
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (battleCamera == null || !TryReadPointer(out var screenPosition, out var isPressed, out var pressedThisFrame, out var releasedThisFrame))
            {
                return;
            }

            if (pressedThisFrame)
            {
                TryStartDrag(screenPosition);
            }

            if (selectedTower != null && isPressed && TryProjectToDragPlane(screenPosition, dragPlaneHeight, out var worldPosition))
            {
                selectedTower.DragTo(worldPosition + dragOffset);
            }

            if (releasedThisFrame && selectedTower != null)
            {
                selectedTower.SnapToBoard();
                selectedTower = null;
            }
        }

        private bool TryStartDrag(Vector2 screenPosition)
        {
            var ray = battleCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 100f, raycastMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            var draggable = hit.collider.GetComponentInParent<PrototypeTowerDraggable>();
            if (draggable == null)
            {
                return false;
            }

            dragPlaneHeight = draggable.DragPlaneHeight;
            if (!TryProjectToDragPlane(screenPosition, dragPlaneHeight, out var worldPosition))
            {
                return false;
            }

            selectedTower = draggable;
            dragOffset = draggable.transform.position - worldPosition;
            return true;
        }

        private bool TryProjectToDragPlane(Vector2 screenPosition, float planeHeight, out Vector3 worldPosition)
        {
            worldPosition = default;
            var ray = battleCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            worldPosition = ray.GetPoint(enter);
            return true;
        }

        private static bool TryReadPointer(out Vector2 screenPosition, out bool isPressed, out bool pressedThisFrame, out bool releasedThisFrame)
        {
            if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame))
            {
                screenPosition = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                return true;
            }

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    isPressed = touch.press.isPressed;
                    pressedThisFrame = touch.press.wasPressedThisFrame;
                    releasedThisFrame = touch.press.wasReleasedThisFrame;
                    return true;
                }
            }

            screenPosition = default;
            isPressed = false;
            pressedThisFrame = false;
            releasedThisFrame = false;
            return false;
        }
    }
}
