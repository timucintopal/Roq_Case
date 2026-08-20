using UnityEngine;
using UnityEngine.InputSystem;
// Yeni Input sistemi kütüphanesi

namespace Case1_FitTheShape.Scripts
{
    public class MInput : MonoBehaviour
    {
        [Tooltip("Inspector'dan tıklanabilir objelerin layer'ını (örn: Shape) seçin.")]
        [SerializeField] private LayerMask shapeLayer;

        void Update()
        {
            // Pointer (Fare veya Dokunmatik) algılayıcısı var mı ve bu karede basıldı mı?
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                Debug.Log("Pressed Input");
                // Yeni sisteme göre ekran pozisyonunu al (Mouse Position yerine)
                Vector2 screenPosition = Pointer.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(screenPosition);
                RaycastHit hit;

                // Işını fırlat (Maksimum 100 birim uzağa, SADECE shapeLayer maskesine sahip objelere çarpar)
                if (Physics.Raycast(ray, out hit, 1000f, shapeLayer))
                {
                    Debug.Log("Pressed Input " + hit.collider.name);
                    // Çarptığı objede ShapeController scripti var mı diye kontrol et
                    var rb = hit.collider.attachedRigidbody;
                    if (rb == null) return;
                    ShapeController shape = rb.GetComponent<ShapeController>();
                    if (shape != null)
                    {
                        shape.JumpToTarget();
                    }
                }
            }
        }
    }
}
