using System.Collections.Generic;
using UnityEngine;

namespace Floresta
{
    /// <summary>Comandos de um jogador neste quadro (base para o co-op: o host recebe isto dos outros).</summary>
    public struct PlayerInput
    {
        public Vector2 move;      // x = lado, y = frente (relativo à câmera)
        public float yaw;         // direção da câmera (graus)
        public bool sprint, jump, use, eat;
        public int tool;          // 0 machado, 1 enxada, 2 sementes, 3 regador
        public bool nextSeed;     // Q: troca o tipo de semente
        public Vector3 aimOrigin, aimDir;   // raio da mira (1ª pessoa); o host refaz o raio no co-op
    }

    public enum Tool { Machado, Enxada, Sementes, Regador }

    /// <summary>Andarilho: movimento com CharacterController, coleta, vida e fome.</summary>
    public class PlayerController : MonoBehaviour
    {
        public CharacterController cc;
        public Rig rig;
        public float speed = 4.2f, sprintSpeed = 7f, jumpSpeed = 5.2f, gravity = 18f;
        public float health = 100, hunger = 100;
        public readonly Dictionary<Item, int> inventory = new();
        public bool alive = true;
        public float swing;               // 0..1 animação do golpe
        public bool FirstPerson;          // esconde o próprio corpo na 1ª pessoa
        Vector3 vel;
        float walkPhase, swingCd, hurtT, stepT, deadT;
        public float HurtFlash => Mathf.Clamp01(hurtT);
        public Gatherable Target { get; private set; }
        public string TargetLabel { get; private set; }
        public string TargetInfo { get; private set; }     // ex.: "Cenoura · 40% · seco"
        public Tool tool;
        public int seedIndex;                              // 0 cenoura, 1 abóbora
        public int water;                                  // cargas no regador
        public const int WaterMax = 6;
        public Vector2Int? AimCell { get; private set; }
        public bool AimOk { get; private set; }
        public CropDef SeedCrop => seedIndex == 0 ? Farm.Cenoura : Farm.Abobora;

        public static PlayerController Create(Vector3 pos, Color cloak)
        {
            var go = new GameObject("Jogador");
            go.transform.position = pos;
            var p = go.AddComponent<PlayerController>();
            p.cc = go.AddComponent<CharacterController>();
            p.cc.height = 1.8f; p.cc.radius = 0.35f; p.cc.center = new Vector3(0, 0.9f, 0); p.cc.stepOffset = 0.5f; p.cc.slopeLimit = 50;
            p.rig = Rig.Wanderer(cloak, go.transform);
            foreach (Item i in System.Enum.GetValues(typeof(Item))) p.inventory[i] = 0;
            return p;
        }

        public void Tick(PlayerInput inp, float dt)
        {
            if (!alive)
            {
                deadT += dt;
                rig.root.localRotation = Quaternion.Euler(-Mathf.Min(1, deadT * 2) * 85, 0, 0);
                return;
            }
            swingCd -= dt; hurtT -= dt * 2;

            // fome e vida
            hunger = Mathf.Max(0, hunger - dt * 0.09f);
            if (hunger <= 0) Damage(dt * 1.5f, false);
            else if (hunger > 60 && health < 100) health = Mathf.Min(100, health + dt * 0.6f);

            // movimento relativo à câmera
            var fwd = Quaternion.Euler(0, inp.yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, inp.yaw, 0) * Vector3.right;
            var dir = (fwd * inp.move.y + right * inp.move.x);
            if (dir.sqrMagnitude > 1) dir.Normalize();
            float sp = inp.sprint && hunger > 5 ? sprintSpeed : speed;
            var h = dir * sp;
            if (cc.isGrounded)
            {
                vel.y = -2f;
                if (inp.jump) { vel.y = jumpSpeed; Audio.Play("step", 0.5f); }
            }
            vel.y -= gravity * dt;
            cc.Move((h + Vector3.up * vel.y) * dt);
            if (transform.position.y < -20) transform.position = World.BaseCenter + Vector3.up * 5;

            bool moving = dir.sqrMagnitude > 0.01f;
            if (moving)
            {
                walkPhase += dt * (inp.sprint ? 13 : 9);
                // vira o corpo para onde anda (na 1ª pessoa, para onde olha)
                var look = FirstPerson ? fwd : dir;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), dt * 12);
                stepT -= dt * (inp.sprint ? 1.5f : 1);
                if (stepT <= 0 && cc.isGrounded) { stepT = 0.38f; Audio.Play("step", 0.25f); }
            }
            else if (FirstPerson) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd), dt * 12);

            // ferramenta
            if (inp.tool != (int)tool) { tool = (Tool)inp.tool; rig.SetTool(inp.tool); }
            if (inp.nextSeed) { seedIndex = 1 - seedIndex; Game.I?.Hud.Popup("Sementes de " + SeedCrop.name.ToLower()); }

            // alvo: canteiro mirado (fazenda) ou árvore/pedra/arbusto à frente
            FindTarget(FirstPerson ? fwd : transform.forward);
            AimFarm(inp);
            if (inp.use && swingCd <= 0) { swingCd = tool == Tool.Machado ? 0.45f : 0.35f; swing = 0.001f; }
            if (swing > 0)
            {
                float prev = swing;
                swing += dt / 0.4f;
                if (prev < 0.5f && swing >= 0.5f) Strike(FirstPerson ? fwd : transform.forward);
                if (swing >= 1) swing = 0;
            }
            if (inp.eat) Eat();

            rig.Animate(walkPhase, moving ? (inp.sprint ? 1.2f : 1) : 0, swing);
            foreach (var r in rig.renderers) r.enabled = !FirstPerson;
        }

        void FindTarget(Vector3 fwd)
        {
            Target = null; TargetLabel = null;
            var origin = transform.position + Vector3.up * 1.0f;
            var hits = Physics.OverlapSphere(origin + fwd * 1.3f, 1.2f, 1 << Layers.Gather);
            float best = float.MaxValue;
            foreach (var h in hits)
            {
                var g = h.GetComponent<Gatherable>();
                if (g == null) continue;
                float d = (h.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; Target = g; }
            }
            if (Target != null && tool != Tool.Machado) { TargetLabel = "USE O MACHADO (1)"; return; }
            if (Target != null)
                TargetLabel = Target.drop switch
                {
                    Item.Madeira => "CLIQUE: CORTAR ÁRVORE",
                    Item.Pedra => "CLIQUE: QUEBRAR PEDRA",
                    Item.Frutinha => "CLIQUE: COLHER FRUTINHAS",
                    Item.Cogumelo => "CLIQUE: PEGAR COGUMELOS",
                    _ => "CLIQUE: COLETAR",
                };
        }

        /// <summary>Escolhe o canteiro da fazenda: na 1ª pessoa, onde a mira toca o chão; na 3ª, o quadrado à frente.</summary>
        void AimFarm(PlayerInput inp)
        {
            AimCell = null; AimOk = false; TargetInfo = null;
            var farm = Game.I?.Farm;
            if (farm == null) return;
            Vector3? at = null;
            if (FirstPerson)
            {
                if (Physics.Raycast(inp.aimOrigin, inp.aimDir, out var hit, 6f, 1 << Layers.Ground) && (hit.point - transform.position).magnitude < 4.5f) at = hit.point;
            }
            else at = transform.position + transform.forward * 1.5f;
            if (at == null || !Game.I.World.InBase(at.Value)) { RiverPrompt(); return; }

            var cell = farm.CellOf(at.Value);
            var plot = farm.At(cell);
            bool farming = tool != Tool.Machado || (plot != null && plot.Ripe);
            if (!farming) return;
            AimCell = cell;
            TargetInfo = farm.Describe(plot);
            string why = null;
            if (plot != null && plot.Ripe) { AimOk = true; TargetLabel = "CLIQUE: COLHER " + plot.crop.name.ToUpper(); return; }
            switch (tool)
            {
                case Tool.Enxada:
                    AimOk = farm.CanTill(cell, out why);
                    TargetLabel = AimOk ? "CLIQUE: ARAR A TERRA" : why.ToUpper();
                    break;
                case Tool.Sementes:
                    var seed = SeedCrop.seed;
                    AimOk = plot != null && plot.crop == null && inventory[seed] > 0;
                    TargetLabel = plot == null ? "ARE A TERRA PRIMEIRO (ENXADA)" : plot.crop != null ? null : inventory[seed] <= 0 ? "SEM SEMENTES DE " + SeedCrop.name.ToUpper() : "CLIQUE: PLANTAR " + SeedCrop.name.ToUpper();
                    break;
                case Tool.Regador:
                    AimOk = plot != null && water > 0 && plot.wet < 0.95f;
                    TargetLabel = plot == null ? null : water <= 0 ? "REGADOR VAZIO — ENCHA NO RIO" : plot.wet >= 0.95f ? "JÁ ESTÁ MOLHADO" : "CLIQUE: REGAR";
                    break;
            }
            if (TargetLabel != null) Target = null;     // a ação da fazenda tem prioridade
        }

        /// <summary>Com o regador perto da água, oferece encher.</summary>
        void RiverPrompt()
        {
            if (tool != Tool.Regador || NearWater() == false) return;
            TargetLabel = water >= WaterMax ? "REGADOR CHEIO" : "CLIQUE: ENCHER O REGADOR";
        }

        bool NearWater()
        {
            var w = Game.I?.World;
            if (w == null) return false;
            var p = transform.position + transform.forward * 1.5f;
            return w.Height(p.x, p.z) < World.WaterLevel - 0.1f || w.Height(transform.position.x, transform.position.z) < World.WaterLevel + 0.2f;
        }

        void Strike(Vector3 fwd)
        {
            // criaturas primeiro (qualquer ferramenta serve, o machado machuca mais)
            var origin = transform.position + Vector3.up * 1.0f;
            foreach (var c in Physics.OverlapSphere(origin + fwd * 1.2f, 1.1f, 1 << Layers.Creature))
            {
                var cr = c.GetComponentInParent<Creature>();
                if (cr != null) { cr.Hit(tool == Tool.Machado ? 20 : 10, transform.position); Audio.Play("hit"); return; }
            }
            var farm = Game.I?.Farm;
            if (farm != null && AimCell != null)
            {
                var cell = AimCell.Value; var plot = farm.At(cell);
                if (plot != null && plot.Ripe)
                {
                    var (item, n, seeds) = farm.Harvest(cell);
                    Give(item, n); inventory[plot.crop?.seed ?? (item == Item.Cenoura ? Item.SementeCenoura : Item.SementeAbobora)] += seeds;
                    return;
                }
                if (tool == Tool.Enxada) { farm.Till(cell); return; }
                if (tool == Tool.Sementes && inventory[SeedCrop.seed] > 0 && farm.Plant(cell, SeedCrop)) { inventory[SeedCrop.seed]--; return; }
                if (tool == Tool.Regador && water > 0 && farm.Water(cell)) { water--; return; }
                if (tool != Tool.Machado) return;
            }
            if (tool == Tool.Regador && NearWater())
            {
                if (water < WaterMax) { water = WaterMax; Audio.Play("rustle", 0.6f); Game.I?.Hud.Popup("Regador cheio"); }
                return;
            }
            if (Target != null && tool == Tool.Machado)
            {
                int got = Target.Hit(transform.position);
                if (got > 0)
                {
                    Give(Target.drop, got);
                    // entre as folhas do arbusto às vezes há sementes silvestres
                    if (Target.drop == Item.Frutinha && Random.value < 0.35f) Give(Random.value < 0.6f ? Item.SementeCenoura : Item.SementeAbobora, 1);
                }
            }
        }

        public void Give(Item i, int n)
        {
            inventory[i] += n;
            Audio.Play("pickup", 0.4f);
            Game.I?.Hud.Popup($"+{n} {Hud.Name(i)}");
        }

        void Eat()
        {
            // come o que mata mais a fome sem desperdiçar
            float need = 100 - hunger;
            (Item item, float food, float hp)[] menu =
            {
                (Item.Abobora, 40, 6), (Item.Cenoura, 22, 2), (Item.Frutinha, 14, 0), (Item.Cogumelo, 28, -4),
            };
            foreach (var m in menu)
            {
                if (inventory[m.item] <= 0) continue;
                if (m.food > need + 15 && m.item != Item.Frutinha && HasSmaller(m.food)) continue;
                inventory[m.item]--; hunger = Mathf.Min(100, hunger + m.food); health = Mathf.Clamp(health + m.hp, 1, 100);
                Audio.Play("eat"); Game.I?.Hud.Popup("Comeu " + Hud.Name(m.item).ToLower());
                return;
            }
            Game.I?.Hud.Popup("NADA PARA COMER");

            bool HasSmaller(float f)
            {
                foreach (var m in menu) if (m.food < f && inventory[m.item] > 0 && m.item != Item.Cogumelo) return true;
                return false;
            }
        }

        public void Damage(float d, bool feedback = true)
        {
            if (!alive) return;
            health -= d;
            if (feedback) { hurtT = 1; Audio.Play("hurt"); }
            if (health <= 0) { health = 0; alive = false; deadT = 0; Game.I?.OnPlayerDied(this); }
        }

        public void Respawn(Vector3 p)
        {
            alive = true; health = 60; hunger = Mathf.Max(hunger, 40);
            cc.enabled = false; transform.position = p; cc.enabled = true;
            rig.root.localRotation = Quaternion.identity;
            // perde metade do que carregava
            foreach (Item i in System.Enum.GetValues(typeof(Item))) inventory[i] /= 2;
        }
    }
}
