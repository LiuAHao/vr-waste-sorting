using UnityEngine;

namespace ParkClean.Interaction
{
    public class CrosshairController : MonoBehaviour
    {
        [SerializeField] private float size = 10f;
        [SerializeField] private Color color = Color.white;

        void OnGUI()
        {
            float posX = (Screen.width - size) / 2;
            float posY = (Screen.height - size) / 2;
            GUI.color = color;
            GUI.Box(new Rect(posX, posY, size, size), "");
        }
    }
}