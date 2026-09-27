using System;
using Godot;

namespace MakeCultistGreatAgain;

/// <summary>
/// 用 Godot 原生 AudioStreamPlayer 播放 mod 自带的 ogg。
/// 不走 SfxCmd / NAudioManager：那条链路把 path 交给 FMOD 代理（期望 "event:/..."），
/// res:// 路径能不能播不确定，所以这里直接用 Godot 音频总线，行为可控。
/// </summary>
public static class CrowAudio
{
    /// <summary>用户要求音量在原基础上再大 50%（VolumeLinear 是线性倍率）。</summary>
    private const float Volume = 1.5f;

    private static StringName? _bus;

    private static StringName Bus
    {
        get
        {
            if (_bus != null) return _bus;
            string name = AudioServer.GetBusIndex("SFX") >= 0 ? "SFX" : "Master";
            _bus = new StringName(name);
            return _bus;
        }
    }

    /// <summary>
    /// 延迟 <paramref name="delaySec"/> 秒后随机播放一个乌鸦叫声；实际播出后回调 onPlayed(路径)。
    /// 用 SceneTreeTimer 而不是 Task.Delay：回调一定发生在游戏主线程上，
    /// 后续挂节点、弹气泡才安全。
    /// </summary>
    public static void PlayRandomAfter(double delaySec, Action<string>? onPlayed = null)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            GD.PrintErr("[Testmod] no scene tree, skip sfx");
            return;
        }

        string[] clips = MainFile.CrowClips;
        string path = clips[GD.RandRange(0, clips.Length - 1)];

        SceneTreeTimer timeout = tree.CreateTimer(delaySec);
        timeout.Timeout += () =>
        {
            if (Play(tree, path) && onPlayed != null)
            {
                onPlayed(path);
            }
        };
    }

    private static bool Play(SceneTree tree, string resPath)
    {
        try
        {
            if (!ResourceLoader.Exists(resPath))
            {
                GD.PrintErr($"[Testmod] audio not found in pck: {resPath}");
                return false;
            }

            if (GD.Load<Resource>(resPath) is not AudioStream stream)
            {
                GD.PrintErr($"[Testmod] failed to load audio stream: {resPath}");
                return false;
            }

            var player = new AudioStreamPlayer
            {
                Stream = stream,
                Bus = Bus,
                VolumeLinear = Volume,
            };
            // AudioStreamPlayer 必须在场景树里才会发声；播完自毁。
            player.Finished += () => player.QueueFree();

            tree.Root.AddChild(player);
            player.Play();
            GD.Print($"[Testmod] crow sfx -> {System.IO.Path.GetFileName(resPath)}");
            return true;
        }
        catch (Exception e)
        {
            GD.PrintErr("[Testmod] crow sfx failed: " + e);
            return false;
        }
    }
}
