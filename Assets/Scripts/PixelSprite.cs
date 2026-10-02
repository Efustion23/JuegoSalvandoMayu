using System.Collections.Generic;
using UnityEngine;

// Sprites pixel art pequenos definidos como texto: una letra por pixel ('.' = transparente), a 16 px por unidad.
public static class PixelSprite
{
    public static Sprite Make(string[] rows, Dictionary<char, Color32> palette, float pivotY = 0f)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = x < rows[y].Length ? rows[y][x] : '.';
                tex.SetPixel(x, h - 1 - y, c == '.' ? new Color32(0, 0, 0, 0) : palette[c]);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, pivotY), 16f);
    }
}
