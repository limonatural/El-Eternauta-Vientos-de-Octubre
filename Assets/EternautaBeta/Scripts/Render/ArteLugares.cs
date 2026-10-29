using UnityEngine;

namespace Eternauta.Beta
{
    // Fachadas de los lugares reales del mapa, dibujadas por código en baja resolución:
    // alcanzan para reconocer el cartel y los colores de cada local aunque se vean pixeladas.
    // Cada textura cubre TODA la planta baja de la fachada (128 x 64) y se estira a lo largo del frente.
    public static class ArteLugares
    {
        public const int W = 128, H = 64;

        static Color32 C(int r, int g, int b) { return new Color32(Lienzo.B(r), Lienzo.B(g), Lienzo.B(b), 255); }

        static readonly Color32 Nieve = new Color32(222, 227, 236, 255);

        // Letras de ring de cada lugar (ver tools/generar_mapas.py)
        public const string Letras = "mbicieayruKgf";

        public static bool TieneFrente(char c) { return "mbicieayruKgf".IndexOf(c) >= 0 && c != '\0'; }

        public static Lienzo Frente(char letra)
        {
            var rnd = new System.Random(letra * 17);
            var l = new Lienzo(W, H);
            switch (letra)
            {
                case 'm': McDonalds(l, rnd); break;
                case 'b': BurgerKing(l, rnd); break;
                case 'i': Ciec(l, rnd); break;
                case 'c': Cerrajeria(l, rnd); break;
                case 'e': Escuela(l, rnd); break;
                case 'a': Edificio(l, rnd); break;
                case 'y': Dental(l, rnd); break;
                case 'r': Racing(l, rnd); break;
                case 'u': Universidad(l, rnd); break;
                case 'K': Kiosco(l, rnd); break;
                case 'g': Super(l, rnd); break;
                default: Farmacia(l, rnd); break;
            }
            NieveAlPie(l, rnd);
            return l;
        }

        static void NieveAlPie(Lienzo l, System.Random rnd)
        {
            int prev = 5;
            for (int x = 0; x < l.w; x++)
            {
                int alto = Mathf.Clamp(prev + rnd.Next(-1, 2), 3, 9);
                prev = alto;
                for (int y = l.h - alto; y < l.h; y++)
                {
                    int v = 222 + rnd.Next(-12, 8);
                    l.Set(x, y, C(v - 4, v, v + 8));
                }
            }
        }

        static void Ventanas(Lienzo l, int n, int y0, int alto, Color32 vidrio, Color32 marco, int excluir = -1)
        {
            int ancho = 16;
            int paso = l.w / n;
            for (int k = 0; k < n; k++)
            {
                int x = k * paso + (paso - ancho) / 2;
                l.Rect(x - 1, y0 - 1, ancho + 2, alto + 2, marco);
                l.Rect(x, y0, ancho, alto, vidrio);
                l.Rect(x, y0 + alto / 2, ancho, 1, marco);
                l.Rect(x + ancho / 2, y0, 1, alto, marco);
                // reflejo
                l.Rect(x + 2, y0 + 2, 4, 1, Mez(vidrio, Nieve, 0.3f));
            }
        }

        static Color32 Mez(Color32 a, Color32 b, float t)
        {
            return C((int)(a.r + (b.r - a.r) * t), (int)(a.g + (b.g - a.g) * t), (int)(a.b + (b.b - a.b) * t));
        }

        static void Cartel(Lienzo l, string texto, int escala, Color32 fondo, Color32 letra, int y = 2, int alto = 14)
        {
            l.Rect(0, 0, l.w, alto + y + 1, fondo);
            l.Rect(0, y + alto, l.w, 1, Mez(fondo, C(0, 0, 0), 0.4f));
            int x = (l.w - Lienzo.AnchoTexto(texto, escala)) / 2;
            l.Texto(texto, x, y + (alto - 5 * escala) / 2, escala, letra);
        }

        static void McDonalds(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(214, 196, 150));
            l.Ruido(8, rnd);
            Cartel(l, "MCDONALDS", 2, C(196, 30, 26), C(250, 204, 20), 1, 15);
            // arcos dorados a los costados del cartel
            foreach (int cx in new[] { 8, 118 })
            {
                l.Linea(cx - 5, 14, cx - 4, 4, 2, C(250, 204, 20)); l.Linea(cx - 4, 4, cx, 12, 2, C(250, 204, 20));
                l.Linea(cx + 5, 14, cx + 4, 4, 2, C(250, 204, 20)); l.Linea(cx, 12, cx + 4, 4, 2, C(250, 204, 20));
            }
            l.Rect(0, 17, l.w, 5, C(250, 204, 20));
            Ventanas(l, 5, 25, 26, C(48, 64, 84), C(196, 30, 26));
            l.Rect(0, 52, l.w, 4, C(110, 104, 96));
        }

        static void BurgerKing(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(168, 112, 70));
            l.Ruido(8, rnd);
            l.Rect(0, 0, l.w, 18, C(240, 232, 214));
            // logo: dos panes y una banda azul
            l.Elipse(64, 8, 36, 7, C(222, 118, 36));
            l.Rect(24, 6, 80, 5, C(34, 64, 150));
            string t = "BURGER KING";
            l.Texto(t, (l.w - Lienzo.AnchoTexto(t, 1)) / 2, 6, 1, C(250, 244, 230));
            l.Elipse(64, 13, 36, 5, C(222, 118, 36));
            l.Rect(0, 18, l.w, 2, C(190, 36, 30));
            Ventanas(l, 5, 25, 26, C(54, 70, 82), C(120, 70, 40));
            l.Rect(0, 52, l.w, 4, C(96, 84, 74));
        }

        static void Ciec(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(186, 190, 196));
            l.Ruido(6, rnd);
            Cartel(l, "INSTITUTO CIEC", 1, C(28, 60, 130), C(240, 240, 240), 2, 12);
            l.Rect(0, 15, l.w, 2, C(232, 168, 30));
            Ventanas(l, 6, 22, 28, C(66, 90, 118), C(60, 70, 84));
            l.Rect(0, 52, l.w, 4, C(110, 112, 118));
        }

        static void Cerrajeria(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(150, 140, 120));
            l.Ruido(8, rnd);
            l.Rect(0, 0, l.w, 20, C(204, 156, 30));
            l.Texto("CERRAJERIA FERRETERIA", (l.w - Lienzo.AnchoTexto("CERRAJERIA FERRETERIA", 1)) / 2, 3, 1, C(150, 30, 26));
            l.Texto("25 DE MAYO", (l.w - Lienzo.AnchoTexto("25 DE MAYO", 2)) / 2, 10, 2, C(150, 30, 26));
            // persiana metálica a medio bajar
            for (int y = 24; y < 52; y += 3)
            {
                l.Rect(4, y, l.w - 8, 2, C(130, 134, 138));
                l.Rect(4, y + 2, l.w - 8, 1, C(80, 84, 88));
            }
            // llaves y herramientas pintadas
            l.Circulo(20, 36, 4, C(230, 200, 60)); l.Rect(23, 35, 14, 3, C(230, 200, 60));
            l.Rect(100, 30, 3, 14, C(190, 190, 196)); l.Rect(96, 28, 11, 4, C(190, 190, 196));
            l.Rect(0, 52, l.w, 4, C(100, 96, 90));
        }

        static void Escuela(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(206, 190, 150));
            l.Ruido(8, rnd);
            Cartel(l, "ESCUELA TECNICA N 7", 1, C(34, 52, 104), C(246, 240, 220), 2, 12);
            // estrella (símbolo de la escuela en el mapa)
            for (int k = 0; k < 5; k++)
            {
                float a0 = -Mathf.PI / 2 + k * Mathf.PI * 2 / 5, a1 = -Mathf.PI / 2 + (k + 2) * Mathf.PI * 2 / 5;
                l.Linea(12 + Mathf.Cos(a0) * 6, 24 + Mathf.Sin(a0) * 6, 12 + Mathf.Cos(a1) * 6, 24 + Mathf.Sin(a1) * 6, 1, C(224, 180, 30));
            }
            Ventanas(l, 7, 24, 24, C(60, 78, 96), C(110, 90, 60));
            l.Rect(0, 15, l.w, 2, C(150, 130, 90));
            l.Rect(0, 52, l.w, 4, C(122, 110, 90));
        }

        static void Edificio(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(176, 168, 152));
            l.Ruido(8, rnd);
            l.Rect(0, 0, l.w, 12, C(96, 90, 80));
            l.Texto("EDIFICIO", (l.w - Lienzo.AnchoTexto("EDIFICIO", 1)) / 2, 3, 1, C(230, 216, 170));
            for (int y = 14; y < 52; y += 6) l.Rect(0, y, l.w, 1, C(150, 142, 126));
            Ventanas(l, 4, 20, 22, C(50, 58, 70), C(90, 84, 74));
            l.Rect(0, 52, l.w, 4, C(100, 96, 88));
        }

        static void Dental(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(226, 232, 232));
            l.Ruido(5, rnd);
            Cartel(l, "DENTAL TOTAL", 1, C(24, 124, 140), C(250, 250, 250), 2, 12);
            // muela
            l.Rect(10, 19, 9, 5, C(250, 250, 250)); l.Rect(10, 24, 3, 4, C(250, 250, 250)); l.Rect(16, 24, 3, 4, C(250, 250, 250));
            l.Borde(10, 19, 9, 5, C(24, 124, 140));
            Ventanas(l, 5, 28, 22, C(86, 150, 164), C(24, 124, 140));
            l.Rect(0, 52, l.w, 4, C(150, 160, 160));
        }

        static void Racing(Lienzo l, System.Random rnd)
        {
            // franjas verticales celestes y blancas
            for (int x = 0; x < l.w; x++) l.Rect(x, 0, 1, H, (x / 8) % 2 == 0 ? C(116, 190, 236) : C(240, 244, 246));
            l.Ruido(5, rnd);
            l.Rect(0, 0, l.w, 16, C(30, 44, 80));
            l.Texto("RACING CLUB", (l.w - Lienzo.AnchoTexto("RACING CLUB", 1)) / 2, 5, 1, C(116, 190, 236));
            // escudo
            l.Circulo(14, 30, 7, C(250, 250, 250)); l.Circulo(14, 30, 5, C(116, 190, 236)); l.Rect(13, 26, 2, 8, C(250, 250, 250));
            l.Rect(24, 22, 80, 28, C(40, 56, 80));
            l.Rect(26, 24, 76, 24, C(70, 96, 124));
            l.Rect(0, 52, l.w, 4, C(120, 130, 140));
        }

        static void Universidad(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(90, 96, 108));
            l.Ruido(6, rnd);
            l.Rect(0, 0, l.w, 16, C(240, 140, 30));
            l.Texto("SIGLO 21", (l.w - Lienzo.AnchoTexto("SIGLO 21", 2)) / 2, 3, 2, C(34, 40, 84));
            l.Texto("UNIVERSIDAD", (l.w - Lienzo.AnchoTexto("UNIVERSIDAD", 1)) / 2, 18, 1, C(236, 236, 240));
            Ventanas(l, 5, 26, 24, C(52, 80, 110), C(190, 196, 206));
            l.Rect(0, 52, l.w, 4, C(100, 104, 112));
        }

        static void Kiosco(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(196, 150, 70));
            l.Ruido(8, rnd);
            // toldo a rayas
            for (int x = 0; x < l.w; x++) l.Rect(x, 0, 1, 10, (x / 6) % 2 == 0 ? C(210, 40, 40) : C(240, 236, 226));
            l.Rect(0, 10, l.w, 6, C(40, 46, 80));
            l.Texto("KIOSCO", (l.w - Lienzo.AnchoTexto("KIOSCO", 1)) / 2, 11, 1, C(250, 220, 90));
            // mostrador con revistas y golosinas
            l.Rect(6, 22, 116, 28, C(52, 44, 40));
            Color32[] cols = { C(230, 60, 60), C(60, 150, 220), C(240, 200, 40), C(90, 190, 90), C(220, 120, 200) };
            for (int i = 0; i < 17; i++) l.Rect(9 + i * 6, 26 + (i % 3) * 5, 5, 8, cols[i % cols.Length]);
            l.Texto("24 HS", 90, 41, 1, C(250, 250, 250));
            l.Rect(0, 52, l.w, 4, C(120, 100, 70));
        }

        static void Super(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(206, 206, 196));
            l.Ruido(6, rnd);
            Cartel(l, "SUPERMERCADO", 2, C(30, 130, 60), C(250, 250, 240), 1, 14);
            l.Rect(0, 16, l.w, 2, C(210, 40, 36));
            Ventanas(l, 5, 22, 28, C(60, 86, 76), C(210, 40, 36));
            // carteles de ofertas
            for (int i = 0; i < 5; i++) l.Rect(6 + i * 25, 43, 12, 6, C(250, 226, 60));
            l.Rect(0, 52, l.w, 4, C(110, 112, 104));
        }

        static void Farmacia(Lienzo l, System.Random rnd)
        {
            l.Rellenar(C(226, 230, 226));
            l.Ruido(5, rnd);
            Cartel(l, "FARMACIA", 2, C(30, 140, 70), C(250, 250, 250), 1, 14);
            // cruz verde
            l.Rect(8, 19, 4, 12, C(30, 170, 80)); l.Rect(4, 23, 12, 4, C(30, 170, 80));
            l.Rect(112, 19, 4, 12, C(30, 170, 80)); l.Rect(108, 23, 12, 4, C(30, 170, 80));
            Ventanas(l, 4, 22, 26, C(84, 124, 110), C(30, 140, 70));
            l.Rect(0, 52, l.w, 4, C(150, 160, 152));
        }

        // Muro de nieve acumulada: el límite del mapa.
        public static Lienzo MuroNieve(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.11f + semilla, y * 0.09f);
                    int v = 206 + (int)(n * 30) - (y > 40 ? (y - 40) / 3 : 0);
                    l.Set(x, y, C(v - 6, v - 1, v + 6));
                }
            for (int k = 0; k < 7; k++)
            {
                int x = rnd.Next(64), y = rnd.Next(64), r = rnd.Next(5, 12);
                l.Elipse(x, y, r, r * 0.6f, C(176, 190, 214));
            }
            for (int k = 0; k < 40; k++) l.Set(rnd.Next(64), rnd.Next(64), C(250, 252, 255));
            return l;
        }

        // Piso de la plaza: baldosas grises bajo la nieve y canteros.
        public static Lienzo Plaza(int semilla)
        {
            var rnd = new System.Random(semilla);
            var l = new Lienzo(64, 64);
            l.Rellenar(C(208, 214, 224));
            for (int y = 0; y < 64; y += 16)
                for (int x = 0; x < 64; x += 16)
                {
                    int d = rnd.Next(-10, 6);
                    l.Rect(x + 1, y + 1, 14, 14, C(176 + d, 180 + d, 188 + d));
                }
            for (int k = 0; k < 90; k++) l.Set(rnd.Next(64), rnd.Next(64), C(240, 244, 250));
            l.Ruido(7, rnd);
            for (int k = 0; k < 4; k++) l.Elipse(rnd.Next(64), rnd.Next(64), rnd.Next(4, 9), rnd.Next(3, 6), C(222, 228, 238));
            return l;
        }
    }
}
