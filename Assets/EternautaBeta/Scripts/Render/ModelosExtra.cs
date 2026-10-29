using UnityEngine;

namespace Eternauta.Beta
{
    // Modelos nuevos de la muestra anual: contenedores, armas, comida, soldados y el cascarudo.
    // Mismo estilo que Modelos.cs (cajas y tubos de baja resolución, frente hacia -Z).
    public static partial class Modelos
    {
        static void CalcomaniasExtra(System.Random rnd)
        {
            // Cuadro de la familia: Pablo (con uniforme de policía), Celeste y sus hijos Mateo y Patricio
            var l = new Lienzo(48, 36);
            l.Rellenar(C(168, 186, 200));
            l.Rect(0, 24, 48, 12, C(96, 120, 84));
            Color32 piel = C(214, 170, 140);
            // Pablo
            l.Rect(7, 17, 9, 13, C(32, 48, 96)); l.Circulo(11.5f, 13, 3.5f, piel); l.Rect(8, 9, 7, 3, C(40, 30, 24));
            l.Rect(10, 19, 3, 2, C(220, 190, 70));
            // Celeste
            l.Rect(32, 17, 9, 13, C(168, 52, 70)); l.Circulo(36.5f, 13, 3.5f, piel); l.Rect(32, 8, 9, 4, C(96, 56, 30)); l.Rect(32, 12, 2, 9, C(96, 56, 30)); l.Rect(39, 12, 2, 9, C(96, 56, 30));
            // Mateo y Patricio
            l.Rect(19, 22, 5, 9, C(60, 130, 90)); l.Circulo(21.5f, 19, 2.8f, piel); l.Rect(19, 15, 5, 2, C(60, 40, 26));
            l.Rect(25, 24, 4, 7, C(210, 150, 50)); l.Circulo(27, 21.5f, 2.3f, piel); l.Rect(25, 18, 4, 2, C(30, 24, 20));
            l.Ruido(6, rnd);
            atlas.Agregar("familia", l);

            // Ascensor: puertas metálicas con visor
            l = new Lienzo(24, 32); l.Rellenar(C(150, 154, 158));
            l.Rect(11, 0, 2, 32, C(60, 64, 68)); l.Borde(0, 0, 24, 32, C(70, 74, 80));
            l.Rect(3, 4, 5, 8, C(30, 36, 44)); l.Rect(16, 4, 5, 8, C(30, 36, 44));
            l.Texto("1", 10, 17, 1, C(255, 200, 80)); l.Ruido(6, rnd);
            atlas.Agregar("ascensor", l);

            // Etiqueta de la munición
            l = new Lienzo(24, 12); l.Rellenar(C(196, 156, 50)); l.Texto("9MM", 7, 3, 1, C(40, 30, 10)); l.Rect(0, 10, 24, 2, C(30, 30, 30));
            atlas.Agregar("municion", l);
            // Cartel de comida
            l = new Lienzo(20, 12); l.Rellenar(C(200, 40, 30)); l.Rect(0, 0, 20, 3, C(240, 200, 40)); l.Texto("COLA", 2, 5, 1, C(240, 240, 230));
            atlas.Agregar("gaseosa", l);
        }

        static bool CrearExtra(string nombre, Modelo m, ConstructorMalla b, System.Random rnd)
        {
            switch (nombre)
            {
                case "cajon": Cajon(m); return true;
                case "heladera": Heladera(m); return true;
                case "ascensor": Ascensor(b); return true;
                case "cuadro_familiar": CuadroFamiliar(b); return true;
                case "silla": Silla(b, Vector3.zero, 0f, Madera); return true;
                case "sillon": Sillon(b); return true;
                case "mostrador": Mostrador(b); return true;
                case "pizarron": Pizarron(b); return true;
                case "planta": Planta(b); return true;
                case "camion": Camion(b); return true;
                case "bolsas_arena": BolsasArena(b); return true;
                case "monumento_base": MonumentoBase(b); return true;
                case "cartel_puente": CartelDosPostes(b, "cartel_puente", 1.3f, 0.43f); return true;
                case "cartel_puente_chico": CartelDosPostes(b, "cartel_puente_chico", 0.84f, 0.23f); return true;
                case "soldado": Soldado(b, false); return true;
                case "soldado_muerto": SoldadoMuerto(b); return true;
                case "glock": Glock(b); return true;
                case "cuchillo": Cuchillo(b); return true;
                case "palo": Palo(b); return true;
                case "bate": Bate(b); return true;
                case "municion": Municion(b); return true;
                case "carne": Carne(b); return true;
                case "hamburguesa": Hamburguesa(b); return true;
                case "gaseosa": Gaseosa(b); return true;
                case "cascarudo": Cascarudo(m); return true;
            }
            return false;
        }

        // ---------------- Contenedores ----------------
        // Cómoda de dos cajones; el de arriba sale hacia el frente (-Z) al abrirse.
        static void Cajon(Modelo m)
        {
            var b = m.malla;
            b.Caja(new Vector3(0, 0.23f, 0.02f), new Vector3(0.62f, 0.4f, 0.34f), Madera);
            b.Caja(new Vector3(0, 0.45f, 0.02f), new Vector3(0.66f, 0.04f, 0.38f), MaderaClara);
            b.Caja(new Vector3(0, 0.13f, -0.16f), new Vector3(0.56f, 0.15f, 0.02f), Var(Madera, 14));
            b.Caja(new Vector3(0, 0.13f, -0.175f), new Vector3(0.1f, 0.025f, 0.015f), C(190, 170, 90));
            b.Caja(new Vector3(0, 0.02f, 0.02f), new Vector3(0.6f, 0.04f, 0.32f), MaderaOsc);
            var p = NuevaParte(m, "cajon", new Vector3(0, 0.32f, -0.16f));
            p.malla.Caja(new Vector3(0, 0, 0), new Vector3(0.56f, 0.15f, 0.02f), Var(Madera, 14));
            p.malla.Caja(new Vector3(0, 0, -0.015f), new Vector3(0.1f, 0.025f, 0.015f), C(190, 170, 90));
            p.malla.Caja(new Vector3(0, -0.02f, 0.14f), new Vector3(0.5f, 0.1f, 0.28f), MaderaOsc);   // el cajón por dentro
            p.malla.Caja(new Vector3(0, 0.035f, 0.14f), new Vector3(0.46f, 0.02f, 0.24f), C(120, 100, 76));
        }

        // Heladera blanca; la puerta gira sobre la bisagra izquierda.
        static void Heladera(Modelo m)
        {
            var b = m.malla;
            Color32 blanca = C(214, 218, 216), gris = C(150, 156, 158);
            b.Caja(new Vector3(0, 0.46f, 0.2f), new Vector3(0.54f, 0.92f, 0.03f), blanca);          // fondo
            b.Caja(new Vector3(-0.255f, 0.46f, 0.0f), new Vector3(0.03f, 0.92f, 0.42f), blanca);     // lados
            b.Caja(new Vector3(0.255f, 0.46f, 0.0f), new Vector3(0.03f, 0.92f, 0.42f), blanca);
            b.Caja(new Vector3(0, 0.915f, 0.0f), new Vector3(0.54f, 0.03f, 0.42f), blanca);          // techo
            b.Caja(new Vector3(0, 0.02f, 0.0f), new Vector3(0.54f, 0.04f, 0.42f), gris);              // base
            b.Caja(new Vector3(0, 0.93f, 0.0f), new Vector3(0.55f, 0.02f, 0.43f), Nieve);
            for (int k = 0; k < 4; k++) b.Caja(new Vector3(0, 0.2f + k * 0.2f, 0.0f), new Vector3(0.48f, 0.015f, 0.38f), C(170, 190, 200)); // estantes
            Color32[] cols = { C(170, 40, 36), C(46, 104, 60), C(200, 170, 60), C(220, 220, 214) };
            for (int k = 0; k < 4; k++)
                for (int i = 0; i < 2; i++)
                    b.Caja(new Vector3(-0.12f + i * 0.24f, 0.26f + k * 0.2f, 0.02f), new Vector3(0.12f, 0.08f, 0.16f), cols[(k + i) % 4]);
            var puerta = NuevaParte(m, "puerta", new Vector3(-0.27f, 0.46f, -0.215f));
            puerta.malla.Caja(new Vector3(0.27f, 0, 0), new Vector3(0.54f, 0.92f, 0.04f), blanca);
            puerta.malla.Caja(new Vector3(0.46f, 0.12f, -0.03f), new Vector3(0.025f, 0.34f, 0.025f), gris);
            puerta.malla.Caja(new Vector3(0.27f, 0.2f, -0.022f), new Vector3(0.5f, 0.014f, 0.006f), gris);
            puerta.malla.Caja(new Vector3(0.27f, 0.2f, -0.022f), new Vector3(0.5f, 0.004f, 0.006f), C(40, 44, 48));
        }

        static void Ascensor(ConstructorMalla b)
        {
            var r = atlas["ascensor"];
            b.Caja(new Vector3(0, 0.55f, 0.04f), new Vector3(0.62f, 1.1f, 0.08f), C(88, 92, 98));
            b.Caja(new Vector3(0, 0.52f, -0.005f), new Vector3(0.5f, 1.04f, 0.03f), C(210, 210, 210), r, r);
            b.brillo = 1f;
            b.Caja(new Vector3(0, 1.12f, 0.0f), new Vector3(0.14f, 0.05f, 0.03f), C(255, 190, 60));
            b.brillo = 0f;
            b.Caja(new Vector3(0.38f, 0.5f, 0.0f), new Vector3(0.06f, 0.1f, 0.03f), C(40, 44, 48));
        }

        static void CuadroFamiliar(ConstructorMalla b)
        {
            var r = atlas["familia"];
            b.Caja(new Vector3(0, 0.65f, 0), new Vector3(0.58f, 0.44f, 0.04f), MaderaOsc);
            b.Caja(new Vector3(0, 0.65f, -0.022f), new Vector3(0.5f, 0.36f, 0.004f), ConstructorMalla.Blanco, r);
        }

        // ---------------- Muebles nuevos ----------------
        static void Sillon(ConstructorMalla b)
        {
            Color32 tela = C(96, 74, 62);
            b.Caja(new Vector3(0, 0.12f, 0), new Vector3(0.86f, 0.2f, 0.4f), tela);
            b.Caja(new Vector3(0, 0.34f, 0.17f), new Vector3(0.86f, 0.3f, 0.08f), Var(tela, -8));
            b.Caja(new Vector3(-0.44f, 0.24f, 0), new Vector3(0.08f, 0.26f, 0.4f), Var(tela, -14));
            b.Caja(new Vector3(0.44f, 0.24f, 0), new Vector3(0.08f, 0.26f, 0.4f), Var(tela, -14));
            b.Caja(new Vector3(0, 0.235f, -0.02f), new Vector3(0.7f, 0.06f, 0.3f), Var(tela, 12));
            b.Caja(new Vector3(-0.2f, 0.31f, 0.1f), new Vector3(0.2f, 0.16f, 0.06f), C(150, 60, 56));
        }

        static void Mostrador(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.22f, 0), new Vector3(1.0f, 0.44f, 0.34f), Madera);
            b.Caja(new Vector3(0, 0.455f, -0.01f), new Vector3(1.06f, 0.04f, 0.4f), MaderaClara);
            b.Caja(new Vector3(0, 0.22f, -0.176f), new Vector3(0.9f, 0.34f, 0.01f), Var(Madera, -16));
            b.Caja(new Vector3(-0.28f, 0.52f, 0.0f), new Vector3(0.16f, 0.1f, 0.14f), C(80, 84, 88));   // caja registradora
            b.Caja(new Vector3(0.22f, 0.49f, -0.06f), new Vector3(0.2f, 0.03f, 0.12f), C(220, 214, 196));
        }

        static void Pizarron(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.62f, 0), new Vector3(1.1f, 0.58f, 0.04f), MaderaOsc);
            b.Caja(new Vector3(0, 0.62f, -0.022f), new Vector3(1.04f, 0.52f, 0.004f), C(36, 62, 52));
            b.Caja(new Vector3(-0.2f, 0.66f, -0.026f), new Vector3(0.4f, 0.01f, 0.004f), C(210, 210, 200));
            b.Caja(new Vector3(0.1f, 0.58f, -0.026f), new Vector3(0.5f, 0.01f, 0.004f), C(210, 210, 200));
            b.Caja(new Vector3(0, 0.34f, -0.03f), new Vector3(1.0f, 0.02f, 0.05f), MaderaClara);
            b.Caja(new Vector3(0.3f, 0.355f, -0.035f), new Vector3(0.08f, 0.012f, 0.025f), Blanco);
        }

        static void Planta(ConstructorMalla b)
        {
            b.Cilindro(Vector3.zero, 0.1f, 0.08f, 0.16f, 8, C(150, 86, 60));
            b.Cilindro(new Vector3(0, 0.15f, 0), 0.09f, 0.09f, 0.01f, 8, C(70, 50, 36));
            Color32 hoja = C(96, 112, 78);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f;
                b.Barra(new Vector3(0, 0.16f, 0), new Vector3(Mathf.Cos(a) * 0.14f, 0.34f + (i % 3) * 0.05f, Mathf.Sin(a) * 0.14f), 0.025f, Var(hoja, (i % 3) * 10 - 10));
            }
        }

        static void Camion(ConstructorMalla b)
        {
            Color32 oliva = C(72, 82, 56);
            b.Caja(new Vector3(0, 0.55f, 0), new Vector3(1.6f, 0.7f, 0.6f), oliva);
            b.Caja(new Vector3(0.95f, 0.5f, 0), new Vector3(0.45f, 0.8f, 0.58f), Var(oliva, -8));
            b.Caja(new Vector3(1.0f, 0.65f, 0), new Vector3(0.36f, 0.3f, 0.6f), C(40, 54, 66));
            b.Caja(new Vector3(0, 1.05f, 0), new Vector3(1.5f, 0.3f, 0.64f), C(90, 100, 70));
            b.Caja(new Vector3(0, 1.22f, 0), new Vector3(1.5f, 0.04f, 0.6f), Nieve);
            foreach (var wx in new[] { -0.5f, 0.35f, 1.0f })
                foreach (var wz in new[] { -0.32f, 0.32f })
                    b.CilindroZ(new Vector3(wx, 0.2f, wz), 0.2f, 0.12f, 8, Negro);
        }

        static void BolsasArena(ConstructorMalla b)
        {
            Color32 arena = C(150, 136, 100);
            for (int i = 0; i < 4; i++) b.Caja(new Vector3(-0.54f + i * 0.36f, 0.09f, 0), new Vector3(0.36f, 0.18f, 0.22f), Var(arena, (i % 2) * 8));
            for (int i = 0; i < 3; i++) b.Caja(new Vector3(-0.36f + i * 0.36f, 0.26f, 0), new Vector3(0.36f, 0.16f, 0.22f), Var(arena, -8 + (i % 2) * 8));
            b.Caja(new Vector3(0, 0.355f, 0), new Vector3(1.1f, 0.025f, 0.2f), Nieve);
        }

        static void MonumentoBase(ConstructorMalla b)
        {
            b.CajaNevada(new Vector3(0, 0.07f, 0), new Vector3(2.0f, 0.14f, 2.0f), Var(Piedra, -14));
            b.CajaNevada(new Vector3(0, 0.2f, 0), new Vector3(1.6f, 0.12f, 1.6f), Var(Piedra, -8));
        }

        // ---------------- Soldados ----------------
        static void Soldado(ConstructorMalla b, bool _)
        {
            Color32 uniforme = C(76, 86, 58), casco = C(62, 72, 48);
            Figura(b, uniforme, uniforme, Piel, casco, false, 1f);
            b.Esfera(new Vector3(0, 0.9f, 0), 0.075f, 8, 5, casco, 0.8f);
            b.Caja(new Vector3(0, 0.88f, 0), new Vector3(0.15f, 0.015f, 0.15f), Var(casco, -10));
            // fusil cruzado sobre el pecho
            b.Barra(new Vector3(-0.1f, 0.5f, -0.08f), new Vector3(0.14f, 0.66f, -0.1f), 0.03f, C(36, 38, 40));
            b.Barra(new Vector3(-0.18f, 0.44f, -0.08f), new Vector3(-0.1f, 0.5f, -0.08f), 0.04f, MaderaOsc);
            b.Caja(new Vector3(0, 0.62f, -0.12f), new Vector3(0.2f, 0.12f, 0.03f), Var(uniforme, -14));
            // nieve en los hombros
            b.Caja(new Vector3(-0.1f, 0.745f, 0), new Vector3(0.1f, 0.02f, 0.12f), Nieve);
            b.Caja(new Vector3(0.1f, 0.745f, 0), new Vector3(0.1f, 0.02f, 0.12f), Nieve);
        }

        static void SoldadoMuerto(ConstructorMalla b)
        {
            Color32 uniforme = C(76, 86, 58);
            b.Caja(new Vector3(0.05f, 0.002f, 0), new Vector3(0.9f, 0.004f, 0.5f), C(96, 34, 30));   // mancha
            b.Push(); b.Mover(0, 0.07f, 0); b.RotarY(12f); b.RotarZ(90f);
            Figura(b, uniforme, uniforme, Piel, uniforme, false, 1f);
            b.Pop();
            b.Esfera(new Vector3(-0.42f, 0.09f, 0.04f), 0.075f, 8, 5, C(62, 72, 48), 0.8f);
            b.Barra(new Vector3(0.1f, 0.03f, -0.22f), new Vector3(0.62f, 0.04f, -0.2f), 0.035f, C(36, 38, 40));
            b.Caja(new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.02f, 0.18f), Nieve);
        }

        // ---------------- Armas (apuntan a +Z; origen cerca de la empuñadura) ----------------
        static void Glock(ConstructorMalla b)
        {
            Color32 corredera = C(30, 32, 34), cuerpo = C(46, 48, 52);
            b.Caja(new Vector3(0, 0.065f, 0.1f), new Vector3(0.036f, 0.04f, 0.22f), corredera);                // corredera
            b.Caja(new Vector3(0, 0.038f, 0.09f), new Vector3(0.034f, 0.026f, 0.2f), cuerpo);                  // armazón
            b.Push(); b.Mover(0, 0.03f, 0.0f); b.RotarX(-14f);
            b.Caja(new Vector3(0, -0.04f, 0), new Vector3(0.034f, 0.095f, 0.05f), cuerpo);                      // empuñadura
            b.Pop();
            b.Caja(new Vector3(0, 0.0f, 0.075f), new Vector3(0.02f, 0.014f, 0.045f), cuerpo);                  // guardamonte
            b.Caja(new Vector3(0, 0.082f, 0.205f), new Vector3(0.012f, 0.012f, 0.012f), C(200, 200, 200));     // mira
            b.Caja(new Vector3(0, 0.082f, 0.0f), new Vector3(0.016f, 0.012f, 0.012f), C(200, 200, 200));
            b.Caja(new Vector3(0, 0.065f, 0.215f), new Vector3(0.012f, 0.012f, 0.01f), Negro);                 // boca del cañón
        }

        static void Cuchillo(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0, -0.04f), new Vector3(0.026f, 0.034f, 0.11f), C(40, 34, 30));              // mango
            b.Caja(new Vector3(0, 0, 0.018f), new Vector3(0.044f, 0.05f, 0.012f), Metal);                      // guarda
            b.Caja(new Vector3(0, 0.004f, 0.1f), new Vector3(0.008f, 0.04f, 0.16f), C(206, 210, 214));         // hoja
            b.Caja(new Vector3(0, 0.026f, 0.1f), new Vector3(0.01f, 0.006f, 0.16f), C(236, 240, 244));
        }

        static void Palo(ConstructorMalla b)
        {
            b.CilindroZ(new Vector3(0, 0, 0.3f), 0.013f, 0.78f, 6, C(176, 134, 86));
            b.CilindroZ(new Vector3(0, 0, 0.66f), 0.015f, 0.06f, 6, C(110, 84, 54));
        }

        static void Bate(ConstructorMalla b)
        {
            Color32 madera = C(184, 150, 100);
            b.CilindroZ(new Vector3(0, 0, -0.02f), 0.015f, 0.18f, 8, C(30, 30, 34));                          // mango con cinta
            b.Tubo(new Vector3(0, 0, 0.06f), new Vector3(0, 0, 0.3f), 0.014f, 0.03f, 8, madera);
            b.Tubo(new Vector3(0, 0, 0.3f), new Vector3(0, 0, 0.62f), 0.03f, 0.036f, 8, madera);
            b.Tubo(new Vector3(0, 0, 0.62f), new Vector3(0, 0, 0.66f), 0.036f, 0.02f, 8, madera);
            b.Caja(new Vector3(0, 0, 0.2f), new Vector3(0.05f, 0.012f, 0.012f), C(150, 40, 30));
        }

        // ---------------- Objetos del botín ----------------
        static void Municion(ConstructorMalla b)
        {
            var r = atlas["municion"];
            b.Caja(new Vector3(0, 0.03f, 0), new Vector3(0.14f, 0.06f, 0.09f), C(206, 190, 150), r, r);
            for (int i = 0; i < 4; i++)
            {
                b.Cilindro(new Vector3(-0.05f + i * 0.033f, 0.06f, -0.02f), 0.011f, 0.011f, 0.035f, 6, C(196, 150, 60));
                b.Cilindro(new Vector3(-0.05f + i * 0.033f, 0.095f, -0.02f), 0.011f, 0.005f, 0.015f, 6, C(150, 90, 40));
            }
        }

        static void Carne(ConstructorMalla b)
        {
            b.Caja(new Vector3(0, 0.04f, 0), new Vector3(0.2f, 0.08f, 0.14f), C(120, 58, 56));
            b.Caja(new Vector3(0.02f, 0.085f, 0.01f), new Vector3(0.14f, 0.02f, 0.1f), C(200, 214, 224));
            b.Caja(new Vector3(-0.06f, 0.05f, -0.07f), new Vector3(0.06f, 0.05f, 0.015f), C(226, 226, 230));
            b.Caja(new Vector3(0.08f, 0.03f, 0.07f), new Vector3(0.05f, 0.04f, 0.015f), C(226, 226, 230));
        }

        static void Hamburguesa(ConstructorMalla b)
        {
            b.Cilindro(new Vector3(0, 0.0f, 0), 0.1f, 0.1f, 0.035f, 10, C(200, 150, 84));
            b.Cilindro(new Vector3(0, 0.035f, 0), 0.105f, 0.105f, 0.025f, 10, C(96, 52, 38));
            b.Cilindro(new Vector3(0, 0.06f, 0), 0.1f, 0.1f, 0.008f, 10, C(240, 190, 60));
            b.Cilindro(new Vector3(0, 0.068f, 0), 0.1f, 0.075f, 0.05f, 10, C(206, 154, 86));
            b.Caja(new Vector3(0, 0.118f, 0), new Vector3(0.12f, 0.012f, 0.12f), Nieve);
        }

        static void Gaseosa(ConstructorMalla b)
        {
            var r = atlas["gaseosa"];
            b.Cilindro(Vector3.zero, 0.04f, 0.04f, 0.17f, 8, C(200, 40, 30));
            b.Caja(new Vector3(0, 0.09f, -0.041f), new Vector3(0.07f, 0.06f, 0.004f), ConstructorMalla.Blanco, r);
            b.Cilindro(new Vector3(0, 0.17f, 0), 0.025f, 0.025f, 0.04f, 8, C(220, 220, 220));
            b.Cilindro(new Vector3(0, 0.21f, 0), 0.028f, 0.028f, 0.014f, 8, C(30, 30, 30));
            b.Caja(new Vector3(0, 0.03f, 0), new Vector3(0.09f, 0.01f, 0.09f), Nieve);
        }

        // ---------------- Bicho ----------------
        // Cascarudo gigante: cuerpo oval marrón, antenas y seis patas. El frente mira a -Z.
        // Partes móviles: patasA / patasB (patas alternadas), antenas.
        static void Cascarudo(Modelo m)
        {
            var b = m.malla;
            Color32 caparazon = C(78, 38, 22), claro = C(132, 74, 36), panza = C(46, 30, 24), pata = C(52, 30, 20);
            // abdomen y caparazón (dos élitros con una línea al medio)
            b.Esfera(new Vector3(0, 0.15f, 0.12f), 0.3f, 10, 6, caparazon, 0.5f);
            b.Caja(new Vector3(0, 0.285f, 0.1f), new Vector3(0.012f, 0.01f, 0.5f), Negro);
            b.Esfera(new Vector3(-0.08f, 0.2f, 0.13f), 0.14f, 8, 4, claro, 0.45f);
            b.Esfera(new Vector3(0.08f, 0.2f, 0.13f), 0.14f, 8, 4, claro, 0.45f);
            b.Caja(new Vector3(0, 0.07f, 0.12f), new Vector3(0.4f, 0.08f, 0.5f), panza);
            // tórax y cabeza
            b.Esfera(new Vector3(0, 0.15f, -0.2f), 0.15f, 8, 5, Var(caparazon, 10), 0.6f);
            b.Esfera(new Vector3(0, 0.12f, -0.34f), 0.1f, 8, 5, Var(caparazon, 22), 0.75f);
            // ojos rojos que brillan en la niebla
            b.brillo = 1f;
            b.Caja(new Vector3(-0.055f, 0.17f, -0.4f), new Vector3(0.03f, 0.03f, 0.02f), C(255, 40, 30));
            b.Caja(new Vector3(0.055f, 0.17f, -0.4f), new Vector3(0.03f, 0.03f, 0.02f), C(255, 40, 30));
            b.brillo = 0f;
            // mandíbulas
            b.Barra(new Vector3(-0.03f, 0.08f, -0.42f), new Vector3(-0.07f, 0.07f, -0.5f), 0.02f, Negro);
            b.Barra(new Vector3(0.03f, 0.08f, -0.42f), new Vector3(0.07f, 0.07f, -0.5f), 0.02f, Negro);
            // escarcha sobre el lomo
            b.Caja(new Vector3(0.1f, 0.3f, 0.2f), new Vector3(0.08f, 0.012f, 0.06f), Nieve);
            b.Caja(new Vector3(-0.09f, 0.28f, 0.0f), new Vector3(0.06f, 0.012f, 0.05f), Nieve);

            // patas: tres de cada lado; A = delantera izq, media der, trasera izq (andar en trípode)
            var a = NuevaParte(m, "patasA", new Vector3(0, 0.1f, 0));
            var c = NuevaParte(m, "patasB", new Vector3(0, 0.1f, 0));
            float[] zs = { -0.2f, 0.0f, 0.22f };
            for (int i = 0; i < 3; i++)
                foreach (var lado in new[] { -1f, 1f })
                {
                    var parte = ((i % 2 == 0) == (lado < 0f)) ? a : c;
                    float x0 = lado * 0.14f, z0 = zs[i] - 0.05f;
                    parte.malla.Tubo(new Vector3(x0, 0.02f, z0), new Vector3(lado * 0.3f, 0.12f, z0 - 0.06f + i * 0.05f), 0.022f, 0.018f, 5, pata);
                    parte.malla.Tubo(new Vector3(lado * 0.3f, 0.12f, z0 - 0.06f + i * 0.05f), new Vector3(lado * 0.38f, -0.1f, z0 - 0.02f + i * 0.08f), 0.018f, 0.01f, 5, pata);
                }
            var ant = NuevaParte(m, "antenas", new Vector3(0, 0.2f, -0.38f));
            ant.malla.Barra(new Vector3(-0.03f, 0, 0), new Vector3(-0.2f, 0.12f, -0.26f), 0.012f, pata);
            ant.malla.Barra(new Vector3(0.03f, 0, 0), new Vector3(0.2f, 0.12f, -0.26f), 0.012f, pata);
        }

        // Brazo en primera persona (sin linterna): la mano agarra lo que lleve el protagonista. Origen = muñeca; apunta a +Z.
        public static ConstructorMalla Brazo(bool conTraje)
        {
            Inicializar();
            var b = Nueva();
            var guante = conTraje ? C(78, 90, 76) : Piel;
            var manga = conTraje ? C(92, 104, 88) : C(112, 58, 48);
            b.Caja(new Vector3(0.0f, 0.0f, 0.02f), new Vector3(0.05f, 0.05f, 0.07f), guante);
            for (int i = 0; i < 4; i++)
                b.Caja(new Vector3(-0.026f, 0.0f, -0.006f + i * 0.019f), new Vector3(0.016f, 0.03f, 0.016f), Var(guante, -12));
            b.Caja(new Vector3(0.022f, 0.03f, 0.03f), new Vector3(0.016f, 0.016f, 0.04f), Var(guante, -6));
            b.Tubo(new Vector3(0.0f, 0.0f, -0.01f), new Vector3(0.03f, -0.05f, -0.24f), 0.032f, 0.05f, 8, manga);
            return b;
        }
    }
}
