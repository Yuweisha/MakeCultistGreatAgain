using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MakeCultistGreatAgain.Relics;

/// <summary>
/// 仪式匕首：每场战斗开始时触发鸟叫（+抖动气泡），同时获得 3 层"仪式"
/// （游戏原生 RitualPower：每回合结束 +3 力量）。上限 -10 在获得时一次性扣除。
/// </summary>
public sealed class RitualDaggerRelic : CrowRelicBase
{
    protected override string IconName => "ritual_dagger_relic";

    protected override int RitualStacksAtCombatStart => 3;

    /// <summary>获得遗物时立即：最大生命值 -10（当前值夹到不超过新上限，游戏逻辑自带）。</summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature != null)
        {
            // isFromCard=false：这属于"诅咒/代价"类变化。
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, 10m, false);
        }
    }
}
