using Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;


namespace BurstTimers;

public static class FontLoader
{
    private static unsafe ushort[]? GetCharacterRanges(ImGuiIOPtr io, bool chinese, bool korean)
    {
        if (!chinese && !korean) return null;

        using (ImGuiHelpers.NewFontGlyphRangeBuilderPtrScoped(out var builder))
        {
            if (chinese)
            {
                // GetGlyphRangesChineseFull() includes Default + Hiragana,
                // Katakana, Half-Width, Selection of 1946 Ideographs
                // https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
                builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
            }

            if (korean)
            {
                builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
            }

            return builder.BuildRangesToArray();
        }
    }

    public static IFontHandle LoadFont(Dalamud.Plugin.IDalamudPluginInterface pi, float size)
    {
        return pi.UiBuilder.FontAtlas.NewDelegateFontHandle
        (
            e => e.OnPreBuild
            (
                tk =>
                {
                    var config = new SafeFontConfig
                    {
                        SizePx = size * ImGuiHelpers.GlobalScale,
                        GlyphRanges = FontLoader.GetCharacterRanges(ImGui.GetIO(), true, true),
                    };
                    tk.Font = tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, config);
                }
            )
        );
    }
}

