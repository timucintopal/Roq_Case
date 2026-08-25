using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Case4_Buca.Scripts
{
    public class MInput : MonoBehaviour
    {
        public static MInput Instance { get; private set; }

        public event Action<Vector2> OnPointerDownEvent;
        public event Action<Vector2> OnPointerDragEvent;
        public event Action<Vector2> OnPointerUpEvent;

        private bool isDragging = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Pointer.current == null) return;

            if (Pointer.current.press.wasPressedThisFrame)
            {
                isDragging = true;
                OnPointerDownEvent?.Invoke(Pointer.current.position.ReadValue());
            }
            else if (Pointer.current.press.isPressed && isDragging)
            {
                OnPointerDragEvent?.Invoke(Pointer.current.position.ReadValue());
            }
            else if (Pointer.current.press.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                OnPointerUpEvent?.Invoke(Pointer.current.position.ReadValue());
            }
        }
    }
}
