using UnityEngine;

namespace Case4_Buca.Scripts
{
    public class MDisk : MonoBehaviour
    {
        public static MDisk Instance { get; private set; }

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
    }
}
