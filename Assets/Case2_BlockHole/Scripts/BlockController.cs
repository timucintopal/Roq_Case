using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class BlockController : MonoBehaviour
    {
        public Hole.HoleColor holeColor;
        
        [SerializeField] MeshRenderer mainRenderer;
        
        [SerializeField] List<GameObject> blocks = new List<GameObject>();

        public void MoveToHole(Transform target)
        {
            StartCoroutine(MoveSequence(target));
        }

        private Vector3 _originalScale;
        private Tween _scaleTween;
        private Material _outlineMaterial;

        private void Start()
        {
            _originalScale = transform.localScale;
            // Dinamik olarak Outline materyalini oluşturuyoruz
            _outlineMaterial = new Material(Shader.Find("Custom/SimpleOutline"));
            // Glow yapması için rengi şiddetlendiriyoruz (HDR Color - PostProcessing Bloom varsa parlaklık saçar)
            _outlineMaterial.SetColor("_OutlineColor", Color.white * 4f);
            _outlineMaterial.SetFloat("_OutlineWidth", 4f);
        }

        public void OnPickup()
        {
            _scaleTween?.Kill();
            // Sadece ilk tutulduğunda bir kere büyüyüp hafifçe küçülecek (tatlı bir pop efekti)
            _scaleTween = transform.DOScale(_originalScale * 1.15f, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(_originalScale * 1.05f, 0.15f).SetEase(Ease.InOutSine));

            if (mainRenderer != null)
            {
                // Mevcut materyallerin sonuna outline materyalini ekliyoruz
                var mats = mainRenderer.materials;
                var newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                newMats[mats.Length] = _outlineMaterial;
                mainRenderer.materials = newMats;
            }
        }

        public void OnDrop()
        {
            _scaleTween?.Kill();
            // Orijinal boyutuna tatlı bir şekilde geri dönsün
            transform.DOScale(_originalScale, 0.2f).SetEase(Ease.OutBack);

            if (mainRenderer != null)
            {
                // Outline materyalini listeden çıkarıyoruz
                var mats = mainRenderer.materials;
                if (mats.Length > 0 && mats[mats.Length - 1].shader.name == "Custom/SimpleOutline")
                {
                    var newMats = new Material[mats.Length - 1];
                    for (int i = 0; i < newMats.Length; i++) newMats[i] = mats[i];
                    mainRenderer.materials = newMats;
                }
            }
        }

        IEnumerator MoveSequence(Transform target )
        {
            yield return transform.DOMove(target.position, .4f).SetEase(Ease.OutBack).WaitForCompletion();

            yield return new WaitForSeconds(.1f);

            mainRenderer.enabled = false;
                
            foreach(var block in blocks)
                block.SetActive(true);
        }
        
        
    }
}