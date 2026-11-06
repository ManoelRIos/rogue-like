using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Diretor da noite: decide quantos Vultos saem da mata (mais a cada noite),
    /// de onde saem (bordas da floresta longe dos jogadores), e cria os "olhos no escuro"
    /// — pares de olhos que observam da neblina e somem quando você se aproxima.
    /// </summary>
    public class NightDirector
    {
        readonly World world;
        readonly Flora flora;
        readonly Campfire fire;
        public Farm farm;
        public readonly List<Creature> creatures = new();
        float raidWarnT;
        public bool CropsUnderAttack { get; private set; }
        int toSpawn, night;
        float spawnT, eyesT;
        readonly List<(Transform t, float life)> eyes = new();
        readonly Mesh eyeMesh;

        public float Danger { get; private set; }

        public NightDirector(World w, Flora f, Campfire c)
        {
            world = w; flora = f; fire = c;
            var mb = new MeshBuilder();
            var rng = new Rng(5);
            mb.Blob(new Vector3(-0.1f, 0, 0), Vector3.one * 0.045f, Col.Hex("#ffd040"), 0, rng, 0);
            mb.Blob(new Vector3(0.1f, 0, 0), Vector3.one * 0.045f, Col.Hex("#ffd040"), 0, rng, 0);
            eyeMesh = mb.ToMesh("olhos_escuro");
        }

        public void BeginNight(int n)
        {
            night = n;
            toSpawn = Mathf.Min(2 + n * 2, 16);
            spawnT = 8f;          // um respiro antes da primeira criatura
        }

        public void EndNight()
        {
            toSpawn = 0;
            foreach (var c in creatures) if (c) c.Dissolve();
            foreach (var e in eyes) if (e.t) Object.Destroy(e.t.gameObject);
            eyes.Clear();
        }

        public void Tick(bool isNight, float darkness, List<PlayerController> players, Camera cam, float dt)
        {
            creatures.RemoveAll(c => c == null);
            var camPos = cam.transform.position; var camFwd = cam.transform.forward;

            if (isNight && toSpawn > 0)
            {
                spawnT -= dt;
                if (spawnT <= 0)
                {
                    spawnT = Random.Range(10f, 22f) / Mathf.Sqrt(night);
                    if (TrySpawn(players)) toSpawn--;
                }
            }

            float nearest = 999;
            foreach (var c in creatures)
            {
                PlayerController best = null; float bd = float.MaxValue;
                foreach (var p in players)
                {
                    if (!p.alive) continue;
                    float d = (p.transform.position - c.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                c.Tick(best, camPos, camFwd, dt);
                nearest = Mathf.Min(nearest, c.DistToPlayer);
            }
            Danger = Mathf.Clamp01(1 - (nearest - 4) / 26f);

            // aviso: algo está comendo a plantação
            CropsUnderAttack = false;
            foreach (var c in creatures) if (c.Eating) CropsUnderAttack = true;
            raidWarnT -= dt;
            if (CropsUnderAttack && raidWarnT <= 0)
            {
                raidWarnT = 20;
                Game.I?.Hud.Banner("A PLANTAÇÃO!", "algo está comendo a horta — espante com o machado ou aumente a fogueira", 4f);
                Audio.Play("screech", 0.5f);
            }

            // olhos no escuro (só ambientação)
            if (isNight && darkness > 0.7f && players.Count > 0)
            {
                eyesT -= dt;
                if (eyesT <= 0 && eyes.Count < 4)
                {
                    eyesT = Random.Range(5f, 12f);
                    var p = players[0].transform.position;
                    float a = Random.value * Mathf.PI * 2, r = Random.Range(22f, 34f);
                    var at = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                    if (!world.InBase(at) && world.Height(at.x, at.z) > World.WaterLevel)
                    {
                        at.y = world.Height(at.x, at.z) + Random.Range(0.6f, 2.2f);
                        var go = Mats.Object("olhos", eyeMesh, Mats.Glow, null, false);
                        go.transform.position = at;
                        go.transform.rotation = Quaternion.LookRotation((p - at).normalized);
                        eyes.Add((go.transform, Random.Range(4f, 9f)));
                    }
                }
            }
            for (int i = eyes.Count - 1; i >= 0; i--)
            {
                var (t, life) = eyes[i];
                life -= dt;
                bool close = players.Count > 0 && (players[0].transform.position - t.position).magnitude < 14;
                // pisca de vez em quando
                t.localScale = new Vector3(1, (Mathf.Repeat(Time.time + i, 3.1f) < 0.12f) ? 0.1f : 1, 1);
                if (life <= 0 || close)
                {
                    if (close) Audio.Play("rustle", 0.5f);
                    Object.Destroy(t.gameObject); eyes.RemoveAt(i);
                }
                else eyes[i] = (t, life);
            }
        }

        bool TrySpawn(List<PlayerController> players)
        {
            var spots = flora.ForestEdge;
            if (spots.Count == 0 || players.Count == 0) return false;
            for (int tries = 0; tries < 20; tries++)
            {
                var s = spots[Random.Range(0, spots.Count)];
                float dMin = float.MaxValue;
                foreach (var p in players) dMin = Mathf.Min(dMin, (p.transform.position - s).magnitude);
                if (dMin < 32 || dMin > 70 || world.InBase(s)) continue;
                bool raider = farm != null && farm.UnprotectedCrop(s) != null && Random.value < 0.5f;
                creatures.Add(Creature.Spawn(world, fire, farm, s, night, raider));
                return true;
            }
            return false;
        }
    }
}
