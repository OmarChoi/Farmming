using System;
using System.Collections;
using System.Collections.Generic;
using static CultivateAbility;

public sealed class SowCultivationService
{
    private readonly HelperController _owner;
    private readonly CultivateAbility _cultivateAbility;
    private readonly SowTargetResolver _targetResolver;
    private readonly int _cultivateExperience;
    private readonly float _epicThreeTileLookDuration;
    private readonly Func<IEnumerator> _replayEpicSowAndWait;
    private readonly Action<IEnumerator> _startCoroutine;

    public SowCultivationService(
        HelperController owner,
        CultivateAbility cultivateAbility,
        SowTargetResolver targetResolver,
        int cultivateExperience,
        float epicThreeTileLookDuration,
        Func<IEnumerator> replayEpicSowAndWait,
        Action<IEnumerator> startCoroutine)
    {
        _owner = owner;
        _cultivateAbility = cultivateAbility;
        _targetResolver = targetResolver;
        _cultivateExperience = cultivateExperience;
        _epicThreeTileLookDuration = epicThreeTileLookDuration;
        _replayEpicSowAndWait = replayEpicSowAndWait;
        _startCoroutine = startCoroutine;
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        return _targetResolver.CanCultivate(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }

        switch (_owner.Grade.CurrentGrade)
        {
            case EHelperGrade.Epic:
                HandleEpicCultivation(cell);
                break;
            case EHelperGrade.Legendary:
                HandleLegendaryCultivation(cell);
                break;
            default:
                HandleNormalCultivation(cell);
                break;
        }
    }

    private void HandleNormalCultivation(TerrainCell cell)
    {
        if (_targetResolver.NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ConvertLateralFarmTiles(cell);
                }
            });

            return;
        }

        FarmTile farmTile = _targetResolver.GetFarmTile(cell);
        if (farmTile == null)
        {
            return;
        }

        _owner.BeginAction();

        if (!farmTile.HasSeed)
        {
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () => ConvertLateralFarmTiles(cell)
            });
            return;
        }

        _cultivateAbility.JumpAndCultivate(cell, null);
    }

    private void HandleEpicCultivation(TerrainCell cell)
    {
        Action onEpicLeft = () =>
        {
            _targetResolver.TryConvertLateralCell(cell, -1);
            ReplayEpicSow();
        };
        Action onEpicRight = () =>
        {
            _targetResolver.TryConvertLateralCell(cell, +1);
        };

        if (cell.CurrentObject != null)
        {
            TerrainCell leftCell = _targetResolver.GetLateralCell(cell, -1);
            TerrainCell rightCell = _targetResolver.GetLateralCell(cell, +1);

            bool leftBlocked = leftCell == null || leftCell.CurrentObject != null;
            bool rightBlocked = rightCell == null || rightCell.CurrentObject != null;

            if (leftBlocked && rightBlocked)
            {
                return;
            }

            if (leftBlocked)
            {
                CultivateSingleCellWithEpicReplay(rightCell);
                return;
            }

            if (rightBlocked)
            {
                CultivateSingleCellWithEpicReplay(leftCell);
                return;
            }

            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(leftCell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    leftCell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ReplayEpicSow();
                },
                EpicLook = true,
                EpicLookLeft = false,
                EpicLookRight = true,
                EpicRightLookDuration = _epicThreeTileLookDuration,
                OnEpicLookRightMid = () =>
                {
                    _targetResolver.TryConvertLateralCell(leftCell, +1);
                    ReplayEpicSow();
                },
                OnEpicLookRight = () => _targetResolver.TryConvertLateralCell(leftCell, +2),
                OnEpicLookRightRoutine = _replayEpicSowAndWait
            });
            return;
        }

        if (_targetResolver.NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ReplayEpicSow();
                },
                EpicLook = true,
                OnEpicLookLeft = onEpicLeft,
                OnEpicLookRight = onEpicRight,
                OnEpicLookRightRoutine = _replayEpicSowAndWait
            });
            return;
        }

        FarmTile farmTile = _targetResolver.GetFarmTile(cell);
        if (farmTile == null) return;

        _owner.BeginAction();

        _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
        {
            EpicLook = true,
            OnEpicLookLeft = onEpicLeft,
            OnEpicLookRight = onEpicRight,
            OnEpicLookRightRoutine = _replayEpicSowAndWait
        });
    }

    private void HandleLegendaryCultivation(TerrainCell cell)
    {
        if (cell.CurrentObject != null && cell.Data.ObjectType == EGridObjectType.Tree)
            return;

        if (_targetResolver.NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ConvertLateralFarmTiles(cell);
                },
                Spin = true
            });
            return;
        }

        FarmTile farmTile = _targetResolver.GetFarmTile(cell);
        if (farmTile == null) return;

        List<TerrainCell> lateralCells = _targetResolver.GetLateralCells(cell);
        bool anyLateralToConvert = lateralCells.Exists(_targetResolver.NeedsFarmConversion);

        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
        {
            OnCultivate = anyLateralToConvert ? () =>
            {
                bool anyConverted = ConvertFarmTiles(lateralCells);
                if (anyConverted) _owner.Experience.Add(_cultivateExperience);
            } : null,
            Spin = true
        });
    }

    private void CultivateSingleCellWithEpicReplay(TerrainCell cell)
    {
        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
        {
            OnCultivate = () =>
            {
                cell.TryConvertToFarm();
                _owner.Experience.Add(_cultivateExperience);
                ReplayEpicSow();
            }
        });
    }

    private void ConvertLateralFarmTiles(TerrainCell centerCell)
    {
        ConvertFarmTiles(_targetResolver.GetLateralCells(centerCell));
    }

    private bool ConvertFarmTiles(List<TerrainCell> cells)
    {
        bool anyConverted = false;
        foreach (TerrainCell lateralCell in cells)
        {
            if (_targetResolver.NeedsFarmConversion(lateralCell))
            {
                lateralCell.TryConvertToFarm();
                anyConverted = true;
            }
        }
        return anyConverted;
    }

    private void ReplayEpicSow()
    {
        if (_replayEpicSowAndWait == null || _startCoroutine == null)
        {
            return;
        }

        _startCoroutine(_replayEpicSowAndWait());
    }
}
