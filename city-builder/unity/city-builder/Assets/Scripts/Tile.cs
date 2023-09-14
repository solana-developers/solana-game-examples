using System;
using System.Collections;
using DefaultNamespace;
using DG.Tweening;
using Lumberjack.Types;
using Unity.VisualScripting;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool IsLocked;
    public TileConfig currentConfig;
    public TileData currentTileData;
    public AudioClip MergeClip;
    public AudioSource MergeAudioSource;

    public Cell Cell;
    private Vector3 originalScale;
    private GameObject Model;
    private CollectionIndicator _collectionIndicator;
    private ProductionIndicator _productionIndicator;

    private void Awake()
    {
        var cachedTransform = transform;
        originalScale = cachedTransform.localScale;
        cachedTransform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.15f);
    }
    
    public void Init(TileConfig config, TileData tileData, bool updateVisuals = true)
    {
        currentConfig = config;
        currentTileData = tileData;

        if (updateVisuals)
        {
            UpdateVisualState();
        }
    }
    
    public void UpdateVisualState() 
    {
        if (Model == null || Model.name != currentConfig.Prefab.name)
        {
            Destroy(Model);
            Model = Instantiate(currentConfig.Prefab, transform);
            Model.name = currentConfig.Prefab.name;
        }
        StartCoroutine(DoNextFrame());
    }

    private IEnumerator DoNextFrame()
    {
        yield return null;
        yield return null;
        _productionIndicator = GetComponentInChildren<ProductionIndicator>(true);
        if (_productionIndicator != null)
        {
            _productionIndicator.SetData(currentTileData);
        }
        var healthbar = GetComponentInChildren<HealthBar>(true);
        if (healthbar != null)
        {
            healthbar.SetData((int) currentTileData.BuildingHealth, 9000);
        }
    }

    public void Spawn(Cell cell)
    {
        if (Cell != null) {
            Cell.Tile = null;
        }

        Cell = cell;
        Cell.Tile = this;

        transform.position = cell.transform.position;
    }

    public void PlaySpawnSound(AudioClip mergeClip = null)
    {
        MergeAudioSource.pitch = 1 + 0.06f * (currentConfig.Index);
        if (mergeClip != null)
        {
            MergeAudioSource.PlayOneShot(mergeClip);  
        }
        else
        {
            MergeAudioSource.PlayOneShot(MergeClip);
        }
    }
}
