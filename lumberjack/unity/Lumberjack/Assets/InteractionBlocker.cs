using UnityEngine;

public class InteractionBlocker : MonoBehaviour
{
    public GameObject BlockingSpinner;
    public GameObject NonBlocking;
    
    void Update()
    {
        BlockingSpinner.gameObject.SetActive(AnchorService.Instance.IsAnyBlockingTransactionInProgress);
        NonBlocking.gameObject.SetActive(AnchorService.Instance.IsAnyNonBlockingTransactionInProgress);
    }
}
