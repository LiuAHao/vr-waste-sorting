using UnityEngine;

namespace ParkClean.Interaction
{
    public class DesktopGarbageInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Player player;
        [SerializeField] private float interactDistance = 4f;
        [SerializeField] private LayerMask interactMask = Physics.DefaultRaycastLayers;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private float followSpeed = 15f;
        [SerializeField] private LineRenderer rayLine;

        private GarbageItem _currentHover;
        private SelectableHighlighter _currentHighlighter;
        private GarbageItem _heldItem;
        private Rigidbody _heldRigidbody;

        public void Configure(Camera cameraRef, Player playerRef, Transform holdPointRef, LineRenderer lineRendererRef)
        {
            playerCamera = cameraRef;
            player = playerRef;
            holdPoint = holdPointRef;
            rayLine = lineRendererRef;
        }

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<Camera>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (player == null)
            {
                player = FindObjectOfType<Player>();
            }

            if (rayLine != null)
            {
                rayLine.positionCount = 2;
                rayLine.startWidth = 0.01f;
                rayLine.endWidth = 0.005f;
                rayLine.useWorldSpace = true;
            }
        }

        private void Update()
        {
            if (!CanProcessInteraction())
            {
                if (_heldItem != null)
                {
                    Release();
                }

                ClearHover();
                UpdateVisualRay(false);
                return;
            }

            UpdateVisualRay(true);

            if (_heldItem == null)
            {
                UpdateSelection();
                if (Input.GetMouseButtonDown(0))
                {
                    TryGrab();
                }
            }
            else
            {
                UpdateHeldItem();
                if (Input.GetMouseButtonUp(0))
                {
                    Release();
                }
            }
        }

        private bool CanProcessInteraction()
        {
            return playerCamera != null
                && holdPoint != null
                && player != null
                && player.InputEnabled;
        }

        private void UpdateVisualRay(bool active)
        {
            if (rayLine == null || playerCamera == null)
            {
                return;
            }

            rayLine.enabled = active;
            if (!active)
            {
                return;
            }

            Vector3 startPos = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.8f));
            Vector3 endPos = startPos + playerCamera.transform.forward * interactDistance;
            rayLine.SetPosition(0, startPos);
            rayLine.SetPosition(1, endPos);
        }

        private void UpdateSelection()
        {
            GarbageItem nextHover = null;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            {
                GarbageItem item = ResolveGarbageItem(hit.collider);
                if (item != null && item.CanInteract())
                {
                    nextHover = item;
                }
            }

            if (_currentHover == nextHover)
            {
                return;
            }

            ClearHover();
            if (nextHover == null)
            {
                return;
            }

            _currentHover = nextHover;
            _currentHighlighter = nextHover.GetComponent<SelectableHighlighter>();
            if (_currentHighlighter == null)
            {
                _currentHighlighter = nextHover.gameObject.AddComponent<SelectableHighlighter>();
            }

            _currentHighlighter.SetHighlight(true);
        }

        private void TryGrab()
        {
            if (_currentHover == null || holdPoint == null)
            {
                return;
            }

            _heldItem = _currentHover;
            _heldRigidbody = _heldItem.GetComponent<Rigidbody>();
            if (_heldRigidbody != null)
            {
                _heldRigidbody.velocity = Vector3.zero;
                _heldRigidbody.angularVelocity = Vector3.zero;
                _heldRigidbody.isKinematic = true;
                _heldRigidbody.useGravity = false;
            }

            _heldItem.SetHeld(true);
            if (_currentHighlighter != null)
            {
                _currentHighlighter.SetHighlight(false);
            }

            _currentHover = null;
            _currentHighlighter = null;
        }

        private void UpdateHeldItem()
        {
            if (_heldItem == null || holdPoint == null)
            {
                return;
            }

            _heldItem.transform.position = Vector3.Lerp(
                _heldItem.transform.position,
                holdPoint.position,
                Time.deltaTime * followSpeed);
        }

        private void Release()
        {
            if (_heldItem == null)
            {
                return;
            }

            _heldItem.SetHeld(false);
            if (_heldRigidbody != null)
            {
                _heldRigidbody.isKinematic = false;
                _heldRigidbody.useGravity = true;
                _heldRigidbody.velocity = Vector3.zero;
                _heldRigidbody.angularVelocity = Vector3.zero;
            }

            _heldItem = null;
            _heldRigidbody = null;
        }

        private void ClearHover()
        {
            if (_currentHighlighter != null)
            {
                _currentHighlighter.SetHighlight(false);
            }

            _currentHover = null;
            _currentHighlighter = null;
        }

        private static GarbageItem ResolveGarbageItem(Collider other)
        {
            if (other == null)
            {
                return null;
            }

            GarbageItem item = other.GetComponent<GarbageItem>();
            if (item != null)
            {
                return item;
            }

            if (other.attachedRigidbody != null)
            {
                item = other.attachedRigidbody.GetComponent<GarbageItem>();
                if (item != null)
                {
                    return item;
                }
            }

            item = other.GetComponentInParent<GarbageItem>();
            if (item != null)
            {
                return item;
            }

            return other.GetComponentInChildren<GarbageItem>();
        }
    }
}
