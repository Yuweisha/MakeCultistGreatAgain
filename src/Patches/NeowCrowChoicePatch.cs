using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MakeCultistGreatAgain.Relics;

namespace MakeCultistGreatAgain.Patches;

/// <summary>
/// 涅奥首屏注入（机制照搬 RandomMonsterMod 的已验证做法）：
/// Postfix AncientEventModel.GenerateInitialOptionsWrapper——
///   1. 捕获涅奥原生选项页；
///   2. 换成 3 个自定义选项（邪教徒头套 / 仪式匕首 / 跳过）；
///   3. 玩家做出选择后，把捕获的原生页恢复回去（SetEventState + IsFinished=false），
///      涅奥照常显示原版遗物三选一。选中的 mod 遗物在恢复前发给你。
/// 防重复：已有任一乌鸦遗物的玩家不再注入；回调内也做获得去重。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
internal static class NeowCrowChoicePatch
{
    private const string Table = "ancients";
    private const string DescKey = "NEOW.pages.INITIAL.description";

    internal const string HeadpieceKey = "TESTMOD-CULTIST_HEADPIECE";
    internal const string DaggerKey = "TESTMOD-RITUAL_DAGGER";
    internal const string SkipKey = "TESTMOD-SKIP_CROW_CHOICE";

    private sealed class ChoiceState
    {
        public IReadOnlyList<EventOption> CapturedOptions { get; set; } = Array.Empty<EventOption>();
        public bool SelectionInProgress { get; set; }
    }

    private static readonly ConditionalWeakTable<Neow, ChoiceState> States = new();

    private static readonly MethodInfo SetEventState =
        AccessTools.Method(typeof(EventModel), "SetEventState",
            new[] { typeof(LocString), typeof(IEnumerable<EventOption>) });

    private static readonly MethodInfo SetEventFinishedFlag =
        AccessTools.PropertySetter(typeof(EventModel), "IsFinished");

    private static readonly MethodInfo SetOptionRelic =
        AccessTools.PropertySetter(typeof(EventOption), "Relic");

    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    private static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance is not Neow neow) return;
        var owner = ((EventModel)neow).Owner;
        if (owner == null) return;

        // 本局已选过（任一玩家已持有乌鸦遗物）就不再注入。
        if (owner.RunState.Players.Any(p => p.Relics.Any(r => r is CrowRelicBase))) return;

        IReadOnlyList<EventOption> captured = __result?.ToArray() ?? Array.Empty<EventOption>();
        if (captured.Count == 0)
        {
            GDLog("涅奥原生页为空，跳过注入");
            return;
        }

        try
        {
            ChoiceState state = States.GetOrCreateValue(neow);
            state.CapturedOptions = captured;

            __result = new[]
            {
                MakeRelicOption<CultistHeadpieceRelic>(neow, state, HeadpieceKey),
                MakeRelicOption<RitualDaggerRelic>(neow, state, DaggerKey),
                MakeSkipOption(neow, state),
            };
            GDLog($"已注入乌鸦选择页（捕获涅奥原生选项 {captured.Count} 个）");
        }
        catch (Exception e)
        {
            GDLog("注入失败，保留涅奥原生页: " + e);
            __result = captured;
        }
    }

    private static EventOption MakeRelicOption<TRelic>(Neow neow, ChoiceState state, string optionKey)
        where TRelic : RelicModel
    {
        RelicModel canonical = ModelDb.Relic<TRelic>();
        var option = new EventOption(neow, () => ChooseRelic(neow, state, canonical),
            new LocString(Table, $"NEOW.pages.INITIAL.options.{optionKey}.title"),
            new LocString(Table, $"NEOW.pages.INITIAL.options.{optionKey}.description"),
            optionKey, Array.Empty<IHoverTip>());
        // Relic 属性 private set；用反射挂上，按钮才会显示遗物图标。
        SetOptionRelic.Invoke(option, new object[] { canonical });
        return option;
    }

    private static EventOption MakeSkipOption(Neow neow, ChoiceState state)
    {
        return new EventOption(neow, () => Skip(neow, state),
            new LocString(Table, $"NEOW.pages.INITIAL.options.{SkipKey}.title"),
            new LocString(Table, $"NEOW.pages.INITIAL.options.{SkipKey}.description"),
            SkipKey, Array.Empty<IHoverTip>());
    }

    private static async Task ChooseRelic(Neow neow, ChoiceState state, RelicModel canonical)
    {
        if (state.SelectionInProgress) return;
        state.SelectionInProgress = true;
        try
        {
            var owner = ((EventModel)neow).Owner!;
            // 重复保护：读档后再次走到这里也不会重复获得。
            if (!owner.Relics.Any(r => r.GetType() == canonical.GetType()))
            {
                RestoreNeowPage(neow, state);
                await RelicCmd.Obtain(canonical.ToMutable(), owner, -1);
                await Cmd.Wait(0.35f, false);
            }
            else
            {
                RestoreNeowPage(neow, state);
            }
            GDLog($"已发放 {canonical.Id.Entry}，涅奥回到原生选择页");
        }
        catch (Exception e)
        {
            GDLog("发放/恢复失败: " + e);
            try { RestoreNeowPage(neow, state); } catch { /* 再失败则维持现状 */ }
        }
        finally
        {
            state.SelectionInProgress = false;
        }
    }

    private static Task Skip(Neow neow, ChoiceState state)
    {
        if (state.SelectionInProgress) return Task.CompletedTask;
        state.SelectionInProgress = true;
        try
        {
            RestoreNeowPage(neow, state);
            GDLog("跳过乌鸦选择，涅奥回到原生选择页");
        }
        catch (Exception e)
        {
            GDLog("跳过时恢复失败: " + e);
        }
        finally
        {
            state.SelectionInProgress = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>把首屏恢复为涅奥原生选项页（清 IsFinished，再交回捕获的原生 options）。</summary>
    private static void RestoreNeowPage(Neow neow, ChoiceState state)
    {
        if (state.CapturedOptions.Count == 0)
            throw new InvalidOperationException("捕获的涅奥页为空");
        SetEventFinishedFlag.Invoke(neow, new object[] { false });
        SetEventState.Invoke(neow, new object[] { new LocString(Table, DescKey), state.CapturedOptions });
    }

    private static void GDLog(string msg) => Godot.GD.Print($"[Testmod] {msg}");
}
