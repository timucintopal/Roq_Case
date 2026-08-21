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