using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Pieza móvil de un modelo (cabeza que gira, llama que titila, hoja de una puerta...).
    public class Parte
    {
        public string nombre;
        public Vector3 pivote;              // posición dentro del modelo
        public ConstructorMalla malla;      // construida alrededor del pivote
    }

    public class Modelo
    {
        public ConstructorMalla malla;
        public readonly List<Parte> partes = new List<Parte>();
    }

    // Modelos 3D low-poly de todos los objetos del juego, armados por código.
    // Escala: 1 unidad = 1 celda del mapa = 2 metros. El frente de cada modelo mira a -Z.
    public static class Modelos
    {
        public static Atlas atlas;
        static Region ruido;

        // Paleta
        static readonly Color32 Madera = C(108, 76, 48), MaderaOsc = C(70, 48, 30), MaderaClara = C(150, 116, 78);
        static readonly Color32 Metal = C(104, 112, 120), MetalOsc = C(46, 50, 56), Negro = C(24, 26, 30);
        static readonly Color32 Nieve = ConstructorMalla.ColNieve, Blanco = C(222, 222, 216);
        static readonly Color32 Piel = C(196, 152, 120), Bronce = C(70, 98, 86), Piedra = C(150, 146, 138);

        static Color32 C(int r, int g, int b) { return ConstructorMalla.Col(r, g, b); }
        static Color32 Var(Color32 c, int d) { return C(c.r + d, c.g + d, c.b + d); }

        public static void Inicializar()
        {
            if (atlas != null) return;
            atlas = new Atlas(256);
            var l = new Lienzo(16, 16);
            var rnd = new System.Random(3);
            for (int i = 0; i < l.px.Length; i++) { int v = 214 + rnd.Next(-26, 30); l.px[i] = new Color32(Lienzo.B(v), Lienzo.B(v), Lienzo.B(v), 255); }
            ruido = atlas.Agregar("ruido", l);
            Calcomanias();
        }

        public static Region Ruido { get { Inicializar(); return ruido; } }

        public static ConstructorMalla Nueva() { Inicializar(); return new ConstructorMalla { region = ruido }; }

        static Parte NuevaParte(Modelo m, string nombre, Vector3 pivote)
        {
            var p = new Parte { nombre = nombre, pivote = pivote, malla = Nueva() };
            m.partes.Add(p);
            return p;
        }

        // ------------------------------------------------------------------
        // CALCOMANÍAS (etiquetas, carteles, caras) en el atlas
        // ------------------------------------------------------------------
        static void Calcomanias()
        {
            var rnd = new System.Random(11);
            Lienzo l;

            l = new Lienzo(16, 12); l.Rellenar(C(226, 226, 220)); l.Rect(6, 1, 4, 10, C(180, 30, 30)); l.Rect(2, 4, 12, 4, C(180, 30, 30)); l.Ruido(5, rnd);
            atlas.Agregar("cruz", l);

            l = new Lienzo(36, 16); l.Rellenar(C(46, 104, 60)); l.Rect(0, 0, 36, 2, C(200, 190, 150)); l.Rect(0, 14, 36, 2, C(200, 190, 150));
            l.Texto("ARVEJAS", 4, 5, 1, C(240, 236, 220)); l.Circulo(33, 8, 2, C(110, 170, 80)); l.Ruido(5, rnd);
            atlas.Agregar("lata", l);

            l = new Lienzo(44, 16); l.Rellenar(C(226, 214, 170)); l.Rect(0, 0, 44, 3, C(176, 40, 36)); l.Rect(0, 13, 44, 3, C(176, 40, 36));
            l.Texto("GALLETITAS", 3, 5, 1, C(150, 30, 30)); l.Ruido(5, rnd);
            atlas.Agregar("galletitas", l);

            l = new Lienzo(20, 24); l.Rellenar(C(214, 206, 186)); l.Rect(2, 2, 16, 20, C(120, 150, 170)); l.Rect(2, 14, 16, 8, C(110, 130, 80));
            l.Circulo(7, 10, 2, C(200, 160, 130)); l.Rect(5, 12, 4, 6, C(150, 50, 50)); l.Circulo(13, 9, 2, C(200, 160, 130)); l.Rect(11, 11, 4, 7, C(50, 70, 120));
            l.Circulo(10, 15, 1.5f, C(200, 160, 130)); l.Rect(9, 16, 2, 3, C(200, 170, 60)); l.Ruido(7, rnd);
            atlas.Agregar("foto", l);

            l = new Lienzo(32, 24); l.Rellenar(C(222, 210, 176));
            l.Linea(0, 4, 31, 7, 2, C(60, 90, 140)); // Riachuelo
            for (int k = 0; k < 6; k++) l.Linea(rnd.Next(32), 9, rnd.Next(32), 23, 1, C(150, 140, 120));
            l.Linea(14, 23, 16, 6, 1, C(170, 40, 40)); l.Texto("X", 13, 1, 1, C(170, 40, 40)); l.Ruido(5, rnd);
            atlas.Agregar("mapa", l);

            l = new Lienzo(52, 20); l.Rellenar(C(214, 212, 202)); l.Borde(1, 1, 50, 18, C(30, 50, 100));
            l.Texto("PLAZA", (52 - Lienzo.AnchoTexto("PLAZA", 1)) / 2, 4, 1, C(30, 50, 100));
            l.Texto("ALSINA", (52 - Lienzo.AnchoTexto("ALSINA", 1)) / 2, 11, 1, C(30, 50, 100)); l.Ruido(5, rnd);
            atlas.Agregar("cartel_plaza", l);

            l = new Lienzo(52, 14); l.Rellenar(C(30, 62, 124)); l.Borde(1, 1, 50, 12, C(232, 232, 228));
            l.Texto("AV. MITRE", (52 - Lienzo.AnchoTexto("AV. MITRE", 1)) / 2, 5, 1, C(232, 232, 228)); l.Ruido(4, rnd);
            atlas.Agregar("cartel_mitre", l);

            l = new Lienzo(52, 14); l.Rellenar(C(22, 92, 52)); l.Borde(1, 1, 50, 12, C(232, 232, 228));
            l.Texto("PUENTE ^", (52 - Lienzo.AnchoTexto("PUENTE ^", 1)) / 2, 5, 1, C(232, 232, 228)); l.Ruido(4, rnd);
            atlas.Agregar("cartel_puente_chico", l);

            l = new Lienzo(96, 32); l.Rellenar(C(22, 92, 52)); l.Borde(2, 2, 92, 28, C(232, 232, 228));
            l.Texto("PUENTE PUEYRREDON", (96 - Lienzo.AnchoTexto("PUENTE PUEYRREDON", 1)) / 2, 7, 1, C(236, 236, 228));
            l.Texto("CAPITAL FEDERAL ^", (96 - Lienzo.AnchoTexto("CAPITAL FEDERAL ^", 1)) / 2, 19, 1, C(236, 236, 228)); l.Ruido(4, rnd);
            atlas.Agregar("cartel_puente", l);

            l = new Lienzo(64, 16); l.Rellenar(C(124, 30, 30)); l.Borde(1, 1, 62, 14, C(200, 190, 170));
            l.Texto("ALMACEN", (64 - Lienzo.AnchoTexto("ALMACEN", 2)) / 2, 3, 2, C(230, 224, 210)); l.Ruido(8, rnd);
            atlas.Agregar("cartel_almacen", l);

            l = new Lienzo(36, 12); l.Rellenar(C(96, 86, 54)); l.Borde(0, 0, 36, 12, C(140, 124, 70));
            l.Texto("ALSINA", (36 - Lienzo.AnchoTexto("ALSINA", 1)) / 2, 4, 1, C(200, 186, 120));
            atlas.Agregar("placa", l);

            l = new Lienzo(24, 8); l.Rellenar(C(214, 190, 120)); for (int x = 2; x < 22; x += 3) l.Rect(x, 1, 1, 3, C(60, 40, 20));
            l.Rect(13, 0, 1, 8, C(180, 30, 20));
            atlas.Agregar("dial", l);

            l = new Lienzo(16, 16); l.Rellenar(C(90, 70, 50));
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if ((x + y) % 2 == 0) l.Set(x, y, C(70, 54, 40));
            atlas.Agregar("rejilla", l);

            // Cara del informante: barba de varios días, venda en la frente
            l = new Lienzo(16, 16); l.Rellenar(Piel);
            l.Rect(0, 0, 16, 4, C(226, 222, 210)); l.Rect(10, 1, 3, 2, C(160, 50, 40));
            l.Rect(3, 6, 3, 2, C(240, 240, 236)); l.Rect(10, 6, 3, 2, C(240, 240, 236));
            l.Rect(4, 6, 2, 2, C(40, 50, 60)); l.Rect(10, 6, 2, 2, C(40, 50, 60));
            l.Rect(2, 5, 4, 1, C(80, 60, 44)); l.Rect(10, 5, 4, 1, C(80, 60, 44));
            l.Rect(7, 8, 2, 3, Var(Piel, -30));
            l.Rect(2, 11, 12, 5, C(92, 78, 66)); l.Rect(5, 12, 6, 1, C(120, 50, 50));
            l.Ruido(6, rnd);
            atlas.Agregar("cara", l);

            l = ArteProcedural.Puerta(11);
            atlas.Agregar("puerta", l);

            l = new Lienzo(24, 16); l.Rellenar(C(220, 216, 200));
            l.Texto("DIARIO", 1, 1, 1, C(30, 30, 30));
            for (int y = 8; y < 15; y += 2) l.Rect(1, y, 22, 1, C(120, 120, 120));
            atlas.Agregar("diario", l);
        }

        // ------------------------------------------------------------------
        // FÁBRICA
        // ------------------------------------------------------------------
        public static Modelo Crear(string nombre, int semilla)
        {
            Inicializar();
            var m = new Modelo { malla = Nueva() };
            var b = m.malla;
            var rnd = new System.Random(semilla * 7919 + nombre.Length);
            switch (nombre)
            {
                case "mesa_trabajo": MesaTrabajo(b); break;
                case "lona": Lona(b); break;
                case "alambre": Alambre(b); break;
                case "botiquin": Botiquin(b); break;
                case "cama": Cama(b); break;
                case "lampara": Lampara(m); break;
                case "foto": Foto(b); break;
                case "mesa": Mesa(b); break;
                case "monumento": Monumento(b); break;
                case "arbol": Arbol(b, rnd); break;
                case "banco": Banco(b); break;
                case "farola": Farola(b); break;
                case "auto_nevado": Auto(b, rnd, false); break;
                case "auto_volcado": Auto(b, rnd, true); break;
                case "cartel_plaza": CartelDosPostes(b, "cartel_plaza", 0.62f, 0.24f); break;
                case "cartel_avenida": CartelAvenida(b); break;
                case "cartel_caido": CartelCaido(b); break;
                case "lata": Lata(b); break;
                case "galletitas": Galletitas(b); break;
                case "chatarra": Chatarra(b); break;
                case "radio": Radio(m); break;
                case "silla_rota": SillaRota(b); break;
                case "informante": Informante(m); break;
                case "choripan": Choripan(b); break;
                case "estanteria": Estanteria(b, rnd); break;
                case "vela": Vela(m); break;
                case "semaforo": Semaforo(m); break;
                case "mapa": Mapa(b); break;
                default:
                    Debug.LogWarning("[Eternauta] Falta el modelo " + nombre);
                    b.Caja(new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.3f, 0.3f), C(200, 0, 200));
                    break;
            }
            return m;
        }

        // ------------------------------------------------------------------
        // MUEBLES
        // ------------------------------------------------------------------
        static void Patas(ConstructorMalla b, float ancho, float prof, float alto, float g, Color32 c)
        {
            float x = ancho * 0.5f - g * 0.5f, z = prof * 0.5f - g * 0.5f;
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    b.Caja(new Vector3(sx * x, alto * 0.5f, sz * z), new Vector3(g, alto, g), c);
        }

        static void MesaTrabajo(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.43f, 0), new Vector3(1.0f, 0.05f, 0.46f), MaderaClara);
            Patas(b, 0.94f, 0.4f, 0.41f, 0.05f, MaderaOsc);
            b.Caja(new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.03f, 0.36f), Madera);
            // tablero de herramientas atrás
            b.Caja(new Vector3(0, 0.74f, 0.21f), new Vector3(1.0f, 0.56f, 0.03f), Var(MaderaClara, -16));
            b.Barra(new Vector3(-0.3f, 0.86f, 0.19f), new Vector3(-0.3f, 0.66f, 0.19f), 0.025f, MaderaOsc); // martillo
            b.Caja(new Vector3(-0.3f, 0.87f, 0.18f), new Vector3(0.09f, 0.03f, 0.03f), MetalOsc);
            b.Caja(new Vector3(0.0f, 0.78f, 0.19f), new Vector3(0.22f, 0.08f, 0.01f), Metal);     // serrucho
            b.Caja(new Vector3(0.14f, 0.78f, 0.18f), new Vector3(0.06f, 0.05f, 0.03f), C(150, 50, 40));
            b.Barra(new Vector3(0.32f, 0.92f, 0.19f), new Vector3(0.38f, 0.62f, 0.19f), 0.02f, C(180, 160, 40)); // destornillador
            // morsa
            b.Caja(new Vector3(0.38f, 0.5f, -0.12f), new Vector3(0.12f, 0.08f, 0.1f), C(60, 80, 110));
            b.Caja(new Vector3(0.38f, 0.56f, -0.12f), new Vector3(0.14f, 0.04f, 0.03f), C(60, 80, 110));
            // trozos de lona y cinta sobre la mesa
            b.Caja(new Vector3(-0.18f, 0.47f, -0.04f), new Vector3(0.36f, 0.03f, 0.24f), C(86, 96, 64));
            b.Cilindro(new Vector3(0.12f, 0.455f, 0.02f), 0.04f, 0.04f, 0.03f, 8, C(150, 150, 150));
            // lámpara de brazo
            b.Barra(new Vector3(-0.42f, 0.455f, 0.12f), new Vector3(-0.42f, 0.7f, 0.05f), 0.02f, MetalOsc);
            b.Barra(new Vector3(-0.42f, 0.7f, 0.05f), new Vector3(-0.3f, 0.66f, -0.05f), 0.02f, MetalOsc);
            b.brillo = 1f;
            b.Cilindro(new Vector3(-0.3f, 0.6f, -0.05f), 0.05f, 0.025f, 0.06f, 8, C(255, 226, 150));
            b.brillo = 0f;
        }

        static void Mesa(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.38f, 0), new Vector3(0.74f, 0.04f, 0.5f), Madera);
            Patas(b, 0.68f, 0.44f, 0.36f, 0.04f, MaderaOsc);
            Silla(b, new Vector3(-0.2f, 0, -0.38f), 0f, Madera);
            Silla(b, new Vector3(0.24f, 0, 0.36f), 170f, Madera);
            b.Cilindro(new Vector3(-0.12f, 0.4f, 0.04f), 0.07f, 0.075f, 0.012f, 10, Blanco);         // plato
            b.Cilindro(new Vector3(0.16f, 0.4f, -0.06f), 0.03f, 0.03f, 0.06f, 8, C(150, 60, 50));    // taza
            b.Push(); b.Mover(0.06f, 0.405f, 0.12f); b.RotarY(20f);
            b.Caja(Vector3.zero, new Vector3(0.2f, 0.008f, 0.14f), Blanco, null, null, Blanco);
            b.Pop();
            b.Push(); b.Mover(0.06f, 0.41f, 0.12f); b.RotarY(20f); b.RotarX(90f);
            b.Caja(Vector3.zero, new Vector3(0.18f, 0.12f, 0.002f), Blanco, atlas["diario"]);
            b.Pop();
        }

        static void Silla(ConstructorMalla b, Vector3 pos, float giro, Color32 c)
        {
            b.Push();
            b.Mover(pos); b.RotarY(giro);
            b.Caja(new Vector3(0, 0.22f, 0), new Vector3(0.24f, 0.03f, 0.24f), c);
            Patas(b, 0.22f, 0.22f, 0.21f, 0.03f, Var(c, -30));
            b.Caja(new Vector3(0, 0.38f, 0.11f), new Vector3(0.24f, 0.3f, 0.03f), c);
            b.Pop();
        }

        static void SillaRota(ConstructorMalla b)
        {
            b.Push();
            b.Mover(0, 0.12f, 0); b.RotarZ(78f); b.RotarY(25f);
            b.Caja(new Vector3(0, 0, 0), new Vector3(0.24f, 0.03f, 0.24f), Madera);
            b.Caja(new Vector3(0, 0.16f, 0.11f), new Vector3(0.24f, 0.3f, 0.03f), Madera);
            b.Caja(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(0.03f, 0.2f, 0.03f), MaderaOsc);
            b.Caja(new Vector3(0.1f, -0.1f, -0.1f), new Vector3(0.03f, 0.2f, 0.03f), MaderaOsc);
            b.Caja(new Vector3(0.1f, -0.06f, 0.1f), new Vector3(0.03f, 0.1f, 0.03f), MaderaOsc);
            b.Pop();
            b.Push(); b.Mover(0.2f, 0.015f, -0.18f); b.RotarY(60f);
            b.Caja(Vector3.zero, new Vector3(0.03f, 0.03f, 0.2f), MaderaOsc); // pata rota en el piso
            b.Pop();
        }

        static void Cama(ConstructorMalla b)
        {
            // a lo largo de X
            b.Caja(new Vector3(0, 0.13f, 0), new Vector3(1.0f, 0.08f, 0.55f), MaderaOsc);
            Patas(b, 0.98f, 0.53f, 0.1f, 0.05f, MaderaOsc);
            b.Caja(new Vector3(-0.52f, 0.26f, 0), new Vector3(0.05f, 0.34f, 0.57f), Madera); // respaldo
            b.Caja(new Vector3(0.52f, 0.2f, 0), new Vector3(0.05f, 0.2f, 0.57f), Madera);
            b.Caja(new Vector3(0, 0.21f, 0), new Vector3(0.96f, 0.08f, 0.5f), C(150, 160, 170));    // colchón
            b.Caja(new Vector3(0.12f, 0.26f, 0), new Vector3(0.74f, 0.04f, 0.54f), C(120, 40, 40)); // frazada
            b.Caja(new Vector3(0.12f, 0.2f, -0.27f), new Vector3(0.74f, 0.12f, 0.02f), C(110, 36, 36));
            b.Caja(new Vector3(-0.38f, 0.28f, 0), new Vector3(0.16f, 0.06f, 0.34f), Blanco);       // almohada
        }

        static void Lampara(Modelo m)
        {
            var b = m.malla;
            b.Cilindro(Vector3.zero, 0.1f, 0.09f, 0.03f, 8, MetalOsc);
            b.Cilindro(new Vector3(0, 0.03f, 0), 0.015f, 0.015f, 0.68f, 6, MetalOsc);
            b.brillo = 1f;
            b.Cilindro(new Vector3(0, 0.62f, 0), 0.16f, 0.09f, 0.15f, 8, C(255, 222, 160), false);
            b.Cilindro(new Vector3(0, 0.62f, 0), 0.155f, 0.088f, 0.15f, 8, C(255, 210, 140), false); // interior (otra cara)
            b.brillo = 0f;
        }

        static void Estanteria(ConstructorMalla b, System.Random rnd)
        {
            float ancho = 1.0f, alto = 1.15f, prof = 0.3f;
            b.Caja(new Vector3(-ancho / 2, alto / 2, 0), new Vector3(0.03f, alto, prof), MaderaOsc);
            b.Caja(new Vector3(ancho / 2, alto / 2, 0), new Vector3(0.03f, alto, prof), MaderaOsc);
            b.Caja(new Vector3(0, alto / 2, prof / 2 - 0.01f), new Vector3(ancho, alto, 0.02f), Var(Madera, -20));
            Color32[] colores = { C(170, 40, 36), C(46, 104, 60), C(200, 170, 60), C(60, 80, 140), C(210, 210, 200), C(140, 90, 50) };
            for (int k = 0; k < 4; k++)
            {
                float y = 0.06f + k * 0.34f;
                b.Caja(new Vector3(0, y, 0), new Vector3(ancho, 0.025f, prof), Madera);
                float x = -ancho / 2 + 0.06f;
                while (x < ancho / 2 - 0.08f)
                {
                    float w = 0.05f + (float)rnd.NextDouble() * 0.08f;
                    if (rnd.Next(3) > 0)
                    {
                        var c = colores[rnd.Next(colores.Length)];
                        float h = 0.07f + (float)rnd.NextDouble() * 0.12f;
                        if (rnd.Next(2) == 0) b.Cilindro(new Vector3(x + w / 2, y + 0.0125f, -0.02f), w / 2, w / 2, h, 8, c);
                        else b.Caja(new Vector3(x + w / 2, y + 0.0125f + h / 2, -0.02f), new Vector3(w, h, 0.12f), c);
                    }
                    x += w + 0.02f;
                }
            }
        }

        static void Vela(Modelo m)
        {
            var b = m.malla;
            b.Caja(new Vector3(0, 0.12f, 0), new Vector3(0.3f, 0.24f, 0.26f), Madera);   // cajón de madera
            b.Caja(new Vector3(0, 0.12f, -0.131f), new Vector3(0.28f, 0.03f, 0.005f), MaderaOsc);
            b.Cilindro(new Vector3(0.04f, 0.24f, 0), 0.05f, 0.055f, 0.012f, 8, Blanco);
            b.Cilindro(new Vector3(0.04f, 0.252f, 0), 0.018f, 0.016f, 0.07f, 8, C(232, 226, 200));
            var p = NuevaParte(m, "llama", new Vector3(0.04f, 0.33f, 0));
            p.malla.brillo = 1f;
            p.malla.Caja(new Vector3(0, 0.01f, 0), new Vector3(0.022f, 0.035f, 0.022f), C(255, 200, 90));
            p.malla.Caja(new Vector3(0, 0.035f, 0), new Vector3(0.012f, 0.02f, 0.012f), C(255, 246, 200));
        }

        // ------------------------------------------------------------------
        // OBJETOS RECOGIBLES (giran sobre su eje)
        // ------------------------------------------------------------------
        static void Botiquin(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.1f, 0), new Vector3(0.28f, 0.2f, 0.1f), Blanco, atlas["cruz"], atlas["cruz"]);
            b.Barra(new Vector3(-0.06f, 0.2f, 0), new Vector3(-0.06f, 0.24f, 0), 0.02f, MetalOsc);
            b.Barra(new Vector3(0.06f, 0.2f, 0), new Vector3(0.06f, 0.24f, 0), 0.02f, MetalOsc);
            b.Barra(new Vector3(-0.06f, 0.24f, 0), new Vector3(0.06f, 0.24f, 0), 0.02f, MetalOsc);
            b.Caja(new Vector3(0, 0.15f, -0.052f), new Vector3(0.04f, 0.03f, 0.01f), MetalOsc);
        }

        static void Lata(ConstructorMalla b)
        {
            b.region = atlas["lata"];
            b.Cilindro(new Vector3(0, 0.012f, 0), 0.08f, 0.08f, 0.17f, 10, ConstructorMalla.Blanco, true, Metal);
            b.region = ruido;
            b.Cilindro(Vector3.zero, 0.081f, 0.081f, 0.012f, 10, Metal);
            b.Cilindro(new Vector3(0, 0.182f, 0), 0.081f, 0.081f, 0.01f, 10, Metal);
        }

        static void Galletitas(ConstructorMalla b)
        {
            var reg = atlas["galletitas"];
            b.Caja(new Vector3(0, 0.07f, 0), new Vector3(0.26f, 0.09f, 0.12f), C(226, 214, 170), reg, reg);
            b.Caja(new Vector3(-0.14f, 0.07f, 0), new Vector3(0.03f, 0.08f, 0.1f), C(176, 40, 36));
            b.Caja(new Vector3(0.14f, 0.07f, 0), new Vector3(0.03f, 0.08f, 0.1f), C(176, 40, 36));
        }

        static void Choripan(ConstructorMalla b)
        {
            b.CilindroX(new Vector3(0, 0.07f, 0), 0.06f, 0.3f, 8, C(206, 160, 96));
            b.CilindroX(new Vector3(0, 0.1f, -0.01f), 0.04f, 0.36f, 8, C(120, 46, 36));
            b.Caja(new Vector3(0, 0.12f, 0.02f), new Vector3(0.26f, 0.03f, 0.06f), C(196, 150, 88));
            b.Caja(new Vector3(0.05f, 0.14f, -0.02f), new Vector3(0.05f, 0.01f, 0.03f), C(60, 110, 50)); // chimichurri
            b.Caja(new Vector3(-0.07f, 0.14f, -0.02f), new Vector3(0.04f, 0.01f, 0.02f), C(220, 230, 240)); // escarcha
        }

        static void Lona(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.05f, 0), new Vector3(0.32f, 0.1f, 0.24f), C(86, 96, 64));
            b.Push(); b.Mover(0.01f, 0.13f, 0); b.RotarY(8f);
            b.Caja(Vector3.zero, new Vector3(0.28f, 0.06f, 0.2f), C(96, 106, 70));
            b.Pop();
            b.Caja(new Vector3(-0.07f, 0.09f, 0), new Vector3(0.025f, 0.19f, 0.25f), C(196, 170, 110)); // soga
            b.Caja(new Vector3(0.08f, 0.09f, 0), new Vector3(0.025f, 0.19f, 0.25f), C(196, 170, 110));
        }

        static void Alambre(ConstructorMalla b)
        {
            int n = 12;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                for (int capa = 0; capa < 3; capa++)
                {
                    float r = 0.1f + capa * 0.012f, y = 0.02f + capa * 0.018f;
                    b.Barra(new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r), new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r), 0.016f, Var(Metal, capa * 12));
                }
            }
            b.Cilindro(new Vector3(0.0f, 0.075f, 0.0f), 0.055f, 0.055f, 0.04f, 10, C(170, 170, 176));
            b.Cilindro(new Vector3(0.0f, 0.076f, 0.0f), 0.025f, 0.025f, 0.042f, 8, C(150, 120, 90));
        }

        static void Chatarra(ConstructorMalla b)
        {
            b.Cilindro(new Vector3(-0.05f, 0, 0), 0.1f, 0.1f, 0.03f, 10, Metal);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2 / 10;
                b.Caja(new Vector3(-0.05f + Mathf.Cos(a) * 0.11f, 0.015f, Mathf.Sin(a) * 0.11f), new Vector3(0.03f, 0.03f, 0.03f), Metal);
            }
            b.CilindroX(new Vector3(0.05f, 0.07f, 0.03f), 0.025f, 0.28f, 8, C(120, 80, 60));
            b.Push(); b.Mover(0.02f, 0.06f, -0.06f); b.RotarZ(25f); b.RotarY(30f);
            b.Caja(Vector3.zero, new Vector3(0.18f, 0.01f, 0.12f), C(110, 116, 120));
            b.Pop();
        }

        static void Foto(ConstructorMalla b)
        {
            b.Push(); b.Mover(0, 0.14f, 0); b.RotarX(-8f);
            b.Caja(Vector3.zero, new Vector3(0.22f, 0.27f, 0.025f), MaderaOsc);
            b.Caja(new Vector3(0, 0, -0.0135f), new Vector3(0.17f, 0.21f, 0.002f), ConstructorMalla.Blanco, atlas["foto"]);
            b.Pop();
            b.Barra(new Vector3(0, 0.2f, 0.02f), new Vector3(0, 0.0f, 0.1f), 0.02f, MaderaOsc);
        }

        static void Mapa(ConstructorMalla b)
        {
            var reg = atlas["mapa"];
            b.Push(); b.Mover(-0.075f, 0.11f, 0); b.RotarY(-18f);
            b.Caja(Vector3.zero, new Vector3(0.15f, 0.22f, 0.006f), C(222, 210, 176), reg, reg);
            b.Pop();
            b.Push(); b.Mover(0.075f, 0.11f, 0); b.RotarY(18f);
            b.Caja(Vector3.zero, new Vector3(0.15f, 0.22f, 0.006f), C(222, 210, 176), reg, reg);
            b.Pop();
        }

        // ------------------------------------------------------------------
        // PLAZA Y CALLE
        // ------------------------------------------------------------------
        static void Monumento(ConstructorMalla b)
        {
            b.CajaNevada(new Vector3(0, 0.08f, 0), new Vector3(1.3f, 0.16f, 1.3f), Var(Piedra, -10));
            b.CajaNevada(new Vector3(0, 0.24f, 0), new Vector3(1.0f, 0.16f, 1.0f), Piedra);
            b.Caja(new Vector3(0, 0.8f, 0), new Vector3(0.62f, 0.96f, 0.62f), Var(Piedra, 8), atlas["placa"]);
            b.CajaNevada(new Vector3(0, 1.31f, 0), new Vector3(0.74f, 0.08f, 0.74f), Var(Piedra, -6));
            // la estatua (bronce)
            b.Push(); b.Mover(0, 1.39f, 0);
            Figura(b, Bronce, Bronce, Bronce, Bronce, true, 0.85f);
            b.Pop();
        }

        // Figura humana de pie (también la estatua). Altura total aprox. 0.9 * escala.
        static void Figura(ConstructorMalla b, Color32 ropa, Color32 pantalon, Color32 piel, Color32 pelo, bool estatua, float esc)
        {
            b.Push(); b.Escalar(esc);
            // piernas y botas
            b.Tubo(new Vector3(-0.06f, 0.42f, 0), new Vector3(-0.06f, 0.04f, 0), 0.045f, 0.04f, 6, pantalon);
            b.Tubo(new Vector3(0.06f, 0.42f, 0), new Vector3(0.06f, 0.04f, 0), 0.045f, 0.04f, 6, pantalon);
            b.Caja(new Vector3(-0.06f, 0.025f, -0.02f), new Vector3(0.07f, 0.05f, 0.12f), estatua ? ropa : Negro);
            b.Caja(new Vector3(0.06f, 0.025f, -0.02f), new Vector3(0.07f, 0.05f, 0.12f), estatua ? ropa : Negro);
            // levita
            b.Tubo(new Vector3(0, 0.3f, 0), new Vector3(0, 0.72f, 0), 0.13f, 0.11f, 8, ropa);
            b.Tubo(new Vector3(0, 0.72f, 0), new Vector3(0, 0.76f, 0), 0.11f, 0.05f, 8, ropa);
            // brazos: uno levantado (estatua) o a los costados
            b.Tubo(new Vector3(-0.13f, 0.7f, 0), new Vector3(-0.17f, 0.42f, -0.02f), 0.035f, 0.03f, 6, ropa);
            if (estatua) b.Tubo(new Vector3(0.13f, 0.7f, 0), new Vector3(0.24f, 0.98f, -0.08f), 0.035f, 0.03f, 6, ropa);
            else b.Tubo(new Vector3(0.13f, 0.7f, 0), new Vector3(0.17f, 0.42f, -0.02f), 0.035f, 0.03f, 6, ropa);
            // cabeza
            b.Tubo(new Vector3(0, 0.76f, 0), new Vector3(0, 0.8f, 0), 0.035f, 0.035f, 6, piel);
            b.Esfera(new Vector3(0, 0.86f, 0), 0.065f, 8, 6, piel, 1.15f);
            if (estatua)
            {
                b.Caja(new Vector3(0, 0.94f, 0), new Vector3(0.1f, 0.025f, 0.1f), Nieve);
                b.Caja(new Vector3(-0.1f, 0.745f, 0), new Vector3(0.1f, 0.02f, 0.12f), Nieve);
                b.Caja(new Vector3(0.1f, 0.745f, 0), new Vector3(0.1f, 0.02f, 0.12f), Nieve);
            }
            b.Pop();
        }

        static void Arbol(ConstructorMalla b, System.Random rnd)
        {
            var tronco = C(58, 48, 40);
            float alto = 1.0f + (float)rnd.NextDouble() * 0.35f;
            b.Tubo(new Vector3(0, 0, 0), new Vector3(0.03f, alto, 0.02f), 0.085f, 0.04f, 6, tronco);
            b.Cilindro(new Vector3(0, 0, 0), 0.22f, 0.1f, 0.06f, 8, Nieve); // nieve acumulada al pie
            int ramas = 5 + rnd.Next(3);
            for (int i = 0; i < ramas; i++)
            {
                float a = (i / (float)ramas) * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.6f;
                float y = alto * (0.5f + 0.45f * i / ramas);
                float largo = 0.45f + (float)rnd.NextDouble() * 0.35f;
                var p0 = new Vector3(0.02f, y, 0.01f);
                var p1 = p0 + new Vector3(Mathf.Cos(a) * largo, 0.35f + (float)rnd.NextDouble() * 0.3f, Mathf.Sin(a) * largo);
                b.Tubo(p0, p1, 0.035f, 0.016f, 5, tronco);
                b.Caja(p1 + new Vector3(0, 0.02f, 0), new Vector3(0.06f, 0.03f, 0.06f), Nieve);
                // ramitas
                for (int k = 0; k < 2; k++)
                {
                    var q0 = Vector3.Lerp(p0, p1, 0.55f + k * 0.2f);
                    float a2 = a + (k == 0 ? 0.8f : -0.8f);
                    var q1 = q0 + new Vector3(Mathf.Cos(a2) * 0.22f, 0.18f, Mathf.Sin(a2) * 0.22f);
                    b.Tubo(q0, q1, 0.014f, 0.006f, 4, tronco);
                }
                b.Caja(Vector3.Lerp(p0, p1, 0.5f) + new Vector3(0, 0.03f, 0), new Vector3(0.07f, 0.025f, 0.07f), Nieve);
            }
            b.Tubo(new Vector3(0.03f, alto, 0.02f), new Vector3(0.0f, alto + 0.35f, 0.05f), 0.035f, 0.012f, 5, tronco);
        }

        static void Banco(ConstructorMalla b)
        {
            var hierro = C(40, 44, 42);
            foreach (var sx in new[] { -0.4f, 0.4f })
            {
                b.Caja(new Vector3(sx, 0.11f, 0), new Vector3(0.04f, 0.22f, 0.2f), hierro);
                b.Barra(new Vector3(sx, 0.22f, 0.08f), new Vector3(sx, 0.44f, 0.13f), 0.035f, hierro);
            }
            for (int i = 0; i < 3; i++) b.Caja(new Vector3(0, 0.235f, -0.07f + i * 0.07f), new Vector3(0.95f, 0.025f, 0.055f), Madera);
            for (int i = 0; i < 2; i++)
            {
                b.Push(); b.Mover(0, 0.31f + i * 0.09f, 0.1f + i * 0.02f); b.RotarX(-12f);
                b.Caja(Vector3.zero, new Vector3(0.95f, 0.055f, 0.025f), Madera);
                b.Pop();
            }
            b.Caja(new Vector3(0, 0.262f, 0), new Vector3(0.9f, 0.03f, 0.2f), Nieve);
            b.Caja(new Vector3(0, 0.44f, 0.135f), new Vector3(0.9f, 0.025f, 0.04f), Nieve);
        }

        static void Farola(ConstructorMalla b)
        {
            var metal = C(44, 50, 52);
            b.Cilindro(Vector3.zero, 0.07f, 0.05f, 0.14f, 8, metal);
            b.Cilindro(new Vector3(0, 0.14f, 0), 0.03f, 0.022f, 2.0f, 6, metal);
            b.Barra(new Vector3(0, 2.05f, 0), new Vector3(0, 2.12f, -0.36f), 0.03f, metal);
            b.Caja(new Vector3(0, 2.1f, -0.42f), new Vector3(0.12f, 0.05f, 0.2f), C(70, 76, 80));
            b.Caja(new Vector3(0, 2.065f, -0.42f), new Vector3(0.09f, 0.02f, 0.16f), C(120, 120, 104));
            b.Caja(new Vector3(0, 2.135f, -0.42f), new Vector3(0.13f, 0.02f, 0.21f), Nieve);
        }

        static void Semaforo(Modelo m)
        {
            var b = m.malla;
            var verde = C(46, 70, 52);
            b.Cilindro(Vector3.zero, 0.05f, 0.04f, 0.08f, 8, verde);
            b.Cilindro(new Vector3(0, 0.08f, 0), 0.025f, 0.025f, 1.5f, 6, verde);
            b.Caja(new Vector3(0, 1.58f, -0.04f), new Vector3(0.13f, 0.36f, 0.1f), C(36, 40, 36));
            b.Caja(new Vector3(0, 1.775f, -0.04f), new Vector3(0.14f, 0.03f, 0.11f), Nieve);
            float[] ys = { 1.69f, 1.58f, 1.47f };
            Color32[] apagados = { C(70, 20, 18), C(70, 56, 18), C(20, 56, 30) };
            for (int i = 0; i < 3; i++)
            {
                b.Caja(new Vector3(0, ys[i], -0.093f), new Vector3(0.07f, 0.07f, 0.01f), apagados[i]);
                b.Caja(new Vector3(0, ys[i] + 0.045f, -0.11f), new Vector3(0.09f, 0.012f, 0.05f), C(30, 32, 30));
            }
            // luz amarilla intermitente (sin energía casi en toda la ciudad)
            var p = NuevaParte(m, "ambar", new Vector3(0, ys[1], -0.1f));
            p.malla.brillo = 1f;
            p.malla.Caja(Vector3.zero, new Vector3(0.068f, 0.068f, 0.008f), C(255, 180, 50));
        }

        static void CartelDosPostes(ConstructorMalla b, string reg, float ancho, float alto)
        {
            var metal = C(60, 64, 68);
            b.Cilindro(new Vector3(-ancho * 0.4f, 0, 0), 0.02f, 0.02f, 0.62f + alto, 6, metal);
            b.Cilindro(new Vector3(ancho * 0.4f, 0, 0), 0.02f, 0.02f, 0.62f + alto, 6, metal);
            var r = atlas[reg];
            b.Caja(new Vector3(0, 0.62f + alto * 0.5f, -0.025f), new Vector3(ancho, alto, 0.025f), C(200, 200, 200), r, r);
            b.Caja(new Vector3(0, 0.62f + alto + 0.012f, -0.025f), new Vector3(ancho + 0.01f, 0.025f, 0.035f), Nieve);
            b.Cilindro(Vector3.zero, 0.36f, 0.2f, 0.05f, 8, Nieve);
        }

        static void CartelAvenida(ConstructorMalla b)
        {
            var metal = C(60, 64, 68);
            b.Cilindro(Vector3.zero, 0.025f, 0.025f, 1.5f, 6, metal);
            var r1 = atlas["cartel_mitre"];
            b.Caja(new Vector3(0.22f, 1.42f, 0), new Vector3(0.52f, 0.14f, 0.02f), C(200, 200, 200), r1, r1);
            b.Caja(new Vector3(0.22f, 1.5f, 0), new Vector3(0.53f, 0.02f, 0.03f), Nieve);
            b.Push(); b.Mover(0, 1.24f, 0.2f); b.RotarY(90f);
            var r2 = atlas["cartel_puente_chico"];
            b.Caja(Vector3.zero, new Vector3(0.44f, 0.12f, 0.02f), C(200, 200, 200), r2, r2);
            b.Pop();
        }

        static void CartelCaido(ConstructorMalla b)
        {
            var metal = C(60, 64, 68);
            b.Barra(new Vector3(-0.55f, 0.04f, 0.05f), new Vector3(0.45f, 0.06f, -0.05f), 0.05f, metal);
            b.Push(); b.Mover(0.5f, 0.07f, -0.1f); b.RotarY(15f); b.RotarX(-82f);
            var r = atlas["cartel_mitre"];
            b.Caja(Vector3.zero, new Vector3(0.52f, 0.14f, 0.02f), C(200, 200, 200), r, r);
            b.Pop();
            b.Esfera(new Vector3(0.62f, 0.0f, 0.02f), 0.2f, 8, 4, Nieve, 0.35f);
            b.Esfera(new Vector3(-0.3f, 0.0f, 0.1f), 0.18f, 8, 4, Nieve, 0.3f);
        }

        // Auto a lo largo de X. "volcado" = patas para arriba e inclinado.
        static void Auto(ConstructorMalla b, System.Random rnd, bool volcado)
        {
            Color32[] colores = { C(128, 50, 44), C(60, 74, 100), C(168, 158, 130), C(70, 92, 72), C(150, 150, 146) };
            var c = colores[rnd.Next(colores.Length)];
            var vidrio = C(34, 44, 54);
            b.Push();
            if (volcado) { b.Mover(0, 0.7f, 0); b.RotarX(168f); b.RotarZ(6f); }
            b.Caja(new Vector3(0, 0.26f, 0), new Vector3(2.0f, 0.3f, 0.86f), c);
            b.Caja(new Vector3(-0.08f, 0.5f, 0), new Vector3(1.04f, 0.18f, 0.8f), vidrio);
            b.Caja(new Vector3(-0.08f, 0.6f, 0), new Vector3(1.0f, 0.03f, 0.8f), c);
            foreach (var sx in new[] { -0.6f, 0.44f })
                foreach (var sz in new[] { -0.39f, 0.39f })
                    b.Caja(new Vector3(sx, 0.5f, sz), new Vector3(0.05f, 0.2f, 0.04f), c);
            b.Caja(new Vector3(-0.08f, 0.5f, -0.4f), new Vector3(0.05f, 0.2f, 0.02f), c);
            b.Caja(new Vector3(-0.08f, 0.5f, 0.4f), new Vector3(0.05f, 0.2f, 0.02f), c);
            foreach (var sx in new[] { -0.64f, 0.64f })
                foreach (var sz in new[] { -0.4f, 0.4f })
                    b.CilindroZ(new Vector3(sx, 0.14f, sz), 0.14f, 0.1f, 8, Negro);
            b.Caja(new Vector3(1.01f, 0.2f, 0), new Vector3(0.04f, 0.08f, 0.88f), Metal);
            b.Caja(new Vector3(-1.01f, 0.2f, 0), new Vector3(0.04f, 0.08f, 0.88f), Metal);
            b.Caja(new Vector3(1.005f, 0.32f, -0.3f), new Vector3(0.02f, 0.06f, 0.14f), C(220, 214, 180));
            b.Caja(new Vector3(1.005f, 0.32f, 0.3f), new Vector3(0.02f, 0.06f, 0.14f), C(220, 214, 180));
            b.Caja(new Vector3(-1.005f, 0.32f, -0.3f), new Vector3(0.02f, 0.06f, 0.14f), C(150, 30, 30));
            b.Caja(new Vector3(-1.005f, 0.32f, 0.3f), new Vector3(0.02f, 0.06f, 0.14f), C(150, 30, 30));
            if (!volcado)
            {
                // nieve acumulada sobre techo, capot y baúl
                b.Caja(new Vector3(-0.08f, 0.66f, 0), new Vector3(1.0f, 0.1f, 0.78f), Nieve);
                b.Caja(new Vector3(0.72f, 0.44f, 0), new Vector3(0.52f, 0.07f, 0.84f), Nieve);
                b.Caja(new Vector3(-0.85f, 0.44f, 0), new Vector3(0.28f, 0.06f, 0.84f), Nieve);
            }
            b.Pop();
            if (volcado)
            {
                b.Caja(new Vector3(0, 0.77f, 0), new Vector3(1.6f, 0.05f, 0.7f), Nieve);
                for (int i = 0; i < 6; i++)
                    b.Caja(new Vector3((float)rnd.NextDouble() * 1.6f - 0.8f, 0.01f, -0.55f - (float)rnd.NextDouble() * 0.2f), new Vector3(0.05f, 0.01f, 0.04f), C(120, 140, 150));
            }
            // nieve amontonada contra el costado
            b.Esfera(new Vector3(0.2f, 0, -0.46f), 0.4f, 8, 4, Nieve, 0.4f);
        }

        // ------------------------------------------------------------------
        // RADIO ANTIGUA
        // ------------------------------------------------------------------
        static void Radio(Modelo m)
        {
            var b = m.malla;
            // mueble
            b.Caja(new Vector3(0, 0.3f, 0), new Vector3(0.56f, 0.04f, 0.36f), Madera);
            Patas(b, 0.52f, 0.32f, 0.28f, 0.04f, MaderaOsc);
            b.Caja(new Vector3(0, 0.1f, 0), new Vector3(0.48f, 0.02f, 0.28f), MaderaOsc);
            // radio a válvulas
            b.Caja(new Vector3(0, 0.45f, 0.02f), new Vector3(0.44f, 0.26f, 0.2f), C(96, 60, 36));
            b.Push(); b.Mover(0, 0.58f, 0.02f); b.RotarX(90f);
            b.Cilindro(new Vector3(0, -0.1f, 0), 0.22f, 0.22f, 0.2f, 10, C(96, 60, 36));
            b.Pop();
            var rejilla = atlas["rejilla"];
            b.Caja(new Vector3(-0.09f, 0.47f, -0.081f), new Vector3(0.18f, 0.16f, 0.004f), ConstructorMalla.Blanco, rejilla);
            b.CilindroZ(new Vector3(0.09f, 0.4f, -0.085f), 0.025f, 0.02f, 8, C(200, 186, 150));
            b.CilindroZ(new Vector3(0.16f, 0.4f, -0.085f), 0.025f, 0.02f, 8, C(200, 186, 150));
            b.Barra(new Vector3(0.18f, 0.72f, 0.06f), new Vector3(0.3f, 1.0f, 0.12f), 0.01f, Metal); // antena
            var p = NuevaParte(m, "dial", new Vector3(0.12f, 0.5f, -0.083f));
            p.malla.brillo = 1f;
            p.malla.Caja(Vector3.zero, new Vector3(0.14f, 0.05f, 0.004f), ConstructorMalla.Blanco, atlas["dial"]);
        }

        // ------------------------------------------------------------------
        // EL INFORMANTE (sentado sobre un cajón, herido)
        // ------------------------------------------------------------------
        static void Informante(Modelo m)
        {
            var b = m.malla;
            Color32 abrigo = C(84, 76, 62), pantalon = C(56, 60, 70), bufanda = C(130, 40, 40), gorro = C(70, 80, 96);
            // cajón
            b.Caja(new Vector3(0, 0.13f, 0.06f), new Vector3(0.34f, 0.26f, 0.3f), MaderaClara);
            b.Caja(new Vector3(0, 0.13f, -0.091f), new Vector3(0.32f, 0.03f, 0.005f), MaderaOsc);
            // piernas: muslos hacia adelante y canillas hacia el piso
            foreach (var sx in new[] { -0.065f, 0.065f })
            {
                b.Tubo(new Vector3(sx, 0.3f, 0.06f), new Vector3(sx * 1.2f, 0.29f, -0.16f), 0.05f, 0.045f, 8, pantalon);
                b.Tubo(new Vector3(sx * 1.2f, 0.29f, -0.16f), new Vector3(sx * 1.25f, 0.05f, -0.18f), 0.045f, 0.038f, 8, pantalon);
                b.Caja(new Vector3(sx * 1.25f, 0.03f, -0.21f), new Vector3(0.075f, 0.06f, 0.14f), Negro);
                b.Esfera(new Vector3(sx * 1.2f, 0.29f, -0.16f), 0.048f, 8, 4, pantalon);
            }
            // frazada sobre las piernas
            b.Caja(new Vector3(0, 0.33f, -0.06f), new Vector3(0.26f, 0.03f, 0.22f), C(120, 96, 56));
            b.Caja(new Vector3(0, 0.25f, -0.18f), new Vector3(0.26f, 0.16f, 0.025f), C(112, 90, 52));

            // torso (respira)
            var torso = NuevaParte(m, "torso", new Vector3(0, 0.3f, 0.07f)).malla;
            torso.Tubo(new Vector3(0, 0, 0), new Vector3(0, 0.3f, -0.01f), 0.12f, 0.115f, 8, abrigo);
            torso.Tubo(new Vector3(0, 0.3f, -0.01f), new Vector3(0, 0.33f, -0.01f), 0.115f, 0.06f, 8, abrigo);
            torso.Caja(new Vector3(0, 0.15f, -0.115f), new Vector3(0.02f, 0.26f, 0.01f), Var(abrigo, -24)); // cierre
            torso.Tubo(new Vector3(0, 0.3f, -0.01f), new Vector3(0, 0.36f, -0.01f), 0.07f, 0.06f, 8, bufanda);
            torso.Caja(new Vector3(0.05f, 0.24f, -0.11f), new Vector3(0.05f, 0.12f, 0.02f), bufanda);
            // brazo izquierdo apoyado en la rodilla
            torso.Tubo(new Vector3(-0.13f, 0.28f, 0), new Vector3(-0.15f, 0.1f, -0.06f), 0.04f, 0.036f, 8, abrigo);
            torso.Tubo(new Vector3(-0.15f, 0.1f, -0.06f), new Vector3(-0.1f, 0.03f, -0.2f), 0.036f, 0.032f, 8, abrigo);
            torso.Caja(new Vector3(-0.095f, 0.025f, -0.235f), new Vector3(0.05f, 0.04f, 0.06f), Piel);
            // brazo derecho en cabestrillo (herido)
            torso.Tubo(new Vector3(0.13f, 0.28f, 0), new Vector3(0.14f, 0.13f, -0.04f), 0.04f, 0.036f, 8, abrigo);
            torso.Tubo(new Vector3(0.14f, 0.13f, -0.04f), new Vector3(0.02f, 0.15f, -0.13f), 0.036f, 0.032f, 8, abrigo);
            torso.Caja(new Vector3(0.0f, 0.155f, -0.14f), new Vector3(0.05f, 0.04f, 0.05f), Piel);
            torso.Barra(new Vector3(0.1f, 0.13f, -0.12f), new Vector3(-0.06f, 0.33f, -0.06f), 0.03f, Blanco);

            // cabeza (mira al jugador cuando se acerca)
            var cabeza = NuevaParte(m, "cabeza", new Vector3(0, 0.68f, 0.06f)).malla;
            cabeza.Caja(new Vector3(0, 0.06f, 0), new Vector3(0.12f, 0.13f, 0.12f), Piel, atlas["cara"]);
            cabeza.Caja(new Vector3(0, 0.055f, -0.066f), new Vector3(0.022f, 0.03f, 0.02f), Var(Piel, -14)); // nariz
            cabeza.Caja(new Vector3(-0.064f, 0.06f, 0), new Vector3(0.012f, 0.035f, 0.025f), Var(Piel, -10)); // orejas
            cabeza.Caja(new Vector3(0.064f, 0.06f, 0), new Vector3(0.012f, 0.035f, 0.025f), Var(Piel, -10));
            cabeza.Caja(new Vector3(0, 0.135f, 0.004f), new Vector3(0.13f, 0.05f, 0.13f), gorro);       // gorro de lana
            cabeza.Caja(new Vector3(0, 0.168f, 0.004f), new Vector3(0.1f, 0.025f, 0.1f), gorro);
            cabeza.Esfera(new Vector3(0, 0.19f, 0.004f), 0.025f, 6, 4, C(180, 60, 50));
            cabeza.Caja(new Vector3(0, 0.112f, 0.004f), new Vector3(0.136f, 0.016f, 0.136f), Var(gorro, 30));
        }

        // ------------------------------------------------------------------
        // PUERTA (la hoja gira sobre la bisagra; pivote en el borde izquierdo)
        // ------------------------------------------------------------------
        public static ConstructorMalla HojaPuerta(float ancho, float alto, float grosor)
        {
            Inicializar();
            var b = Nueva();
            var r = atlas["puerta"];
            var reg = new Region(r.u0 + (r.u1 - r.u0) * 0.125f, r.v0, r.u1 - (r.u1 - r.u0) * 0.125f, r.v1 - (r.v1 - r.v0) * 0.03f);
            b.Caja(new Vector3(ancho * 0.5f, alto * 0.5f, 0), new Vector3(ancho, alto, grosor), C(104, 72, 44), reg, reg);
            b.Esfera(new Vector3(ancho - 0.07f, alto * 0.47f, -grosor * 0.5f - 0.015f), 0.018f, 6, 4, C(200, 176, 90));
            b.Esfera(new Vector3(ancho - 0.07f, alto * 0.47f, grosor * 0.5f + 0.015f), 0.018f, 6, 4, C(200, 176, 90));
            return b;
        }

        // ------------------------------------------------------------------
        // MANO CON LINTERNA (primera persona). Origen = muñeca; la linterna apunta a +Z.
        // ------------------------------------------------------------------
        public static ConstructorMalla Mano(bool conTraje)
        {
            Inicializar();
            var b = Nueva();
            var guante = conTraje ? C(78, 90, 76) : Piel;
            var manga = conTraje ? C(92, 104, 88) : C(112, 58, 48);
            // linterna
            b.CilindroZ(new Vector3(0, 0.02f, 0.05f), 0.016f, 0.16f, 8, C(40, 42, 46));
            b.CilindroZ(new Vector3(0, 0.02f, 0.14f), 0.024f, 0.035f, 8, C(70, 72, 78));
            b.brillo = 1f;
            b.CilindroZ(new Vector3(0, 0.02f, 0.159f), 0.019f, 0.004f, 8, C(255, 250, 220));
            b.brillo = 0f;
            b.Caja(new Vector3(0, 0.038f, 0.06f), new Vector3(0.008f, 0.006f, 0.014f), C(150, 30, 30)); // botón
            // mano que la agarra
            b.Caja(new Vector3(0.0f, 0.02f, 0.02f), new Vector3(0.05f, 0.05f, 0.07f), guante);
            for (int i = 0; i < 4; i++)
                b.Caja(new Vector3(-0.026f, 0.0f + 0.0f, 0.0f + i * 0.019f), new Vector3(0.016f, 0.03f, 0.016f), Var(guante, -12));
            b.Caja(new Vector3(0.022f, 0.045f, 0.03f), new Vector3(0.016f, 0.016f, 0.04f), Var(guante, -6)); // pulgar
            // puño y manga
            b.Tubo(new Vector3(0.0f, 0.015f, -0.01f), new Vector3(0.02f, -0.02f, -0.2f), 0.032f, 0.045f, 8, manga);
            if (conTraje) b.Tubo(new Vector3(0.0f, 0.015f, -0.02f), new Vector3(0.002f, 0.012f, -0.04f), 0.036f, 0.036f, 8, C(60, 70, 58));
            return b;
        }
    }
}
