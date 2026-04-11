using System.Collections;
using Photon.Pun;
using UnityEngine;

public class LavaTileTransition : MonoBehaviour
{
    [SerializeField] private float _transitionDuration = 2f;
    [SerializeField] private Material _blendMaterial;
    private Material _instanceMat;
    private static readonly int BlendFactorID = Shader.PropertyToID("_BlendFactor");

    private void Awake()
    {
        _instanceMat = new Material(_blendMaterial);
    }

    public void StartTransition(TerrainCell cell)
    {
        StartCoroutine(TransitionCoroutine(cell));
    }

    private IEnumerator TransitionCoroutine(TerrainCell cell)
    {
        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            ApplyStoneCell(cell);
            yield break;
        }

        foreach (var r in renderers)
            r.material = _instanceMat;

        float elapsed = 0f;
        while (elapsed < _transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _transitionDuration);
            _instanceMat.SetFloat(BlendFactorID, t);
            yield return null;
        }

        ApplyStoneCell(cell);
        Destroy(_instanceMat);
    }

    private void ApplyStoneCell(TerrainCell cell)
    {
        TerrainGridManager.Instance.SetCell(cell.GridPosition,
            new TerrainCellData(
                cell.Data.CellType,
                ETileType.Dungeon3LavaStone,
                cell.Data.DirtLevel,
                cell.Data.ObjectType,
                cell.Data.ObjectLevel,
                cell.Data.IsIndestructible,
                cell.Data.IsTop
            ));

        if (PhotonNetwork.IsMasterClient)
            MapSyncManager.Instance?.BroadcastTerrainCellStateFromMaster(cell.GridPosition);
    }
}
