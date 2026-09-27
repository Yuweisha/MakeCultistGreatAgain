using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MakeCultistGreatAgain.Relics;

namespace MakeCultistGreatAgain;

/// <summary>
/// Mod 入口（带 [ModInitializer]，由游戏 ModManager 调用）。
/// 遗物模型由 ModelDb.Init() 自动扫描注册，不要手动 Inject（会 DuplicateModelException）。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MakeCultistGreatAgain";
    public const string ResPath = "res://" + ModId;

    /// <summary>3 个乌鸦叫声 ogg（随 pck 发布，路径在 Testmod 资源目录里）。</summary>
    public static readonly string[] CrowClips =
    {
        $"{ResPath}/sounds/STS_VO_CrowCultist_1a.ogg",
        $"{ResPath}/sounds/STS_VO_CrowCultist_1b.ogg",
        $"{ResPath}/sounds/STS_VO_CrowCultist_1c.ogg",
    };

    public static void Initialize()
    {
        try
        {
            // 1. 遗物挂进事件池：保证 RelicModel.Pool 能解析到池（未入池的遗物访问 Pool 会抛）。
            ModHelper.AddModelToPool<EventRelicPool, CultistHeadpieceRelic>();
            ModHelper.AddModelToPool<EventRelicPool, RitualDaggerRelic>();

            // 2. Harmony：涅奥首屏注入乌鸦三选一（NeowCrowChoicePatch）。
            var harmony = new Harmony("Kita." + ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            GD.Print("[Testmod] loaded");
        }
        catch (Exception e)
        {
            GD.PrintErr("[Testmod] init failed: " + e);
        }
    }
}
