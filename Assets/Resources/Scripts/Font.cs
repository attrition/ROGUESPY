using System;
using System.Collections.Generic;
using UnityEngine;

public class Font
{
    private string[] lines;
    private Texture2D bitmap;
    private Dictionary<char, Color[]> letters;
    
    private const int height = 12;
    private const int width = 6;
    private const int margin = 1;

    public Font()
    {
        lines = new string[] {
            "0123456789",
            "ABCDEFGHIJKLMNOPQR",
            "STUVWXYZ ",
            ":/-%!"
        };
        bitmap = Resources.Load("Textures/Font") as Texture2D;

        CreateFontDictionary();
    }

    public void CreateFontDictionary()
    {
        letters = new Dictionary<char, Color[]>();

        // go through each line, which is offset by (y * height) + (y * margin)
        for (int ly = 0; ly < lines.Length; ly++)
        {
            // HERE BE DRAGONS
            var top = bitmap.height - (12 - ly * margin) - (ly * height) - (ly * (margin + 1));

            // each line has a number of characters
            for (int lx = 0; lx < lines[ly].Length; lx++)
            {
                var left = (lx * width) + (lx * margin);
                var curr = lines[ly][lx]; // current character we're mapping
                Color[] letter = new Color[width * height];

                for (int cy = 0; cy < height; cy++)
                    for (int cx = 0; cx < width; cx++)
                        letter = bitmap.GetPixels(left, top, width, height);

                letters[curr] = letter;
            }
        }
    }

    public int GetWidth(string text)
    {
        return text.Length * width + ((text.Length * margin) - 1);
    }

    public int GetHeight(string text)
    {
        return height;
    }

    public Color[] GetString(string text, Color tint)
    {
        text = text.ToUpper();
        var len = text.Length;
        var colors = new List<Color>();

        for (int y = 0; y < height; y++)
        {
            for (int i = 0; i < len; i++)
            {                
                for (int x = 0; x < width; x++)
                {
                    var color = letters[text[i]][y * width + x];
                    colors.Add(color * tint);
                }
                
                // add space between characters
                if (i < len - 1)
                    for (int space = 0; space < margin; space++)
                        colors.Add(Color.clear);
            }
        }

        return colors.ToArray();
    }
}
