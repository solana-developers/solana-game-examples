using System.Collections;
using System.Drawing;
using DG.Tweening;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public GameObject ShotPrefab;
    
    public Vector3 TargetPosition;
    public Vector2 GridPosition;
    public Vector2 LastGridPosition;
    public Vector3 LastPosition;
    public Vector3 UpVector = Vector3.left;
    public Animator Animator;
    
    public void Init(Vector2 startPosition)
    {
        transform.position = new Vector3(10 * startPosition.x + 5f, 1.4f, (10 * startPosition.y) - 5f);
        TargetPosition = transform.position;
        GridPosition = startPosition;
        LastGridPosition = startPosition;
    }

    private void Update()
    {
        var transformPosition = transform.position - LastPosition;
        if (transformPosition.magnitude > 0.03f)
        {
            transform.rotation = Quaternion.LookRotation(transformPosition, UpVector);   
        }
        LastPosition = transform.position;
    }
    
    public void SetNewTargetPosition(Vector2 newPosition)
    {
        TargetPosition = new Vector3((10 * newPosition.x) + 5f, 1.4f, (10 * newPosition.y) - 5f);
        
        if ((newPosition - LastGridPosition).magnitude  > 3)
        {
            transform.DOKill();
            transform.position = new Vector3(10 * newPosition.x + 5f, 1.4f, (10 * newPosition.y) - 5f);
            LastPosition = transform.position;
        }
        else
        {
            transform.DOMove(TargetPosition, 1.5f);
        }

        LastGridPosition = newPosition;
        GridPosition = newPosition;
    }

    public void Shoot()
    {
        var shootInstance = Instantiate(ShotPrefab, transform);
        StartCoroutine(KillDelayed(shootInstance));
    }

    private IEnumerator KillDelayed(GameObject shootInstance)
    {
        yield return new WaitForSeconds(2);
        Destroy(shootInstance);
    }
}