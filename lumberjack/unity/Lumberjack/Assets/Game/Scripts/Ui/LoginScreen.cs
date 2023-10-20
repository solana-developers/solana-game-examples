using Lumberjack.Accounts;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the connection to the players wallet.
/// </summary>
public class LoginScreen : MonoBehaviour
{
    public Button LoginButton;
    public Button LoginWalletAdapterButton;
    
    public GameObject LoggedInRoot;
    public GameObject NotLoggedInRoot;
    
    void Start()
    {
        LoggedInRoot.SetActive(false);
        NotLoggedInRoot.SetActive(true);
        
        LoginButton.onClick.AddListener(OnEditorLoginClicked);
        LoginWalletAdapterButton.onClick.AddListener(OnLoginWalletAdapterButtonClicked);
        AnchorService.OnPlayerDataChanged += OnPlayerDataChanged;
        AnchorService.OnInitialDataLoaded += UpdateContent;
    }

    private async void OnLoginWalletAdapterButtonClicked()
    {
        await Web3.Instance.LoginWalletAdapter();
    }

    private void OnPlayerDataChanged(PlayerData playerData)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        LoggedInRoot.SetActive(Web3.Account != null);
        NotLoggedInRoot.SetActive(Web3.Account == null);
    }

    private async void OnEditorLoginClicked()
    {
        var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);

        // Dont use this one for production. Its only ment for editor login
        var account = await Web3.Instance.LoginInGameWallet("1234") ??
                      await Web3.Instance.CreateAccount(newMnemonic.ToString(), "1234");
    }
}
