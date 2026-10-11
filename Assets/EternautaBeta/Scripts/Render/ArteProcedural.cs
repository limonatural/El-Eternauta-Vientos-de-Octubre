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
            foreach (char ch0 in texto.ToUpperInvariant())
            {
                char ch = SinTilde(ch0);
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

        static char SinTilde(char c)
        {
            switch (c)
            {
                case 'Á': return 'A'; case 'É': return 'E'; case 'Í': return 'I';
                case 'Ó': return 'O'; case 'Ú': case 'Ü': return 'U'; case 'Ñ': return 'N';
                default: return c;
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
            {' ',"000000000000000"},{'/',"001001010100100"},{',',"000000000010100"},{':',"000010000010000"},
            {'!',"010010010000010"},{'+',"000010111010000"},{'<',"001010100010001"},
        };
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

        public static Lienzo Ladrillo(bool exterior, int semilla, bool nieve = true)
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
            if (exterior && nieve) NieveAbajo(l, 3, 8, rnd);
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
            // Restos de nieve pisada, suaves para que el piso no se vea "manchado" al repetirse.
            for (int k = 0; k < 4; k++)
                l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(6, 14), rnd.Next(3, 7), C(96, 100, 108));
            l.Ruido(8, rnd);
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
        // Nubes de tormenta (se repiten alrededor). Fila 0 = arriba, la última fila = horizonte.
        public static Lienzo Cielo()
        {
            var rnd = new System.Random(77);
            const int W = 256, H = 64;
            var l = new Lienzo(W, H);
            var arriba = C(46, 52, 62);
            var horizonte = C(118, 126, 138);
            int n = 16;
            var nodos = new float[n * 8];
            for (int i = 0; i < nodos.Length; i++) nodos[i] = (float)rnd.NextDouble();
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);
                for (int x = 0; x < W; x++)
                {
                    float fx = x / (float)W * n, fy = t * 7f;
                    int ix = (int)fx, iy = (int)fy;
                    float ax = fx - ix, ay = fy - iy;
                    float a = nodos[iy * n + ix % n], b = nodos[iy * n + (ix + 1) % n];
                    float c = nodos[Mathf.Min(iy + 1, 7) * n + ix % n], d = nodos[Mathf.Min(iy + 1, 7) * n + (ix + 1) % n];
                    float nube = Mathf.Lerp(Mathf.Lerp(a, b, ax), Mathf.Lerp(c, d, ax), ay);
                    float luz = (nube - 0.5f) * 30f;
                    var col = Color32.Lerp(arriba, horizonte, t * t);
                    l.Set(x, y, Var(col, (int)luz + rnd.Next(-2, 3)));
                }
            }
            return l;
        }

        // ---------------- TEXTURAS DEL MUNDO 3D ----------------

        // Piso superior de una fachada: revoque con ventana oscura y alféizar nevado.
        public static Lienzo FachadaAlta(int semilla, Color32 tono)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(tono);
            l.Ruido(12, rnd);
            l.Rect(16, 12, 32, 34, Var(tono, -40));
            l.Rect(19, 15, 26, 30, C(22, 26, 32));
            if (rnd.Next(3) == 0) l.Rect(19, 15, 26, 30, C(96, 84, 60)); // persiana de madera bajada
            l.Rect(31, 15, 2, 30, Var(tono, -46));
            l.Rect(14, 46, 36, 4, Var(tono, 10));
            l.Rect(14, 44, 36, 2, Nieve);
            for (int k = 0; k < 3; k++)
            {
                float x = rnd.Next(T), y = rnd.Next(4, 56);
                for (int s = 0; s < 6; s++)
                {
                    float nx = x + rnd.Next(-3, 4), ny = y + rnd.Next(1, 4);
                    l.Linea(x, y, nx, ny, 1, Var(tono, -30));
                    x = nx; y = ny;
                }
            }
            l.Rect(0, 60, T, 4, Var(tono, -24));
            return l;
        }

        public static Lienzo LadrilloLiso(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(92, 86, 80));
            for (int fila = 0; fila < 8; fila++)
            {
                int off = (fila % 2) * 8;
                for (int bx = -16; bx < T; bx += 16)
                {
                    int d = rnd.Next(-18, 18);
                    l.Rect(bx + off + 1, fila * 8 + 1, 14, 6, C(112 + d, 58 + d / 2, 46 + d / 3));
                }
            }
            l.Ruido(9, rnd);
            return l;
        }

        public static Lienzo Hormigon(int semilla, int tono = 128)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(tono, tono - 2, tono - 6));
            l.Ruido(10, rnd);
            for (int k = 0; k < 8; k++) l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(3, 10), rnd.Next(2, 6), C(tono - 22, tono - 24, tono - 26));
            l.Rect(0, 0, T, 1, C(tono - 30, tono - 30, tono - 32));
            l.Rect(0, 31, T, 1, C(tono - 26, tono - 26, tono - 28));
            for (int k = 0; k < 3; k++)
            {
                float x = rnd.Next(T), y = 0;
                for (int s = 0; s < 10; s++) { float nx = x + rnd.Next(-2, 3), ny = y + rnd.Next(2, 7); l.Linea(x, y, nx, ny, 1, C(tono - 40, tono - 40, tono - 42)); x = nx; y = ny; }
            }
            l.Ruido(4, rnd);
            return l;
        }

        // Río Riachuelo: agua oscura con placas de hielo y nieve.
        public static Lienzo Agua(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            l.Rellenar(C(30, 38, 44));
            l.Ruido(6, rnd);
            for (int k = 0; k < 14; k++)
                l.Linea(rnd.Next(T), rnd.Next(T), rnd.Next(T), rnd.Next(T), 1, C(48, 58, 66));
            for (int k = 0; k < 7; k++)
            {
                int x = rnd.Next(T), y = rnd.Next(T), w = rnd.Next(5, 16), h = rnd.Next(3, 9);
                l.Elipse(x, y, w, h, C(176, 186, 198));
                l.Elipse(x - 1, y - 1, w - 2, h - 2, C(206, 214, 224));
            }
            l.Ruido(5, rnd);
            return l;
        }

        // Vereda: baldosas grises con nieve en las juntas.
        public static Lienzo Vereda(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                    l.Set(x, y, (x % 16 == 0 || y % 16 == 0) ? C(196, 202, 210) : C(132, 132, 128));
            l.Ruido(9, rnd);
            for (int k = 0; k < 9; k++) l.Elipse(rnd.Next(T), rnd.Next(T), rnd.Next(6, 16), rnd.Next(4, 10), C(204, 210, 220));
            l.Ruido(5, rnd);
            return l;
        }

        // Acero del puente (pintura gris-verdosa con óxido).
        public static Lienzo Acero(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(32, 32);
            l.Rellenar(C(84, 98, 98));
            l.Ruido(10, rnd);
            for (int k = 0; k < 10; k++) l.Elipse(rnd.Next(32), rnd.Next(32), rnd.Next(1, 4), rnd.Next(1, 3), C(120, 70, 44));
            for (int x = 0; x < 32; x += 8) for (int y = 3; y < 32; y += 8) l.Set(x + 3, y, C(50, 56, 56));
            return l;
        }
    }
}
