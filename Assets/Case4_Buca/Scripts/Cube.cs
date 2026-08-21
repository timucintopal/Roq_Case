using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Case4_Buca.Scripts
{
    public class Cube : MonoBehaviour
    {
        [SerializeField] Renderer _renderer;
        [SerializeField] Rigidbody _rigidbody;
        
        [SerializeField] LayerMask triggerLayers;

        [Space]
        [SerializeField] Color targetColor;
        [SerializeField] float coloringDuration;

        [SerializeField] bool isHit = false;

        [ContextMenu("SET")]
        private void Set()
        {
            _renderer = GetComponent<Renderer>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (isHit) return;

            if ((triggerLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                isHit = true;
                _renderer.material.DOColor(targetColor, coloringDuration);
            }
        }

        IEnumerator CheckVelocity()
        {
            yield return new WaitForSeconds(1);
            
            yield return new WaitUntil(() => _rigidbody.linearVelocity.sqrMagnitude <= 0.05f);
        }
    }
}
