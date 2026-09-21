using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.ManagedFontAtlas;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace BurstTimers;

public sealed unsafe class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly ITargetManager targets;
    private readonly IObjectTable objects;
    private readonly ICommandManager commands;
    private readonly Configuration config;
    private readonly StatusCooldownTimer dokumori = new();
    private readonly PartyBuffTracker[] partyBuffs = PartyBuffTracker.CreateAll();
    private readonly HashSet<uint> activePlayerStatuses = [];
    private bool settingsOpen;
    private bool preview;
    private float potionRemaining;
    private float potionTotal;
    private IFontHandle? timelineFont;
    private float loadedFontSize;
    private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

    public Plugin(IDalamudPluginInterface pi, IFramework framework, IClientState client,
        ITargetManager targets, ICommandManager commands, IObjectTable objects)
    {
        this.pi = pi;
        this.framework = framework;
        this.client = client;
        this.targets = targets;
        this.objects = objects;
        this.commands = commands;
        config = pi.GetPluginConfig() as Configuration ?? new();
        commands.AddHandler("/bursttimers", new CommandInfo(OnCommand) { HelpMessage = "Open countdown bar settings." });
        framework.Update += Update;
        client.Logout += Logout;
        pi.UiBuilder.Draw += Draw;
        pi.UiBuilder.OpenConfigUi += OpenSettings;
        pi.UiBuilder.OpenMainUi += OpenSettings;
    }

    private void OnCommand(string command, string args) => OpenSettings();
    private void OpenSettings() => settingsOpen = true;
    private void Save() => pi.SavePluginConfig(config);
    private void Logout(int type, int code)
    {
        dokumori.Reset();
        foreach (var buff in partyBuffs)
            buff.Timer.Reset();
        activePlayerStatuses.Clear();
        potionRemaining = potionTotal = 0;
    }

    private void Update(IFramework _)
    {
        if (!client.IsLoggedIn)
            return;
        bool present = targets.Target is IBattleChara target &&
            target.StatusList.Any(status => status.StatusId == 3849 && status.RemainingTime > 0);
        var now = Now;
        dokumori.Update(now, present);
        activePlayerStatuses.Clear();
        if (objects.LocalPlayer is { } player)
            foreach (var status in player.StatusList)
                if (status.RemainingTime > 0)
                    activePlayerStatuses.Add(status.StatusId);
        foreach (var buff in partyBuffs)
            buff.Update(now, activePlayerStatuses);

        // Shared combat stat-potion recast group, including tinctures and gemdraughts.
        // Read actual recast data so HQ/NQ durations and game resets are respected.
        var manager = ActionManager.Instance();
        var recast = manager == null ? null : manager->GetRecastGroupDetail(58);
        potionTotal = recast != null && recast->IsActive ? recast->Total : 0;
        potionRemaining = recast != null && recast->IsActive ? Math.Max(0, recast->Total - recast->Elapsed) : 0;
    }

    private void Draw()
    {
        if (settingsOpen)
            DrawSettings();
        if (!client.IsLoggedIn)
            return;
        var now = Now;
        var dokuLeft = (float)dokumori.Remaining(now);
        var showPreview = preview || !config.Locked;
        int buffCount = partyBuffs.Count(buff => showPreview || buff.Timer.Remaining(now) > 0);
        if (!showPreview && dokuLeft <= 0 && potionRemaining <= 0 && buffCount == 0)
            return;

        if (timelineFont == null || loadedFontSize != config.FontSize)
        {
            timelineFont?.Dispose();
            timelineFont = FontLoader.LoadFont(pi, config.FontSize);
            loadedFontSize = config.FontSize;
        }
        using var fontScope = timelineFont.Push();

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove;
        if (config.Locked)
            flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove;
        ImGui.SetNextWindowPos(config.Position, ImGuiCond.Always);
        int count = (dokuLeft > 0 || showPreview ? 1 : 0) + (potionRemaining > 0 || showPreview ? 1 : 0) + buffCount;
        float rowHeight = Math.Max(config.Height, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
        ImGui.SetNextWindowSize(new Vector2(config.Width, count * (rowHeight + ImGui.GetStyle().ItemSpacing.Y)));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        if (ImGui.Begin("Burst Timers##bars", flags))
        {
            if (dokuLeft > 0 || showPreview)
                DrawBar("Dokumori", dokuLeft > 0 ? dokuLeft : 72, 120, offensive: true);
            foreach (var buff in partyBuffs)
            {
                var remaining = (float)buff.Timer.Remaining(now);
                if (remaining > 0 || showPreview)
                    DrawBar(buff.Name, remaining > 0 ? remaining : 72, 120);
            }
            if (potionRemaining > 0 || showPreview)
                DrawBar("Potion", potionRemaining > 0 ? potionRemaining : 45, potionRemaining > 0 ? potionTotal : 270);
            if (!config.Locked && ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                config.Position += ImGui.GetIO().MouseDelta;
            }
            if (!config.Locked && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                Save();
        }
        ImGui.End();
        ImGui.PopStyleVar(3);
    }

    // Adapted from Cactus Watcher RaidbossTimelineWindow (MIT; see LICENSE.CactusWatcher).
    private void DrawBar(string label, float remaining, float total, bool offensive = false)
    {
        var size = new Vector2(config.Width * 0.98f, config.Height);
        var progress = Math.Clamp(remaining / total, 0, 1);
        var hundredths = (int)(Math.Max(0, remaining) * 100);
        var caption = $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00} - {label}";
        // Dokumori uses an offensive orange palette, deepening near completion.
        // Other bars retain Cactus Watcher's periwinkle/salmon preview palette.
        uint color = offensive
            ? (progress < 0.2f ? 0xFF2870FFu : 0xFF40B0FFu)
            : (progress < 0.2f ? 0xFF7878FFu : 0xFFFF8888u);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
        try
        {
            ImGui.ProgressBar(progress, size, string.Empty);
            ImGui.SameLine(size.X * 0.1f);
            ImGui.TextUnformatted(caption);
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }
    private void DrawSettings()
    {
        ImGui.SetNextWindowSize(new Vector2(440, 340), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Burst Timers", ref settingsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted("Dokumori: one 120-second timer, any Ninja on your target.");
            ImGui.TextUnformatted("Further applications do not restart an active timer.");
            ImGui.TextUnformatted("Party buffs on you: one 120-second timer per buff.");
            ImGui.TextUnformatted("Embolden, Starry Muse, Brotherhood, Searing Light.");
            ImGui.TextUnformatted("Potion: your actual tincture / gemdraught cooldown.");
            ImGui.Separator();
            bool changed = ImGui.Checkbox("Lock bars (click through)", ref config.Locked);
            ImGui.Checkbox("Preview bars", ref preview);
            ImGui.TextDisabled("Unlock and drag the bars to move them.");
            changed |= ImGui.SliderFloat("Width", ref config.Width, 180, 800, "%.0f");
            changed |= ImGui.SliderFloat("Height", ref config.Height, 20, 60, "%.0f");
            changed |= ImGui.SliderFloat("Text size", ref config.FontSize, 12, 40, "%.0f");
            ImGui.TextDisabled("Cactus Watcher font, progress bars, colors and label layout.");
            if (ImGui.Button("Reset position")) { config.Position = new(400, 400); changed = true; }
            ImGui.SameLine();
            if (ImGui.Button("Clear Dokumori timer")) dokumori.Reset();
            if (ImGui.Button("Clear party buff timers"))
                foreach (var buff in partyBuffs)
                    buff.Timer.Reset();
            if (changed) Save();
        }
        ImGui.End();
    }

    public void Dispose()
    {
        framework.Update -= Update;
        client.Logout -= Logout;
        pi.UiBuilder.Draw -= Draw;
        pi.UiBuilder.OpenConfigUi -= OpenSettings;
        pi.UiBuilder.OpenMainUi -= OpenSettings;
        commands.RemoveHandler("/bursttimers");
        timelineFont?.Dispose();
        Save();
    }
}

