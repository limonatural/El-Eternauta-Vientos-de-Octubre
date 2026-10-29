using UnityEngine;

namespace Eternauta.Beta
{
    // Cascarudo gigante: único enemigo. Camina lento, ve a unos 9 celdas con línea libre,
    // oye los disparos y muerde cada 1,5 s (20 % de la salud). No abre puertas.
    public class Bicho
    {
        public int indice;                 // posición en la lista de spawns (para guardar quién murió)
        public float x, y, angulo;         // angulo en radianes (hacia donde mira)
        public float vida = ContenidoJuego.VidaBicho;
        public bool vivo = true;
        public bool interior;
        public float tMuerte;              // segundos desde que murió (el cadáver desaparece a los 15)
        public float tMordida;             // animación de ataque (0..1)
        public float tGolpe;               // destello al recibir daño
        public float espera;               // tiempo hasta poder morder
        public float tPersecucion;         // sigue al jugador este tiempo después de perderlo de vista
        public float fase;                 // animación de caminata
        public bool camina;
        float objX, objY, tMerodeo;
        readonly float semilla;

        public const float Radio = 0.34f;
        public const float Velocidad = 1.45f;
        public const float Vision = 9f;
        public const float TiempoCadaver = 15f;

        public Bicho(int indice, float x, float y, bool interior, System.Random rnd)
        {
            this.indice = indice; this.x = x; this.y = y; this.interior = interior;
            angulo = (float)(rnd.NextDouble() * Mathf.PI * 2f);
            objX = x; objY = y;
            semilla = (float)rnd.NextDouble();
            tMerodeo = (float)rnd.NextDouble() * 3f;
            espera = 0.5f;
        }

        // Alerta por ruido (disparo): lo oyen los que están a menos de `alcance`.
        public void Oir(float px, float py, float alcance)
        {
            if (!vivo) return;
            float dx = px - x, dy = py - y;
            if (dx * dx + dy * dy < alcance * alcance) tPersecucion = Mathf.Max(tPersecucion, 6f);
        }

        public void Danar(float danio, float dirX, float dirY, float empuje, Mundo m)
        {
            if (!vivo) return;
            vida -= danio;
            tGolpe = 0.25f;
            tPersecucion = 8f;
            espera = Mathf.Max(espera, 0.6f);     // el golpe lo aturde un momento
            if (empuje > 0f) Mover(m, dirX * empuje, dirY * empuje);
            if (vida <= 0f) { vivo = false; tMuerte = 0f; camina = false; }
        }

        // Devuelve true si mordió al jugador en este cuadro.
        public bool Actualizar(Mundo m, Jugador j, float dt)
        {
            tGolpe = Mathf.Max(0f, tGolpe - dt);
            if (!vivo) { tMuerte += dt; return false; }
            tMordida = Mathf.Max(0f, tMordida - dt * 2.2f);
            espera -= dt;
            tPersecucion -= dt;

            float dx = j.x - x, dy = j.y - y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < Vision && j.vida > 0f && m.LineaLibre(x, y, j.x, j.y)) tPersecucion = Mathf.Max(tPersecucion, 3f);

            camina = false;
            if (tPersecucion > 0f && j.vida > 0f)
            {
                float objA = Mathf.Atan2(dy, dx);
                angulo = Giro(angulo, objA, dt * 7f);
                if (d > 0.85f)
                {
                    if (espera <= 0f) { Mover(m, Mathf.Cos(angulo) * Velocidad * dt, Mathf.Sin(angulo) * Velocidad * dt); camina = true; }
                }
                else if (espera <= 0f)
                {
                    espera = 1.5f;
                    tMordida = 1f;
                    return true;
                }
            }
            else
            {
                // merodea: elige un punto cercano y va despacio
                tMerodeo -= dt;
                float ex = objX - x, ey = objY - y;
                if (tMerodeo <= 0f || ex * ex + ey * ey < 0.1f)
                {
                    tMerodeo = 2.5f + semilla * 3f;
                    float a = Random.value * Mathf.PI * 2f, r = Random.value * 2.2f;
                    objX = x + Mathf.Cos(a) * r; objY = y + Mathf.Sin(a) * r;
                    if (Random.value < 0.4f) { objX = x; objY = y; }
                }
                else
                {
                    angulo = Giro(angulo, Mathf.Atan2(ey, ex), dt * 4f);
                    if (!Mover(m, Mathf.Cos(angulo) * Velocidad * 0.45f * dt, Mathf.Sin(angulo) * Velocidad * 0.45f * dt)) tMerodeo = 0f;
                    camina = true;
                }
            }
            if (camina) fase += dt * 7f;
            return false;
        }

        static float Giro(float a, float objetivo, float paso)
        {
            float delta = Mathf.DeltaAngle(a * Mathf.Rad2Deg, objetivo * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            return a + Mathf.Clamp(delta, -paso, paso);
        }

        bool Mover(Mundo m, float mx, float my)
        {
            bool movio = false;
            float nx = x + mx;
            if (!Choca(m, nx, y)) { x = nx; movio = true; }
            float ny = y + my;
            if (!Choca(m, x, ny)) { y = ny; movio = true; }
            return movio;
        }

        static bool Choca(Mundo m, float px, float py)
        {
            const float r = 0.28f;
            if (m.SolidaPunto(px - r, py - r) || m.SolidaPunto(px + r, py - r) || m.SolidaPunto(px - r, py + r) || m.SolidaPunto(px + r, py + r)) return true;
            // objetos sólidos (autos, muebles)
            foreach (var e in m.entidades)
            {
                if (!e.activa || !e.def.solido) continue;
                float dx = px - e.x, dy = py - e.y, min = r + e.def.radio * 0.8f;
                if (dx * dx + dy * dy < min * min) return true;
            }
            return false;
        }
    }
}
