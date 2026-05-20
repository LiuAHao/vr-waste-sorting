using UnityEngine;

public class Garbage : MonoBehaviour
{
    private GarbageItem _garbageItem;

    private void Awake()
    {
        _garbageItem = GetComponent<GarbageItem>();
        if (_garbageItem == null)
        {
            _garbageItem = GetComponentInParent<GarbageItem>();
        }

        if (_garbageItem == null)
        {
            _garbageItem = GetComponentInChildren<GarbageItem>();
        }
    }

    private void OnMouseDown()
    {
        if (_garbageItem != null)
        {
            return;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
