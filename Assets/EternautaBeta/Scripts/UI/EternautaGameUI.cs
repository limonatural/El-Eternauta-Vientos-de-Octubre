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
        float origenX, origenY; // dónde empieza el área de referencia en la pantalla
        bool pantallaChica;

        // Fuentes incluidas en el proyecto (Resources/Fuentes). No se piden al sistema
        // operativo, así que no aparece el aviso "Unable to load font face".
        Font fuenteAngosta, fuenteAngostaNegrita, fuenteNormal, fuenteNormalNegrita;
        GUIStyle estilo;
        readonly GUIContent contenido = new GUIContent();
        Texture2D iconoCorazon, iconoMochila, iconoAlerta;
        Texture2D texVineta, texLineas, texRuido;

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

        // Formato de un texto: fuente, tamaño en px de referencia (1920×1080), color y alineación.
        struct Letra
        {
            public Font fuente;
            public int tam;
            public Color color;
            public TextAnchor anc;
            public bool ajuste;
        }

        // Ninguna letra baja de este tamaño real (en px de pantalla) salvo que no entre en su caja.
        const int LetraMinimaReal = 15;

        void PrepararEstilos()
        {
            if (estilo != null) return;
            var respaldo = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fuenteAngosta = CargarFuente("Fuentes/RobotoCondensed-Regular", respaldo);
            fuenteAngostaNegrita = CargarFuente("Fuentes/RobotoCondensed-Bold", fuenteAngosta);
            fuenteNormal = CargarFuente("Fuentes/LiberationSans-Regular", respaldo);
            fuenteNormalNegrita = CargarFuente("Fuentes/LiberationSans-Bold", fuenteNormal);
            estilo = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                richText = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                fontStyle = FontStyle.Normal,
            };

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
            CrearTexturasEfectos();
        }

        static Font CargarFuente(string ruta, Font respaldo)
        {
            var f = Resources.Load<Font>(ruta);
            if (f == null) Debug.LogWarning("[Eternauta] No se encontró la fuente " + ruta + "; se usa la fuente integrada de Unity.");
            return f != null ? f : respaldo;
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

        // Texturas de los efectos de pantalla: viñeta (escarcha, alerta), líneas de VHS y grano.
        void CrearTexturasEfectos()
        {
            const int n = 128;
            texVineta = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.45f) / 0.75f);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            texVineta.SetPixels32(px);
            texVineta.Apply();

            texLineas = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            texLineas.SetPixels32(new[] { new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0) });
            texLineas.Apply();

            texRuido = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var r = new System.Random(7);
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)r.Next(256);
                px[i] = new Color32(v, v, v, 255);
            }
            texRuido.SetPixels32(px);
            texRuido.Apply();
        }

        void PrepararEscala()
        {
            float aspecto = Screen.width / (float)Mathf.Max(1, Screen.height);
            if (aspecto >= 16f / 9f)
            {
                escalaUI = Screen.height / 1080f;
                AH = 1080f;
                origenX = Mathf.Round((Screen.width - AW * escalaUI) * 0.5f); // área segura 16:9 centrada (PC wide 21:9)
            }
            else
            {
                escalaUI = Screen.width / AW;
                AH = Screen.height / escalaUI;
                origenX = 0f;
            }
            origenY = 0f;
            pantallaChica = Screen.height <= 600;
        }

        // Rectángulo de referencia (1920×1080) → píxeles de pantalla, redondeado para que las letras salgan nítidas.
        Rect S(Rect r)
        {
            float x0 = Mathf.Round(origenX + r.x * escalaUI), y0 = Mathf.Round(origenY + r.y * escalaUI);
            float x1 = Mathf.Round(origenX + r.xMax * escalaUI), y1 = Mathf.Round(origenY + r.yMax * escalaUI);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        // Posición del mouse en coordenadas de referencia.
        Vector2 Raton()
        {
            var p = Event.current.mousePosition;
            return new Vector2((p.x - origenX) / escalaUI, (p.y - origenY) / escalaUI);
        }

        Letra Estilo(bool angosta, int tam, bool negrita, Color c, TextAnchor anc = TextAnchor.MiddleLeft, bool ajuste = false)
        {
            return new Letra
            {
                fuente = angosta ? (negrita ? fuenteAngostaNegrita : fuenteAngosta) : (negrita ? fuenteNormalNegrita : fuenteNormal),
                tam = tam,
                color = c,
                anc = anc,
                ajuste = ajuste,
            };
        }

        int TamReal(Letra l)
        {
            int t = Mathf.RoundToInt(l.tam * escalaUI);
            return Mathf.Max(t, Mathf.Min(l.tam, LetraMinimaReal));
        }

        GUIStyle Aplicar(Letra l, int tamReal)
        {
            estilo.font = l.fuente;
            estilo.fontSize = tamReal;
            estilo.normal.textColor = l.color;
            estilo.alignment = l.anc;
            estilo.wordWrap = l.ajuste;
            return estilo;
        }

        GUIContent Contenido(string s) { contenido.text = s; return contenido; }

        bool Entra(GUIStyle st, GUIContent c, Rect r)
        {
            if (st.wordWrap) return st.CalcHeight(c, r.width) <= r.height + 1f;
            var t = st.CalcSize(c);
            return t.x <= r.width + 1f && t.y <= r.height * 1.2f + 1f;
        }

        // Dibuja un texto dentro de su caja. Si no entra, baja el tamaño de a pasos hasta que entre.
        void Texto(Rect r, string s, Letra l)
        {
            if (string.IsNullOrEmpty(s) || l.color.a <= 0.003f) return;
            var rs = S(r);
            int t = TamReal(l);
            var st = Aplicar(l, t);
            var c = Contenido(s);
            int minimo = Mathf.Max(9, t / 2);
            while (t > minimo && !Entra(st, c, rs))
            {
                t = Mathf.Max(minimo, t - Mathf.Max(1, t / 12));
                st.fontSize = t;
            }
            GUI.Label(rs, c, st);
        }

        // Ancho de un texto de una línea, en px de referencia.
        float AnchoTexto(Letra l, string s)
        {
            return Aplicar(l, TamReal(l)).CalcSize(Contenido(s)).x / escalaUI;
        }

        // Alto de un texto con salto de línea dentro de un ancho dado (ambos en px de referencia).
        float AltoTexto(Letra l, string s, float ancho)
        {
            var st = Aplicar(l, TamReal(l));
            st.wordWrap = true;
            return st.CalcHeight(Contenido(s), ancho * escalaUI) / escalaUI;
        }

        void Caja(Rect r, Color c)
        {
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(S(r), Texture2D.whiteTexture);
            GUI.color = antes;
        }

        void CajaReal(Rect r, Color c)
        {
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
        }

        void Marco(Rect r, Color c, float g)
        {
            var rs = S(r);
            float gr = Mathf.Max(1f, Mathf.Round(g * escalaUI));
            CajaReal(new Rect(rs.x, rs.y, rs.width, gr), c);
            CajaReal(new Rect(rs.x, rs.yMax - gr, rs.width, gr), c);
            CajaReal(new Rect(rs.x, rs.y, gr, rs.height), c);
            CajaReal(new Rect(rs.xMax - gr, rs.y, gr, rs.height), c);
        }

        void MarcoPunteado(Rect r, Color c)
        {
            var rs = S(r);
            float g = Mathf.Max(1f, Mathf.Round(1.5f * escalaUI)), paso = Mathf.Max(4f, 12f * escalaUI), largo = paso / 2f;
            for (float x = rs.x; x < rs.xMax; x += paso) { CajaReal(new Rect(x, rs.y, Mathf.Min(largo, rs.xMax - x), g), c); CajaReal(new Rect(x, rs.yMax - g, Mathf.Min(largo, rs.xMax - x), g), c); }
            for (float y = rs.y; y < rs.yMax; y += paso) { CajaReal(new Rect(rs.x, y, g, Mathf.Min(largo, rs.yMax - y)), c); CajaReal(new Rect(rs.xMax - g, y, g, Mathf.Min(largo, rs.yMax - y)), c); }
        }

        void Icono(Rect r, Texture t, Color c)
        {
            if (t == null) return;
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(S(r), t, ScaleMode.ScaleToFit, true);
            GUI.color = antes;
        }

        // Tapa toda la pantalla (incluidas las bandas fuera del área 16:9).
        void Fondo(Color c) { CajaReal(new Rect(0, 0, Screen.width, Screen.height), c); }

        // Botón con los 4 estados de la Etapa 12 (lámina 5B). Devuelve true al activarse con el mouse.
        bool Boton(Rect r, string etiqueta, bool habilitado, bool seleccionado, int id, int tamLetra = 36, TextAnchor anc = TextAnchor.MiddleCenter)
        {
            var ev = Event.current;
            bool encima = r.Contains(Raton());
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
                // Margen interno para que la etiqueta nunca toque el borde del botón.
                var rt = new Rect(r.x + 12, r.y, r.width - 24, r.height);
                if (!habilitado)
                {
                    Caja(r, ConAlfa(BotonNormal, 0.5f));
                    MarcoPunteado(r, ConAlfa(Blanco, 0.5f));
                    Texto(rt, etiqueta, Estilo(true, tamLetra, false, ConAlfa(Blanco, 0.5f), anc));
                }
                else if (apretado)
                {
                    Caja(r, BotonPresionado);
                    Marco(r, Blanco, 2f);
                    Caja(new Rect(r.x + 2, r.y + 2, r.width - 4, 4), new Color(0, 0, 0, 0.6f));
                    Texto(new Rect(rt.x, rt.y + 3, rt.width, rt.height), etiqueta, Estilo(true, tamLetra, true, Blanco, anc));
                }
                else if (seleccionado)
                {
                    Caja(new Rect(r.x + 4, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.45f));
                    Caja(r, BotonSel);
                    Marco(r, Azul, 2f);
                    Texto(rt, etiqueta, Estilo(true, tamLetra, true, Blanco, anc));
                }
                else
                {
                    Caja(new Rect(r.x + 4, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.45f));
                    Caja(r, BotonNormal);
                    Marco(r, ConAlfa(Blanco, 0.35f), 1f);
                    Texto(rt, etiqueta, Estilo(true, tamLetra, false, Blanco, anc));
                }
            }
            return activado;
        }

        // Elige con el mouse la opción que está debajo del cursor.
        bool Encima(Rect r) { return !fondo && mouseMovido && r.Contains(Raton()); }

        void OnGUI()
        {
            PrepararEstilos();
            GUI.depth = 0;
            GUI.matrix = Matrix4x4.identity;
            PrepararEscala();

            if (Event.current.type == EventType.Repaint)
            {
                if (visual != null && visual.Imagen != null)
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), visual.Imagen, ScaleMode.StretchToFill, false);
                DibujarEfectos();
            }

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
            DibujarAvisoCaptura();
        }

        // ---------------- EFECTOS DE PANTALLA ----------------
        // Escarcha en los bordes al exponerse a la nieve, alerta roja con poca salud,
        // destello al recoger objetos, fundido del final y líneas/grano de VHS (F8).
        void DibujarEfectos()
        {
            var pantalla = new Rect(0, 0, Screen.width, Screen.height);
            var antes = GUI.color;
            if (escarcha > 0.01f)
            {
                GUI.color = new Color(0.82f, 0.9f, 1f, Mathf.Clamp01(escarcha) * 0.85f);
                GUI.DrawTexture(pantalla, texVineta, ScaleMode.StretchToFill, true);
            }
            if (alertaRoja > 0.01f)
            {
                GUI.color = new Color(0.55f, 0.04f, 0.04f, Mathf.Clamp01(alertaRoja));
                GUI.DrawTexture(pantalla, texVineta, ScaleMode.StretchToFill, true);
            }
            if (destello > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 0.92f, destello * 0.35f);
                GUI.DrawTexture(pantalla, Texture2D.whiteTexture);
            }
            if (efectoVHS && !modoFoto)
            {
                float lineas = Mathf.Max(1f, (visual != null ? visual.Alto : 240) / 2f);
                GUI.color = new Color(1f, 1f, 1f, 0.16f);
                GUI.DrawTextureWithTexCoords(pantalla, texLineas, new Rect(0, 0, 1, lineas));
                float u = Random.value, v = Random.value;
                float esc = Mathf.Max(1f, Screen.height / 360f);
                GUI.color = new Color(1f, 1f, 1f, 0.05f);
                GUI.DrawTextureWithTexCoords(pantalla, texRuido, new Rect(u, v, Screen.width / (128f * esc), Screen.height / (128f * esc)));
                // Banda de "tracking" que baja lentamente.
                float banda = Mathf.Repeat(Time.unscaledTime * 0.07f, 1.3f) - 0.15f;
                GUI.color = new Color(1f, 1f, 1f, 0.035f);
                GUI.DrawTexture(new Rect(0, banda * Screen.height, Screen.width, Screen.height * 0.05f), Texture2D.whiteTexture);
            }
            if (fundido > 0.01f)
            {
                GUI.color = new Color(0, 0, 0, Mathf.Clamp01(fundido));
                GUI.DrawTexture(pantalla, Texture2D.whiteTexture);
            }
            GUI.color = antes;
        }

        // ---------------- HUD (láminas 6 a 6D y 9C/9D) ----------------
        void DibujarHUD()
        {
            if (modoFoto || Event.current.type != EventType.Repaint) return;
            const float m = 48f, alto = 64f;
            float x = m, y = m;

            // Mira: un punto chico en el centro para apuntar a puertas y objetos.
            Caja(new Rect(AW / 2 - 3, AH / 2 - 3, 6, 6), ConAlfa(Blanco, HayInteraccion ? 0.9f : 0.35f));

            // Salud
            int seg = jugador.Segmentos();
            bool baja = seg <= 3 && seg > 0, cero = seg <= 0;
            float segAncho = 18f, segAlto = 26f, segGap = 4f;
            string rotulo = cero ? "SALUD 0" : (baja ? "SALUD BAJA" : "");
            var stRot = Estilo(false, 28, true, Blanco);
            float anchoRot = rotulo.Length > 0 ? AnchoTexto(stRot, rotulo) + 52f : 0f;
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
                Texto(new Rect(rx + 40, y, anchoRot - 40, alto), rotulo, stRot);
            }
            x = rSalud.xMax + 16;

            // Recursos
            string rec = (pantallaChica ? "" : "RECURSOS ") + inventario.Total().ToString("00");
            var stRec = Estilo(false, 30, true, Blanco);
            float anchoRec = AnchoTexto(stRec, rec);
            var rRec = new Rect(x, y, 58 + anchoRec + 20, alto);
            Caja(rRec, ConAlfa(Negro, 0.72f));
            Icono(new Rect(x + 14, y + (alto - 34) / 2, 32, 34), iconoMochila, Blanco);
            Texto(new Rect(x + 58, y, anchoRec + 8, alto), rec, stRec);
            x = rRec.xMax + 16;

            // Objetivo activo: punto azul + nombre (uno a la vez, sin minimapa ni flecha)
            var obj = progresion.Actual;
            if (obj != null)
            {
                string t = obj.titulo.ToUpperInvariant();
                var stObj = Estilo(false, 30, true, Blanco);
                float libre = AW - m - x - 48 - 20;
                float anchoObj = Mathf.Min(AnchoTexto(stObj, t), libre);
                if (anchoObj > 60f)
                {
                    var rObj = new Rect(x, y, 48 + anchoObj + 20, alto);
                    Caja(rObj, ConAlfa(Negro, 0.72f));
                    Caja(new Rect(x + 18, y + alto / 2 - 8, 16, 16), Azul);
                    Texto(new Rect(x + 48, y, anchoObj + 4, alto), t, stObj);
                }
            }

            // Panel "OBJETIVO ACTUAL" (Etapa 11, wireframe 04) cuando cambia el objetivo
            if (avisoObjetivo > 0f && obj != null)
            {
                float a = Mathf.Clamp01(avisoObjetivo);
                var r = new Rect(AW / 2 - 460, 160, 920, 236);
                Caja(r, ConAlfa(Panel, 0.92f * a));
                Marco(r, ConAlfa(Azul, a), 2f);
                Texto(new Rect(r.x + 32, r.y + 16, r.width - 64, 36), "OBJETIVO ACTUAL", Estilo(false, 28, true, ConAlfa(Gris, a)));
                Texto(new Rect(r.x + 32, r.y + 58, r.width - 64, 104), obj.descripcion, Estilo(false, 32, false, ConAlfa(Blanco, a), TextAnchor.UpperLeft, true));
                Texto(new Rect(r.x + 32, r.y + 176, 420, 40), "ZONA: " + obj.zona, Estilo(false, 28, false, ConAlfa(Gris, a)));
                Texto(new Rect(r.x + 472, r.y + 176, r.width - 504, 40), "DESTINO: " + obj.destino, Estilo(false, 28, false, ConAlfa(Gris, a)));
            }
            else if (avisoZona > 0f && !string.IsNullOrEmpty(nombreZona))
            {
                float a = Mathf.Clamp01(avisoZona);
                var st = Estilo(true, 44, true, ConAlfa(Blanco, a), TextAnchor.MiddleCenter);
                string z = nombreZona.ToUpperInvariant();
                float w = Mathf.Min(1200f, AnchoTexto(st, z) + 80f);
                var r = new Rect(AW / 2 - w / 2, 180, w, 70);
                Caja(r, ConAlfa(Negro, 0.6f * a));
                Texto(new Rect(r.x + 20, r.y, r.width - 40, r.height), z, st);
            }

            // Interacción: centrada abajo, solo cuando hay algo al alcance
            if (HayInteraccion && estado == Estado.Jugando)
            {
                string nombre, accion;
                if (puertaX >= 0)
                {
                    bool abierta = mundo.Celda(puertaX, puertaY) == 'd';
                    nombre = abierta ? "Puerta abierta" : "Puerta cerrada";
                    accion = abierta ? "CERRAR" : "ABRIR";
                }
                else
                {
                    nombre = objetivoInteraccion.def.tipo == TipoEntidad.Recurso
                        ? ContenidoJuego.Recurso(objetivoInteraccion.def.idRecurso).nombre
                        : objetivoInteraccion.def.nombre;
                    accion = objetivoInteraccion.def.tipo == TipoEntidad.Recurso ? "RECOGER" : "INTERACTUAR";
                }
                var st = Estilo(false, 32, true, Blanco, TextAnchor.MiddleCenter);
                string t = "[E]  " + accion;
                float w = AnchoTexto(st, t) + 64;
                var r = new Rect(AW / 2 - w / 2, AH - m - 64, w, 64);
                Caja(r, ConAlfa(Panel, 0.92f));
                Marco(r, Blanco, 1.5f);
                Texto(new Rect(r.x + 16, r.y, r.width - 32, r.height), t, st);
                if (!string.IsNullOrEmpty(nombre))
                {
                    var st2 = Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter);
                    float w2 = Mathf.Min(1000f, AnchoTexto(st2, nombre) + 48);
                    var r2 = new Rect(AW / 2 - w2 / 2, r.y - 54, w2, 44);
                    Caja(r2, ConAlfa(Negro, 0.7f));
                    Texto(new Rect(r2.x + 16, r2.y, r2.width - 32, r2.height), nombre, st2);
                }
            }
        }

        void DibujarMensajes()
        {
            if (mensajes.Count == 0 || Event.current.type != EventType.Repaint) return;
            var msj = mensajes[0];
            float a = Mathf.Clamp01(msj.tiempo * 3f);
            var st = Estilo(false, 30, false, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true);
            float w = Mathf.Min(1300f, AnchoTexto(st, msj.texto) + 64f);
            float h = Mathf.Min(220f, AltoTexto(st, msj.texto, w - 48f) + 28f);
            // Queda arriba del cartel de interacción (que ocupa hasta AH - 48 - 64 - 54).
            var r = new Rect(AW / 2 - w / 2, AH - 48 - 64 - 54 - 24 - h, w, h);
            if (estado == Estado.Dialogo) r.y = AH - 400 - h;
            else if (estado == Estado.Inventario) r.y = 890 + 50;
            Caja(r, ConAlfa(Negro, 0.78f * a));
            Texto(new Rect(r.x + 24, r.y + 6, r.width - 48, r.height - 12), msj.texto, st);
        }

        void DibujarAvisoCaptura()
        {
            string aviso = Capturas.AvisoVisible();
            if (aviso == null || Event.current.type != EventType.Repaint) return;
            var r = new Rect(AW - 820, AH - 96, 780, 56);
            Caja(r, ConAlfa(Negro, 0.85f));
            Texto(new Rect(r.x + 16, r.y, r.width - 32, r.height), aviso, Estilo(false, 26, false, Blanco, TextAnchor.MiddleCenter));
        }

        // ---------------- MENÚ PRINCIPAL (lámina 8) ----------------
        void DibujarMenuPrincipal()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(ConAlfa(Negro, 0.35f));
                CajaReal(new Rect(0, 0, S(new Rect(0, 0, 820, 1)).xMax, Screen.height), ConAlfa(Negro, 0.6f));
                Texto(new Rect(150, 140, 1200, 130), ContenidoJuego.NombreJuego, Estilo(true, 96, true, Blanco));
                Texto(new Rect(156, 262, 1200, 60), "V I E N T O S   D E   O C T U B R E", Estilo(true, 34, false, Azul));
                Texto(new Rect(156, AH - 96, 1600, 44), "BETA 1.1  ·  F12 captura  ·  F11 modo foto  ·  F9 modo desarrollador", Estilo(false, 28, false, Gris));
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
            var r = new Rect(AW / 2 - 460, AH / 2 - 210, 920, 420);
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(ConAlfa(Negro, 0.6f));
                Caja(r, Panel);
                Marco(r, ConAlfa(Blanco, 0.35f), 1f);
                Texto(new Rect(r.x + 40, r.y + 36, r.width - 80, 76), titulo, Estilo(true, 56, true, Blanco, TextAnchor.MiddleCenter));
                Texto(new Rect(r.x + 60, r.y + 128, r.width - 120, 130), texto, Estilo(false, 32, false, Blanco, TextAnchor.MiddleCenter, true));
            }
            var b0 = new Rect(r.x + 110, r.yMax - 120, 320, 72);
            var b1 = new Rect(r.xMax - 430, r.yMax - 120, 320, 72);
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
                Fondo(ConAlfa(Negro, 0.72f));
                Texto(new Rect(0, 170, AW, 80), "PAUSA", Estilo(true, 56, true, Blanco, TextAnchor.MiddleCenter));
                var obj = progresion.Actual;
                if (obj != null)
                {
                    Texto(new Rect(AW / 2 - 600, 720, 1200, 44), "OBJETIVO: " + obj.titulo.ToUpperInvariant(), Estilo(false, 28, true, Gris, TextAnchor.MiddleCenter));
                    Texto(new Rect(AW / 2 - 600, 770, 1200, 90), obj.descripcion, Estilo(false, 28, false, Gris, TextAnchor.UpperCenter, true));
                }
            }
            var ops = OpcionesPausa();
            for (int i = 0; i < ops.Length; i++)
            {
                var r = new Rect(AW / 2 - 260, 300 + i * 92, 520, 72);
                if (estado == Estado.Pausa && Encima(r)) selPausa = i;
                if (Boton(r, ops[i], true, selPausa == i && estado == Estado.Pausa, 300 + i) && estado == Estado.Pausa) Activar(i);
            }
        }

        // ---------------- OPCIONES (lámina 8C) ----------------
        void DibujarOpciones()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(ConAlfa(Negro, 0.8f));
                Texto(new Rect(0, 150, AW, 80), "OPCIONES", Estilo(true, 56, true, Blanco, TextAnchor.MiddleCenter));
            }
            string[] etiquetas = { "VOLUMEN", "PANTALLA COMPLETA", "RESOLUCIÓN" };
            for (int i = 0; i < 3; i++)
            {
                var fila = new Rect(AW / 2 - 500, 300 + i * 110, 1000, 84);
                if (Encima(fila)) selOpciones = i;
                bool sel = selOpciones == i;
                if (Event.current.type == EventType.Repaint)
                {
                    Caja(fila, sel ? BotonSel : BotonNormal);
                    if (sel) Marco(fila, Azul, 2f); else Marco(fila, ConAlfa(Blanco, 0.35f), 1f);
                    Texto(new Rect(fila.x + 30, fila.y, 440, fila.height), etiquetas[i], Estilo(true, 36, sel, Blanco));
                }
                var zona = new Rect(fila.x + 500, fila.y + 12, 470, 60);
                if (i == 0)
                {
                    var barra = new Rect(zona.x, zona.y + 24, 330, 12);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Caja(barra, ConAlfa(Gris, 0.35f));
                        Caja(new Rect(barra.x, barra.y, barra.width * volumen / 100f, barra.height), sel ? Azul : Blanco);
                        Caja(new Rect(barra.x + barra.width * volumen / 100f - 6, barra.y - 10, 12, 32), Blanco);
                        Texto(new Rect(barra.xMax + 28, zona.y, 100, 60), volumen.ToString(), Estilo(false, 32, true, Blanco));
                    }
                    var ev = Event.current;
                    var p = Raton();
                    if ((ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag) && ev.button == 0 &&
                        new Rect(barra.x - 10, zona.y, barra.width + 20, zona.height).Contains(p))
                    {
                        volumen = Mathf.Clamp(Mathf.RoundToInt((p.x - barra.x) / barra.width * 100f), 0, 100);
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
                    if (Boton(new Rect(zona.x, zona.y, 64, 60), "<", true, false, 402, 32)) CambiarOpcion(2, -1);
                    if (Event.current.type == EventType.Repaint)
                        Texto(new Rect(zona.x + 72, zona.y, 254, 60), res.x + " × " + res.y, Estilo(false, 32, true, Blanco, TextAnchor.MiddleCenter));
                    if (Boton(new Rect(zona.x + 334, zona.y, 64, 60), ">", true, false, 403, 32)) CambiarOpcion(2, 1);
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
                Texto(new Rect(AW / 2 - 800, 760, 1600, 44), nota, Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter));
                Texto(new Rect(AW / 2 - 800, 812, 1600, 44), "Mirar arriba y abajo: mouse, R / F o RePág / AvPág.", Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter));
            }
        }

        // ---------------- CRÉDITOS (lámina 8D) ----------------
        void DibujarCreditos()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(ConAlfa(Negro, origenCreditos == Estado.Final ? 1f : 0.88f));
                Texto(new Rect(0, 100, AW, 120), ContenidoJuego.NombreJuego, Estilo(true, 96, true, Blanco, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 215, AW, 50), "V I E N T O S   D E   O C T U B R E", Estilo(true, 34, false, Azul, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 320, AW, 50), "EQUIPO", Estilo(true, 36, true, Gris, TextAnchor.MiddleCenter));
                for (int i = 0; i < ContenidoJuego.Creditos.Length; i++)
                    Texto(new Rect(0, 384 + i * 58, AW, 50), ContenidoJuego.Creditos[i], Estilo(false, 32, false, Blanco, TextAnchor.MiddleCenter));
            }
            var r = new Rect(AW / 2 - 200, 720, 400, 72);
            if (Boton(r, "VOLVER", true, true, 500)) Activar(0);
        }

        // ---------------- INTRO ----------------
        void DibujarIntro()
        {
            if (Event.current.type != EventType.Repaint) return;
            Fondo(Negro);
            if (introIdx >= ContenidoJuego.Intro.Length) return;
            float a = Mathf.Clamp01(introTiempo * 1.5f) * Mathf.Clamp01((4.5f - introTiempo) * 1.5f);
            Texto(new Rect(160, AH / 2 - 160, AW - 320, 320), ContenidoJuego.Intro[introIdx],
                Estilo(introIdx == 0, introIdx == 0 ? 56 : 40, introIdx == 0, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true));
            Texto(new Rect(0, AH - 100, AW, 44), "[Enter] continuar   ·   [Esc] saltar", Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter));
        }

        // ---------------- DIÁLOGO ----------------
        void DibujarDialogo()
        {
            var r = new Rect(AW / 2 - 740, AH - 380, 1480, 330);
            if (Event.current.type == EventType.Repaint)
            {
                Caja(r, ConAlfa(Panel, 0.94f));
                Marco(r, Azul, 2f);
                Texto(new Rect(r.x + 40, r.y + 18, r.width - 80, 50), ContenidoJuego.Informante.nombre.ToUpperInvariant(), Estilo(true, 36, true, Azul));
                string linea = dialogoIdx < dialogo.Count ? dialogo[dialogoIdx] : "";
                // Con la decisión en pantalla, la línea ocupa menos alto para dejar lugar a la pregunta y a los botones.
                float altoLinea = decisionActiva ? 92f : 170f;
                Texto(new Rect(r.x + 40, r.y + 74, r.width - 80, altoLinea), "“" + linea + "”", Estilo(false, 32, false, Blanco, TextAnchor.UpperLeft, true));
                if (!decisionActiva)
                    Texto(new Rect(r.x + 40, r.yMax - 58, r.width - 80, 40), "[E] Continuar", Estilo(false, 28, false, Gris, TextAnchor.MiddleRight));
                else
                    Texto(new Rect(r.x + 40, r.y + 172, r.width - 80, 40), ContenidoJuego.DecisionPregunta, Estilo(false, 28, true, Gris));
            }
            if (decisionActiva)
            {
                float w = (r.width - 80 - 40) / 2f;
                if (Boton(new Rect(r.x + 40, r.yMax - 92, w, 68), "[1]  " + ContenidoJuego.DecisionDar.ToUpperInvariant(), true, false, 600, 32)) ElegirDecision(true);
                if (Boton(new Rect(r.x + 80 + w, r.yMax - 92, w, 68), "[2]  " + ContenidoJuego.DecisionNoDar.ToUpperInvariant(), true, false, 601, 32)) ElegirDecision(false);
            }
        }

        // ---------------- INVENTARIO (láminas 7, 7B, 7C) ----------------
        void DibujarInventario()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(ConAlfa(Negro, 0.85f));
                Texto(new Rect(0, 90, AW, 80), "INVENTARIO", Estilo(true, 56, true, Blanco, TextAnchor.MiddleCenter));
            }
            var izq = new Rect(180, 210, 680, 620);
            var der = new Rect(900, 210, 840, 620);
            if (Event.current.type == EventType.Repaint)
            {
                Caja(izq, Panel); Marco(izq, ConAlfa(Blanco, 0.25f), 1f);
                Caja(der, Panel); Marco(der, ConAlfa(Blanco, 0.25f), 1f);
                Texto(new Rect(izq.x + 30, izq.y + 16, 420, 52), "RECURSOS", Estilo(true, 36, true, Gris));
                Texto(new Rect(izq.xMax - 170, izq.y + 16, 140, 52), inventario.Total().ToString("00"), Estilo(true, 36, true, Gris, TextAnchor.MiddleRight));
                Texto(new Rect(der.x + 30, der.y + 16, der.width - 60, 52), "OBJETO SELECCIONADO", Estilo(true, 36, true, Gris));
            }
            for (int i = 0; i < 4; i++)
            {
                var cat = Inventario.Orden[i];
                var fila = new Rect(izq.x + 20, izq.y + 90 + i * 124, izq.width - 40, 100);
                bool sel = invCategoria == i;
                var ev = Event.current;
                if (ev.type == EventType.MouseDown && ev.button == 0 && fila.Contains(Raton())) { invCategoria = i; invObjeto = 0; audioJuego.Menu(); ev.Use(); }
                if (ev.type == EventType.Repaint)
                {
                    Caja(fila, sel ? BotonSel : BotonNormal);
                    if (sel) Marco(fila, Azul, 2f); else Marco(fila, ConAlfa(Blanco, 0.2f), 1f);
                    Texto(new Rect(fila.x + 24, fila.y, fila.width - 24 - 140, fila.height), Inventario.NombreCategoria(cat), Estilo(false, 32, sel, Blanco));
                    Texto(new Rect(fila.xMax - 130, fila.y, 106, fila.height), inventario.CantidadCategoria(cat).ToString("00"), Estilo(false, 32, true, Blanco, TextAnchor.MiddleRight));
                }
            }

            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count == 0)
            {
                if (visual != null) visual.ElegirVistaPrevia(null);
                if (Event.current.type == EventType.Repaint)
                    Texto(new Rect(der.x + 30, der.y + 80, der.width - 60, der.height - 160), "No hay objetos disponibles.", Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter));
            }
            else
            {
                invObjeto = Mathf.Clamp(invObjeto, 0, lista.Count - 1);
                var r = lista[invObjeto];
                if (visual != null) visual.ElegirVistaPrevia(r.sprite);
                if (Event.current.type == EventType.Repaint)
                {
                    // Vista previa: el modelo 3D del objeto girando.
                    var marcoIcono = new Rect(der.x + 30, der.y + 86, 190, 190);
                    Caja(marcoIcono, BotonNormal);
                    Marco(marcoIcono, ConAlfa(Blanco, 0.2f), 1f);
                    if (visual != null && visual.ImagenVistaPrevia != null)
                        Icono(new Rect(marcoIcono.x + 6, marcoIcono.y + 6, 178, 178), visual.ImagenVistaPrevia, Color.white);
                    float tx = der.x + 250, tw = der.xMax - 30 - tx;
                    Texto(new Rect(tx, der.y + 90, tw, 64), r.nombre.ToUpperInvariant(), Estilo(true, 44, true, Blanco));
                    Texto(new Rect(tx, der.y + 160, tw, 96), r.tipo + " · " + r.utilidad, Estilo(false, 28, false, Gris, TextAnchor.UpperLeft, true));
                    Texto(new Rect(der.x + 30, der.y + 296, der.width - 60, 150), r.descripcion, Estilo(false, 30, false, Blanco, TextAnchor.UpperLeft, true));
                    Texto(new Rect(der.x + 30, der.y + 456, 420, 50), "CANTIDAD: " + inventario.Cantidad(r.id).ToString("00"), Estilo(false, 32, true, Blanco));
                    if (lista.Count > 1)
                        Texto(new Rect(der.xMax - 260, der.y + 456, 230, 50), (invObjeto + 1) + " / " + lista.Count, Estilo(false, 32, true, Azul, TextAnchor.MiddleRight));
                }
                if (Boton(new Rect(der.x + 30, der.yMax - 96, 320, 72), "UTILIZAR", r.curacion > 0, r.curacion > 0, 700)) UtilizarSeleccionado();
                if (lista.Count > 1 && Boton(new Rect(der.xMax - 230, der.yMax - 96, 90, 72), "<", true, false, 701)) invObjeto = (invObjeto + lista.Count - 1) % lista.Count;
                if (lista.Count > 1 && Boton(new Rect(der.xMax - 120, der.yMax - 96, 90, 72), ">", true, false, 702)) invObjeto = (invObjeto + 1) % lista.Count;
            }

            if (Event.current.type == EventType.Repaint)
                Texto(new Rect(AW / 2 - 800, 856, 1600, 44), "[W/S] categoría   ·   [A/D] objeto   ·   [E] utilizar   ·   [Esc] volver", Estilo(false, 28, false, Gris, TextAnchor.MiddleCenter));
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
                    var st = Estilo(false, 32, false, ConAlfa(Blanco, a), TextAnchor.MiddleCenter, true);
                    var r = new Rect(AW / 2 - 680, AH - 210, 1360, 100);
                    Caja(r, ConAlfa(Negro, 0.6f * a));
                    Texto(new Rect(r.x + 32, r.y + 8, r.width - 64, r.height - 16), "A lo lejos, entre la tormenta de nieve, se distingue la silueta del Obelisco.", st);
                }
            }
            else if (finalFase == 1)
            {
                Fondo(Color.black);
                float a1 = Mathf.Clamp01(finalTiempo / 1.2f), a2 = Mathf.Clamp01((finalTiempo - 1.5f) / 1.2f), a3 = Mathf.Clamp01((finalTiempo - 3f) / 1.2f);
                float salida = Mathf.Clamp01((7.5f - finalTiempo) / 1f);
                Texto(new Rect(0, AH / 2 - 270, AW, 120), ContenidoJuego.NombreJuego, Estilo(true, 96, true, ConAlfa(Blanco, a1 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 - 150, AW, 50), "V I E N T O S   D E   O C T U B R E", Estilo(true, 34, false, ConAlfa(Azul, a1 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 - 40, AW, 80), "FIN DEL PRÓLOGO", Estilo(true, 56, true, ConAlfa(Blanco, a2 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(0, AH / 2 + 56, AW, 50), "FRÍO.  AISLAMIENTO.  SUPERVIVENCIA.", Estilo(false, 28, true, ConAlfa(Gris, a2 * salida), TextAnchor.MiddleCenter));
                Texto(new Rect(160, AH / 2 + 140, AW - 320, 110), ContenidoJuego.FraseFinal, Estilo(false, 36, false, ConAlfa(Blanco, a3 * salida), TextAnchor.UpperCenter, true));
            }
        }

        // ---------------- MODO DESARROLLADOR (Etapa 5, rol tester interno) ----------------
        void DibujarDesarrollador()
        {
            if (Event.current.type != EventType.Repaint) return;
            var r = new Rect(AW - 740, 140, 700, 600);
            Caja(r, ConAlfa(Negro, 0.8f));
            Marco(r, Rojo, 2f);
            var st = Estilo(false, 24, false, Blanco, TextAnchor.UpperLeft, true);
            var esc = ContenidoJuego.EscenarioEn(jugador.x, jugador.y);
            var obj = progresion.Actual;
            string info =
                "MODO DESARROLLADOR (F9)\n" +
                "FPS: " + Mathf.RoundToInt(fps) + "   Render: " + (visual != null ? visual.Ancho + "×" + visual.Alto : "-") + "\n" +
                "Posición: " + jugador.x.ToString("0.0") + ", " + jugador.y.ToString("0.0") + "   Ángulo: " + Mathf.RoundToInt(Mathf.Repeat(jugador.angulo * Mathf.Rad2Deg, 360f)) + "°   Mirada: " + Mathf.RoundToInt(jugador.inclinacion) + "°\n" +
                "Zona: " + esc.nombre + " (id " + esc.id + ")   Bajo techo: " + (jugador.bajoTecho ? "sí" : "no") + "\n" +
                "Salud: " + jugador.vida.ToString("0") + "   Exposición: " + Mathf.RoundToInt(jugador.exposicion * 100) + "%\n" +
                "Objetivo: " + (obj != null ? obj.id + " - " + obj.titulo : "ninguno") + "\n" +
                "Invulnerable: " + (jugador.invulnerable ? "sí" : "no") + "\n\n" +
                "F2 invulnerable   F3 completar objetivo\n" +
                "F4 ir a la siguiente postal\n" +
                "F5 +1 de cada recurso   F6 cambiar salud\n" +
                "F7 saltar al puente   F8 efecto VHS\n" +
                "F11 modo foto   F12 captura";
            Texto(new Rect(r.x + 24, r.y + 18, r.width - 48, r.height - 36), info, st);
        }
    }
}
