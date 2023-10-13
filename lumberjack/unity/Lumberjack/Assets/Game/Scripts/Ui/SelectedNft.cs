using Frictionless;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using UnityEngine;

public class SelectedNft : MonoBehaviour
{
    public NftItemView NftItemView;

    private void Awake()
    {
        NftItemView.gameObject.SetActive(false);
    }

    void Start()
    {
        MessageRouter.AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
        UpdateContent();
    }

    private void OnNftSelectedMessage(NftSelectedMessage message)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        var nftService = ServiceFactory.Resolve<NftService>();
        if (nftService != null && nftService.SelectedNft != null)
        {
            NftItemView.gameObject.SetActive(true);
            NftItemView.SetData(nftService.SelectedNft, view => { Debug.Log("Selected Nft clicked."); });
        }
        else
        {
            NftItemView.gameObject.SetActive(false);
        }
    }
}
