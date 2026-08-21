using DG.Tweening;
using UnityEngine;

namespace Case3_Stickerdom.Scripts
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class StickerController : MonoBehaviour
    {
        public StickerType stickerType;

        [Space, Header("References")]
        public Transform snapTarget;
        public Transform shadowTransform;

        [Header("Settings")]
        public float snapDistance = 1.5f;
        public float maxPeelAmount = 0.776f;
        public float peelDuration = 0.3f;

        private Material stickerMaterial;
        private Vector3 originalPosition;
    
        private int peelAmountPropId;
        private int peelDirPropId;

        private Vector3 dragOffset;
        private bool isDragging = false;
        private bool isPlaced = false;
        private Camera mainCam;
        private float originalZ;

        private Renderer activeRenderer;

        public bool IsPlaced => isPlaced;

        void Start()
        {

            peelAmountPropId = Shader.PropertyToID("_PeelAmount");
            peelDirPropId = Shader.PropertyToID("_PeelDirection");

            originalPosition = transform.position;
            originalZ = transform.position.z;
            mainCam = Camera.main;

            if (activeRenderer == null)
            {
                Renderer childRenderer = GetComponentInChildren<MeshRenderer>();
                if (childRenderer != null)
                {
                    SetActiveRenderer(childRenderer);
                }
                else
                {
                    SetActiveRenderer(GetComponent<SpriteRenderer>());
                }
            }
        }

        public void SetActiveRenderer(Renderer rend)
        {
            if (rend == null) return;
        
            activeRenderer = rend;
        
            if (activeRenderer.material != null)
            {
                stickerMaterial = new Material(activeRenderer.sharedMaterial);
                activeRenderer.material = stickerMaterial;
            
                if (peelAmountPropId != 0) 
                {
                    stickerMaterial.SetFloat(peelAmountPropId, 0f);
                }
            }
        }

        public void OnInputDown(Vector3 clickWorldPos)
        {
            if (isPlaced) return;

            Debug.Log("Tıklama Başarılı! Obje: " + gameObject.name);
            isDragging = true;
            dragOffset = transform.position - clickWorldPos;
            StartPeel(clickWorldPos);
        }

        public void OnInputDrag(Vector3 dragWorldPos)
        {
            if (!isDragging || isPlaced) return;

            transform.position = dragWorldPos + dragOffset;

            if (stickerMaterial != null)
            {
                float currentPeel = stickerMaterial.GetFloat(peelAmountPropId);
                if (currentPeel < maxPeelAmount)
                {
                    float newPeel = Mathf.Lerp(currentPeel, maxPeelAmount, Time.deltaTime * 5f);
                    stickerMaterial.SetFloat(peelAmountPropId, newPeel);
                }
            }
        }

        public void OnInputUp()
        {
            if (!isDragging || isPlaced) return;
        
            Debug.Log("Sürükleme Bırakıldı! Obje: " + gameObject.name);
            isDragging = false;
            EndPeel();
        }

        private void StartPeel(Vector3 clickWorldPos)
        {
            Vector3 clickLocalDir = transform.InverseTransformPoint(clickWorldPos).normalized;
            Vector2 peelDir = new Vector2(-clickLocalDir.x, -clickLocalDir.y);
        
            if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1); 
        
            if (stickerMaterial != null)
            {
                stickerMaterial.SetVector(peelDirPropId, peelDir);
                DOTween.Kill(stickerMaterial);
                stickerMaterial.DOFloat(0.2f, "_PeelAmount", peelDuration / 2f);
            }
        
            transform.position = new Vector3(transform.position.x, transform.position.y, originalZ - 1f);

            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(new Vector3(0.15f, -0.15f, 0.5f), peelDuration);
                SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowSR != null) shadowSR.DOFade(0.4f, peelDuration);
            }
        }

        private void EndPeel()
        {
            float distanceToTarget = snapTarget != null ? Vector2.Distance(transform.position, snapTarget.position) : float.MaxValue;

            if (stickerMaterial != null) DOTween.Kill(stickerMaterial);

            if (distanceToTarget <= snapDistance && snapTarget != null)
            {
                isPlaced = true;
                transform.DOMove(snapTarget.position, 0.2f).SetEase(Ease.OutBack);
            
                if (stickerMaterial != null)
                {
                    stickerMaterial.DOFloat(0f, "_PeelAmount", 0.2f).OnComplete(() => {
                        transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 5, 0.5f);
                        transform.position = new Vector3(transform.position.x, transform.position.y, snapTarget.position.z - 0.1f);
                    });
                }

                if (shadowTransform != null)
                {
                    shadowTransform.DOLocalMove(Vector3.zero, 0.2f);
                    SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                    if (shadowSR != null) shadowSR.DOFade(0f, 0.2f);
                }
            }
            else
            {
                transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutQuad);
                transform.DOMoveZ(originalZ, 0.3f);

                if (stickerMaterial != null)
                {
                    stickerMaterial.DOFloat(0f, "_PeelAmount", 0.3f);
                }
            
                if (shadowTransform != null)
                {
                    shadowTransform.DOLocalMove(Vector3.zero, 0.3f);
                    SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                    if (shadowSR != null) shadowSR.DOFade(0f, 0.3f);
                }
            }
        }
    }
}
