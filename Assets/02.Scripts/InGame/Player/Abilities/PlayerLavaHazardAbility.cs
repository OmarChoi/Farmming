using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class PlayerLavaHazardAbility : PlayerAbility
{
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpDuration = 0.5f;
    [SerializeField] private int _searchRadius = 5;
    [SerializeField] private float _lavaStaminaCost = 10f;
    [SerializeField, Min(0f)] private float _steamSfxCooldown = 1f;

    private CharacterController _cc;
    private PlayerAnimationAbility _animation;
    private PlayerStaminaAbility _stamina;
    private bool _isJumping;
    private float _lastSteamSfxTime = float.MinValue;

    private void Start()
    {
        _cc = _owner.GetComponent<CharacterController>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
        _stamina = _owner.GetAbility<PlayerStaminaAbility>();
    }

    private void Update()
    {
        if (!_owner.IsMine)
        {
            return;
        }

        if (_isJumping)
        {
            return;
        }

        if (_cc == null || !_cc.enabled || !_cc.isGrounded)
        {
            return;
        }

        CheckGround();
    }

    private void CheckGround()
    {
        var gridManager = TerrainGridManager.Instance;
        if (gridManager == null) return;

        Vector3Int gridPos = gridManager.WorldToGrid(_owner.transform.position);
        Vector3Int contactGrid = gridPos;

        // 발밑 셀 확인 (현재 위치 또는 한 칸 아래)
        var cell = gridManager.GetGridData().GetCell(gridPos);
        if (cell == null)
        {
            contactGrid = gridPos + Vector3Int.down;
            cell = gridManager.GetGridData().GetCell(contactGrid);
        }

        if (cell == null) return;
        if (cell.TileType != ETileType.Dungeon3Lava) return;

        OnLavaContact(contactGrid).Forget();
    }

    private async UniTaskVoid OnLavaContact(Vector3Int currentGrid)
    {
        _isJumping = true;
        _owner.LockAction();
        _stamina?.Consume(_lavaStaminaCost);
        _animation?.PlayLavaHit();
        PlaySteamSfx(currentGrid);

        Vector3? safePos = FindNearestSafeTile(currentGrid);
        if (safePos.HasValue)
            await JumpTo(safePos.Value);

        _owner.UnlockAction();
        _isJumping = false;
    }

    private Vector3? FindNearestSafeTile(Vector3Int center)
    {
        var gridManager = TerrainGridManager.Instance;
        var gridData = gridManager.GetGridData();

        for (int radius = 1; radius <= _searchRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                        continue;

                    // 같은 높이 → 아래로 탐색하며 IsTop 셀 찾기
                    for (int dy = 0; dy >= -3; dy--)
                    {
                        var pos = center + new Vector3Int(dx, dy, dz);
                        var cell = gridData.GetCell(pos);
                        if (cell == null) continue;
                        if (!cell.IsTop) continue;
                        if (cell.TileType == ETileType.Dungeon3Lava) continue;

                        return gridManager.GridToWorld(pos + Vector3Int.up);
                    }
                }
            }
        }

        return null;
    }

    private async UniTask JumpTo(Vector3 target)
    {
        if (_cc != null) _cc.enabled = false;

        Vector3 start = _owner.transform.position;

        // 타겟 방향으로 회전
        Vector3 dir = start - target;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            _owner.transform.rotation = Quaternion.LookRotation(dir);

        float elapsed = 0f;
        while (elapsed < _jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _jumpDuration);

            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * _jumpHeight;

            _owner.transform.position = pos;
            await UniTask.Yield();
        }

        _owner.transform.position = target;

        if (_cc != null) _cc.enabled = true;
    }

    private void PlaySteamSfx(Vector3Int lavaGrid)
    {
        if (Time.time - _lastSteamSfxTime < _steamSfxCooldown)
            return;

        _lastSteamSfxTime = Time.time;

        TerrainGridManager gridManager = TerrainGridManager.Instance;
        Vector3 position = gridManager != null
            ? gridManager.GridToWorld(lavaGrid) + Vector3.up * (gridManager.CellSize * 0.5f)
            : _owner.transform.position;

        PlaySteamSfxAt(position);

        if (_owner.PhotonView != null && PhotonNetwork.IsConnected)
        {
            _owner.PhotonView.RpcSafe(
                nameof(PlayerController.RPC_PlayLavaSteamSfx),
                RpcTarget.Others,
                position);
        }
    }

    private static void PlaySteamSfxAt(Vector3 position)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.Steam,
            spatialMode: ESpatialMode.Positional3D,
            position: position));
    }
}
