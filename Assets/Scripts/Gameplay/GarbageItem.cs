using UnityEngine;

public class GarbageItem : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private WasteCategory category;
    [SerializeField] private string wrongReason;

    private Rigidbody _rigidbody;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _isCompleted;
    private bool _isHeld;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public WasteCategory Category => category;
    public string WrongReason => wrongReason;
    public bool IsCompleted => _isCompleted;
    public bool IsHeld => _isHeld;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    public bool CanInteract()
    {
        return !_isCompleted && !_isHeld;
    }

    public void SetHeld(bool held)
    {
        _isHeld = held;
    }

    public void MarkCompleted()
    {
        _isCompleted = true;
        _isHeld = false;
    }

    public void ResetToStartPosition()
    {
        _isHeld = false;

        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(_startPosition, _startRotation);

        if (_rigidbody != null)
        {
            _rigidbody.Sleep();
        }
    }
}
