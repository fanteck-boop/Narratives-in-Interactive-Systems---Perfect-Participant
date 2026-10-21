using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PerfectParticipant.Editor
{
    // Procedural lab surfaces, written once to Textures/*.png (fixed seeds, delete a file to regenerate).
    // Each texture covers Span metres: tile it by surface size / Span.
    public static class LabTextures
    {
        const string Dir = "Assets/_Project/Textures";
        const int N = 512;
        public const float Span = 1.2f;

        // Vinyl floor: 0.3 m tiles in two close shades, thin grout, fine speckle.
        public static Texture2D Floor() => Make("T_FloorVinyl", 1, (x, y, r) =>
        {
            int tile = N / 4;
            if (x % tile < 2 || y % tile < 2) return G(0.30f);
            float b = (x / tile + y / tile) % 2 == 0 ? 0.52f : 0.48f;
            float s = (float)r.NextDouble();
            return new Color(b * 0.95f, b, b * 0.97f) * (s > 0.97f ? 0.82f : s < 0.03f ? 1.12f : 1f);
        });

        // Drop ceiling: 0.6 m acoustic tiles, light T-bar grid, perforation noise.
        public static Texture2D Ceiling() => Make("T_CeilingTile", 2, (x, y, r) =>
        {
            int tile = N / 2;
            if (x % tile < 4 || y % tile < 4) return G(0.80f);
            return G((float)r.NextDouble() < 0.12f ? 0.78f : 0.93f);
        });

        // Painted block wall: faint mottling only, so any tiling looks right.
        public static Texture2D Wall() => Make("T_WallPaint", 3, (x, y, r) => G(0.94f + 0.05f * (float)r.NextDouble()));

        static Color G(float v) => new Color(v, v, v);

        static Texture2D Make(string name, int seed, Func<int, int, System.Random, Color> px)
        {
            string path = $"{Dir}/{name}.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Dir);
                var r = new System.Random(seed);
                var t = new Texture2D(N, N, TextureFormat.RGB24, false);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++) t.SetPixel(x, y, px(x, y, r));
                File.WriteAllBytes(path, t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.anisoLevel = 4;
                imp.maxTextureSize = N;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
