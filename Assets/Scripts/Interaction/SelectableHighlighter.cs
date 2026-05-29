using UnityEngine;

namespace ParkClean.Interaction
{
    public class SelectableHighlighter : MonoBehaviour
    {
        [SerializeField] private Color highlightColor = Color.yellow;

        private MeshRenderer _meshRenderer;
        private Color _originalColor;
        private bool _initialized;

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_meshRenderer != null)
            {
                _originalColor = _meshRenderer.material.color;
                _initialized = true;
            }
        }

        public void SetHighlight(bool status)
        {
            if (!_initialized || _meshRenderer == null)
            {
                return;
            }

            _meshRenderer.material.color = status ? highlightColor : _originalColor;
        }

        private void OnDisable()
        {
            if (_initialized && _meshRenderer != null)
            {
                _meshRenderer.material.color = _originalColor;
            }
        }
    }
}
