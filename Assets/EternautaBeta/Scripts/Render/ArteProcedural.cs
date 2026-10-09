using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Imagen en memoria (fila 0 = arriba). Todas las texturas y sprites de la beta
    // se dibujan por código, en baja resolución, para lograr el aspecto "Doom".
    // Para reemplazarlas por arte propio: ver README_BETA.md.
    public class Lienzo
    {
        public readonly int w, h;
        public readonly Color32[] px;
        public bool brillaSola; // sprites que no se oscurecen con la niebla (velas, lámparas)

        public Lienzo(int w, int h)
        {
            this.w = w; this.h = h;
            px = new Color32[w * h];
        }

        public Color32 Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return new Color32(0, 0, 0, 0);
            return px[y * w + x];
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        public void Rellenar(Color32 c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        public void Rect(int x, int y, int rw, int rh, Color32 c)
        {
            for (int j = y; j < y + rh; j++)
                for (int i = x; i < x + rw; i++) Set(i, j, c);
        }

        public void Borde(int x, int y, int rw, int rh, Color32 c)
        {
            for (int i = x; i < x + rw; i++) { Set(i, y, c); Set(i, y + rh - 1, c); }
            for (int j = y; j < y + rh; j++) { Set(x, j, c); Set(x + rw - 1, j, c); }
        }

        public void Elipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int j = (int)(cy - ry) - 1; j <= (int)(cy + ry) + 1; j++)
                for (int i = (int)(cx - rx) - 1; i <= (int)(cx + rx) + 1; i++)
                {
                    float dx = (i + 0.5f - cx) / rx, dy = (j + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(i, j, c);
                }
        }

        public void Circulo(float cx, float cy, float r, Color32 c) { Elipse(cx, cy, r, r, c); }

        public void Linea(float x0, float y0, float x1, float y1, float grosor, Color32 c)
        {
            float len = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            int pasos = Mathf.Max(1, (int)(len * 2));
            for (int s = 0; s <= pasos; s++)
            {
                float t = s / (float)pasos;
                float x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t);
                if (grosor <= 1f) Set((int)x, (int)y, c);
                else Circulo(x, y, grosor * 0.5f, c);
            }
        }

        // Varía el brillo de los píxeles opacos de una zona.
        public void Ruido(int x, int y, int rw, int rh, int intensidad, System.Random rnd)
        {
            for (int j = y; j < y + rh; j++)
                for (int i = x; i < x + rw; i++)
                {
                    if (i < 0 || j < 0 || i >= w || j >= h) continue;
                    var c = px[j * w + i];
                    if (c.a == 0) continue;
                    int d = rnd.Next(-intensidad, intensidad + 1);
                    px[j * w + i] = new Color32(B(c.r + d), B(c.g + d), B(c.b + d), c.a);
                }
        }

        public void Ruido(int intensidad, System.Random rnd) { Ruido(0, 0, w, h, intensidad, rnd); }

        // Agrega nieve sobre los bordes superiores de todo lo que esté dibujado.
        public void NieveSobre(int grosor, System.Random rnd)
        {
            var copia = (Color32[])px.Clone();
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    if (copia[j * w + i].a == 0) continue;
                    bool arribaVacio = j == 0 || copia[(j - 1) * w + i].a == 0;
                    if (!arribaVacio) continue;
                    int g = grosor + rnd.Next(0, 2);
                    for (int k = 0; k < g; k++)
                    {
                        int yy = j + k;
                        if (yy >= h || copia[yy * w + i].a == 0) break;
                        int v = 222 + rnd.Next(-10, 12);
                        Set(i, yy, new Color32(B(v - 4), B(v), B(v + 8), 255));
                    }
                }
        }

        public void Texto(string texto, int x, int y, int escala, Color32 c)
        {
            int cx = x;
            foreach (char ch in texto.ToUpperInvariant())
            {
                string g;
                if (Fuente3x5.TryGetValue(ch, out g))
                {
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 3; i++)
                            if (g[j * 3 + i] == '1') Rect(cx + i * escala, y + j * escala, escala, escala, c);
                }
                cx += 4 * escala;
            }
        }

        public static int AnchoTexto(string texto, int escala) { return texto.Length * 4 * escala - escala; }

        public static byte B(int v) { return (byte)(v < 0 ? 0 : (v > 255 ? 255 : v)); }

        static readonly Dictionary<char, string> Fuente3x5 = new Dictionary<char, string>
        {
            {'A',"010101111101101"},{'B',"110101110101110"},{'C',"011100100100011"},{'D',"110101101101110"},
            {'E',"111100110100111"},{'F',"111100110100100"},{'G',"011100101101011"},{'H',"101101111101101"},
            {'I',"111010010010111"},{'J',"001001001101010"},{'K',"101101110101101"},{'L',"100100100100111"},
            {'M',"101111111101101"},{'N',"110101101101101"},{'O',"010101101101010"},{'P',"110101110100100"},
            {'Q',"010101101110011"},{'R',"110101110101101"},{'S',"011100010001110"},{'T',"111010010010010"},
            {'U',"101101101101111"},{'V',"101101101101010"},{'W',"101101111111101"},{'X',"101101010101101"},
            {'Y',"101101010010010"},{'Z',"111001010100111"},{'0',"111101101101111"},{'1',"010110010010111"},
            {'2',"110001010100111"},{'3',"110001010001110"},{'4',"101101111001001"},{'5',"111100110001110"},
            {'6',"011100111101111"},{'7',"111001010010010"},{'8',"111101111101111"},{'9',"111101111001110"},
            {'.',"000000000000010"},{'-',"000000111000000"},{'>',"100010001010100"},{'^',"010111010010010"},
            {' ',"000000000000000"},
        };
    }

    public class SpriteDef
    {
        public Lienzo img;
        public float ancho, alto; // tamaño en el mundo (1 = alto de una pared)
    }

    public static class ArteProcedural
    {
        public const int T = 64; // tamaño de las texturas de paredes y pisos

        static Color32 C(int r, int g, int b, int a = 255) { return new Color32(Lienzo.B(r), Lienzo.B(g), Lienzo.B(b), (byte)a); }

        static Color32 Var(Color32 c, int d) { return C(c.r + d, c.g + d, c.b + d, c.a); }

        static readonly Color32 Nieve = new Color32(222, 227, 236, 255);

        static void NieveAbajo(Lienzo l, int min, int max, System.Random rnd)
        {
            for (int x = 0; x < l.w; x++)
            {
                int alto = rnd.Next(min, max + 1);
                if (x > 0 && rnd.Next(3) > 0) alto = Mathf.Clamp(AltoAnterior + rnd.Next(-1, 2), min, max);
                AltoAnterior = alto;
                for (int y = l.h - alto; y < l.h; y++) l.Set(x, y, Var(Nieve, rnd.Next(-12, 8)));
            }
        }
        static int AltoAnterior;

        // ---------------- PAREDES ----------------

        public static Lienzo Ladrillo(bool exterior, int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(exterior ? C(92, 86, 80) : C(52, 46, 42));
            for (int fila = 0; fila < 8; fila++)
            {
                int off = (fila % 2) * 8;
                for (int bx = -16; bx < T; bx += 16)
                {
                    int d = rnd.Next(-18, 18);
                    var baseC = exterior ? C(116 + d, 58 + d / 2, 44 + d / 3) : C(78 + d, 42 + d / 2, 34 + d / 3);
                    l.Rect(bx + off + 1, fila * 8 + 1, 14, 6, baseC);
                }
            }
            l.Ruido(9, rnd);
            for (int k = 0; k < 6; k++)
            {
                int x = rnd.Next(T), largo = rnd.Next(10, 40), y0 = rnd.Next(0, 20);
                for (int y = y0; y < y0 + largo && y < T; y++) { var c = l.Get(x, y); l.Set(x, y, Var(c, -22)); }
            }
            if (exterior) NieveAbajo(l, 3, 8, rnd);
            return l;
        }

        public static Lienzo Revoque(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(136, 126, 110));
            l.Ruido(14, rnd);
            l.Rect(0, 0, T, 6, C(104, 96, 86));
            l.Rect(0, 6, T, 1, C(160, 152, 138));
            // ventana con marco y vidrio roto
            l.Rect(19, 15, 26, 28, C(58, 44, 34));
            l.Rect(21, 17, 22, 24, C(26, 31, 38));
            l.Rect(31, 17, 2, 24, C(58, 44, 34));
            l.Rect(21, 28, 22, 2, C(58, 44, 34));
            for (int k = 0; k < 5; k++) l.Linea(22 + rnd.Next(18), 18 + rnd.Next(20), 22 + rnd.Next(18), 18 + rnd.Next(20), 1, C(70, 82, 96));
            l.Rect(17, 43, 30, 3, C(120, 112, 100));
            l.Rect(17, 41, 30, 2, Nieve);
            // grietas
            for (int k = 0; k < 4; k++)
            {
                float x = rnd.Next(T), y = rnd.Next(8, 50);
                for (int s = 0; s < 8; s++)
                {
                    float nx = x + rnd.Next(-3, 4), ny = y + rnd.Next(1, 4);
                    l.Linea(x, y, nx, ny, 1, C(80, 72, 64));
                    x = nx; y = ny;
                }
            }
            l.Rect(0, 52, T, 12, C(88, 82, 74));
            l.Ruido(0, 52, T, 12, 8, rnd);
            NieveAbajo(l, 4, 9, rnd);
            return l;
        }

        public static Lienzo Empapelado(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            for (int x = 0; x < T; x++)
            {
                var c = (x / 8) % 2 == 0 ? C(76, 90, 72) : C(64, 78, 62);
                for (int y = 0; y < T; y++) l.Set(x, y, c);
            }
            for (int y = 4; y < T; y += 10)
                for (int x = 4; x < T; x += 8) l.Rect(x - 1, y, 2, 2, C(96, 104, 80));
            l.Ruido(7, rnd);
            for (int k = 0; k < 3; k++)
                l.Elipse(rnd.Next(T), rnd.Next(10, 50), rnd.Next(4, 10), rnd.Next(5, 12), C(56, 58, 46, 255));
            for (int k = 0; k < 2; k++)
                l.Rect(rnd.Next(T - 12), rnd.Next(8, 40), rnd.Next(5, 12), rnd.Next(5, 12), C(140, 132, 116));
            l.Ruido(4, rnd);
            l.Rect(0, 0, T, 3, C(90, 84, 74));
            l.Rect(0, 55, T, 9, C(70, 48, 32));
            l.Rect(0, 55, T, 1, C(96, 68, 46));
            return l;
        }

        public static Lienzo Persiana(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(60, 64, 70));
            for (int y = 2; y < T; y += 4)
            {
                l.Rect(2, y, T - 4, 3, C(110, 116, 122));
                l.Rect(2, y + 2, T - 4, 1, C(76, 80, 86));
            }
            l.Ruido(8, rnd);
            for (int k = 0; k < 9; k++)
                l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(2, 6), rnd.Next(1, 4), C(118, 70, 40));
            // grafiti
            var g = rnd.Next(2) == 0 ? C(30, 30, 34) : C(130, 36, 36);
            float gx = 10, gy = 30;
            for (int s = 0; s < 10; s++)
            {
                float nx = gx + rnd.Next(2, 6), ny = 24 + rnd.Next(0, 14);
                l.Linea(gx, gy, nx, ny, 2, g);
                gx = nx; gy = ny;
            }
            NieveAbajo(l, 3, 7, rnd);
            return l;
        }

        public static Lienzo Azulejos(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rect(0, 0, T, 28, C(140, 130, 102));
            l.Ruido(0, 0, T, 28, 8, rnd);
            l.Rect(0, 28, T, 36, C(120, 124, 120));
            for (int y = 29; y < T; y += 9)
                for (int x = 0; x < T; x += 9)
                    l.Rect(x + 1, y, 8, 8, Var(C(196, 202, 196), rnd.Next(-14, 6)));
            l.Rect(0, 27, T, 2, C(40, 90, 80));
            l.Ruido(0, 28, T, 36, 6, rnd);
            for (int y = 50; y < T; y++)
                for (int x = 0; x < T; x++) { var c = l.Get(x, y); l.Set(x, y, Var(c, -(y - 50) * 3)); }
            return l;
        }

        public static Lienzo CartelAlmacen(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = Persiana(semilla);
            l.Rect(0, 0, T, 20, C(124, 30, 30));
            l.Ruido(0, 0, T, 20, 10, rnd);
            l.Borde(1, 1, T - 2, 18, C(200, 190, 170));
            string t = "ALMACEN";
            l.Texto(t, (T - Lienzo.AnchoTexto(t, 2)) / 2, 5, 2, C(230, 224, 210));
            l.Rect(0, 0, T, 2, Nieve);
            return l;
        }

        public static Lienzo VentanaTapiada(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = Ladrillo(true, semilla);
            l.Rect(13, 12, 38, 32, C(20, 22, 26));
            for (int k = 0; k < 3; k++)
            {
                int y = 16 + k * 9 + rnd.Next(-2, 3);
                var madera = C(122 + rnd.Next(-10, 10), 90, 56);
                l.Linea(10, y + rnd.Next(-3, 3), 54, y + rnd.Next(-3, 3), 6, madera);
            }
            l.Ruido(12, 10, 42, 36, 8, rnd);
            for (int k = 0; k < 6; k++) l.Set(14 + rnd.Next(36), 16 + rnd.Next(26), C(40, 40, 40));
            l.Rect(11, 44, 42, 2, Nieve);
            return l;
        }

        public static Lienzo CartelPuente(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(118, 120, 122));
            l.Ruido(10, rnd);
            l.Rect(4, 8, 56, 32, C(22, 92, 52));
            l.Borde(5, 9, 54, 30, C(230, 230, 220));
            l.Texto("PUENTE", (T - Lienzo.AnchoTexto("PUENTE", 2)) / 2, 13, 2, C(236, 236, 228));
            l.Texto("PUEYRREDON", (T - Lienzo.AnchoTexto("PUEYRREDON", 1)) / 2, 26, 1, C(236, 236, 228));
            l.Texto("^", 30, 32, 1, C(236, 236, 228));
            l.Rect(4, 6, 56, 2, Nieve);
            NieveAbajo(l, 4, 8, rnd);
            return l;
        }

        public static Lienzo Baranda(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T); // transparente arriba
            var acero = C(72, 86, 102);
            for (int x = 0; x < T; x += 16) l.Rect(x, 0, 4, 40, acero);
            l.Rect(0, 0, T, 4, acero);
            l.Rect(0, 20, T, 2, acero);
            for (int x = 0; x < T; x += 16)
            {
                l.Linea(x + 3, 4, x + 16, 20, 2, acero);
                l.Linea(x + 16, 4, x + 3, 20, 2, acero);
                l.Linea(x + 3, 22, x + 16, 39, 2, acero);
                l.Linea(x + 16, 22, x + 3, 39, 2, acero);
            }
            l.Ruido(0, 0, T, 40, 8, rnd);
            l.Rect(0, 40, T, 24, C(128, 128, 124));
            l.Ruido(0, 40, T, 24, 12, rnd);
            l.NieveSobre(2, rnd);
            for (int y = 56; y < T; y++)
                for (int x = 0; x < T; x++) { var c = l.Get(x, y); l.Set(x, y, Var(c, -(y - 56) * 4)); }
            return l;
        }

        public static Lienzo Puerta(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(60, 42, 30));
            l.Rect(8, 2, 48, 62, C(104, 72, 44));
            l.Ruido(8, 2, 48, 62, 8, rnd);
            l.Rect(13, 8, 38, 14, C(28, 34, 42));
            l.Rect(31, 8, 2, 14, C(80, 56, 36));
            l.Borde(14, 26, 16, 32, C(82, 56, 34));
            l.Borde(34, 26, 16, 32, C(82, 56, 34));
            l.Circulo(48, 38, 2.2f, C(180, 160, 80));
            NieveAbajo(l, 1, 4, rnd);
            return l;
        }

        // ---------------- PISOS Y TECHOS ----------------

        public static Lienzo PisoNieve(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(196, 201, 210));
            for (int k = 0; k < 10; k++)
                l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(4, 14), rnd.Next(3, 8), C(182, 189, 202));
            l.Ruido(10, rnd);
            for (int k = 0; k < 14; k++) l.Set(rnd.Next(T), rnd.Next(T), C(120, 118, 116));
            return l;
        }

        public static Lienzo PisoAsfalto(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(60, 62, 66));
            l.Ruido(10, rnd);
            for (int k = 0; k < 7; k++)
                l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(5, 16), rnd.Next(3, 9), C(188, 194, 204));
            l.Ruido(6, rnd);
            for (int k = 0; k < 3; k++) l.Linea(rnd.Next(T), rnd.Next(T), rnd.Next(T), rnd.Next(T), 1, C(40, 40, 42));
            return l;
        }

        public static Lienzo PisoMadera(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            for (int y = 0; y < T; y += 8)
            {
                var c = C(94 + rnd.Next(-10, 10), 64, 40);
                l.Rect(0, y, T, 7, c);
                l.Rect(0, y + 7, T, 1, C(42, 28, 20));
                int corte = rnd.Next(T);
                l.Rect(corte, y, 1, 7, C(42, 28, 20));
            }
            l.Ruido(7, rnd);
            for (int k = 0; k < 12; k++) l.Linea(rnd.Next(T), rnd.Next(T), rnd.Next(T), 0, 1, C(80, 54, 34));
            return l;
        }

        public static Lienzo PisoBaldosas(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                    l.Set(x, y, ((x / 16) + (y / 16)) % 2 == 0 ? C(186, 182, 172) : C(42, 42, 46));
            l.Ruido(9, rnd);
            for (int k = 0; k < 5; k++) l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(3, 8), rnd.Next(3, 8), C(110, 100, 86));
            return l;
        }

        public static Lienzo Techo(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(112, 110, 104));
            l.Ruido(9, rnd);
            l.Elipse(40, 24, 14, 10, C(96, 86, 70));
            l.Elipse(40, 24, 9, 6, C(90, 80, 64));
            for (int s = 0, x = 4, y = 60; s < 12; s++)
            {
                int nx = x + rnd.Next(1, 6), ny = y - rnd.Next(2, 6);
                l.Linea(x, y, nx, ny, 1, C(76, 72, 66));
                x = nx; y = ny;
            }
            return l;
        }

        // ---------------- CIELO ----------------
        // Panorama de 360°: nubes de tormenta y la silueta de Capital con el Obelisco hacia el norte.
        public const int CieloAncho = 1024, CieloAlto = 128;

        public static void Cielo(out Color32[] cielo, out byte[] siluetas)
        {
            var rnd = new System.Random(77);
            cielo = new Color32[CieloAncho * CieloAlto];
            siluetas = new byte[CieloAncho * CieloAlto];
            var arriba = C(56, 62, 72);
            var horizonte = C(124, 130, 140);
            // ruido de valor periódico para las nubes
            int n = 32;
            var nodos = new float[n * 8];
            for (int i = 0; i < nodos.Length; i++) nodos[i] = (float)rnd.NextDouble();
            for (int y = 0; y < CieloAlto; y++)
            {
                float t = y / (float)(CieloAlto - 1);
                for (int x = 0; x < CieloAncho; x++)
                {
                    float fx = x / (float)CieloAncho * n, fy = t * 7f;
                    int ix = (int)fx, iy = (int)fy;
                    float ax = fx - ix, ay = fy - iy;
                    float a = nodos[iy * n + ix % n], b = nodos[iy * n + (ix + 1) % n];
                    float c = nodos[Mathf.Min(iy + 1, 7) * n + ix % n], d = nodos[Mathf.Min(iy + 1, 7) * n + (ix + 1) % n];
                    float nube = Mathf.Lerp(Mathf.Lerp(a, b, ax), Mathf.Lerp(c, d, ax), ay);
                    float luz = (nube - 0.5f) * 26f;
                    var col = Color32.Lerp(arriba, horizonte, t * t);
                    cielo[y * CieloAncho + x] = Var(col, (int)luz + rnd.Next(-2, 3));
                }
            }
            // edificios de Capital (más densos hacia el norte = 768)
            int px = 0;
            while (px < CieloAncho)
            {
                int ancho = rnd.Next(6, 22);
                float distNorte = Mathf.Abs(Mathf.DeltaAngle(px / (float)CieloAncho * 360f, 270f));
                float densidad = distNorte < 70f ? 1f : (distNorte < 120f ? 0.5f : 0.12f);
                if (rnd.NextDouble() < densidad)
                {
                    int alto = rnd.Next(4, distNorte < 70f ? 30 : 14);
                    for (int x = px; x < px + ancho && x < CieloAncho; x++)
                        for (int y = CieloAlto - alto; y < CieloAlto; y++) siluetas[y * CieloAncho + x] = 200;
                    // ventanas apagadas
                    for (int k = 0; k < ancho * alto / 30; k++)
                    {
                        int wx = px + rnd.Next(ancho), wy = CieloAlto - rnd.Next(2, alto);
                        if (wx < CieloAncho) siluetas[wy * CieloAncho + wx] = 150;
                    }
                }
                px += ancho + rnd.Next(0, 4);
            }
            // el Obelisco
            int ox = 768, alturaOb = 84, baseY = CieloAlto - 1;
            for (int y = 0; y < alturaOb; y++)
            {
                int yy = baseY - y;
                float t = y / (float)alturaOb;
                float medio = Mathf.Lerp(4.2f, 2.6f, t);
                if (y > alturaOb - 7) medio = 2.6f * (alturaOb - y) / 7f;
                for (int x = ox - Mathf.CeilToInt(medio); x <= ox + Mathf.CeilToInt(medio); x++)
                    if (Mathf.Abs(x - ox) <= medio) siluetas[yy * CieloAncho + x] = 255;
            }
        }

        // ---------------- SPRITES ----------------

        public static Dictionary<string, SpriteDef> Sprites()
        {
            var d = new Dictionary<string, SpriteDef>();
            System.Random rnd;

            // Árbol pelado con nieve
            rnd = new System.Random(1);
            var arbol = new Lienzo(64, 96);
            var tronco = C(50, 40, 32);
            arbol.Rect(29, 42, 6, 54, tronco);
            arbol.Linea(32, 46, 12, 10, 4, tronco); arbol.Linea(32, 44, 54, 8, 4, tronco);
            arbol.Linea(31, 40, 30, 2, 3, tronco); arbol.Linea(20, 26, 4, 20, 2, tronco);
            arbol.Linea(45, 26, 62, 16, 2, tronco); arbol.Linea(16, 18, 18, 4, 2, tronco);
            arbol.Linea(48, 18, 44, 2, 2, tronco);
            for (int k = 0; k < 14; k++)
            {
                float x = rnd.Next(8, 56), y = rnd.Next(4, 36);
                arbol.Linea(x, y, x + rnd.Next(-6, 7), y - rnd.Next(2, 7), 1, tronco);
            }
            arbol.Ruido(6, rnd);
            arbol.NieveSobre(2, rnd);
            arbol.Elipse(32, 95, 18, 3, Nieve);
            d["arbol"] = new SpriteDef { img = arbol, ancho = 1.4f, alto = 2.1f };

            // Farola apagada
            rnd = new System.Random(2);
            var farola = new Lienzo(32, 96);
            var metal = C(40, 44, 48);
            farola.Rect(14, 10, 4, 84, metal);
            farola.Linea(16, 11, 26, 8, 2, metal);
            farola.Rect(21, 6, 9, 5, C(70, 74, 78));
            farola.Rect(23, 11, 5, 2, C(110, 110, 100));
            farola.Rect(11, 88, 10, 8, metal);
            farola.NieveSobre(2, rnd);
            d["farola"] = new SpriteDef { img = farola, ancho = 0.4f, alto = 2.3f };

            // Banco de plaza
            rnd = new System.Random(3);
            var banco = new Lienzo(64, 32);
            var hierro = C(36, 40, 44);
            var liston = C(100, 70, 44);
            banco.Rect(6, 4, 3, 28, hierro); banco.Rect(55, 4, 3, 28, hierro);
            banco.Rect(4, 4, 56, 3, liston); banco.Rect(4, 9, 56, 3, liston);
            banco.Rect(4, 16, 56, 4, liston);
            banco.Ruido(8, rnd);
            banco.NieveSobre(3, rnd);
            d["banco"] = new SpriteDef { img = banco, ancho = 1.3f, alto = 0.65f };

            // Monumento de la plaza
            rnd = new System.Random(4);
            var mon = new Lienzo(64, 96);
            mon.Rect(12, 52, 40, 44, C(120, 118, 112));
            mon.Rect(8, 48, 48, 6, C(104, 102, 98));
            mon.Rect(9, 90, 46, 6, C(100, 98, 94));
            mon.Rect(22, 64, 20, 10, C(110, 86, 40));
            mon.Ruido(10, rnd);
            var bronce = C(58, 62, 52);
            mon.Rect(25, 16, 14, 32, bronce);
            mon.Circulo(32, 10, 6, bronce);
            mon.Linea(37, 20, 50, 3, 3, bronce);
            mon.Linea(26, 20, 18, 36, 3, bronce);
            mon.Rect(22, 36, 20, 12, C(52, 56, 48));
            mon.NieveSobre(3, rnd);
            mon.Elipse(32, 95, 26, 3, Nieve);
            d["monumento"] = new SpriteDef { img = mon, ancho = 1.3f, alto = 2.0f };

            // Auto tapado de nieve
            rnd = new System.Random(5);
            var auto = new Lienzo(96, 48);
            auto.Rect(4, 22, 88, 16, C(96, 34, 34));
            for (int y = 8; y < 22; y++) auto.Rect(26 - (y - 8), y, 46 + 2 * (y - 8), 1, C(90, 32, 32));
            auto.Rect(30, 11, 16, 9, C(26, 30, 36)); auto.Rect(50, 11, 16, 9, C(26, 30, 36));
            auto.Circulo(22, 40, 7, C(18, 18, 20)); auto.Circulo(74, 40, 7, C(18, 18, 20));
            auto.Rect(4, 26, 6, 4, C(160, 150, 120));
            auto.Ruido(8, rnd);
            auto.NieveSobre(6, rnd);
            for (int k = 0; k < 6; k++) auto.Elipse(rnd.Next(10, 86), rnd.Next(18, 26), rnd.Next(5, 12), rnd.Next(2, 4), Var(Nieve, rnd.Next(-10, 4)));
            auto.Elipse(48, 47, 46, 3, Nieve);
            d["auto_nevado"] = new SpriteDef { img = auto, ancho = 2.0f, alto = 1.0f };

            // Auto volcado
            rnd = new System.Random(6);
            var volc = new Lienzo(96, 48);
            volc.Rect(6, 10, 84, 15, C(58, 70, 92));
            for (int y = 25; y < 40; y++) volc.Rect(22 + (y - 25), y, 52 - 2 * (y - 25), 1, C(52, 64, 84));
            volc.Rect(28, 27, 16, 9, C(30, 34, 40)); volc.Rect(50, 27, 14, 9, C(30, 34, 40));
            volc.Linea(30, 28, 40, 35, 1, C(140, 150, 160)); volc.Linea(52, 34, 60, 28, 1, C(140, 150, 160));
            volc.Circulo(24, 9, 7, C(18, 18, 20)); volc.Circulo(72, 9, 7, C(18, 18, 20));
            volc.Circulo(24, 9, 2.5f, C(90, 90, 90)); volc.Circulo(72, 9, 2.5f, C(90, 90, 90));
            volc.Ruido(9, rnd);
            volc.NieveSobre(3, rnd);
            volc.Elipse(48, 46, 44, 4, Nieve);
            d["auto_volcado"] = new SpriteDef { img = volc, ancho = 2.0f, alto = 0.95f };

            // Semáforo caído
            rnd = new System.Random(7);
            var sem = new Lienzo(32, 96);
            sem.Linea(10, 95, 20, 24, 3, C(50, 54, 58));
            sem.Rect(14, 6, 11, 24, C(30, 32, 34));
            sem.Circulo(19.5f, 11, 3, C(90, 22, 22)); sem.Circulo(19.5f, 18, 3, C(96, 76, 22)); sem.Circulo(19.5f, 25, 3, C(22, 64, 34));
            sem.NieveSobre(2, rnd);
            d["semaforo"] = new SpriteDef { img = sem, ancho = 0.45f, alto = 2.0f };

            // Carteles
            d["cartel_plaza"] = new SpriteDef { img = Cartel("PLAZA", "ALSINA", C(214, 212, 202), C(30, 50, 100), 8), ancho = 0.9f, alto = 1.4f };
            d["cartel_avenida"] = new SpriteDef { img = Cartel("AV MITRE", ">", C(30, 62, 124), C(232, 232, 228), 9), ancho = 0.9f, alto = 1.4f };

            rnd = new System.Random(10);
            var caido = new Lienzo(64, 24);
            caido.Rect(2, 8, 60, 12, C(22, 92, 52));
            caido.Borde(3, 9, 58, 10, C(230, 230, 220));
            caido.Texto("CAPITAL", 10, 12, 1, C(236, 236, 228));
            caido.Texto(">", 44, 12, 1, C(236, 236, 228));
            caido.Rect(40, 4, 24, 20, new Color32(0, 0, 0, 0));
            caido.Elipse(50, 16, 16, 8, Nieve);
            caido.Ruido(6, rnd);
            d["cartel_caido"] = new SpriteDef { img = caido, ancho = 1.2f, alto = 0.45f };

            // Mesa de trabajo
            rnd = new System.Random(11);
            var mt = new Lienzo(64, 48);
            var mad = C(92, 64, 40);
            mt.Rect(2, 18, 60, 5, mad); mt.Rect(5, 23, 4, 25, C(70, 48, 30)); mt.Rect(55, 23, 4, 25, C(70, 48, 30));
            mt.Rect(5, 38, 54, 3, C(70, 48, 30));
            mt.Rect(8, 10, 12, 8, C(70, 72, 76)); mt.Rect(12, 6, 4, 4, C(70, 72, 76));
            mt.Linea(26, 16, 40, 12, 2, C(120, 120, 124)); mt.Rect(38, 10, 6, 4, C(80, 50, 30));
            mt.Rect(44, 12, 16, 6, C(70, 90, 60));
            mt.Ruido(7, rnd);
            d["mesa_trabajo"] = new SpriteDef { img = mt, ancho = 1.2f, alto = 0.9f };

            // Mesa con mate y termo
            rnd = new System.Random(12);
            var mesa = new Lienzo(64, 40);
            mesa.Rect(4, 14, 56, 5, mad); mesa.Rect(7, 19, 4, 21, C(70, 48, 30)); mesa.Rect(53, 19, 4, 21, C(70, 48, 30));
            mesa.Rect(40, 1, 6, 13, C(170, 40, 40)); mesa.Rect(40, 0, 6, 2, C(200, 200, 200));
            mesa.Elipse(28, 10, 4, 4, C(110, 80, 40)); mesa.Linea(29, 9, 32, 2, 1, C(200, 190, 120));
            mesa.Ruido(6, rnd);
            d["mesa"] = new SpriteDef { img = mesa, ancho = 1.1f, alto = 0.75f };

            // Cama
            rnd = new System.Random(13);
            var cama = new Lienzo(64, 32);
            cama.Rect(2, 14, 60, 12, C(80, 56, 36)); cama.Rect(2, 26, 4, 6, C(70, 48, 30)); cama.Rect(58, 26, 4, 6, C(70, 48, 30));
            cama.Rect(4, 10, 56, 6, C(190, 186, 176));
            cama.Rect(22, 8, 38, 10, C(110, 36, 36));
            for (int x = 24; x < 60; x += 6) cama.Rect(x, 8, 1, 10, C(70, 24, 24));
            cama.Rect(6, 7, 13, 5, C(210, 206, 196));
            cama.Ruido(6, rnd);
            d["cama"] = new SpriteDef { img = cama, ancho = 1.4f, alto = 0.6f };

            // Lámpara de kerosene (brilla)
            rnd = new System.Random(14);
            var lamp = new Lienzo(32, 48);
            lamp.Rect(6, 28, 20, 4, mad); lamp.Rect(8, 32, 3, 16, C(70, 48, 30)); lamp.Rect(21, 32, 3, 16, C(70, 48, 30));
            lamp.Rect(11, 20, 10, 8, C(150, 116, 50));
            lamp.Rect(13, 8, 6, 12, C(255, 220, 140)); lamp.Rect(15, 11, 2, 6, C(255, 250, 220));
            lamp.Rect(12, 6, 8, 2, C(90, 80, 60));
            lamp.brillaSola = true;
            d["lampara"] = new SpriteDef { img = lamp, ancho = 0.5f, alto = 0.9f };

            var vela = new Lienzo(24, 32);
            vela.Rect(3, 20, 18, 12, C(96, 70, 44));
            vela.Rect(10, 10, 4, 10, C(226, 220, 196));
            vela.Elipse(12, 6, 2.2f, 4, C(255, 196, 90)); vela.Elipse(12, 7, 1, 2, C(255, 250, 220));
            vela.brillaSola = true;
            d["vela"] = new SpriteDef { img = vela, ancho = 0.35f, alto = 0.45f };

            // Estantería saqueada
            rnd = new System.Random(15);
            var est = new Lienzo(64, 80);
            est.Rect(2, 0, 4, 80, C(70, 48, 30)); est.Rect(58, 0, 4, 80, C(70, 48, 30));
            for (int s = 0; s < 4; s++)
            {
                int y = 16 + s * 18;
                est.Rect(2, y, 60, 3, mad);
                for (int x = 8; x < 56; x += 7)
                {
                    if (rnd.Next(3) == 0) continue;
                    int alto = rnd.Next(6, 13);
                    var col = C(rnd.Next(60, 200), rnd.Next(40, 160), rnd.Next(30, 120));
                    est.Rect(x, y - alto, 5, alto, col);
                    est.Rect(x, y - alto, 5, 1, Var(col, 30));
                }
            }
            est.Ruido(6, rnd);
            d["estanteria"] = new SpriteDef { img = est, ancho = 1.2f, alto = 1.5f };

            var silla = new Lienzo(40, 40);
            silla.Linea(6, 30, 34, 26, 3, mad); silla.Linea(8, 30, 4, 39, 2, mad); silla.Linea(30, 27, 34, 39, 2, mad);
            silla.Linea(30, 26, 38, 6, 3, mad); silla.Linea(34, 15, 24, 14, 2, mad);
            d["silla_rota"] = new SpriteDef { img = silla, ancho = 0.7f, alto = 0.7f };

            // Radio antigua sobre una mesita
            rnd = new System.Random(16);
            var radio = new Lienzo(48, 48);
            radio.Rect(6, 30, 36, 4, mad); radio.Rect(8, 34, 3, 14, C(70, 48, 30)); radio.Rect(37, 34, 3, 14, C(70, 48, 30));
            radio.Rect(8, 10, 32, 20, C(110, 70, 40));
            radio.Rect(11, 13, 15, 14, C(40, 30, 24));
            for (int y = 14; y < 27; y += 2) radio.Rect(11, y, 15, 1, C(70, 54, 40));
            radio.Circulo(33, 17, 4, C(220, 206, 160)); radio.Linea(33, 17, 35, 14, 1, C(120, 30, 30));
            radio.Circulo(33, 26, 2, C(60, 40, 30));
            radio.Linea(36, 10, 44, 0, 1, C(150, 150, 150));
            radio.Ruido(6, rnd);
            d["radio"] = new SpriteDef { img = radio, ancho = 0.8f, alto = 0.85f };

            // El informante, sentado y herido
            rnd = new System.Random(17);
            var inf = new Lienzo(48, 64);
            inf.Rect(18, 52, 26, 7, C(40, 40, 48));
            inf.Rect(40, 50, 7, 10, C(30, 26, 22));
            inf.Rect(13, 24, 22, 30, C(72, 68, 50));
            inf.Rect(13, 24, 2, 30, C(56, 52, 40));
            inf.Rect(14, 21, 20, 5, C(122, 32, 32));
            inf.Circulo(24, 15, 7, C(178, 138, 108));
            inf.Rect(18, 17, 12, 5, C(70, 60, 50));
            inf.Rect(19, 14, 3, 2, C(30, 24, 20)); inf.Rect(26, 14, 3, 2, C(30, 24, 20));
            inf.Rect(16, 6, 16, 6, C(60, 60, 92));
            inf.Linea(16, 10, 32, 9, 2, C(220, 220, 214));
            inf.Circulo(11, 44, 3.4f, C(192, 206, 222)); inf.Circulo(37, 44, 3.4f, C(192, 206, 222));
            inf.Rect(10, 40, 4, 4, C(72, 68, 50)); inf.Rect(34, 40, 4, 4, C(72, 68, 50));
            inf.Ruido(6, rnd);
            d["informante"] = new SpriteDef { img = inf, ancho = 0.85f, alto = 1.15f };

            // ---- Recursos (objetos chicos en el piso) ----
            var botiq = new Lienzo(24, 24);
            botiq.Rect(2, 8, 20, 14, C(222, 222, 216)); botiq.Borde(2, 8, 20, 14, C(150, 150, 146));
            botiq.Rect(10, 10, 4, 10, C(184, 30, 30)); botiq.Rect(7, 13, 10, 4, C(184, 30, 30));
            botiq.Rect(9, 5, 6, 3, C(120, 120, 116));
            d["botiquin"] = new SpriteDef { img = botiq, ancho = 0.4f, alto = 0.4f };

            var lata = new Lienzo(24, 24);
            lata.Rect(7, 6, 10, 17, C(162, 166, 172)); lata.Rect(7, 10, 10, 8, C(40, 112, 52));
            lata.Rect(7, 5, 10, 2, C(200, 204, 210)); lata.Rect(9, 12, 6, 3, C(220, 210, 120));
            d["lata"] = new SpriteDef { img = lata, ancho = 0.35f, alto = 0.35f };

            var gall = new Lienzo(24, 24);
            gall.Rect(2, 11, 20, 11, C(204, 164, 40)); gall.Rect(2, 14, 20, 4, C(180, 40, 30));
            gall.Rect(5, 15, 6, 2, C(240, 230, 200));
            d["galletitas"] = new SpriteDef { img = gall, ancho = 0.35f, alto = 0.35f };

            rnd = new System.Random(18);
            var chori = new Lienzo(24, 24);
            chori.Elipse(12, 16, 11, 5, C(200, 150, 80)); chori.Elipse(12, 13.5f, 9, 2.6f, C(130, 50, 40));
            for (int k = 0; k < 8; k++) chori.Set(3 + rnd.Next(18), 11 + rnd.Next(8), C(230, 236, 244));
            d["choripan"] = new SpriteDef { img = chori, ancho = 0.35f, alto = 0.3f };

            var lona = new Lienzo(24, 24);
            lona.Rect(2, 12, 20, 10, C(70, 92, 62)); lona.Rect(2, 16, 20, 1, C(50, 66, 44)); lona.Rect(2, 19, 20, 1, C(50, 66, 44));
            lona.Linea(8, 12, 8, 22, 1, C(170, 150, 100));
            d["lona"] = new SpriteDef { img = lona, ancho = 0.4f, alto = 0.35f };

            var alam = new Lienzo(24, 24);
            for (int k = 0; k < 3; k++) { alam.Elipse(9, 15 + k, 7, 5, C(140, 140, 144)); alam.Elipse(9, 15 + k, 5, 3, new Color32(0, 0, 0, 0)); }
            alam.Elipse(18, 18, 4, 4, C(170, 172, 176)); alam.Elipse(18, 18, 1.6f, 1.6f, new Color32(0, 0, 0, 0));
            d["alambre"] = new SpriteDef { img = alam, ancho = 0.35f, alto = 0.35f };

            rnd = new System.Random(19);
            var chat = new Lienzo(24, 24);
            for (int k = 0; k < 6; k++) chat.Rect(rnd.Next(2, 16), rnd.Next(12, 20), rnd.Next(4, 9), rnd.Next(2, 5), C(rnd.Next(90, 140), rnd.Next(70, 100), rnd.Next(60, 90)));
            d["chatarra"] = new SpriteDef { img = chat, ancho = 0.35f, alto = 0.3f };

            var foto = new Lienzo(24, 24);
            foto.Rect(5, 4, 14, 18, C(96, 66, 40)); foto.Rect(7, 6, 10, 14, C(152, 122, 92));
            foto.Circulo(10, 11, 2, C(90, 70, 50)); foto.Circulo(14, 11, 2, C(90, 70, 50));
            foto.Rect(8, 13, 4, 6, C(90, 70, 50)); foto.Rect(12, 13, 4, 6, C(90, 70, 50));
            d["foto"] = new SpriteDef { img = foto, ancho = 0.3f, alto = 0.3f };

            var mapa = new Lienzo(24, 24);
            mapa.Rect(2, 8, 20, 14, C(212, 202, 160)); mapa.Rect(11, 8, 1, 14, C(170, 160, 120));
            mapa.Linea(4, 18, 9, 12, 1, C(40, 70, 140)); mapa.Linea(13, 20, 19, 10, 1, C(170, 40, 40));
            d["mapa"] = new SpriteDef { img = mapa, ancho = 0.35f, alto = 0.3f };

            return d;
        }

        static Lienzo Cartel(string linea1, string linea2, Color32 fondo, Color32 letra, int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(48, 64);
            l.Rect(22, 24, 4, 40, C(50, 50, 54));
            l.Rect(2, 4, 44, 22, fondo);
            l.Borde(3, 5, 42, 20, letra);
            l.Texto(linea1, (48 - Lienzo.AnchoTexto(linea1, 1)) / 2, 8, 1, letra);
            l.Texto(linea2, (48 - Lienzo.AnchoTexto(linea2, 1)) / 2, 16, 1, letra);
            l.Ruido(5, rnd);
            l.NieveSobre(2, rnd);
            return l;
        }

        // Mano del protagonista con la linterna (primera persona, como el arma del Doom).
        public static Lienzo Manos(bool conTraje)
        {
            var rnd = new System.Random(conTraje ? 21 : 20);
            var l = new Lienzo(96, 64);
            var manga = conTraje ? C(86, 98, 86) : C(112, 58, 48);
            var mano = conTraje ? C(66, 76, 66) : C(190, 150, 120);
            l.Linea(46, 30, 74, 64, 10, C(48, 50, 54));
            l.Linea(48, 31, 72, 60, 2, C(90, 92, 98));
            l.Elipse(43, 26, 9, 7, C(60, 62, 66));
            l.Elipse(41, 24, 6, 4.5f, C(226, 224, 186));
            l.Elipse(66, 50, 13, 11, mano);
            l.Rect(56, 44, 4, 8, Var(mano, -20)); l.Rect(62, 42, 4, 8, Var(mano, -20));
            l.Rect(72, 50, 24, 14, manga);
            l.Linea(70, 48, 96, 44, 12, manga);
            if (conTraje) l.Rect(74, 47, 22, 3, C(60, 70, 60));
            l.Ruido(6, rnd);
            return l;
        }
    }
}
