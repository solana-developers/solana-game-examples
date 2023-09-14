using System.Collections;
using UnityEngine;

public class CollectionIndicator : MonoBehaviour
{
    public Animator Animator;
    
    void OnEnable()
    {
        Animator.enabled = false;
        StartCoroutine(StartDelayed());
    }

    private IEnumerator StartDelayed()
    {
        yield  return new WaitForSeconds(Random.Range(0, 4));
        Animator.enabled = true;
    }
}
