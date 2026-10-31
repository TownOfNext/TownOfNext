using UnityEngine;

namespace TONX;

public static class ColorHelper
{
    public static bool TryParseHtmlString(string htmlString, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(htmlString)) return false;
        if (htmlString[0] != '#')
        {
            htmlString = htmlString.ToLowerInvariant() switch
            {
                "red" => "#FF0000", "cyan" => "#00FFFF", "blue" => "#0000FF",
                "darkblue" => "#0000A0", "lightblue" => "#ADD8E6", "purple" => "#800080",
                "yellow" => "#FFFF00", "lime" => "#00FF00", "fuchsia" => "#FF00FF",
                "white" => "#FFFFFF", "silver" => "#C0C0C0", "grey" => "#808080",
                "black" => "#000000", "orange" => "#FFA500", "brown" => "#A52A2A",
                "maroon" => "#800000", "green" => "#008000", "olive" => "#808000",
                "navy" => "#000080", "teal" => "#008080", "aqua" => "#00FFFF",
                "magenta" => "#FF00FF", "clear" => "#00000000",
                _ => ""
            };
        }
        if (htmlString.Length != 4 && htmlString.Length != 5 &&
            htmlString.Length != 7 && htmlString.Length != 9) return false;

        uint value = 0;
        for (var i = 1; i < htmlString.Length; i++)
        {
            var c = htmlString[i];
            var digit = c >= '0' && c <= '9' ? c - '0' :
                c >= 'a' && c <= 'f' ? c - 'a' + 10 :
                c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
            if (digit < 0) return false;
            value = (value << 4) | (uint)digit;
        }
        if (htmlString.Length <= 5)
        {
            if (htmlString.Length == 4) value = (value << 4) | 0xF;
            color = new Color(((value >> 12) & 0xF) / 15f, ((value >> 8) & 0xF) / 15f,
                ((value >> 4) & 0xF) / 15f, (value & 0xF) / 15f);
        }
        else
        {
            if (htmlString.Length == 7) value = (value << 8) | 0xFF;
            color = new Color(((value >> 24) & 0xFF) / 255f, ((value >> 16) & 0xFF) / 255f,
                ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);
        }
        return true;
    }

    public static void RGBToHSV(Color color, out float h, out float s, out float v)
    {
        var min = System.Math.Min(color.r, System.Math.Min(color.g, color.b));
        v = System.Math.Max(color.r, System.Math.Max(color.g, color.b));
        var delta = v - min;
        h = 0f;
        s = v == 0f ? 0f : delta / v;
        if (delta == 0f) return;
        if (v == color.r) h = (color.g - color.b) / delta;
        else if (v == color.g) h = 2f + (color.b - color.r) / delta;
        else h = 4f + (color.r - color.g) / delta;
        h /= 6f;
        if (h < 0f) h += 1f;
    }

    /// <summary>蛍光マーカーのような色合いの透過色に変換する</summary>
    /// <param name="bright">最大明度にするかどうか．黒っぽい色を黒っぽいままにしたい場合はfalse</param>
    public static Color ToMarkingColor(this Color color, bool bright = true)
    {
        RGBToHSV(color, out var h, out _, out var v);
        var markingColor = Color.HSVToRGB(h, MarkerSat, bright ? MarkerVal : v).SetAlpha(MarkerAlpha);
        return markingColor;
    }
    /// <summary>白背景での可読性を保てる色に変換する</summary>
    public static Color ToReadableColor(this Color color)
    {
        RGBToHSV(color, out var h, out var s, out var v);
        // 適切な彩度でない場合は彩度を変更
        if (s < ReadableSat)
        {
            s = ReadableSat;
        }
        // 適切な明度でない場合は明度を変更
        if (v > ReadableVal)
        {
            v = ReadableVal;
        }
        return Color.HSVToRGB(h, s, v);
    }

    /// <summary>マーカー色のS値 = 彩度</summary>
    private const float MarkerSat = 1f;
    /// <summary>マーカー色のV値 = 明度</summary>
    private const float MarkerVal = 1f;
    /// <summary>マーカー色のアルファ = 不透明度</summary>
    private const float MarkerAlpha = 0.2f;
    /// <summary>白背景テキスト色の最大S = 彩度</summary>
    private const float ReadableSat = 0.8f;
    /// <summary>白背景テキスト色の最大V = 明度</summary>
    private const float ReadableVal = 0.8f;
}
