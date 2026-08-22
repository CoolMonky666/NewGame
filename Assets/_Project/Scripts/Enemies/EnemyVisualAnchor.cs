using UnityEngine;

namespace MergeDefense.Enemies
{
    public sealed class EnemyVisualAnchor : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool lockLocalPosition = true;
        [SerializeField] private bool lockLocalRotation = true;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        public Transform VisualRoot
        {
            get => visualRoot;
            set => visualRoot = value;
        }

        private void Awake()
        {
            if (visualRoot == null)
            {
                return;
            }

            initialLocalPosition = visualRoot.localPosition;
            initialLocalRotation = visualRoot.localRotation;
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (lockLocalPosition)
            {
                visualRoot.localPosition = initialLocalPosition;
            }

            if (lockLocalRotation)
            {
                visualRoot.localRotation = initialLocalRotation;
            }
        }
    }
}
