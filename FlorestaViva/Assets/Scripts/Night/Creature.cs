using UnityEngine;

namespace Floresta
{
    /// <summary>
    /// Vulto: criatura da noite. Atravessa árvores (é meio espectro), espreita de longe,
    /// avança quando você não está olhando, ataca com as garras e foge da luz da fogueira.
    /// Também drena a fogueira se ficar na borda da luz. Some ao amanhecer.
    /// </summary>
    public class Creature : MonoBehaviour
    {
        enum State { Stalk, Charge, Attack, Flee, Fade }

        public Rig rig;
        public float hp = 60;
        World world;
        Campfire fire;
        Farm farm;
        Plot raid;                       // planta que veio comer
        public bool Eating { get; private set; }
        public Vector3 Position => transform.position;
        State state = State.Stalk;
        float stateT, phase, attackCd, orbit, speedMul, fade = 0, whisperT, curSpeed;
        Vector3 fleeFrom;
        bool watched;

        public bool Dead => state == State.Fade && fade <= 0;
        public float DistToPlayer { get; private set; } = 999;

        public static Creature Spawn(World w, Campfire f, Farm farm, Vector3 p, int night, bool raider)
        {
            var go = new GameObject("Vulto");
            go.layer = Layers.Creature;
            go.transform.position = p;
            var c = go.AddComponent<Creature>();
            c.world = w; c.fire = f; c.farm = farm;
            if (raider && farm != null) c.raid = farm.UnprotectedCrop(p);
            c.rig = Creature.MakeRig(go.transform);
            var col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.height = 2.6f; col.radius = 0.5f; col.center = new Vector3(0, 1.3f, 0);
            c.hp = 50 + night * 10;
            c.speedMul = Random.Range(0.85f, 1.15f) * (1 + Mathf.Min(night - 1, 5) * 0.06f);
            c.orbit = Random.Range(14f, 22f);
            c.phase = Random.value * 10;
            c.whisperT = Random.Range(4f, 10f);
            c.SetAlpha(0);
            return c;
        }

        static Rig MakeRig(Transform t) => Rig.Shade(t);

        void SetAlpha(float a)
        {
            // não há transparência no material low-poly: "surge da neblina" crescendo do chão
            rig.root.localScale = new Vector3(1, Mathf.Max(0.01f, a), 1);
        }

        public void Hit(float dmg, Vector3 from)
        {
            if (state == State.Fade) return;
            hp -= dmg;
            fleeFrom = from;
            Audio.Play("screech", 0.5f, 0.3f);
            if (hp <= 0) { state = State.Fade; fade = 1; return; }
            state = State.Flee; stateT = 1.4f;
        }

        public void Dissolve() { if (state != State.Fade) { state = State.Fade; fade = 1; } }

        /// <summary>Atualiza com o jogador mais próximo (ou null) e a direção para onde a câmera olha.</summary>
        public void Tick(PlayerController target, Vector3 camPos, Vector3 camFwd, float dt)
        {
            phase += dt * 6 * speedMul;
            stateT -= dt; attackCd -= dt;
            var pos = transform.position;

            if (state == State.Fade)
            {
                fade -= dt * 0.8f;
                SetAlpha(fade);
                transform.position += Vector3.up * dt * 0.5f;
                rig.AnimateShade(dt, 0, 0, false, false, false);
                if (fade <= 0) Destroy(gameObject);
                return;
            }
            fade = Mathf.Min(1, fade + dt * 0.6f);
            SetAlpha(fade);

            // a luz da fogueira queima
            float dFire = Flat(pos - fire.Pos).magnitude;
            if (fire.Lit && dFire < fire.SafeRadius)
            {
                if (state != State.Flee) Audio.Play("screech", 0.35f, 1.5f);
                state = State.Flee; stateT = 1.2f; fleeFrom = fire.Pos;
            }
            else if (fire.Lit && dFire < fire.SafeRadius + 3f) fire.Drain += 0.35f;   // suga a chama na borda da luz

            Vector3 goal; float speed;
            Eating = false;
            if (raid != null && (raid.crop == null || (fire.Lit && (raid.pos - fire.Pos).magnitude < fire.SafeRadius))) raid = farm?.UnprotectedCrop(pos);
            bool playerClose = target != null && target.alive && Flat(target.transform.position - pos).magnitude < 6f;
            if (raid != null && playerClose && state != State.Flee) { raid = null; state = State.Charge; Audio.Play("screech", 0.6f, 1f); }
            if (raid != null && state != State.Flee)
            {
                // vai até a plantação e come
                DistToPlayer = target != null ? Flat(target.transform.position - pos).magnitude : 999;
                goal = raid.pos; speed = 3.4f;
                if (Flat(raid.pos - pos).magnitude < 1.3f)
                {
                    speed = 0; Eating = true;
                    if (farm.Eat(raid, Time.deltaTime * 6f)) raid = null;
                }
            }
            else if (target == null || !target.alive)
            {
                // sem jogador: cerca a fogueira
                DistToPlayer = 999;
                goal = fire.Pos + Quaternion.Euler(0, Time.time * 8 + orbit * 10, 0) * Vector3.forward * (fire.SafeRadius + 2f);
                speed = 2.2f;
            }
            else
            {
                var tp = target.transform.position;
                DistToPlayer = Flat(tp - pos).magnitude;
                // o jogador está olhando? (para quem espreita, ser visto faz parar)
                var toMe = (pos + Vector3.up * 1.5f - camPos);
                watched = Vector3.Dot(camFwd, toMe.normalized) > 0.82f && toMe.magnitude < 45;

                switch (state)
                {
                    case State.Stalk:
                        // circula na neblina, chegando mais perto quando não é visto
                        float ang = Mathf.Atan2(pos.z - tp.z, pos.x - tp.x) + dt * 0.25f;
                        orbit = Mathf.MoveTowards(orbit, watched ? orbit : 6f, dt * (watched ? 0 : 1.1f));
                        goal = tp + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * orbit;
                        speed = watched ? 0.4f : 3.2f;
                        if (orbit <= 7f || DistToPlayer < 7f || (stateT < -12 && !watched)) { state = State.Charge; Audio.Play("screech", 0.6f, 1f); }
                        break;
                    case State.Charge:
                        goal = tp; speed = 6.4f;
                        if (DistToPlayer < 1.9f) { state = State.Attack; stateT = 0.6f; }
                        break;
                    case State.Attack:
                        goal = tp; speed = 1.2f;
                        if (stateT <= 0.25f && attackCd <= 0)
                        {
                            attackCd = 1.2f;
                            if (DistToPlayer < 2.6f) target.Damage(14);
                        }
                        if (stateT <= 0) { state = DistToPlayer < 2.5f ? State.Attack : State.Charge; stateT = 0.9f; }
                        break;
                    default: // Flee
                        var away = Flat(pos - fleeFrom).normalized;
                        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                        goal = pos + away * 10; speed = 7f;
                        if (stateT <= 0) { state = State.Stalk; orbit = Random.Range(12f, 18f); stateT = 0; }
                        break;
                }

                whisperT -= dt;
                if (whisperT <= 0 && DistToPlayer < 22) { whisperT = Random.Range(6f, 14f); Audio.Play("whisper", 0.35f, 2f); }
            }

            // movimento (desliza pelo chão; flutua sobre a água)
            var dir = Flat(goal - pos);
            if (dir.magnitude > 0.1f)
            {
                // acelera e freia (não muda de velocidade de uma vez)
                curSpeed = Mathf.MoveTowards(curSpeed, speed * speedMul, (speed * speedMul > curSpeed ? 6f : 10f) * dt);
                pos += dir.normalized * Mathf.Min(dir.magnitude, curSpeed * dt);
            }
            else curSpeed = Mathf.MoveTowards(curSpeed, 0, 10f * dt);
            // olha para onde vai; enquanto espreita, encara o jogador
            var face = Eating && raid != null ? Flat(raid.pos - pos) : state == State.Stalk && target != null ? Flat(target.transform.position - pos) : dir;
            if (face.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized), dt * 6);
            pos.y = Mathf.Max(world.Ground(pos.x, pos.z), World.WaterLevel - 0.2f) + Mathf.Sin(phase * 0.3f) * 0.05f;
            transform.position = pos;

            float swing = state == State.Attack ? Mathf.Clamp01(1 - stateT / 0.6f) : Eating ? Mathf.Abs(Mathf.Sin(phase * 0.8f)) * 0.6f : 0;
            rig.AnimateShade(dt, state == State.Stalk && watched ? 0 : curSpeed, swing, state == State.Stalk && watched, state == State.Charge, Eating);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
