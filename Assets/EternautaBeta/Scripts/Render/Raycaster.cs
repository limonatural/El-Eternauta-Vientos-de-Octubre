using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Motor gráfico estilo Doom / Wolfenstein: "juego 2D con representación en
    // primera persona" (Etapa 11, wireframe 02). Lanza un rayo por columna sobre
    // la grilla del mapa y dibuja paredes, pisos, techo, cielo y sprites en una
    // imagen de baja resolución que después se agranda con píxeles duros.
    public class Raycaster
    {
        public int W { get; private set; }
        public readonly int H;
        public Texture2D Textura { get; private set; }

        Color32[] buf, subida;
        float[] zbuf;
        float[] distFila;

        // Ajustes de cámara
        public float fovGrados = 72f;
        const float AltoCamara = 0.5f;

        // Efectos (los controla el juego)
        public float visibilidadObelisco = 0.3f;
        public float escarcha;      // 0..1 bordes congelados por exposición a la nieve
        public float alertaRoja;    // 0..1 bordes rojos con salud baja
        public float destello;      // 0..1 destello al recoger algo
        public float fundido;       // 0..1 pantalla a negro
        public bool mostrarManos = true;
        public bool conTraje;
        public float faseBalanceo;  // avanza al caminar
        public float ampBalanceo;   // 0..1
        public bool efectoVHS = true;

        readonly Dictionary<char, Lienzo> paredExt = new Dictionary<char, Lienzo>();
        readonly Dictionary<char, Lienzo> paredInt = new Dictionary<char, Lienzo>();
        readonly Lienzo pisoNieve, pisoAsfalto, pisoMadera, pisoBaldosas, techo;
        readonly Color32[] cielo;
        readonly byte[] siluetas;
        readonly Lienzo manosSin, manosCon;

        readonly float[] nieblaExt = new float[1024];
        readonly float[] nieblaInt = new float[1024];
        static readonly Color32 ColorNieblaExt = new Color32(114, 122, 134, 255);
        static readonly Color32 ColorNieblaInt = new Color32(6, 8, 10, 255);
        static readonly Color32 ColorSilueta = new Color32(34, 38, 46, 255);

        float[] vineta;
        readonly sbyte[] grano = new sbyte[4096];
        int cuadro;

        // Nieve que cae (partículas en el mundo)
        const int Copos = 520;
        readonly float[] cx = new float[Copos], cy = new float[Copos], cz = new float[Copos], cv = new float[Copos];
        readonly System.Random rnd = new System.Random(5);

        readonly List<Entidad> visibles = new List<Entidad>();

        public Raycaster(int alto)
        {
            H = alto;
            distFila = new float[H];

            paredExt['#'] = ArteProcedural.Ladrillo(true, 1);
            paredInt['#'] = ArteProcedural.Ladrillo(false, 2);
            paredExt['H'] = ArteProcedural.Revoque(3);
            paredInt['H'] = ArteProcedural.Empapelado(4);
            paredExt['P'] = ArteProcedural.Persiana(5);
            paredInt['P'] = ArteProcedural.Azulejos(6);
            paredExt['K'] = ArteProcedural.CartelAlmacen(7);
            paredInt['K'] = paredInt['P'];
            paredExt['W'] = ArteProcedural.VentanaTapiada(8);
            paredInt['W'] = paredInt['#'];
            paredExt['C'] = ArteProcedural.CartelPuente(9);
            paredInt['C'] = paredExt['C'];
            paredExt['B'] = ArteProcedural.Baranda(10);
            paredInt['B'] = paredExt['B'];
            paredExt['D'] = ArteProcedural.Puerta(11);
            paredInt['D'] = paredExt['D'];

            pisoNieve = ArteProcedural.PisoNieve(12);
            pisoAsfalto = ArteProcedural.PisoAsfalto(13);
            pisoMadera = ArteProcedural.PisoMadera(14);
            pisoBaldosas = ArteProcedural.PisoBaldosas(15);
            techo = ArteProcedural.Techo(16);
            ArteProcedural.Cielo(out cielo, out siluetas);
            manosSin = ArteProcedural.Manos(false);
            manosCon = ArteProcedural.Manos(true);

            for (int i = 0; i < nieblaExt.Length; i++)
            {
                float d = i / 16f;
                nieblaExt[i] = 1f - Mathf.Exp(-d * 0.14f);
                nieblaInt[i] = 1f - Mathf.Exp(-d * 0.2f);
            }
            for (int i = 0; i < grano.Length; i++) grano[i] = (sbyte)rnd.Next(-7, 8);
            for (int i = 0; i < Copos; i++) { cx[i] = 9999f; cz[i] = (float)rnd.NextDouble() * 1.3f; cv[i] = 0.25f + (float)rnd.NextDouble() * 0.35f; }

            AjustarAspecto(16f / 9f);
        }

        // La imagen interna siempre tiene H filas; el ancho sigue la proporción de la pantalla.
        public void AjustarAspecto(float aspecto)
        {
            int ancho = Mathf.Clamp(Mathf.RoundToInt(H * aspecto), 240, 520);
            if (Textura != null && ancho == W) return;
            W = ancho;
            buf = new Color32[W * H];
            subida = new Color32[W * H];
            zbuf = new float[W];
            if (Textura != null) Object.Destroy(Textura);
            Textura = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Textura.filterMode = FilterMode.Point;
            Textura.wrapMode = TextureWrapMode.Clamp;
            vineta = new float[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W * 0.5f) / (W * 0.5f), dy = (y - H * 0.5f) / (H * 0.5f);
                    vineta[y * W + x] = Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.8f + dy * dy) - 0.35f) / 0.85f;
                }
        }

        static Color32 Sombrear(Color32 c, float luz, float niebla, Color32 n)
        {
            float k = luz * (1f - niebla);
            return new Color32(
                (byte)Mathf.Min(255f, c.r * k + n.r * niebla),
                (byte)Mathf.Min(255f, c.g * k + n.g * niebla),
                (byte)Mathf.Min(255f, c.b * k + n.b * niebla), 255);
        }

        static float Tabla(float[] t, float d)
        {
            int i = (int)(d * 16f);
            return t[i < 0 ? 0 : (i >= t.Length ? t.Length - 1 : i)];
        }

        Lienzo TexturaPiso(char c)
        {
            switch (c)
            {
                case '=': return pisoAsfalto;
                case ',': case 'd': case 'D': return pisoMadera;
                case ';': return pisoBaldosas;
                default: return pisoNieve;
            }
        }

        public void Dibujar(Mundo m, float px, float py, float angulo, float dt, Entidad resaltada)
        {
            cuadro++;
            float dirX = Mathf.Cos(angulo), dirY = Mathf.Sin(angulo);
            float largoPlano = Mathf.Tan(fovGrados * 0.5f * Mathf.Deg2Rad);
            float planoX = -dirY * largoPlano, planoY = dirX * largoPlano;
            float escala = W / (2f * largoPlano); // píxeles por unidad a distancia 1

            float bob = Mathf.Sin(faseBalanceo * 2f) * 2.2f * ampBalanceo;
            float horizonte = H * 0.5f + bob;

            for (int y = 0; y < H; y++)
            {
                float fy = y + 0.5f;
                if (fy > horizonte) distFila[y] = AltoCamara * escala / (fy - horizonte);
                else if (fy < horizonte) distFila[y] = (1f - AltoCamara) * escala / (horizonte - fy);
                else distFila[y] = 1000f;
            }

            int T = ArteProcedural.T;
            int CW = ArteProcedural.CieloAncho, CH = ArteProcedural.CieloAlto;

            // ---------- Paredes, pisos, techo y cielo (columna por columna) ----------
            for (int x = 0; x < W; x++)
            {
                float camX = 2f * x / W - 1f;
                float rx = dirX + planoX * camX, ry = dirY + planoY * camX;

                int mx = (int)px, my = (int)py;
                float ddx = rx == 0 ? 1e30f : Mathf.Abs(1f / rx);
                float ddy = ry == 0 ? 1e30f : Mathf.Abs(1f / ry);
                int pasoX, pasoY;
                float ladoX, ladoY;
                if (rx < 0) { pasoX = -1; ladoX = (px - mx) * ddx; } else { pasoX = 1; ladoX = (mx + 1f - px) * ddx; }
                if (ry < 0) { pasoY = -1; ladoY = (py - my) * ddy; } else { pasoY = 1; ladoY = (my + 1f - py) * ddy; }

                int prevX = mx, prevY = my, lado = 0;
                char c = '#';
                for (int paso = 0; paso < 96; paso++)
                {
                    if (ladoX < ladoY) { ladoX += ddx; mx += pasoX; lado = 0; }
                    else { ladoY += ddy; my += pasoY; lado = 1; }
                    c = m.Celda(mx, my);
                    if (Mundo.EsPared(c)) break;
                    prevX = mx; prevY = my;
                }
                float perp = lado == 0 ? ladoX - ddx : ladoY - ddy;
                if (perp < 0.0001f) perp = 0.0001f;
                zbuf[x] = perp;

                bool ladoInterior = m.Interior(prevX, prevY);
                Lienzo tex;
                if (!(ladoInterior ? paredInt : paredExt).TryGetValue(c, out tex)) tex = paredExt['#'];

                float paredX = lado == 0 ? py + perp * ry : px + perp * rx;
                paredX -= Mathf.Floor(paredX);
                int texX = (int)(paredX * T);
                if ((lado == 0 && rx > 0) || (lado == 1 && ry < 0)) texX = T - texX - 1;
                texX = Mathf.Clamp(texX, 0, T - 1);

                float arriba = horizonte - (1f - AltoCamara) * escala / perp;
                float abajo = horizonte + AltoCamara * escala / perp;
                float luzPared = lado == 1 ? 0.78f : 1f;
                float nieblaPared = Tabla(ladoInterior ? nieblaInt : nieblaExt, perp);
                var colorNieblaPared = ladoInterior ? ColorNieblaInt : ColorNieblaExt;
                if (ladoInterior) luzPared *= 0.9f;

                float ang = Mathf.Atan2(ry, rx) / (2f * Mathf.PI);
                int u = (int)(((ang % 1f) + 1f) % 1f * CW) % CW;

                for (int y = 0; y < H; y++)
                {
                    int idx = y * W + x;
                    float fy = y + 0.5f;
                    if (fy >= arriba && fy < abajo)
                    {
                        int texY = (int)((fy - arriba) / (abajo - arriba) * T);
                        if (texY > T - 1) texY = T - 1;
                        var col = tex.px[texY * T + texX];
                        if (col.a > 0)
                        {
                            buf[idx] = Sombrear(col, luzPared, nieblaPared, colorNieblaPared);
                            continue;
                        }
                    }

                    float d = distFila[y];
                    float wx = px + rx * d, wy = py + ry * d;
                    int cxl = Mathf.FloorToInt(wx), cyl = Mathf.FloorToInt(wy);
                    bool interior = m.Interior(cxl, cyl);
                    int tx = (int)((wx - cxl) * T) & (T - 1);
                    int ty = (int)((wy - cyl) * T) & (T - 1);

                    if (fy > horizonte)
                    {
                        var piso = TexturaPiso(m.Celda(cxl, cyl));
                        buf[idx] = interior
                            ? Sombrear(piso.px[ty * T + tx], 0.82f, Tabla(nieblaInt, d), ColorNieblaInt)
                            : Sombrear(piso.px[ty * T + tx], 1f, Tabla(nieblaExt, d), ColorNieblaExt);
                    }
                    else if (interior)
                    {
                        buf[idx] = Sombrear(techo.px[ty * T + tx], 0.7f, Tabla(nieblaInt, d), ColorNieblaInt);
                    }
                    else
                    {
                        int v = (int)(fy / Mathf.Max(1f, horizonte) * CH);
                        if (v > CH - 1) v = CH - 1;
                        var cc = cielo[v * CW + u];
                        byte s = siluetas[v * CW + u];
                        if (s > 0)
                        {
                            float k = (s == 255 ? visibilidadObelisco : visibilidadObelisco * 0.75f) * (s / 255f);
                            cc = Color32.Lerp(cc, ColorSilueta, k);
                        }
                        buf[idx] = cc;
                    }
                }
            }

            // ---------- Sprites (de atrás hacia adelante) ----------
            visibles.Clear();
            foreach (var e in m.entidades)
            {
                if (!e.activa) continue;
                float sx = e.x - px, sy = e.y - py;
                e.dist = sx * sx + sy * sy;
                if (e.dist < 900f) visibles.Add(e);
            }
            visibles.Sort((a, b) => b.dist.CompareTo(a.dist));
            float invDet = 1f / (planoX * dirY - dirX * planoY);
            foreach (var e in visibles)
            {
                float sx = e.x - px, sy = e.y - py;
                float tX = invDet * (dirY * sx - dirX * sy);
                float tY = invDet * (-planoY * sx + planoX * sy);
                if (tY < 0.12f) continue;
                float centro = W * 0.5f * (1f + tX / tY);
                float anchoPx = e.sprite.ancho * escala / tY;
                float arriba = horizonte - (e.sprite.alto - AltoCamara) * escala / tY;
                float abajo = horizonte + AltoCamara * escala / tY;
                int x0 = Mathf.Max(0, (int)(centro - anchoPx * 0.5f));
                int x1 = Mathf.Min(W - 1, (int)(centro + anchoPx * 0.5f));
                int y0 = Mathf.Max(0, (int)arriba), y1 = Mathf.Min(H - 1, (int)abajo);
                var img = e.sprite.img;
                bool interior = m.Interior(e.x, e.y);
                float niebla = img.brillaSola ? 0f : Tabla(interior ? nieblaInt : nieblaExt, tY);
                var colorNiebla = interior ? ColorNieblaInt : ColorNieblaExt;
                float luz = img.brillaSola ? 1.15f : (interior ? 0.85f : 1f);
                bool marcar = e == resaltada;
                for (int x = x0; x <= x1; x++)
                {
                    if (tY >= zbuf[x]) continue;
                    int ix = (int)((x + 0.5f - (centro - anchoPx * 0.5f)) / anchoPx * img.w);
                    if (ix < 0 || ix >= img.w) continue;
                    for (int y = y0; y <= y1; y++)
                    {
                        int iy = (int)((y + 0.5f - arriba) / (abajo - arriba) * img.h);
                        if (iy < 0 || iy >= img.h) continue;
                        var col = img.px[iy * img.w + ix];
                        if (col.a == 0) continue;
                        if (marcar && ((x + y) % 3 != 0) &&
                            (img.Get(ix - 1, iy).a == 0 || img.Get(ix + 1, iy).a == 0 || img.Get(ix, iy - 1).a == 0 || img.Get(ix, iy + 1).a == 0))
                        {
                            buf[y * W + x] = new Color32(91, 126, 153, 255); // azul de selección (Etapa 12)
                            continue;
                        }
                        buf[y * W + x] = Sombrear(col, luz, niebla, colorNiebla);
                    }
                }
            }

            // ---------- Nieve que cae ----------
            float viento = Mathf.Sin(Time.time * 0.3f) * 0.25f + 0.35f;
            for (int i = 0; i < Copos; i++)
            {
                if (Mathf.Abs(cx[i] - px) > 7f || Mathf.Abs(cy[i] - py) > 7f)
                {
                    cx[i] = px + (float)(rnd.NextDouble() * 14 - 7);
                    cy[i] = py + (float)(rnd.NextDouble() * 14 - 7);
                }
                cz[i] -= cv[i] * dt;
                cx[i] += viento * dt;
                cy[i] += Mathf.Sin(Time.time * 1.7f + i) * 0.08f * dt;
                if (cz[i] < 0f)
                {
                    cz[i] = 1.25f;
                    cx[i] = px + (float)(rnd.NextDouble() * 14 - 7);
                    cy[i] = py + (float)(rnd.NextDouble() * 14 - 7);
                }
                if (m.Interior(cx[i], cy[i]) || m.Solida(Mathf.FloorToInt(cx[i]), Mathf.FloorToInt(cy[i]))) continue;
                float sx = cx[i] - px, sy = cy[i] - py;
                float tY = invDet * (-planoY * sx + planoX * sy);
                if (tY < 0.2f) continue;
                float tX = invDet * (dirY * sx - dirX * sy);
                int sxp = (int)(W * 0.5f * (1f + tX / tY));
                if (sxp < 0 || sxp >= W || tY >= zbuf[sxp]) continue;
                int syp = (int)(horizonte + (AltoCamara - cz[i]) * escala / tY);
                int tam = Mathf.Clamp((int)(0.022f * escala / tY), 1, 3);
                float n = Tabla(nieblaExt, tY) * 0.6f;
                var blanco = Sombrear(new Color32(236, 240, 248, 255), 1f, n, ColorNieblaExt);
                for (int a = 0; a < tam; a++)
                    for (int b = 0; b < tam; b++)
                    {
                        int xx = sxp + a, yy = syp + b;
                        if (xx < W && yy >= 0 && yy < H) buf[yy * W + xx] = blanco;
                    }
            }

            // ---------- Mano con linterna ----------
            if (mostrarManos)
            {
                var mano = conTraje ? manosCon : manosSin;
                float esc = H / 200f * 1.25f;
                int anchoM = (int)(mano.w * esc), altoM = (int)(mano.h * esc);
                int ox = (int)(W * 0.62f + Mathf.Cos(faseBalanceo) * 4f * ampBalanceo);
                int oy = H - altoM + 4 + (int)(Mathf.Abs(Mathf.Sin(faseBalanceo)) * 4f * ampBalanceo);
                for (int j = 0; j < altoM; j++)
                {
                    int y = oy + j;
                    if (y < 0 || y >= H) continue;
                    int iy = (int)(j / esc);
                    for (int i = 0; i < anchoM; i++)
                    {
                        int x = ox + i;
                        if (x < 0 || x >= W) continue;
                        var col = mano.px[iy * mano.w + (int)(i / esc)];
                        if (col.a > 0) buf[y * W + x] = col;
                    }
                }
            }

            // ---------- Post-proceso: escarcha, alerta, destello, grano VHS, fundido ----------
            var hielo = new Color32(206, 224, 240, 255);
            var rojo = new Color32(126, 42, 43, 255);
            var flash = new Color32(255, 240, 200, 255);
            int g0 = (cuadro * 1543) & 4095;
            for (int y = 0; y < H; y++)
            {
                bool lineaVHS = efectoVHS && (y & 1) == 1;
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    var c = buf[idx];
                    float v = vineta[idx];
                    if (escarcha > 0.01f)
                    {
                        float k = Mathf.Clamp01(v * v * 1.6f * escarcha + (grano[(idx * 7) & 4095] > 4 ? 0.12f * escarcha * v : 0f));
                        c = Color32.Lerp(c, hielo, k);
                    }
                    if (alertaRoja > 0.01f) c = Color32.Lerp(c, rojo, Mathf.Clamp01(v * v * alertaRoja));
                    if (destello > 0.01f) c = Color32.Lerp(c, flash, destello * 0.35f);
                    if (efectoVHS)
                    {
                        int gr = grano[(idx + g0) & 4095];
                        int r = c.r + gr, gg = c.g + gr, b = c.b + gr;
                        if (lineaVHS) { r = r * 15 / 16; gg = gg * 15 / 16; b = b * 15 / 16; }
                        c = new Color32(Lienzo.B(r), Lienzo.B(gg), Lienzo.B(b), 255);
                    }
                    if (fundido > 0.001f) c = Color32.Lerp(c, new Color32(0, 0, 0, 255), fundido);
                    buf[idx] = c;
                }
            }

            // Unity guarda las texturas de abajo hacia arriba: se invierten las filas al subirla.
            for (int y = 0; y < H; y++) System.Array.Copy(buf, y * W, subida, (H - 1 - y) * W, W);
            Textura.SetPixels32(subida);
            Textura.Apply(false);
        }
    }
}
