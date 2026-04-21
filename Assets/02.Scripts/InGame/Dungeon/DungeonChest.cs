using UnityEngine;

public class DungeonChest : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _openParticleRoot;

    private ChestLootTable _lootTable;
    private bool _opened;

    private static readonly int OpenHash = Animator.StringToHash("Open");

    public void Init(ChestLootTable lootTable)
    {
        _lootTable = lootTable;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_opened) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.IsMine) return;

        _opened = true;

        if (_animator != null)
            _animator.SetTrigger(OpenHash);

        PlayOpenEffects();
        GiveLoot(player);
    }

    private void PlayOpenEffects()
    {
        if (_openParticleRoot != null)
        {
            _openParticleRoot.SetActive(false);
            _openParticleRoot.SetActive(true);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(new SfxPlayRequest(
                clipKey: AssetKey.SFX.ChestOpen,
                spatialMode: ESpatialMode.Positional3D,
                position: transform.position));
        }
    }

    private void GiveLoot(PlayerController player)
    {
        if (_lootTable == null || _lootTable.Entries == null) return;

        var inventory = player.GetAbility<PlayerInventoryAbility>();
        if (inventory == null) return;

        foreach (var entry in _lootTable.Entries)
        {
            if (entry.Item == null) continue;
            inventory.AddItem(entry.Item, entry.Amount);
        }
    }
}