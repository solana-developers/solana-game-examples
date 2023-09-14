using System;
using System.Collections;
using System.Collections.Generic;
using Frictionless;
using SolPlay.Scripts.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SolPlay.Scripts.Services
{
    public class UiService : MonoBehaviour, IMultiSceneSingleton
    {
        [Serializable]
        public class UiRegistration
        {
            public BasePopup PopupPrefab;
            public ScreenType ScreenType;
        }
        
        public enum ScreenType
        {
            TransferNftPopup = 0,
            NftListPopup = 1,
            RefillEnergyPopup = 2,
            UpgradeBuildingPopup = 3,
            BuildBuildingPopup = 4,
            ChopTreePopup = 5,
        }

        public class UiData
        {
            
        }
        
        public List<UiRegistration> UiRegistrations = new List<UiRegistration>();
        
        private readonly Dictionary<ScreenType, BasePopup> openPopups = new Dictionary<ScreenType, BasePopup>();

        public void Awake()
        {
            ServiceFactory.RegisterSingleton(this);
        }

        public void OpenPopup(ScreenType screenType, UiData uiData)
        {
            if (openPopups.TryGetValue(screenType, out BasePopup basePopup))
            {
                basePopup.Open(uiData);
                return;
            }
            
            foreach (var uiRegistration in UiRegistrations)
            {
                if (uiRegistration.ScreenType == screenType)
                {
                    BasePopup newPopup = Instantiate(uiRegistration.PopupPrefab);
                    openPopups.Add(screenType, newPopup);
                    newPopup.Open(uiData);
                    return;
                }
            }
            
            Debug.LogWarning("There was no screen registration for " + screenType);
        }

        public static bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            openPopups.Clear();
            yield return null;
        }
    }
}