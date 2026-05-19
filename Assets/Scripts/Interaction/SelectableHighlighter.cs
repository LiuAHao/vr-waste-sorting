using UnityEngine;

namespace ParkClean.Interaction
{
    public class SelectableHighlighter : MonoBehaviour
    {
        [SerializeField] private Color highlightColor = Color.yellow;
        private Color originalColor;
        private MeshRenderer meshRenderer;

        void Start()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null) originalColor = meshRenderer.material.color;
        }

        public void SetHighlight(bool status)
        {
            if (meshRenderer == null) return;
            meshRenderer.material.color = status ? highlightColor : originalColor;
        }
    }
}