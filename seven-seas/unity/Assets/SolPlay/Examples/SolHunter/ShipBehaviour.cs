using System.Collections;
using DG.Tweening;
using Frictionless;
using NUnit.Framework.Constraints;
using SevenSeas.Types;
using Solana.Unity.Wallet;
using SolPlay.Scripts;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipBehaviour : MonoBehaviour
{
    public GameObject ShotPrefab;
    
    public Vector3 TargetPosition;
    public Vector2 GridPosition;
    public Vector2 LastGridPosition;
    public Vector3 LastPosition;
    public Vector3 UpVector = Vector3.left;
    public Animator Animator;
    public HealthBar HealthBar;
    public TextMeshProUGUI PublicKey;
    public RawImage Avatar;
    public GameObject RotationRoot;
    public Tile currentTile;
    
    public void Init(Vector2 startPosition, Tile tile)
    {
        currentTile = tile;
        transform.position = new Vector3(10 * startPosition.x + 5f, 1.4f, (10 * startPosition.y) - 5f);
        TargetPosition = transform.position;
        GridPosition = startPosition;
        LastGridPosition = startPosition;
        HealthBar.SetHealth(tile.Health, SolHunterService.GetMaxHealthByLevel(tile));
        switch (tile.LookDirection)
        {
            case 0:
                RotationRoot.transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case 1:
                RotationRoot.transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
            case 2:
                RotationRoot.transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case 3:
                RotationRoot.transform.rotation = Quaternion.Euler(0, 270, 0);
                break;
        }
    }

    private void Update()
    {
        var transformPosition = transform.position - LastPosition;
        if (transformPosition.magnitude > 0.03f)
        {
           RotationRoot.transform.rotation = Quaternion.LookRotation(transformPosition, UpVector);   
        }
        else
        {
            switch (currentTile.LookDirection)
            {
                case 0:
                    RotationRoot.transform.rotation = Quaternion.Euler(0, 0, 0);
                    break;
                case 1:
                    RotationRoot.transform.rotation = Quaternion.Euler(0, 90, 0);
                    break;
                case 2:
                    RotationRoot.transform.rotation = Quaternion.Euler(0, 180, 0);
                    break;
                case 3:
                    RotationRoot.transform.rotation = Quaternion.Euler(0, 270, 0);
                    break;
            }
        }
        LastPosition = transform.position;
    }
    
    public void SetNewTargetPosition(Vector2 newPosition, Tile tile)
    {
        currentTile = tile;
        HealthBar.SetHealth(tile.Health, SolHunterService.GetMaxHealthByLevel(tile));
        PublicKey.text = tile.Player.ToString();
        SetNftAvatar(tile.Avatar);
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

    private async void SetNftAvatar(PublicKey avatarPublicKey)
    {
        var avatarNft = ServiceFactory.Resolve<NftService>().GetNftByMintAddress(avatarPublicKey);
        var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;

        if (avatarNft == null)
        {
            avatarNft = SolPlayNft.TryLoadNftFromLocal(avatarPublicKey);
        }

        if (avatarNft == null)
        {
            avatarNft = new SolPlayNft();
            await avatarNft.LoadData(avatarPublicKey, wallet.ActiveRpcClient);
            if (avatarNft.LoadingImageTask != null)
            {
                await avatarNft.LoadingImageTask;
                Avatar.texture = avatarNft.MetaplexData.nftImage.file;
            }
        }

        Avatar.gameObject.SetActive(true);
    }

    public void Shoot()
    {
        var shootInstance = Instantiate(ShotPrefab, RotationRoot.transform);
        StartCoroutine(KillDelayed(shootInstance));
    }

    private IEnumerator KillDelayed(GameObject shootInstance)
    {
        yield return new WaitForSeconds(2);
        Destroy(shootInstance);
    }
}