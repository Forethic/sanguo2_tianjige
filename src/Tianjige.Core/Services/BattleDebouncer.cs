namespace Tianjige.Core.Services;

/// <summary>
/// 战前选将老虎机防抖与交锋状态机
/// </summary>
public class BattleDebouncer
{
    public bool IsLockedIn { get; private set; }
    public string StatusText { get; private set; } = "全军整备 · 静待烽烟";
    public int LockedLeftGeneralId { get; private set; }
    public int LockedRightGeneralId { get; private set; }

    /// <summary>
    /// 更新当前战斗状态 (battleState: 0=大地图, 1=内政, 2=战前选将抽奖中, 3=战斗大门拉开)
    /// </summary>
    public void Update(byte battleState, int leftGeneralId, int rightGeneralId)
    {
        if (battleState == 2)
        {
            // 老虎机快速跳变中，抑制画面抖动，保持 HUD 沉稳
            IsLockedIn = false;
            StatusText = "两军对圆 · 阵前定将中...";
            return;
        }

        if (battleState == 3)
        {
            // 确认交锋锁定
            IsLockedIn = true;
            LockedLeftGeneralId = leftGeneralId;
            LockedRightGeneralId = rightGeneralId;
            StatusText = "战阵拉开 · 两军交锋";
            return;
        }

        // 大地图或内政
        IsLockedIn = false;
        LockedLeftGeneralId = 0;
        LockedRightGeneralId = 0;
        StatusText = battleState == 1 ? "四海晏然 · 治国理政" : "大地图进军 · 帷幄筹谋";
    }
}
