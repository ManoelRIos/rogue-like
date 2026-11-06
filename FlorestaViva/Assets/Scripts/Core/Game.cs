using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Ponto de entrada. Monta tudo por código ao abrir qualquer cena:
    /// mundo → flora → base/fogueira → vida → ciclo dia/noite → jogador → câmera → noite.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public int Seed = 1337;
        public float MouseSensitivity = 2.2f;

        public World World; public Flora Flora; public Life Life; public DayNight Cycle;
        public Campfire Fire; public NightDirector Director; public CameraRig Rig; public Hud Hud; public Farm Farm; public Atmosphere Atmos;
        int selectedTool;
        public readonly List<PlayerController> Players = new();
        public PlayerController Player => Players.Count > 0 ? Players[0] : null;
        public bool Loading { get; private set; } = true;
        public bool NearFire { get; private set; }
        public float RespawnT { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("FlorestaViva").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 120;
            Mats.Init();
            Audio.Init(gameObject);
            Hud = gameObject.AddComponent<Hud>();
            Hud.game = this;
        }

        IEnumerator Start()
        {
            // a cena pode vir com câmera/luz padrão: o jogo cria as próprias
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(l.gameObject);
            Rig = new CameraRig();
            Players.Add(PlayerController.Create(World.BaseCenter + Vector3.up * 30, Col.Hex("#3f6a5a")));   // placeholder até o mundo existir
            yield return null; yield return null;   // deixa a tela de carregamento aparecer

            World = new World(Seed);
            World.Build();
            yield return null;
            Flora = new Flora(World);
            Fire = new Campfire(World, World.Root);
            Fx.Init(World);
            Life = new Life(Flora, World, World.Root);
            Atmos = new Atmosphere(World);
            Cycle = new DayNight { Post = Rig.Post };
            Farm = new Farm(World, Fire, World.Root);
            Director = new NightDirector(World, Flora, Fire) { farm = Farm };
            Cycle.OnNight += () =>
            {
                Audio.Play("nightfall", 0.9f);
                Hud.Banner($"NOITE {Cycle.Day}", "eles estão saindo da mata — mantenha a fogueira acesa");
                Director.BeginNight(Cycle.Day);
                Rig.Shake(0.4f);
            };
            Cycle.OnDay += () =>
            {
                Director.EndNight();
                Audio.Play("dawn", 0.7f);
                Hud.Banner("AMANHECEU", $"você sobreviveu à noite {Cycle.Day - 1}");
            };

            var spawn = SpawnPoint(0);
            Player.Respawn(spawn); Player.health = 100;
            foreach (Item i in System.Enum.GetValues(typeof(Item))) Player.inventory[i] = 0;
            Player.inventory[Item.Madeira] = 2;
            Player.inventory[Item.SementeCenoura] = 6;
            Player.inventory[Item.SementeAbobora] = 3;
            Player.water = PlayerController.WaterMax;
            Rig.Yaw = 0;
            Loading = false;
            Lock(true);
            Hud.Banner("FLORESTA VIVA", "colete madeira, plante a horta e mantenha o fogo aceso", 6f);
        }

        Vector3 SpawnPoint(int index)
        {
            var p = Fire.Pos + Quaternion.Euler(0, 200 + index * 40, 0) * Vector3.forward * 4.5f;
            p.y = World.Ground(p.x, p.z) + 0.2f;
            return p;
        }

        static void Lock(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        void Update()
        {
            if (Loading) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hud.Paused = !Hud.Paused;
                Time.timeScale = Hud.Paused ? 0 : 1;
                Lock(!Hud.Paused);
            }
            if (!Hud.Paused && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) Lock(true);
            float dt = Time.deltaTime;
            if (Hud.Paused) return;
            // teste: segurar T acelera o mundo ×10 (dia, fogueira e plantas)
            float worldDt = Input.GetKey(KeyCode.T) ? dt * 10f : dt;

            // câmera
            Rig.Look(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * MouseSensitivity, Input.mouseScrollDelta.y);
            if (Input.GetKeyDown(KeyCode.V)) Rig.FirstPerson = !Rig.FirstPerson;

            for (int k = 0; k < 4; k++) if (Input.GetKeyDown(KeyCode.Alpha1 + k)) selectedTool = k;

            // entrada local → comandos do jogador (no co-op, cada cliente manda o seu PlayerInput)
            var inp = new PlayerInput
            {
                move = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                                   (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)),
                yaw = Rig.Yaw,
                sprint = Input.GetKey(KeyCode.LeftShift),
                jump = Input.GetKeyDown(KeyCode.Space),
                use = Input.GetMouseButton(0) && Cursor.lockState == CursorLockMode.Locked,
                eat = Input.GetKeyDown(KeyCode.F),
                tool = selectedTool,
                nextSeed = Input.GetKeyDown(KeyCode.Q),
                aimOrigin = Rig.cam.transform.position,
                aimDir = Rig.cam.transform.forward,
            };
            var p = Player;
            p.FirstPerson = Rig.Blend > 0.7f;
            float hpBefore = p.health;
            p.Tick(inp, dt);
            if (p.health < hpBefore - 5) Rig.Shake(0.6f);

            // fogueira
            NearFire = p.alive && (p.transform.position - Fire.Pos).magnitude < 2.6f;
            if (NearFire && Input.GetKeyDown(KeyCode.E))
            {
                if (p.inventory[Item.Madeira] <= 0) Hud.Popup("SEM MADEIRA");
                else if (Fire.AddWood()) { p.inventory[Item.Madeira]--; Audio.Play("chop", 0.4f); }
            }

            // atalho de teste: N pula para o anoitecer
            if (Input.GetKeyDown(KeyCode.N)) Cycle.time = Cycle.IsNight ? 0.24f : 0.72f;

            Rig.Update(p.transform, dt);
            var camPos = Rig.cam.transform.position;
            Cycle.Update(worldDt, camPos);
            Wind.Update(dt, Cycle.Darkness);
            Fx.Update(dt, Rig.cam);
            Fire.Update(worldDt, Cycle.Darkness);
            Farm.Update(worldDt, Cycle.DayLength);
            Farm.ShowMarker(p.alive ? p.AimCell : null, p.AimOk);
            Life.Update(camPos, Cycle.Darkness, Cycle.time);
            Atmos.Update(dt, Rig.cam, Cycle.Darkness, Time.time, Cycle.SunDir);
            Flora.Draw(camPos);
            Director.Tick(Cycle.IsNight, Cycle.Darkness, Players, Rig.cam, dt);
            foreach (var c in Director.creatures)          // os Vultos também abrem caminho na grama
                if (c && (c.Position - camPos).sqrMagnitude < 30 * 30) Wind.Push(c.Position, 1.2f);
            Audio.Mix(Cycle.Darkness, Director.Danger);

            if (!p.alive)
            {
                RespawnT -= dt;
                if (RespawnT <= 0) p.Respawn(SpawnPoint(0));
            }
        }

        public void OnPlayerDied(PlayerController p)
        {
            RespawnT = 6;
            Audio.Play("screech", 0.8f);
            Hud.Popup("você perdeu metade do que carregava");
        }
    }
}
