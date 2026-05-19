using UnityEngine;

public class Garbage : MonoBehaviour
{
    private void OnMouseDown()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}