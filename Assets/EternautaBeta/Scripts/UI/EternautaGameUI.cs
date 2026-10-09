using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Interfaz de la beta según la Etapa 12 (identidad visual, HUD, inventario,
    // menús, opciones, créditos y secuencia final) y los wireframes de la Etapa 11.
    // Se dibuja con IMGUI sobre una resolución de referencia de 1920×1080
    // (Etapa 12, lámina 9B) y escala con la pantalla.
    public partial class EternautaGame
    {
        // Paleta (Etapa 12, sección 3)
        static readonly Color Negro = Hex(0x050709), Panel = Hex(0x1B2025), Blanco = Hex(0xE7ECEF);
        static readonly Color Azul = Hex(0x5B7E99), Rojo = Hex(0x7E2A2B), Gris = Hex(0x9AA5AD);
        static readonly Color BotonNormal = Hex(0x171B1F), BotonSel = Hex(0x2A323A), BotonPresionado = Hex(0x0C0E10);

        static Color Hex(int rgb, float a = 1f)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        }

        static Color ConAlfa(Color c, float a) { c.a = a; return c; }

        // Escala de la interfaz
        float escalaUI = 1f;
        float AH = 1080f; // alto del área de referencia (puede ser mayor en pantallas 4:3)
        const float AW = 1920f;
        bool pantallaChica;

        Font fuenteAngosta, fuenteNormal;
        GUIStyle estilo;
        Texture2D iconoCorazon, iconoMochila, iconoAlerta;
        readonly Dictionary<string, Texture2D> iconos = new Dictionary<string, Texture2D>();

        // Selecciones de menú
        int selMenu, selConfirmar, selPausa, selSalir = 1, selOpciones, selCreditos;
        int presionado = -1;
        bool mouseMovido; // se calcula en Update
        bool fondo; // true mientras se dibuja un menú tapado por una confirmación (no recibe clics)

        // ------------------------------------------------------------------
        // NAVEGACIÓN CON TECLADO
        // ------------------------------------------------------------------
        string[] OpcionesMenu() { return new[] { "NUEVA PARTIDA", "CONTINUAR", "OPCIONES", "CRÉDITOS", "SALIR" }; }
        string[] OpcionesPausa() { return new[] { "CONTINUAR", "INVENTARIO", "OPCIONES", "VOLVER AL MENÚ" }; }

        bool Habilitada(Estado e, int i)
        {
            if (e == Estado.MenuPrincipal && i == 1) return hayPartidaGuardada;
            return true;
        }

        void Mover(ref int sel, int n, int dir, Estado e)
        {
            for (int k = 0; k < n; k++)
            {
                sel = (sel + dir + n) % n;
                if (Habilitada(e, sel)) break;
            }
            audioJuego.Menu();
        }

        void NavegarMenu()
        {
            bool arriba = Entrada.Pulsada(Tecla.W) || Entrada.Pulsada(Tecla.Arriba);
            bool abajo = Entrada.Pulsada(Tecla.S) || Entrada.Pulsada(Tecla.Abajo);
            bool izq = Entrada.Pulsada(Tecla.A) || Entrada.Pulsada(Tecla.Izquierda);
            bool der = Entrada.Pulsada(Tecla.D) || Entrada.Pulsada(Tecla.Derecha);
            bool ok = Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio);
            bool atras = Entrada.Pulsada(Tecla.Escape);

            switch (estado)
            {
                case Estado.MenuPrincipal:
                    if (!Habilitada(estado, selMenu)) selMenu = 0;
                    if (arriba) Mover(ref selMenu, 5, -1, estado);
                    if (abajo) Mover(ref selMenu, 5, 1, estado);
                    if (ok) Activar(selMenu);
                    break;
                case Estado.ConfirmarNueva:
                    if (izq || der || arriba || abajo) { selConfirmar = 1 - selConfirmar; audioJuego.Menu(); }
                    if (ok) Activar(selConfirmar);
                    if (atras) estado = Estado.MenuPrincipal;
                    break;
                case Estado.Pausa:
                    if (arriba) Mover(ref selPausa, 4, -1, estado);
                    if (abajo) Mover(ref selPausa, 4, 1, estado);
                    if (ok) Activar(selPausa);
                    if (atras) estado = Estado.Jugando;
                    break;
                case Estado.ConfirmarSalir:
                    if (izq || der || arriba || abajo) { selSalir = 1 - selSalir; audioJuego.Menu(); }
                    if (ok) Activar(selSalir);
                    if (atras) estado = Estado.Pausa;
                    break;
                case Estado.Opciones:
                    if (arriba) Mover(ref selOpciones, 4, -1, estado);
                    if (abajo) Mover(ref selOpciones, 4, 1, estado);
                    if (izq) CambiarOpcion(selOpciones, -1);
                    if (der) CambiarOpcion(selOpciones, 1);
                    if (ok) { if (selOpciones == 3) Activar(3); else CambiarOpcion(selOpciones, 1); }
                    if (atras) Activar(3);
                    break;
                case Estado.Creditos:
                    if (ok || atras) Activar(0);
                    break;
            }
        }

        void Activar(int i)
        {
            audioJuego.Menu();
            switch (estado)
            {
                case Estado.MenuPrincipal:
                    if (!Habilitada(estado, i)) { audioJuego.Error(); return; }
                    if (i == 0) { if (hayPartidaGuardada) { estado = Estado.ConfirmarNueva; selConfirmar = 0; } else NuevaPartida(); }
                    else if (i == 1) Continuar();
                    else if (i == 2) { origenOpciones = Estado.MenuPrincipal; estado = Estado.Opciones; selOpciones = 0; }
                    else if (i == 3) { origenCreditos = Estado.MenuPrincipal; estado = Estado.Creditos; selCreditos = 0; }
                    else Salir();
                    break;
                case Estado.ConfirmarNueva:
                    if (i == 0) estado = Estado.MenuPrincipal; else NuevaPartida();
                    break;
                case Estado.Pausa:
                    if (i == 0) estado = Estado.Jugando;
                    else if (i == 1) AbrirInventario(Estado.Pausa);
                    else if (i == 2) { origenOpciones = Estado.Pausa; estado = Estado.Opciones; selOpciones = 0; }
                    else { estado = Estado.ConfirmarSalir; selSalir = 1; }
                    break;
                case Estado.ConfirmarSalir:
                    if (i == 0) VolverAlMenu(); else estado = Estado.Pausa;
                    break;
                case Estado.Opciones:
                    if (i == 3) estado = origenOpciones;
                    else CambiarOpcion(i, 1);
                    break;
                case Estado.Creditos:
                    estado = Estado.MenuPrincipal;
                    hayPartidaGuardada = SistemaGuardado.ExistePartida();
                    break;
            }
        }

        void CambiarOpcion(int fila, int dir)
        {
            if (fila == 0) volumen = Mathf.Clamp(volumen + dir * 10, 0, 100);
            else if (fila == 1) pantallaCompleta = !pantallaCompleta;
            else if (fila == 2) resolucionIdx = (resolucionIdx + dir + Resoluciones.Length) % Resoluciones.Length;
            else return;
            audioJuego.Menu();
            AplicarOpciones();
        }

        void NuevaPartida()
        {
            SistemaGuardado.Borrar();
            ReiniciarMundo();
            partidaEnCurso = true;
            estado = Estado.Intro;
            introIdx = 0;
            introTiempo = 0f;
            Guardar(); // estado inicial: CONTINUAR queda habilitado
        }

        void Continuar()
        {
            if (!CargarPartida()) { hayPartidaGuardada = false; audioJuego.Error(); return; }
            partidaEnCurso = true;
            estado = Estado.Jugando;
            avisoObjetivo = progresion.Actual != null ? 6f : 0f;
            Mostrar("Partida cargada", 2f);
        }

        void VolverAlMenu()
        {
            partidaEnCurso = false;
            estado = Estado.MenuPrincipal;
            selMenu = 0;
            mensajes.Clear();
            hayPartidaGuardada = SistemaGuardado.ExistePartida();
        }

        void AbrirPausa()
        {
            estado = Estado.Pausa;
            selPausa = 0;
            audioJuego.Menu();
        }

        void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        // DIBUJO
        // ------------------------------------------------------------------
        void PrepararEstilos()
        {
            if (estilo != null) return;
            fuenteAngosta = Font.CreateDynamicFontFromOSFont(new[] { "Arial Narrow", "Liberation Sans Narrow", "Arial", "Liberation Sans" }, 36);
            fuenteNormal = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Helvetica" }, 32);
            estilo = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false, clipping = TextClipping.Overflow };
            PrecargarFuentes();

            iconoCorazon = Icono(new[]
            {
                ".XX...XX.",
                "XXXX.XXXX",
                "XXXXXXXXX",
                "XXXXXXXXX",
                ".XXXXXXX.",
                "..XXXXX..",
                "...XXX...",
                "....X....",
            });
            iconoMochila = Icono(new[]
            {
                "...XXX...",
                "..X...X..",
                ".XXXXXXX.",
                "XXXXXXXXX",
                "XX.....XX",
                "XXXXXXXXX",
                "XXXXXXXXX",
                "XXXXXXXXX",
                ".XXXXXXX.",
            });
            iconoAlerta = Icono(new[]
            {
                "....X....",
                "...XXX...",
                "...X.X...",
                "..XX.XX..",
                "..XX.XX..",
                ".XXXXXXX.",
                ".XXX.XXX.",
                "XXXXXXXXX",
            });
        }

        // Las fuentes dinámicas guardan cada letra, en cada tamaño y estilo, en una sola
        // textura. Si se piden demasiados tamaños la textura se llena y se reconstruye
        // a cada cuadro, y los textos salen chiquitos, deformados o encimados.
        // Por eso la interfaz usa pocos tamaños fijos y se cargan al empezar.
        static readonly int[] TamanosAngosta = { 36, 44, 56, 96 };
        static readonly int[] TamanosNormal = { 26, 32, 40 };

        static int Ajustar(int tam, int[] tamanos)
        {
            foreach (int t in tamanos) if (tam <= t + (t >= 56 ? 14 : 3)) return t;
            return tamanos[tamanos.Length - 1];
        }

        void PrecargarFuentes()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÑáéíóúñü0123456789 .,:;!¡?¿\"'()[]<>+-·/%×—…“”";
            Precargar(fuenteAngosta, TamanosAngosta, caracteres);
            Precargar(fuenteNormal, TamanosNormal, caracteres);
        }

        static void Precargar(Font f, int[] tamanos, string caracteres)
        {
            if (f == null) return;
            foreach (int t in tamanos)
            {
                f.RequestCharactersInTexture(caracteres, t, FontStyle.Normal);
                f.RequestCharactersInTexture(caracteres, t, FontStyle.Bold);
            }
        }

        static Texture2D Icono(string[] filas)
        {
            int h = filas.Length, w = filas[0].Length;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, h - 1 - y, filas[y][x] == 'X' ? Color.white : Color.clear);
            t.Apply();
            return t;
        }

        Texture2D IconoRecurso(string sprite)
        {
            Texture2D t;
            if (iconos.TryGetValue(sprite, out t)) return t;
            SpriteDef s;
            if (!sprites.TryGetValue(sprite, out s)) return null;
            var l = s.img;
            t = new Texture2D(l.w, l.h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[l.w * l.h];
            for (int y = 0; y < l.h; y++) System.Array.Copy(l.px, y * l.w, px, (l.h - 1 - y) * l.w, l.w);
            t.SetPixels32(px);
            t.Apply();
            iconos[sprite] = t;
            return t;
        }

        void PrepararMatriz()
        {
            float aspecto = Screen.width / (float)Mathf.Max(1, Screen.height);
            if (aspecto >= 16f / 9f)
            {
                escalaUI = Screen.height / 1080f;
                AH = 1080f;
                float ox = (Screen.width - AW * escalaUI) * 0.5f; // área segura 16:9 centrada (PC wide 21:9)
                GUI.matrix = Matrix4x4.TRS(new Vector3(ox, 0, 0), Quaternion.identity, new Vector3(escalaUI, escalaUI, 1));
            }
            else
            {
                escalaUI = Screen.width / AW;
                AH = Screen.height / escalaUI;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escalaUI, escalaUI, 1));
            }
            pantallaChica = Screen.height <= 600;
        }

        // Tamaño de letra en px de referencia (1920×1080). Solo el HUD sube de tamaño en
        // pantallas chicas para respetar el mínimo real de la Etapa 12 (21 px); el resto
        // escala junto con sus paneles para que nada se salga de las cajas.
        int Px(int referencia, int minimoReal = 0)
        {
            if (minimoReal <= 0) return referencia;
            int v = Mathf.CeilToInt(minimoReal / Mathf.Max(0.01f, escalaUI));
            return Mathf.Min(Mathf.Max(referencia, v), referencia * 3 / 2);
        }

        GUIStyle Estilo(bool angosta, int tam, bool negrita, Color c, TextAnchor anc = TextAnchor.MiddleLeft, bool ajuste = false)
        {
            estilo.font = angosta ? fuenteAngosta : fuenteNormal;
            estilo.fontSize = Ajustar(tam, angosta ? TamanosAngosta : TamanosNormal);
            estilo.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
            estilo.normal.textColor = c;
            estilo.alignment = anc;
            estilo.wordWrap = ajuste;
            return estilo;
        }

        static void Caja(Rect r, Color c)
        {
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
        }

        static void Marco(Rect r, Color c, float g)
        {
            Caja(new Rect(r.x, r.y, r.width, g), c);
            Caja(new Rect(r.x, r.yMax - g, r.width, g), c);
            Caja(new Rect(r.x, r.y, g, r.height), c);
            Caja(new Rect(r.xMax - g, r.y, g, r.height), c);
        }

        static void MarcoPunteado(Rect r, Color c)
        {
            for (float x = r.x; x < r.xMax; x += 12) { Caja(new Rect(x, r.y, 6, 1.5f), c); Caja(new Rect(x, r.yMax - 1.5f, 6, 1.5f), c); }
            for (float y = r.y; y < r.yMax; y += 12) { Caja(new Rect(r.x, y, 1.5f, 6), c); Caja(new Rect(r.xMax - 1.5f, y, 1.5f, 6), c); }
        }

        static void Icono(Rect r, Texture2D t, Color c)
        {
            if (t == null) return;
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t, ScaleMode.ScaleToFit, true);
            GUI.color = antes;
        }

        void Texto(Rect r, string s, GUIStyle st) { GUI.Label(r, s, st); }

        // Botón con los 4 estados de la Etapa 12 (lámina 5B). Devuelve true al activarse con el mouse.
        bool Boton(Rect r, string etiqueta, bool habilitado, bool seleccionado, int id, int tamLetra = 36, TextAnchor anc = TextAnchor.MiddleCenter)
        {
            var ev = Event.current;
            bool encima = r.Contains(ev.mousePosition);
            bool activado = false;
            if (habilitado && !fondo)
            {
                if (ev.type == EventType.MouseDown && ev.button == 0 && encima) { presionado = id; ev.Use(); }
                else if (ev.type == EventType.MouseUp && ev.button == 0 && presionado == id)
                {
                    if (encima) activado = true;
                    presionado = -1;
                    ev.Use();
                }
            }
            bool apretado = presionado == id && encima;

            if (ev.type == EventType.Repaint)
            {
                if (!habilitado)
                {
                    Caja(r, ConAlfa(BotonNormal, 0.5f));
                    MarcoPunteado(r, ConAlfa(Blanco, 0.5f));
                    Texto(r, etiqueta, Estilo(true, Px(tamLetra), false, ConAlfa(Blanco, 0.5f), anc));
                }
                else if (apretado)
                {
                    Caja(r, BotonPresionado);
                    Marco(r, Blanco, 2f);
                    Caja(new Rect(r.x + 2, r.y + 2, r.width - 4, 4), new Color(0, 0, 0, 0.6f));
                    Texto(new Rect(r.x, r.y + 3, r.width, r.height), etiqueta, Estilo(true, Px(tamLetra), true, Blanco, anc));
                }
                else if (seleccionado)
                {
                    Caja(new Rect(r.x + 4, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.45f));
                    Caja(r, BotonSel);
                    Marco(r, Azul, 2f);
                    Texto(r, etiqueta, Estilo(true, Px(tamLetra), true, Blanco, anc));
                }
                else
                {
                    Caja(new Rect(r.x + 4, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.45f));
                    Caja(r, BotonNormal);
                    Marco(r, ConAlfa(Blanco, 0.35f), 1f);
                    Texto(r, etiqueta, Estilo(true, Px(tamLetra), false, Blanco, anc));
                }
            }
            return activado;
        }

        // Elige con el mouse la opción que está debajo del cursor.
        bool Encima(Rect r) { return !fondo && mouseMovido && r.Contains(Event.current.mousePosition); }

        void OnGUI()
        {
            PrepararEstilos();
            GUI.depth = 0;

            if (Event.current.type == EventType.Repaint && raycaster != null && raycaster.Textura != null)
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), raycaster.Textura, ScaleMode.StretchToFill, false);
            }
            PrepararMatriz();

            switch (estado)
            {
                case Estado.MenuPrincipal: DibujarMenuPrincipal(); break;
                case Estado.ConfirmarNueva: fondo = true; DibujarMenuPrincipal(); fondo = false; DibujarConfirmacion("NUEVA PARTIDA", "Empezar de nuevo reemplaza la partida guardada. ¿Continuar?", "CANCELAR", "EMPEZAR", ref selConfirmar); break;
                case Estado.Opciones: DibujarOpciones(); break;
                case Estado.Creditos: DibujarCreditos(); break;
                case Estado.Intro: DibujarIntro(); break;
                case Estado.Jugando: DibujarHUD(); break;
                case Estado.Dialogo: DibujarHUD(); DibujarDialogo(); break;
                case Estado.Pausa: DibujarPausa(); break;
                case Estado.ConfirmarSalir: fondo = true; DibujarPausa(); fondo = false; DibujarConfirmacion("¿ESTÁS SEGURO?", "El progreso no guardado podría perderse.", "SÍ", "NO", ref selSalir); break;
                case Estado.Inventario: DibujarInventario(); break;
                case Estado.Final: DibujarFinal(); break;
            }

            if (!modoFoto)
            {
                if (estado == Estado.Jugando || estado == Estado.Dialogo) DibujarMensajes();
                else if (mensajes.Count > 0 && (estado == Estado.Inventario || estado == Estado.Pausa)) DibujarMensajes();
                if (modoDesarrollador) DibujarDesarrollador();
            }
            Capturas.DibujarAviso(AW, AH, this);
        }

        // ---------------- HUD (láminas 6 a 6D y 9C/9D) ----------------
        void DibujarHUD()
        {
            if (modoFoto || Event.current.type != EventType.Repaint) return;
            float m = 48f;
            int tam = Px(32, 21);
            float alto = Mathf.Max(64f, tam + 30f);
            float x = m, y = m;

            // Salud
            int seg = jugador.Segmentos();
            bool baja = seg <= 3 && seg > 0, cero = seg <= 0;
            float segAncho = 18f, segAlto = 26f, segGap = 4f;
            string rotulo = cero ? "SALUD 0" : (baja ? "SALUD BAJA" : "");
            var stRot = Estilo(false, Px(26, 21), true, Blanco);
            float anchoRot = rotulo.Length > 0 ? stRot.CalcSize(new GUIContent(rotulo)).x + 52f : 0f;
            var rSalud = new Rect(x, y, 16 + 40 + 12 + 10 * (segAncho + segGap) + 12 + anchoRot, alto);
            Caja(rSalud, ConAlfa(Negro, 0.72f));
            if (baja || cero) Marco(rSalud, Rojo, 2f);
            Icono(new Rect(x + 16, y + (alto - 32) / 2, 36, 32), iconoCorazon, cero ? Gris : Hex(0xC0392B));
            float pulso = baja ? 0.65f + 0.35f * Mathf.Sin(Time.time * 2.4f) : 1f;
            for (int i = 0; i < 10; i++)
            {
                var r = new Rect(x + 68 + i * (segAncho + segGap), y + (alto - segAlto) / 2, segAncho, segAlto);
                if (i < seg) Caja(r, ConAlfa(Blanco, pulso));
                else Marco(r, ConAlfa(Gris, 0.6f), 1.5f);
            }
            if (rotulo.Length > 0)
            {
                float rx = x + 68 + 10 * (segAncho + segGap) + 12;
                Icono(new Rect(rx, y + (alto - 28) / 2, 30, 28), iconoAlerta, cero ? Gris : Hex(0xC0392B));
                Texto(new Rect(rx + 40, y, anchoRot, alto), rotulo, stRot);
            }
            x = rSalud.xMax + 16;

            // Recursos
            string rec = (pantallaChica ? "" : "RECURSOS ") + inventario.Total().ToString("00");
            var stRec = Estilo(false, tam, true, Blanco);
            float anchoRec = stRec.CalcSize(new GUIContent(rec)).x;
            var rRec = new Rect(x, y, 16 + 32 + 12 + anchoRec + 18, alto);
            Caja(rRec, ConAlfa(Negro, 0.72f));
            Icono(new Rect(x + 14, y + (alto - 34) / 2, 32, 34), iconoMochila, Blanco);
            Texto(new Rect(x + 58, y, anchoRec + 10, alto), rec, stRec);
            x = rRec.xMax + 16;

            // Objetivo activo: punto azul + nombre (uno a la vez, sin minimapa ni flecha)
            var obj = progresion.Actual;
            if (obj != null)
            {
                string t = obj.titulo.ToUpperInvariant();
                var stObj = Estilo(false, tam, true, Blanco);
                float anchoObj = Mathf.Min(stObj.CalcSize(new GUIContent(t)).x, AW - m - x - 60);
                var rObj = new Rect(x, y, 16 + 20 + 14 + anchoObj + 18, alto);
                Caja(rObj, ConAlfa(Negro, 0.72f));
                Caja(new Rect(x + 18, y + alto / 2 - 8, 16, 16), Azul);
                Texto(new Rect(x + 48, y, anchoObj + 10, alto), t, stObj);
            }

            // Panel "OBJETIVO ACTUAL" (Etapa 11, wireframe 04) cuando cambia el objetivo
            if (avisoObjetivo > 0f && obj != null)
            {
                float a = Mathf.Clamp01(avisoObjetivo);
                var r = new Rect(AW / 2 - 420, 170, 840, 210);
                Caja(r, ConAlfa(Panel, 0.92f * a));
                Marco(r, ConAlfa(Azul, a), 2f);
                Texto(new Rect(r.x + 30, r.y + 18, 780, 34), "OBJETIVO ACTUAL", Estilo(false, Px(26), true, ConAlfa(Gris, a)));
                Texto(new Rect(r.x + 30, r.y + 56, 780, 70), obj.descripcion, Estilo(false, Px(32), false, ConAlfa(Blanco, a), TextAnchor.UpperLeft, true));
                Texto(new Rect(r.x + 30, r.y + 140, 380, 40), "ZONA: " + obj.zona, Estilo(false, Px(26), false, ConAlfa(Gris, a)));
                Texto(new Rect(r.x + 420, r.y + 140, 400, 40), "DESTINO: " + obj.destino, Estilo(false, Px(26), false, ConAlfa(Gris, a)));
            }
            else if (avisoZona > 0f && !string.IsNullOrEmpty(nombreZona))
            {
                float a = Mathf.Clamp01(avisoZona);
                var st = Estilo(true, Px(44), true, ConAlfa(Blanco, a), TextAnchor.MiddleCenter);
                var r = new Rect(AW / 2 - 400, 180, 800, 70);
                Caja(new Rect(AW / 2 - 260, 186, 520, 58), ConAlfa(Negro, 0.6f * a));
                Texto(r, nombreZona.ToUpperInvariant(), st);
            }

            // Interacción: centrada abajo, solo cuando hay algo al alcance
            if (HayInteraccion && estado == Estado.Jugando)
            {
                string nombre = "";
                if (puertaX >= 0) nombre = "Puerta";
                else if (objetivoInteraccion.def.tipo == TipoEntidad.Recurso) nombre = ContenidoJuego.Recurso(objetivoInteraccion.def.idRecurso).nombre;
                else nombre = objetivoInteraccion.def.nombre;
                var st = Estilo(false, Px(32, 21), true, Blanco, TextAnchor.MiddleCenter);
                string t = "[E]  +  INTERACTUAR";
                float w = st.CalcSize(new GUIContent(t)).x + 60;
                var r = new Rect(AW / 2 - w / 2, AH - m - 64, w, 64);
                Caja(r, ConAlfa(Panel, 0.92f));
                Marco(r, Blanco, 1.5f);
                Texto(r, t, st);
                if (!string.IsNullOrEmpty(nombre))
                {
                    var st2 = Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter);
                    float w2 = st2.CalcSize(new GUIContent(nombre)).x + 40;
                    var r2 = new Rect(AW / 2 - w2 / 2, r.y - 50, w2, 42);
                    Caja(r2, ConAlfa(Negro, 0.7f));
                    Texto(r2, nombre, st2);
                }
            }
        }

        void DibujarMensajes()
        {
            if (mensajes.Count == 0 || Event.current.type != EventType.Repaint) return;
            var msj = mensajes[0];
            float a = Mathf.Clamp01(msj.tiempo * 3f);
            var st = Estilo(false, Px(30), false, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true);
            float w = Mathf.Min(1300f, st.CalcSize(new GUIContent(msj.texto)).x + 60f);
            float h = st.CalcHeight(new GUIContent(msj.texto), w - 40f) + 28f;
            var r = new Rect(AW / 2 - w / 2, AH - 48 - 64 - 70 - h - 20, w, h);
            if (estado == Estado.Dialogo) r.y = AH - 420 - h;
            Caja(r, ConAlfa(Negro, 0.78f * a));
            Texto(new Rect(r.x + 20, r.y, r.width - 40, r.height), msj.texto, st);
        }

        // ---------------- MENÚ PRINCIPAL (lámina 8) ----------------
        void DibujarMenuPrincipal()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, 0.35f));
                Caja(new Rect(0, 0, 820, AH), ConAlfa(Negro, 0.6f));
                Texto(new Rect(150, 150, 1200, 120), ContenidoJuego.NombreJuego, Estilo(true, Px(96), true, Blanco));
                Texto(new Rect(156, 262, 1200, 60), "V I E N T O S   D E   O C T U B R E", Estilo(true, Px(34), false, Azul));
                Texto(new Rect(156, AH - 90, 1600, 40), "BETA  ·  F12 captura  ·  F11 modo foto  ·  F9 modo desarrollador", Estilo(false, Px(26), false, Gris));
            }
            var ops = OpcionesMenu();
            for (int i = 0; i < ops.Length; i++)
            {
                var r = new Rect(150, 420 + i * 92, 480, 72);
                bool hab = Habilitada(Estado.MenuPrincipal, i);
                if (hab && Encima(r) && estado == Estado.MenuPrincipal) selMenu = i;
                if (Boton(r, ops[i], hab, selMenu == i && estado == Estado.MenuPrincipal, 100 + i) && estado == Estado.MenuPrincipal) Activar(i);
            }
        }

        void DibujarConfirmacion(string titulo, string texto, string op0, string op1, ref int sel)
        {
            var r = new Rect(AW / 2 - 440, AH / 2 - 200, 880, 400);
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, 0.6f));
                Caja(r, Panel);
                Marco(r, ConAlfa(Blanco, 0.35f), 1f);
                Texto(new Rect(r.x, r.y + 40, r.width, 70), titulo, Estilo(true, Px(56), true, Blanco, TextAnchor.MiddleCenter));
                Texto(new Rect(r.x + 60, r.y + 130, r.width - 120, 110), texto, Estilo(false, Px(32), false, Blanco, TextAnchor.MiddleCenter, true));
            }
            var b0 = new Rect(r.x + 110, r.yMax - 120, 300, 72);
            var b1 = new Rect(r.xMax - 410, r.yMax - 120, 300, 72);
            if (Encima(b0)) sel = 0;
            if (Encima(b1)) sel = 1;
            if (Boton(b0, op0, true, sel == 0, 200)) Activar(0);
            if (Boton(b1, op1, true, sel == 1, 201)) Activar(1);
        }

        // ---------------- PAUSA (lámina 7) ----------------
        void DibujarPausa()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, 0.72f));
                Texto(new Rect(0, 170, AW, 80), "PAUSA", Estilo(true, Px(56), true, Blanco, TextAnchor.MiddleCenter));
                var obj = progresion.Actual;
                if (obj != null)
                {
                    Texto(new Rect(AW / 2 - 500, 760, 1000, 40), "OBJETIVO: " + obj.titulo.ToUpperInvariant(), Estilo(false, Px(26), true, Gris, TextAnchor.MiddleCenter));
                    Texto(new Rect(AW / 2 - 500, 800, 1000, 40), obj.descripcion, Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter));
                }
            }
            var ops = OpcionesPausa();
            for (int i = 0; i < ops.Length; i++)
            {
                var r = new Rect(AW / 2 - 260, 320 + i * 92, 520, 72);
                if (estado == Estado.Pausa && Encima(r)) selPausa = i;
                if (Boton(r, ops[i], true, selPausa == i && estado == Estado.Pausa, 300 + i) && estado == Estado.Pausa) Activar(i);
            }
        }

        // ---------------- OPCIONES (lámina 8C) ----------------
        void DibujarOpciones()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, 0.8f));
                Texto(new Rect(0, 150, AW, 80), "OPCIONES", Estilo(true, Px(56), true, Blanco, TextAnchor.MiddleCenter));
            }
            string[] etiquetas = { "VOLUMEN", "PANTALLA COMPLETA", "RESOLUCIÓN" };
            for (int i = 0; i < 3; i++)
            {
                var fila = new Rect(AW / 2 - 480, 300 + i * 110, 960, 84);
                if (Encima(fila)) selOpciones = i;
                bool sel = selOpciones == i;
                if (Event.current.type == EventType.Repaint)
                {
                    Caja(fila, sel ? BotonSel : BotonNormal);
                    if (sel) Marco(fila, Azul, 2f); else Marco(fila, ConAlfa(Blanco, 0.35f), 1f);
                    Texto(new Rect(fila.x + 30, fila.y, 400, fila.height), etiquetas[i], Estilo(true, Px(36), sel, Blanco));
                }
                var zona = new Rect(fila.x + 480, fila.y + 12, 450, 60);
                if (i == 0)
                {
                    var barra = new Rect(zona.x, zona.y + 24, 330, 12);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Caja(barra, ConAlfa(Gris, 0.35f));
                        Caja(new Rect(barra.x, barra.y, barra.width * volumen / 100f, barra.height), sel ? Azul : Blanco);
                        Caja(new Rect(barra.x + barra.width * volumen / 100f - 6, barra.y - 10, 12, 32), Blanco);
                        Texto(new Rect(barra.xMax + 24, zona.y, 90, 60), volumen.ToString(), Estilo(false, Px(32), true, Blanco));
                    }
                    var ev = Event.current;
                    if ((ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag) && ev.button == 0 &&
                        new Rect(barra.x - 10, zona.y, barra.width + 20, zona.height).Contains(ev.mousePosition))
                    {
                        volumen = Mathf.Clamp(Mathf.RoundToInt((ev.mousePosition.x - barra.x) / barra.width * 100f), 0, 100);
                        AplicarOpciones();
                        ev.Use();
                    }
                }
                else if (i == 1)
                {
                    if (Boton(new Rect(zona.x, zona.y, 140, 60), "SÍ", true, pantallaCompleta, 400, 32)) { pantallaCompleta = true; AplicarOpciones(); }
                    if (Boton(new Rect(zona.x + 170, zona.y, 140, 60), "NO", true, !pantallaCompleta, 401, 32)) { pantallaCompleta = false; AplicarOpciones(); }
                }
                else
                {
                    var res = Resoluciones[resolucionIdx];
                    if (Boton(new Rect(zona.x, zona.y, 60, 60), "<", true, false, 402, 32)) CambiarOpcion(2, -1);
                    if (Event.current.type == EventType.Repaint)
                        Texto(new Rect(zona.x + 60, zona.y, 270, 60), res.x + "×" + res.y, Estilo(false, Px(32), true, Blanco, TextAnchor.MiddleCenter));
                    if (Boton(new Rect(zona.x + 330, zona.y, 60, 60), ">", true, false, 403, 32)) CambiarOpcion(2, 1);
                }
            }
            var volver = new Rect(AW / 2 - 200, 650, 400, 72);
            if (Encima(volver)) selOpciones = 3;
            if (Boton(volver, "VOLVER", true, selOpciones == 3, 404)) Activar(3);
            if (Event.current.type == EventType.Repaint)
            {
                string nota = Application.isEditor
                    ? "En el Editor la resolución se elige en la pestaña Game. En el juego compilado se aplica al instante."
                    : "Los cambios se aplican al instante.";
                Texto(new Rect(0, 760, AW, 40), nota, Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter));
            }
        }

        // ---------------- CRÉDITOS (lámina 8D) ----------------
        void DibujarCreditos()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, origenCreditos == Estado.Final ? 1f : 0.88f));
                Texto(new Rect(0, 110, AW, 110), ContenidoJuego.NombreJuego, Estilo(true, Px(96), true, Blanco, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 215, AW, 50), "V I E N T O S   D E   O C T U B R E", Estilo(true, Px(34), false, Azul, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 320, AW, 50), "EQUIPO", Estilo(true, Px(36), true, Gris, TextAnchor.MiddleCenter));
                for (int i = 0; i < ContenidoJuego.Creditos.Length; i++)
                    Texto(new Rect(0, 390 + i * 56, AW, 50), ContenidoJuego.Creditos[i], Estilo(false, Px(32), false, Blanco, TextAnchor.MiddleCenter));
            }
            var r = new Rect(AW / 2 - 200, 720, 400, 72);
            if (Boton(r, "VOLVER", true, true, 500)) Activar(0);
        }

        // ---------------- INTRO ----------------
        void DibujarIntro()
        {
            if (Event.current.type != EventType.Repaint) return;
            Caja(new Rect(-2000, -2000, 6000, 6000), Negro);
            if (introIdx >= ContenidoJuego.Intro.Length) return;
            float a = Mathf.Clamp01(introTiempo * 1.5f) * Mathf.Clamp01((4.5f - introTiempo) * 1.5f);
            Texto(new Rect(160, AH / 2 - 150, AW - 320, 300), ContenidoJuego.Intro[introIdx],
                Estilo(introIdx == 0, Px(introIdx == 0 ? 56 : 40), introIdx == 0, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true));
            Texto(new Rect(0, AH - 100, AW, 40), "[Enter] continuar   ·   [Esc] saltar", Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter));
        }

        // ---------------- DIÁLOGO ----------------
        void DibujarDialogo()
        {
            var r = new Rect(AW / 2 - 720, AH - 360, 1440, 290);
            if (Event.current.type == EventType.Repaint)
            {
                Caja(r, ConAlfa(Panel, 0.94f));
                Marco(r, Azul, 2f);
                Texto(new Rect(r.x + 40, r.y + 20, 800, 50), ContenidoJuego.Informante.nombre.ToUpperInvariant(), Estilo(true, Px(36), true, Azul));
                string linea = dialogoIdx < dialogo.Count ? dialogo[dialogoIdx] : "";
                Texto(new Rect(r.x + 40, r.y + 78, r.width - 80, 110), "“" + linea + "”", Estilo(false, Px(32), false, Blanco, TextAnchor.UpperLeft, true));
                if (!decisionActiva)
                    Texto(new Rect(r.x + 40, r.yMax - 60, r.width - 80, 40), "[E] Continuar", Estilo(false, Px(26), false, Gris, TextAnchor.MiddleRight));
                else
                    Texto(new Rect(r.x + 40, r.y + 160, r.width - 80, 40), ContenidoJuego.DecisionPregunta, Estilo(false, Px(26), true, Gris));
            }
            if (decisionActiva)
            {
                if (Boton(new Rect(r.x + 40, r.yMax - 84, 620, 64), "[1]  " + ContenidoJuego.DecisionDar.ToUpperInvariant(), true, false, 600, 32)) ElegirDecision(true);
                if (Boton(new Rect(r.x + 700, r.yMax - 84, 620, 64), "[2]  " + ContenidoJuego.DecisionNoDar.ToUpperInvariant(), true, false, 601, 32)) ElegirDecision(false);
            }
        }

        // ---------------- INVENTARIO (láminas 7, 7B, 7C) ----------------
        void DibujarInventario()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), ConAlfa(Negro, 0.85f));
                Texto(new Rect(0, 90, AW, 80), "INVENTARIO", Estilo(true, Px(56), true, Blanco, TextAnchor.MiddleCenter));
            }
            var izq = new Rect(200, 210, 680, 600);
            var der = new Rect(940, 210, 780, 600);
            if (Event.current.type == EventType.Repaint)
            {
                Caja(izq, Panel); Marco(izq, ConAlfa(Blanco, 0.25f), 1f);
                Caja(der, Panel); Marco(der, ConAlfa(Blanco, 0.25f), 1f);
                Texto(new Rect(izq.x + 30, izq.y + 16, 400, 50), "RECURSOS", Estilo(true, Px(36), true, Gris));
                Texto(new Rect(izq.xMax - 160, izq.y + 16, 130, 50), inventario.Total().ToString("00"), Estilo(true, Px(36), true, Gris, TextAnchor.MiddleRight));
                Texto(new Rect(der.x + 30, der.y + 16, 600, 50), "OBJETO SELECCIONADO", Estilo(true, Px(36), true, Gris));
            }
            for (int i = 0; i < 4; i++)
            {
                var cat = Inventario.Orden[i];
                var fila = new Rect(izq.x + 20, izq.y + 90 + i * 118, izq.width - 40, 96);
                bool sel = invCategoria == i;
                var ev = Event.current;
                if (ev.type == EventType.MouseDown && ev.button == 0 && fila.Contains(ev.mousePosition)) { invCategoria = i; invObjeto = 0; audioJuego.Menu(); ev.Use(); }
                if (ev.type == EventType.Repaint)
                {
                    Caja(fila, sel ? BotonSel : BotonNormal);
                    if (sel) Marco(fila, Azul, 2f); else Marco(fila, ConAlfa(Blanco, 0.2f), 1f);
                    Texto(new Rect(fila.x + 24, fila.y, 420, fila.height), Inventario.NombreCategoria(cat), Estilo(false, Px(32), sel, Blanco));
                    Texto(new Rect(fila.xMax - 140, fila.y, 116, fila.height), inventario.CantidadCategoria(cat).ToString("00"), Estilo(false, Px(32), true, Blanco, TextAnchor.MiddleRight));
                }
            }

            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count == 0)
            {
                if (Event.current.type == EventType.Repaint)
                    Texto(new Rect(der.x, der.y + 80, der.width, der.height - 160), "No hay objetos disponibles.", Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter));
            }
            else
            {
                invObjeto = Mathf.Clamp(invObjeto, 0, lista.Count - 1);
                var r = lista[invObjeto];
                if (Event.current.type == EventType.Repaint)
                {
                    var marcoIcono = new Rect(der.x + 30, der.y + 90, 170, 170);
                    Caja(marcoIcono, BotonNormal);
                    Icono(new Rect(marcoIcono.x + 10, marcoIcono.y + 10, 150, 150), IconoRecurso(r.sprite), Color.white);
                    Texto(new Rect(der.x + 230, der.y + 90, 520, 60), r.nombre.ToUpperInvariant(), Estilo(true, Px(44), true, Blanco));
                    Texto(new Rect(der.x + 230, der.y + 150, 520, 40), r.tipo + " · " + r.utilidad, Estilo(false, Px(26), false, Gris));
                    Texto(new Rect(der.x + 30, der.y + 290, der.width - 60, 130), r.descripcion, Estilo(false, Px(32), false, Blanco, TextAnchor.UpperLeft, true));
                    Texto(new Rect(der.x + 30, der.y + 430, 400, 50), "CANTIDAD: " + inventario.Cantidad(r.id).ToString("00"), Estilo(false, Px(32), true, Blanco));
                    if (lista.Count > 1)
                        Texto(new Rect(der.xMax - 240, der.y + 430, 210, 50), "<  " + (invObjeto + 1) + " / " + lista.Count + "  >", Estilo(false, Px(32), true, Azul, TextAnchor.MiddleRight));
                }
                if (Boton(new Rect(der.x + 30, der.yMax - 100, 320, 72), "UTILIZAR", r.curacion > 0, r.curacion > 0, 700)) UtilizarSeleccionado();
                if (lista.Count > 1 && Boton(new Rect(der.xMax - 230, der.yMax - 100, 90, 72), "<", true, false, 701)) invObjeto = (invObjeto + lista.Count - 1) % lista.Count;
                if (lista.Count > 1 && Boton(new Rect(der.xMax - 120, der.yMax - 100, 90, 72), ">", true, false, 702)) invObjeto = (invObjeto + 1) % lista.Count;
            }

            if (Event.current.type == EventType.Repaint)
                Texto(new Rect(0, 850, AW, 40), "[W/S] categoría   ·   [A/D] objeto   ·   [E] utilizar   ·   [Esc] volver", Estilo(false, Px(26), false, Gris, TextAnchor.MiddleCenter));
        }

        // ---------------- SECUENCIA FINAL (lámina 8E) ----------------
        void DibujarFinal()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (finalFase == 0)
            {
                float a = Mathf.Clamp01((finalTiempo - 2f) / 1.5f) * Mathf.Clamp01((10f - finalTiempo) / 1.5f);
                if (a > 0f)
                {
                    var st = Estilo(false, Px(32), false, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true);
                    var r = new Rect(AW / 2 - 640, AH - 200, 1280, 90);
                    Caja(r, ConAlfa(Negro, 0.6f * a));
                    Texto(r, "A lo lejos, entre la tormenta de nieve, se distingue la silueta del Obelisco.", st);
                }
            }
            else if (finalFase == 1)
            {
                Caja(new Rect(-2000, -2000, 6000, 6000), Color.black);
                float a1 = Mathf.Clamp01(finalTiempo / 1.2f), a2 = Mathf.Clamp01((finalTiempo - 1.5f) / 1.2f), a3 = Mathf.Clamp01((finalTiempo - 3f) / 1.2f);
                float salida = Mathf.Clamp01((7.5f - finalTiempo) / 1f);
                Texto(new Rect(0, AH / 2 - 260, AW, 110), ContenidoJuego.NombreJuego, Estilo(true, Px(96), true, ConAlfa(Blanco, a1 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 - 155, AW, 50), "V I E N T O S   D E   O C T U B R E", Estilo(true, Px(34), false, ConAlfa(Azul, a1 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 - 40, AW, 80), "FIN DEL PRÓLOGO", Estilo(true, Px(56), true, ConAlfa(Blanco, a2 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 + 60, AW, 50), "FRÍO.  AISLAMIENTO.  SUPERVIVENCIA.", Estilo(false, Px(26), true, ConAlfa(Gris, a2 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 + 150, AW, 60), ContenidoJuego.FraseFinal, Estilo(false, Px(36), false, ConAlfa(Blanco, a3 * salida), TextAnchor.MiddleCenter));
            }
        }

        // ---------------- MODO DESARROLLADOR (Etapa 5, rol tester interno) ----------------
        void DibujarDesarrollador()
        {
            if (Event.current.type != EventType.Repaint) return;
            var r = new Rect(AW - 700, 140, 660, 520);
            Caja(r, ConAlfa(Negro, 0.8f));
            Marco(r, Rojo, 2f);
            var st = Estilo(false, Px(22), false, Blanco, TextAnchor.UpperLeft);
            var esc = ContenidoJuego.EscenarioEn(jugador.x, jugador.y);
            var obj = progresion.Actual;
            string info =
                "MODO DESARROLLADOR (F9)\n" +
                "FPS: " + Mathf.RoundToInt(fps) + "   Render: " + raycaster.W + "×" + raycaster.H + "\n" +
                "Posición: " + jugador.x.ToString("0.0") + ", " + jugador.y.ToString("0.0") + "   Ángulo: " + Mathf.RoundToInt(Mathf.Repeat(jugador.angulo * Mathf.Rad2Deg, 360f)) + "°\n" +
                "Zona: " + esc.nombre + " (id " + esc.id + ")   Bajo techo: " + (jugador.bajoTecho ? "sí" : "no") + "\n" +
                "Salud: " + jugador.vida.ToString("0") + "   Exposición: " + Mathf.RoundToInt(jugador.exposicion * 100) + "%\n" +
                "Objetivo: " + (obj != null ? obj.id + " - " + obj.titulo : "ninguno") + "\n" +
                "Invulnerable: " + (jugador.invulnerable ? "sí" : "no") + "\n\n" +
                "F2 invulnerable   F3 completar objetivo\n" +
                "F4 ir a la siguiente postal\n" +
                "F5 +1 de cada recurso   F6 cambiar salud\n" +
                "F7 saltar al puente   F8 efecto VHS\n" +
                "F11 modo foto   F12 captura";
            Texto(new Rect(r.x + 20, r.y + 16, r.width - 40, r.height - 32), info, st);
        }
    }
}
