using UnityEngine;

public class RandomRotation : MonoBehaviour
{
    
    void Start()
    {
        transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);    
    }

}
