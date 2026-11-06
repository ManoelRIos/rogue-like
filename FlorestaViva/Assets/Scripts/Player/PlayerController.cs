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
    }

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

            // alvo para coletar/atacar: o que está à frente
            FindTarget(FirstPerson ? fwd : transform.forward);
            if (inp.use && swingCd <= 0) { swingCd = 0.45f; swing = 0.001f; }
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

        void Strike(Vector3 fwd)
        {
            // criaturas primeiro
            var origin = transform.position + Vector3.up * 1.0f;
            foreach (var c in Physics.OverlapSphere(origin + fwd * 1.2f, 1.1f, 1 << Layers.Creature))
            {
                var cr = c.GetComponentInParent<Creature>();
                if (cr != null) { cr.Hit(20, transform.position); Audio.Play("hit"); return; }
            }
            if (Target != null)
            {
                int got = Target.Hit(transform.position);
                if (got > 0) Give(Target.drop, got);
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
            if (inventory[Item.Frutinha] > 0) { inventory[Item.Frutinha]--; hunger = Mathf.Min(100, hunger + 18); Audio.Play("eat"); }
            else if (inventory[Item.Cogumelo] > 0) { inventory[Item.Cogumelo]--; hunger = Mathf.Min(100, hunger + 28); health = Mathf.Max(1, health - 4); Audio.Play("eat"); }
            else Game.I?.Hud.Popup("NADA PARA COMER");
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
