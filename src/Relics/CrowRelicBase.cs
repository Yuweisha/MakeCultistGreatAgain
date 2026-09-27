using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MakeCultistGreatAgain.Relics;

/// <summary>
/// 乌鸦系遗物基类：每场战斗开始 2 秒后随机响起一声乌鸦鸣叫（音量 ×1.5），
/// 同时角色头顶弹出 [jitter] 抖动对话气泡。
/// 抽象类不会被 ModelDb.Init 扫描注册，只有具体子类才会。
/// </summary>
public abstract class CrowRelicBase : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>子类覆写：战斗开始时额外叠加的仪式层数（每层=每回合结束+1力量）。</summary>
    protected virtual int RitualStacksAtCombatStart => 0;

    /// <summary>mod 图标资源名（png 三件套随 pck 发布，放 Testmod/images/relics/ 下）。</summary>
    protected abstract string IconName { get; }

    public override string PackedIconPath => $"{MainFile.ResPath}/images/relics/{IconName}.png";
    protected override string PackedIconOutlinePath => $"{MainFile.ResPath}/images/relics/{IconName}_outline.png";
    protected override string BigIconPath => $"{MainFile.ResPath}/images/relics/{IconName}_big.png";

    public override Task BeforeCombatStartLate()
    {
        // 鸟叫延迟 2 秒；仪式在战斗开始瞬间结算。
        CrowAudio.PlayRandomAfter(2.0, _ =>
        {
            LocString? line = LocString.GetIfExists("relics", Id.Entry + ".battlecry");
            if (line != null && Owner?.Creature != null)
            {
                TalkCmd.Play(line, Owner.Creature, VfxColor.Purple, VfxDuration.Standard);
            }
        });

        if (RitualStacksAtCombatStart > 0 && Owner?.Creature != null)
        {
            _ = PowerCmd.Apply<RitualPower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature,
                RitualStacksAtCombatStart, Owner.Creature, null);
        }
        return Task.CompletedTask;
    }
}
