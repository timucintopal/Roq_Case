using UnityEngine;

public class MGame : MonoBehaviour
{
    private void Awake()
    {
        // Arcade tarzı oyunlar için yerçekimini artırarak "Ay'da yürüme" hissini yok ediyoruz.
        Physics.gravity = new Vector3(0, -30f, 0);
    }
}
