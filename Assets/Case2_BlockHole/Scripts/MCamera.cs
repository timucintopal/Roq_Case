using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class MCamera : MonoBehaviour
    {
        public static MCamera Instance;

        private Camera _cam;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _cam = GetComponent<Camera>();
                if (_cam == null) _cam = Camera.main; // Eğer boş objeye atıldıysa Main Camera'yı bul
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShakeCamera(float strength, float duration = 0.25f)
        {
            if (_cam == null || strength <= 0f) return;

            _cam.transform.DOComplete();
            _cam.transform.DOShakePosition(duration, strength, 15, 90f, false, true);
        }
    }
}
