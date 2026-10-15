using UnityEngine;

namespace Pitchlife.P01
{
    /// <summary>Procedural football panels shared by the close-up and the physical ball.</summary>
    public static class FootballBallVisual
    {
        private static Texture2D _surface, _closeup, _dot;
        private static readonly Vector3[] Centers = CreateCenters();
        private static readonly Vector3[][] Tangents = CreateTangents();

        public static Material CreateMaterial(Shader shader)
        {
            if (_surface == null)
            {
                _surface = new Texture2D(512, 256, TextureFormat.RGBA32, true);
                _surface.name = "Procedural Football Panels";
                var pixels = new Color32[512 * 256];
                for (int y = 0; y < 256; y++)
                for (int x = 0; x < 512; x++)
                {
                    float latitude = (y / 255f - .5f) * Mathf.PI;
                    float longitude = x / 511f * Mathf.PI * 2;
                    pixels[y * 512 + x] = Surface(new Vector3(Mathf.Sin(longitude) * Mathf.Cos(latitude),
                        Mathf.Sin(latitude), Mathf.Cos(longitude) * Mathf.Cos(latitude)));
                }
                _surface.SetPixels32(pixels);
                _surface.Apply(true, true);
            }
            var material = new Material(shader);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", _surface);
            material.SetFloat("_Smoothness", .28f);
            return material;
        }

        public static Texture2D Closeup
        {
            get
            {
                if (_closeup != null) return _closeup;
                const int size = 512;
                _closeup = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _closeup.name = "Strike Selection Football";
                var pixels = new Color32[size * size];
                Vector3 light = new Vector3(-.4f, .65f, -.75f).normalized;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + .5f) / size * 2 - 1, ny = (y + .5f) / size * 2 - 1;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    if (radius >= 1) continue;
                    Vector3 n = new Vector3(nx, ny, -Mathf.Sqrt(1 - radius * radius));
                    Color color = Surface(n);
                    color *= .42f + .58f * Mathf.Max(0, Vector3.Dot(n, light));
                    color.a = Mathf.Clamp01((1 - radius) * size * .5f);
                    pixels[y * size + x] = color;
                }
                _closeup.SetPixels32(pixels);
                _closeup.Apply(false, true);
                return _closeup;
            }
        }

        public static Texture2D Dot
        {
            get
            {
                if (_dot != null) return _dot;
                _dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color32[1024];
                for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = new Color(1, 1, 1,
                        Mathf.Clamp01((1 - Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * 16) / 16) * 16));
                _dot.SetPixels32(pixels);
                _dot.Apply(false, true);
                return _dot;
            }
        }

        private static Color Surface(Vector3 normal)
        {
            int best = 0;
            float first = -2, second = -2;
            for (int i = 0; i < Centers.Length; i++)
            {
                float dot = Vector3.Dot(normal, Centers[i]);
                if (dot > first) { second = first; first = dot; best = i; }
                else second = Mathf.Max(second, dot);
            }
            float edge = -2;
            foreach (Vector3 tangent in Tangents[best])
                edge = Mathf.Max(edge, Vector3.Dot(normal, tangent) / first);
            if (edge < .29f) return new Color(.035f, .045f, .055f);
            if (edge < .305f || first - second < .008f) return new Color(.62f, .66f, .68f);
            return new Color(.96f, .97f, .95f);
        }

        private static Vector3[] CreateCenters()
        {
            float p = (1 + Mathf.Sqrt(5)) / 2;
            var result = new Vector3[12];
            int index = 0;
            foreach (int a in new[] { -1, 1 })
            foreach (int b in new[] { -1, 1 })
            {
                result[index++] = new Vector3(0, a, b * p).normalized;
                result[index++] = new Vector3(a, b * p, 0).normalized;
                result[index++] = new Vector3(b * p, 0, a).normalized;
            }
            return result;
        }

        private static Vector3[][] CreateTangents()
        {
            var result = new Vector3[Centers.Length][];
            for (int i = 0; i < Centers.Length; i++)
            {
                var neighbors = new System.Collections.Generic.List<Vector3>();
                for (int j = 0; j < Centers.Length; j++)
                {
                    float dot = Vector3.Dot(Centers[i], Centers[j]);
                    if (dot > .4f && dot < .6f) neighbors.Add((Centers[j] - Centers[i] * dot).normalized);
                }
                result[i] = neighbors.ToArray();
            }
            return result;
        }
    }
}
