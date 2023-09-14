using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    public class BasePopup : MonoBehaviour
    {
        public GameObject Root;
        public Button CloseButton;

        protected UiService.UiData uiData;

        protected void Awake()
        {
            Root.gameObject.SetActive(false);
        }

        public virtual void Open(UiService.UiData uiData)
        {
            this.uiData = uiData;
            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveAllListeners();
                CloseButton.onClick.AddListener(OnCloseButtonClicked);
            }

            Root.gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            Root.gameObject.SetActive(false);
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }
    }
}