using UnityEngine;

namespace Eternauta.Beta
{
    // Movimiento del protagonista (RF01) y supervivencia ante la nieve (RF05).
    public class Jugador
    {
        public float x, y;
        public float angulo;          // radianes; -PI/2 = norte
        public float inclinacion;     // grados; + = mirar hacia arriba
        public const float InclinacionMax = 70f;
        public float vida = 100f;     // 0..100 (1 segmento del HUD = 10 %)
        public float exposicion;      // 0..1, sube a la intemperie y baja bajo techo
        public bool traje;
        public bool invulnerable;     // herramienta de desarrollo
        public bool caminando;
        public bool bajoTecho;
        public bool bloqueadoNorte;  // intentó cruzar el puente antes de tiempo

        public const float Radio = 0.22f;
        public const float VelCaminar = 2.4f, VelCorrer = 3.8f, VelGiro = 2.4f;

        // Pérdida de salud por segundo a la intemperie (con traje) según la exposición.
        public const float DanioNieve = 0.55f;
        public const float RecuperacionTecho = 1.5f; // bajo techo se recupera hasta 30
        public const float VidaRecuperable = 30f;

        public void Actualizar(Mundo m, float avance, float lateral, float giro, float mirar, bool correr, float dt, float limiteNorte)
        {
            angulo += giro;
            inclinacion = Mathf.Clamp(inclinacion + mirar, -InclinacionMax, InclinacionMax);
            float dirX = Mathf.Cos(angulo), dirY = Mathf.Sin(angulo);
            float vel = correr ? VelCorrer : VelCaminar;
            if (vida <= 0f) vel *= 0.5f;
            float mx = (dirX * avance - dirY * lateral);
            float my = (dirY * avance + dirX * lateral);
            float len = Mathf.Sqrt(mx * mx + my * my);
            caminando = len > 0.01f;
            if (len > 1f) { mx /= len; my /= len; }
            mx *= vel * dt; my *= vel * dt;

            float nx = x + mx;
            if (!Choca(m, nx, y)) x = nx;
            float ny = y + my;
            if (y >= limiteNorte && ny < limiteNorte) { ny = limiteNorte; bloqueadoNorte = true; }
            else bloqueadoNorte = false;
            if (!Choca(m, x, ny)) y = ny;

            // Empujar fuera de los objetos sólidos (autos, árboles, muebles)
            foreach (var e in m.entidades)
            {
                if (!e.activa || !e.def.solido) continue;
                float dx = x - e.x, dy = y - e.y;
                float min = Radio + e.def.radio;
                float d2 = dx * dx + dy * dy;
                if (d2 < min * min && d2 > 0.000001f)
                {
                    float d = Mathf.Sqrt(d2);
                    float px = e.x + dx / d * min, py = e.y + dy / d * min;
                    if (!Choca(m, px, y)) x = px;
                    if (!Choca(m, x, py)) y = py;
                }
            }

            // Supervivencia
            bajoTecho = m.Interior(x, y);
            if (bajoTecho)
            {
                exposicion = Mathf.Max(0f, exposicion - dt * 0.25f);
                if (vida < VidaRecuperable) vida = Mathf.Min(VidaRecuperable, vida + RecuperacionTecho * dt);
            }
            else
            {
                exposicion = Mathf.Min(1f, exposicion + dt * (traje ? 0.03f : 0.25f));
                float danio = (traje ? DanioNieve : 8f) * (0.4f + exposicion);
                if (!invulnerable) vida = Mathf.Max(0f, vida - danio * dt);
            }
        }

        public bool Choca(Mundo m, float px, float py)
        {
            return m.SolidaPunto(px - Radio, py - Radio) || m.SolidaPunto(px + Radio, py - Radio) ||
                   m.SolidaPunto(px - Radio, py + Radio) || m.SolidaPunto(px + Radio, py + Radio) ||
                   m.SolidaPunto(px, py);
        }

        public void Curar(int cantidad) { vida = Mathf.Min(100f, vida + cantidad); }

        public int Segmentos() { return Mathf.CeilToInt(vida / 10f - 0.001f); }
    }
}
