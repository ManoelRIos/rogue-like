using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Gerador aleatório com semente (mesma semente = mesmo andar; útil para co-op).</summary>
    public class Rng
    {
        readonly System.Random r;
        public readonly int Seed;

        public Rng(int seed) { Seed = seed; r = new System.Random(seed); }

        public float Value => (float)r.NextDouble();
        public float Range(float a, float b) => a + (b - a) * (float)r.NextDouble();
        /// <summary>Inteiro entre a e b, inclusivo.</summary>
        public int Int(int a, int b) => r.Next(a, b + 1);
        public bool Chance(float p) => r.NextDouble() < p;
        public T Pick<T>(IList<T> list) => list[r.Next(list.Count)];

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public T Weighted<T>(IList<(T item, float w)> entries)
        {
            float total = 0;
            foreach (var e in entries) total += e.w;
            float x = Value * total;
            foreach (var e in entries) { x -= e.w; if (x <= 0) return e.item; }
            return entries[entries.Count - 1].item;
        }
    }

    public static class MathX
    {
        public const float TAU = Mathf.PI * 2f;
        public static Vector2 Dir(float ang) => new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        public static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x);
        public static float Angle(Vector2 from, Vector2 to) => Mathf.Atan2(to.y - from.y, to.x - from.x);
        public static float WrapAngle(float a) => Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a));
        public static float Rand(float a, float b) => Random.Range(a, b);

        /// <summary>Converte posição de jogo (x, y-para-baixo) para mundo Unity (x, altura, z).</summary>
        public static Vector3 ToWorld(Vector2 p, float h = 0) => new Vector3(p.x, h, -p.y);
        public static Vector2 FromWorld(Vector3 w) => new Vector2(w.x, -w.z);
    }
}
