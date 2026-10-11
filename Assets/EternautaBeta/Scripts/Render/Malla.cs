using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Zona de una textura (coordenadas UV de Unity: 0,0 = abajo a la izquierda).
    public struct Region
    {
        public float u0, v0, u1, v1;
        public Region(float u0, float v0, float u1, float v1) { this.u0 = u0; this.v0 = v0; this.u1 = u1; this.v1 = v1; }
        public static readonly Region Completa = new Region(0, 0, 1, 1);
        public Vector2 UV(float s, float t) { return new Vector2(u0 + (u1 - u0) * s, v0 + (v1 - v0) * t); }
    }

    // Arma mallas low-poly por código: cajas, cilindros, esferas y planos con color
    // por vértice (iluminación "horneada" estilo PS1 / Quake). No depende de nada de
    // Unity salvo Vector2/Vector3/Color32, así que se puede probar fuera del Editor.
    public class ConstructorMalla
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Vector2> uv2 = new List<Vector2>(); // x = interior (oscurece a negro), y = brilla solo
        public readonly List<Color32> col = new List<Color32>();
        public readonly List<int> tri = new List<int>();

        // Transformación actual (origen + ejes locales)
        Vector3 o = Vector3.zero, ex = Vector3.right, ey = Vector3.up, ez = Vector3.forward;
        readonly Stack<Vector3[]> pila = new Stack<Vector3[]>();

        public Region region = Region.Completa;   // textura que usan las caras por defecto
        public bool sombrear = true;              // luz direccional por cara
        public float interior;                     // se copia a uv2.x
        public float brillo;                       // se copia a uv2.y (0 = normal, 1 = emite luz)
        public System.Func<Vector3, Vector3> luzExtra; // luz horneada por vértice (RGB, opcional)

        static readonly Vector3 DirLuz = new Vector3(0.38f, 0.82f, 0.43f).normalized;

        public int Vertices { get { return v.Count; } }

        // ---------------- Transformación ----------------
        public void Push() { pila.Push(new[] { o, ex, ey, ez }); }
        public void Pop() { var s = pila.Pop(); o = s[0]; ex = s[1]; ey = s[2]; ez = s[3]; }

        public void Mover(float x, float y, float z) { o += ex * x + ey * y + ez * z; }
        public void Mover(Vector3 d) { Mover(d.x, d.y, d.z); }

        public void Escalar(float s) { ex *= s; ey *= s; ez *= s; }
        public void Escalar(float sx, float sy, float sz) { ex *= sx; ey *= sy; ez *= sz; }

        static void Rot(ref Vector3 a, ref Vector3 b, float grados)
        {
            float r = grados * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            Vector3 na = a * c + b * s, nb = b * c - a * s;
            a = na; b = nb;
        }

        // Rotaciones alrededor de los ejes locales (grados, sentido de Unity).
        public void RotarY(float g) { Rot(ref ez, ref ex, g); }
        public void RotarX(float g) { Rot(ref ey, ref ez, g); }
        public void RotarZ(float g) { Rot(ref ex, ref ey, g); }

        public Vector3 P(Vector3 p) { return o + ex * p.x + ey * p.y + ez * p.z; }
        public Vector3 P(float x, float y, float z) { return o + ex * x + ey * y + ez * z; }

        // ---------------- Primitivas ----------------
        public static Color32 Col(int r, int g, int b) { return new Color32(Lienzo.B(r), Lienzo.B(g), Lienzo.B(b), 255); }

        public static Color32 Mezclar(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
        }

        // El color del vértice se guarda a la mitad: el shader lo multiplica por 2,
        // así las luces cálidas pueden aclarar por encima del color de la textura.
        Color32 Sombra(Color32 c, Vector3 n, Vector3 pos)
        {
            float k = 0.5f;
            if (sombrear) k *= 0.6f + 0.4f * Mathf.Max(0f, Vector3.Dot(n, DirLuz)) + (n.y < -0.5f ? -0.08f : 0f);
            Vector3 l = luzExtra != null ? luzExtra(pos) : Vector3.one;
            return new Color32(Lienzo.B((int)(c.r * k * l.x)), Lienzo.B((int)(c.g * k * l.y)), Lienzo.B((int)(c.b * k * l.z)), 255);
        }

        // Cuadrilátero en coordenadas locales (a, b, c, d en sentido horario visto de frente).
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color, Region r, bool dobleCara = false)
        {
            QuadMundo(P(a), P(b), P(c), P(d), color, r, dobleCara);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color) { Quad(a, b, c, d, color, region); }

        // Cuadrilátero ya en coordenadas del mundo, con UV propias (para paredes con textura repetida).
        public void QuadMundo(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color, Region r, bool dobleCara = false)
        {
            QuadUV(a, b, c, d, color, r.UV(0, 0), r.UV(0, 1), r.UV(1, 1), r.UV(1, 0), dobleCara);
        }

        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, bool dobleCara = false)
        {
            Vector3 n = Vector3.Cross(b - a, d - a);
            if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - b, a - b);
            n = n.normalized;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            var e = new Vector2(interior, brillo);
            for (int k = 0; k < 4; k++) uv2.Add(e);
            col.Add(Sombra(color, n, a)); col.Add(Sombra(color, n, b)); col.Add(Sombra(color, n, c)); col.Add(Sombra(color, n, d));
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
            if (dobleCara)
            {
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
                tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
            }
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color32 color)
        {
            Vector3 wa = P(a), wb = P(b), wc = P(c);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa).normalized;
            int i = v.Count;
            v.Add(wa); v.Add(wb); v.Add(wc);
            uv.Add(region.UV(0, 0)); uv.Add(region.UV(0.5f, 1)); uv.Add(region.UV(1, 0));
            var e = new Vector2(interior, brillo);
            uv2.Add(e); uv2.Add(e); uv2.Add(e);
            col.Add(Sombra(color, n, wa)); col.Add(Sombra(color, n, wb)); col.Add(Sombra(color, n, wc));
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
        }

        // Caja centrada en (cx, cy, cz). "frente" = textura especial para la cara -Z (la que mira a la cámara).
        public void Caja(float cx, float cy, float cz, float sx, float sy, float sz, Color32 c) { Caja(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz), c); }

        public void Caja(Vector3 centro, Vector3 tam, Color32 c, Region? frente = null, Region? atras = null, Color32? arriba = null)
        {
            Vector3 h = tam * 0.5f;
            Vector3 p000 = centro + new Vector3(-h.x, -h.y, -h.z), p100 = centro + new Vector3(h.x, -h.y, -h.z);
            Vector3 p010 = centro + new Vector3(-h.x, h.y, -h.z), p110 = centro + new Vector3(h.x, h.y, -h.z);
            Vector3 p001 = centro + new Vector3(-h.x, -h.y, h.z), p101 = centro + new Vector3(h.x, -h.y, h.z);
            Vector3 p011 = centro + new Vector3(-h.x, h.y, h.z), p111 = centro + new Vector3(h.x, h.y, h.z);
            Color32 cf = c;
            Quad(p000, p010, p110, p100, frente.HasValue ? Blanco : cf, frente ?? region);   // -Z
            Quad(p101, p111, p011, p001, atras.HasValue ? Blanco : cf, atras ?? region);     // +Z
            Quad(p001, p011, p010, p000, cf, region);                                         // -X
            Quad(p100, p110, p111, p101, cf, region);                                         // +X
            Quad(p010, p011, p111, p110, arriba ?? cf, region);                               // +Y
            Quad(p001, p000, p100, p101, cf, region);                                         // -Y
        }

        public static readonly Color32 Blanco = new Color32(255, 255, 255, 255);
        public static readonly Color32 ColNieve = new Color32(226, 231, 238, 255);

        // Caja con una capa de nieve encima.
        public void CajaNevada(Vector3 centro, Vector3 tam, Color32 c, float nieve = 0.035f)
        {
            Caja(centro, tam, c);
            Caja(centro + new Vector3(0, tam.y * 0.5f + nieve * 0.5f, 0), new Vector3(tam.x + 0.01f, nieve, tam.z + 0.01f), ColNieve);
        }

        // Cilindro (o cono truncado) vertical con base en "base".
        public void Cilindro(Vector3 b, float r0, float r1, float alto, int lados, Color32 c, bool tapas = true, Color32? tapa = null)
        {
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = b + d0 * r0, b1 = b + d1 * r0, t0 = b + d0 * r1 + Vector3.up * alto, t1 = b + d1 * r1 + Vector3.up * alto;
                var reg = new Region(region.u0 + (region.u1 - region.u0) * i / lados, region.v0, region.u0 + (region.u1 - region.u0) * (i + 1) / lados, region.v1);
                Quad(b0, t0, t1, b1, c, reg);
                if (tapas)
                {
                    Vector3 ct = b + Vector3.up * alto;
                    if (r1 > 0.0001f) Tri(ct, t1, t0, tapa ?? c);
                    if (r0 > 0.0001f) Tri(b, b0, b1, c);
                }
            }
        }

        // Cilindro acostado a lo largo de X.
        public void CilindroX(Vector3 centro, float r, float largo, int lados, Color32 c, bool tapas = true)
        {
            Push();
            Mover(centro);
            RotarZ(90f);
            Cilindro(new Vector3(0, -largo * 0.5f, 0), r, r, largo, lados, c, tapas);
            Pop();
        }

        // Cilindro acostado a lo largo de Z.
        public void CilindroZ(Vector3 centro, float r, float largo, int lados, Color32 c, bool tapas = true)
        {
            Push();
            Mover(centro);
            RotarX(90f);
            Cilindro(new Vector3(0, -largo * 0.5f, 0), r, r, largo, lados, c, tapas);
            Pop();
        }

        // Esfera low-poly.
        public void Esfera(Vector3 centro, float r, int seg, int anillos, Color32 c, float escalaY = 1f)
        {
            for (int j = 0; j < anillos; j++)
            {
                float t0 = Mathf.PI * j / anillos, t1 = Mathf.PI * (j + 1) / anillos;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = Mathf.PI * 2f * i / seg, a1 = Mathf.PI * 2f * (i + 1) / seg;
                    Vector3 A = Punto(centro, r, t0, a0, escalaY), B = Punto(centro, r, t0, a1, escalaY);
                    Vector3 C = Punto(centro, r, t1, a1, escalaY), D = Punto(centro, r, t1, a0, escalaY);
                    if (j == 0) Tri(A, C, D, c);
                    else if (j == anillos - 1) Tri(A, B, D, c);
                    else Quad(D, A, B, C, c);
                }
            }
        }

        static Vector3 Punto(Vector3 c, float r, float theta, float phi, float ey)
        {
            return c + new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi) * r, Mathf.Cos(theta) * r * ey, Mathf.Sin(theta) * Mathf.Sin(phi) * r);
        }

        // Palo / barra entre dos puntos (sección cuadrada).
        public void Barra(Vector3 a, Vector3 b, float grosor, Color32 c)
        {
            Vector3 d = b - a;
            float largo = d.magnitude;
            if (largo < 1e-5f) return;
            Vector3 f = d / largo;
            Vector3 lado = Mathf.Abs(f.y) > 0.95f ? Vector3.right : Vector3.Cross(Vector3.up, f).normalized;
            Vector3 arriba = Vector3.Cross(f, lado).normalized;
            float g = grosor * 0.5f;
            Vector3[] q = new Vector3[8];
            for (int k = 0; k < 2; k++)
            {
                Vector3 p = k == 0 ? a : b;
                q[k * 4 + 0] = p + (-lado - arriba) * g;
                q[k * 4 + 1] = p + (lado - arriba) * g;
                q[k * 4 + 2] = p + (lado + arriba) * g;
                q[k * 4 + 3] = p + (-lado + arriba) * g;
            }
            for (int s = 0; s < 4; s++)
            {
                int s1 = (s + 1) % 4;
                Quad(q[s1], q[4 + s1], q[4 + s], q[s], c);
            }
            Quad(q[3], q[2], q[1], q[0], c);
            Quad(q[4], q[5], q[6], q[7], c);
        }

        // Tubo (cilindro o cono) entre dos puntos: brazos, piernas, ramas, caños.
        public void Tubo(Vector3 a, Vector3 b, float r0, float r1, int lados, Color32 c, bool tapas = true)
        {
            Vector3 d = b - a;
            float largo = d.magnitude;
            if (largo < 1e-5f) return;
            Vector3 f = d / largo;
            Vector3 lado = Mathf.Abs(f.y) > 0.95f ? Vector3.right : Vector3.Cross(Vector3.up, f).normalized;
            Vector3 arriba = Vector3.Cross(f, lado).normalized;
            var pa = new Vector3[lados];
            var pb = new Vector3[lados];
            for (int i = 0; i < lados; i++)
            {
                float ang = Mathf.PI * 2f * i / lados;
                Vector3 radial = lado * Mathf.Cos(ang) + arriba * Mathf.Sin(ang);
                pa[i] = a + radial * r0;
                pb[i] = b + radial * r1;
            }
            for (int i = 0; i < lados; i++)
            {
                int j = (i + 1) % lados;
                Quad(pa[j], pb[j], pb[i], pa[i], c);
                if (tapas)
                {
                    if (r1 > 0.0001f) Tri(b, pb[i], pb[j], c);
                    if (r0 > 0.0001f) Tri(a, pa[j], pa[i], c);
                }
            }
        }

        // Agrega otra malla (ya construida en coordenadas del mundo) aplicando la transformación actual.
        public void Unir(ConstructorMalla otra)
        {
            int baseIdx = v.Count;
            foreach (var p in otra.v) v.Add(P(p));
            uv.AddRange(otra.uv);
            uv2.AddRange(otra.uv2);
            col.AddRange(otra.col);
            foreach (int i in otra.tri) tri.Add(baseIdx + i);
        }

        // Límites (para centrar íconos y sombras).
        public void Limites(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(1e9f, 1e9f, 1e9f); max = -min;
            foreach (var p in v) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            if (v.Count == 0) { min = max = Vector3.zero; }
        }
    }

    // Junta varias imágenes chicas en una sola textura (los modelos usan una sola textura).
    public class Atlas
    {
        public readonly int tam;
        public readonly Color32[] px; // fila 0 = abajo (como Unity)
        int x, y, altoFila;
        readonly Dictionary<string, Region> regiones = new Dictionary<string, Region>();

        public Atlas(int tam)
        {
            this.tam = tam;
            px = new Color32[tam * tam];
        }

        public Region Agregar(string nombre, Lienzo l)
        {
            Region r;
            if (regiones.TryGetValue(nombre, out r)) return r;
            if (x + l.w + 2 > tam) { x = 0; y += altoFila + 2; altoFila = 0; }
            if (y + l.h + 2 > tam) { Debug.LogWarning("[Eternauta] Atlas lleno: " + nombre); return Region.Completa; }
            int ox = x + 1, oy = y + 1;
            // borde repetido de 1 píxel para que no se mezclen los vecinos
            for (int j = -1; j <= l.h; j++)
                for (int i = -1; i <= l.w; i++)
                {
                    int si = Mathf.Clamp(i, 0, l.w - 1), sj = Mathf.Clamp(j, 0, l.h - 1);
                    var c = l.px[sj * l.w + si];
                    int dx = ox + i, dy = oy + (l.h - 1 - j);
                    px[dy * tam + dx] = c;
                }
            float m = 0.5f / tam;
            r = new Region((ox / (float)tam) + m, (oy / (float)tam) + m, ((ox + l.w) / (float)tam) - m, ((oy + l.h) / (float)tam) - m);
            regiones[nombre] = r;
            x += l.w + 2;
            altoFila = Mathf.Max(altoFila, l.h);
            return r;
        }

        public bool Tiene(string nombre) { return regiones.ContainsKey(nombre); }
        public Region this[string nombre] { get { return regiones[nombre]; } }
    }
}
