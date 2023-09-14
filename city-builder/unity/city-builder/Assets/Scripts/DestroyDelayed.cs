using System;
using System.Collections;
using UnityEngine;

namespace DefaultNamespace
{
    public class DestroyDelayed : MonoBehaviour
    {
        private void Awake()
        {
            StartCoroutine(DestroyGoDelayed());
        }
        
        private IEnumerator DestroyGoDelayed()
        {
            yield return new WaitForSeconds(5);
            Destroy(gameObject);
        }
    }
}