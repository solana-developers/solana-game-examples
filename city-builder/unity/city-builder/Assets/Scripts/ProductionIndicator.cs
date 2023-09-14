using System;
using System.Collections;
using Lumberjack.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductionIndicator : MonoBehaviour
{
    public Image ProgressBar;
    public TextMeshProUGUI Level;
    public TextMeshProUGUI Name;
    public GameObject CollectionIndicator;
    
    private TileData CurrentTileData;
    private Coroutine _coroutine;

    public void SetData(TileData tileData)
    {
        Level.text = tileData.BuildingLevel.ToString();
        Name.text = LumberjackService.GetName(tileData);
        
        CurrentTileData = tileData;
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
        _coroutine = StartCoroutine(UpdateProgressBar());
    }

   /* private void Update()
    {
        if (CurrentTileData == null)
        {
            return;
        }
                    
        long unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        var tileDataBuildingStartCollectTime = CurrentTileData.BuildingStartCollectTime + new TimeSpan(0,0,1,0).TotalSeconds;
        //Debug.Log("Time: " + tileDataBuildingStartCollectTime + " current time " + unixTime + " diff " + (unixTime - tileDataBuildingStartCollectTime));
        
        //tileDataBuildingStartCollectTime < unixTime;
        var dataBuildingStartCollectTime = tileDataBuildingStartCollectTime - unixTime;
        ProgressBar.fillAmount = (float) (dataBuildingStartCollectTime / 60f);
    }*/
    
    private IEnumerator UpdateProgressBar()
    {
        while (true)
        {
            if (CurrentTileData == null)
            {
                continue;
            }
                    
            long unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            var tileDataBuildingStartCollectTime = CurrentTileData.BuildingStartCollectTime + new TimeSpan(0,0,1,0).TotalSeconds;
            //Debug.Log("Time: " + tileDataBuildingStartCollectTime + " current time " + unixTime + " diff " + (unixTime - tileDataBuildingStartCollectTime));
        
            //tileDataBuildingStartCollectTime < unixTime;
            var dataBuildingStartCollectTime = tileDataBuildingStartCollectTime - unixTime;
            ProgressBar.fillAmount = (float) (dataBuildingStartCollectTime / 60f);

            var isCollectable = LumberjackService.IsCollectable(CurrentTileData);
            CollectionIndicator.gameObject.SetActive(isCollectable);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
