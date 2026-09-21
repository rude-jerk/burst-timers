using Dalamud.Configuration;
using System.Numerics;

namespace BurstTimers;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Locked = false;
    public float Width = 340;
    public float Height = 30;
    public float FontSize = 23;
    public Vector2 Position = new(400, 400);
}
