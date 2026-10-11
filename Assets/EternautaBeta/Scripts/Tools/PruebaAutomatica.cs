using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Eternauta.Beta
{
    // Prueba automática de la beta 1.1 (testing de la versión entregada y verificación de ERR-01).
    //
    // Se ejecuta desde el menú de Unity "Eternauta > Ejecutar prueba automática" o, en el juego
    // compilado, abriendo el .exe con el argumento -prueba. Recorre el prólogo completo usando
    // las mismas funciones del juego (recoger, mesa de trabajo, puertas, radio, diálogo, inventario,
    // pausa, guardado, CONTINUAR y final), saca una captura de cada pantalla y escribe un informe
    // en Capturas/Pruebas/<fecha>_<resolución>/INFORME_PRUEBAS.md.
    //
    // Para ERR-01 revisa, en cada cuadro dibujado durante la prueba, todos los textos de la interfaz:
    // que entren en su caja, que no se superpongan con otro texto, su tamaño real en píxeles y
    // si la consola mostró avisos de fuentes ("Unable to load font face").
    public partial class EternautaGame
    {
        public const string ClavePrueba = "EternautaBeta_prueba_automatica";

        class ResultadoPrueba { public string nombre; public bool ok; public string detalle; }

        class TextoDibujado { public Rect area; public string texto; public bool entra; public int tam; }

        readonly List<ResultadoPrueba> resultados = new List<ResultadoPrueba>();
        readonly List<string> capturasPrueba = new List<string>();
        List<TextoDibujado> registroTextos;           // distinto de null mientras corre la prueba
        string pantallaPrueba = "";
        string carpetaPrueba;
        string resumenPrueba;                          // se muestra arriba de la pantalla al terminar
        bool pruebaEnCurso;

        // Resultados de ERR-01
        readonly HashSet<string> textosAnalizados = new HashSet<string>();
        readonly HashSet<string> desbordes = new HashSet<string>();
        readonly HashSet<string> superposiciones = new HashSet<string>();
        readonly HashSet<string> textosChicos = new HashSet<string>();
        readonly List<string> avisosFuente = new List<string>();
        readonly List<string> erroresConsola = new List<string>();
        int tamMinimoVisto = int.MaxValue;
        int cuadrosAnalizados;

        void Start()
        {
            bool pedida = false;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(ClavePrueba, false))
            {
                UnityEditor.SessionState.SetBool(ClavePrueba, false);
                pedida = true;
            }
#endif
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-prueba") pedida = true;
            if (pedida) StartCoroutine(EjecutarPrueba());
        }

        // ------------------------------------------------------------------
        // REGISTRO DE TEXTOS (lo llama Texto() en EternautaGameUI)
        // ------------------------------------------------------------------
        void RegistrarTexto(Rect rs, string s, GUIStyle st, GUIContent c, bool entra, int tam, float alfa)
        {
            if (registroTextos == null || fondo || alfa < 0.1f || Event.current == null || Event.current.type != EventType.Repaint) return;
            Vector2 medida = st.wordWrap ? new Vector2(rs.width, st.CalcHeight(c, rs.width)) : st.CalcSize(c);
            float x = rs.x, y = rs.y;
            switch (st.alignment)
            {
                case TextAnchor.UpperCenter: case TextAnchor.MiddleCenter: case TextAnchor.LowerCenter:
                    x += (rs.width - medida.x) / 2f; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight:
                    x = rs.xMax - medida.x; break;
            }
            switch (st.alignment)
            {
                case TextAnchor.MiddleLeft: case TextAnchor.MiddleCenter: case TextAnchor.MiddleRight:
                    y += (rs.height - medida.y) / 2f; break;
                case TextAnchor.LowerLeft: case TextAnchor.LowerCenter: case TextAnchor.LowerRight:
                    y = rs.yMax - medida.y; break;
            }
            registroTextos.Add(new TextoDibujado { area = new Rect(x, y, medida.x, medida.y), texto = s, entra = entra, tam = tam });
        }

        // Se llama al final de cada OnGUI de dibujo mientras corre la prueba.
        void AnalizarTextos()
        {
            // Solo dentro de OnGUI: fuera de OnGUI no hay Event.current.
            if (registroTextos == null || Event.current == null || Event.current.type != EventType.Repaint) return;
            cuadrosAnalizados++;
            string p = (string.IsNullOrEmpty(pantallaPrueba) ? "" : pantallaPrueba + " / ") + estado;
            for (int i = 0; i < registroTextos.Count; i++)
            {
                var a = registroTextos[i];
                textosAnalizados.Add(p + "|" + a.texto);
                tamMinimoVisto = Mathf.Min(tamMinimoVisto, a.tam);
                if (!a.entra) desbordes.Add(p + ": \"" + Corto(a.texto) + "\" no entra en su caja (" + a.tam + " px)");
                if (a.tam < 14) textosChicos.Add(p + ": \"" + Corto(a.texto) + "\" quedó en " + a.tam + " px");
                for (int j = i + 1; j < registroTextos.Count; j++)
                {
                    var b = registroTextos[j];
                    float w = Mathf.Min(a.area.xMax, b.area.xMax) - Mathf.Max(a.area.x, b.area.x);
                    float h = Mathf.Min(a.area.yMax, b.area.yMax) - Mathf.Max(a.area.y, b.area.y);
                    if (w > 2f && h > 2f)
                        superposiciones.Add(p + ": \"" + Corto(a.texto) + "\" se cruza con \"" + Corto(b.texto) + "\"");
                }
            }
            registroTextos.Clear();
        }

        static string Corto(string s)
        {
            s = s.Replace("\n", " ");
            return s.Length > 50 ? s.Substring(0, 47) + "..." : s;
        }

        void AlRecibirLog(string mensaje, string pila, LogType tipo)
        {
            if (mensaje.IndexOf("font face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                mensaje.IndexOf("Include Font Data", StringComparison.OrdinalIgnoreCase) >= 0)
                avisosFuente.Add(mensaje);
            else if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
                erroresConsola.Add(mensaje);
        }

        // Cartel con el resultado, arriba de todo (no forma parte de las capturas de la prueba).
        void DibujarResumenPrueba()
        {
            if (Event.current.type != EventType.Repaint) return;
            string t = pruebaEnCurso ? "PRUEBA AUTOMÁTICA EN CURSO: " + pantallaPrueba : resumenPrueba;
            if (string.IsNullOrEmpty(t) || (pruebaEnCurso && capturando)) return;
            var r = new Rect(AW / 2 - 700, 8, 1400, 40);
            Caja(r, ConAlfa(Negro, 0.85f));
            Marco(r, pruebaEnCurso ? Azul : Rojo, 1.5f);
            var reg = registroTextos;
            registroTextos = null; // el cartel de la prueba no se analiza
            Texto(new Rect(r.x + 16, r.y, r.width - 32, r.height), t, Estilo(false, 24, true, Blanco, TextAnchor.MiddleCenter));
            registroTextos = reg;
        }

        // ------------------------------------------------------------------
        // RECORRIDO
        // ------------------------------------------------------------------
        bool capturando;

        void Comprobar(string nombre, bool ok, string detalle = "")
        {
            resultados.Add(new ResultadoPrueba { nombre = nombre, ok = ok, detalle = detalle });
            Debug.Log("[Eternauta][Prueba] " + (ok ? "OK    " : "FALLA ") + nombre + (string.IsNullOrEmpty(detalle) ? "" : " - " + detalle));
        }

        IEnumerator Esperar(float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (Time.realtimeSinceStartup < fin) yield return null;
        }

        IEnumerator Captura(string nombre)
        {
            pantallaPrueba = nombre;
            capturando = true;
            yield return null;
            yield return null;
            string archivo = (capturasPrueba.Count + 1).ToString("00") + "_" + nombre + ".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(carpetaPrueba, archivo));
            capturasPrueba.Add(archivo);
            yield return null;
            yield return null;
            capturando = false;
        }

        // Pone al protagonista al lado de un objeto, mirándolo, en un lugar libre del mismo ambiente.
        bool Acercar(Entidad e)
        {
            foreach (float dist in new[] { 0.8f, 1.0f, 0.6f })
                for (int k = 0; k < 16; k++)
                {
                    float a = k * Mathf.PI / 8f;
                    float px = e.x + Mathf.Cos(a) * dist, py = e.y + Mathf.Sin(a) * dist;
                    if (!LugarLibre(px, py) || !LineaLibre(px, py, e.x, e.y)) continue;
                    jugador.x = px; jugador.y = py;
                    jugador.angulo = Mathf.Atan2(e.y - py, e.x - px);
                    jugador.inclinacion = -12f;
                    return true;
                }
            return false;
        }

        bool LugarLibre(float px, float py)
        {
            float r = Jugador.Radio + 0.03f;
            if (mundo.SolidaPunto(px, py) || mundo.SolidaPunto(px - r, py - r) || mundo.SolidaPunto(px + r, py - r) ||
                mundo.SolidaPunto(px - r, py + r) || mundo.SolidaPunto(px + r, py + r)) return false;
            foreach (var o in mundo.entidades)
            {
                if (!o.activa || !o.def.solido) continue;
                float dx = o.x - px, dy = o.y - py;
                if (dx * dx + dy * dy < (o.def.radio + r) * (o.def.radio + r)) return false;
            }
            return true;
        }

        bool LineaLibre(float x0, float y0, float x1, float y1)
        {
            for (int i = 1; i < 8; i++)
            {
                float t = i / 8f;
                if (mundo.Solida(Mathf.FloorToInt(Mathf.Lerp(x0, x1, t)), Mathf.FloorToInt(Mathf.Lerp(y0, y1, t)))) return false;
            }
            return true;
        }

        // Se acerca al objeto, espera que el juego lo detecte como interactuable y lo usa.
        IEnumerator UsarEntidad(int id, string prueba, string captura = null)
        {
            var e = mundo.BuscarEntidad(id);
            if (e == null) { Comprobar(prueba, false, "No existe el objeto " + id); yield break; }
            if (!Acercar(e)) Comprobar(prueba + " (acercarse)", false, "No se encontró un lugar libre al lado de " + id);
            yield return Esperar(0.5f);
            bool detectado = objetivoInteraccion == e;
            if (!detectado) Comprobar(prueba + " (detección)", false, "El juego no marcó el objeto como interactuable");
            if (captura != null) yield return Captura(captura);
            objetivoInteraccion = e;
            puertaX = puertaY = -1;
            Interactuar();
            yield return Esperar(0.3f);
        }

        IEnumerator EjecutarPrueba()
        {
            pruebaEnCurso = true;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            carpetaPrueba = Path.Combine(Capturas.Carpeta, "Pruebas", stamp + "_" + (Application.isEditor ? "editor" : "exe") + "_" + Screen.width + "x" + Screen.height);
            Directory.CreateDirectory(carpetaPrueba);
            Application.logMessageReceived += AlRecibirLog;
            registroTextos = new List<TextoDibujado>();
            float inicio = Time.realtimeSinceStartup;

            // La prueba usa su propia partida: la partida guardada del jugador se respalda y se repone al final.
            string ruta = SistemaGuardado.Ruta;
            string respaldo = File.Exists(ruta) ? File.ReadAllText(ruta) : null;

            var pasos = new List<KeyValuePair<string, Func<IEnumerator>>>
            {
                new KeyValuePair<string, Func<IEnumerator>>("Apertura y menús", PasoApertura),
                new KeyValuePair<string, Func<IEnumerator>>("Nueva partida", PasoNuevaPartida),
                new KeyValuePair<string, Func<IEnumerator>>("Objetivo 1: traje y puertas", PasoTraje),
                new KeyValuePair<string, Func<IEnumerator>>("Objetivo 2: comida y radio", PasoComidaYRadio),
                new KeyValuePair<string, Func<IEnumerator>>("Objetivo 3: Informante", PasoInformante),
                new KeyValuePair<string, Func<IEnumerator>>("Inventario", PasoInventario),
                new KeyValuePair<string, Func<IEnumerator>>("Pausa", PasoPausa),
                new KeyValuePair<string, Func<IEnumerator>>("Guardado y CONTINUAR", PasoGuardado),
                new KeyValuePair<string, Func<IEnumerator>>("Final del prólogo", PasoFinal),
            };
            foreach (var paso in pasos)
            {
                // Corre cada paso atrapando excepciones: si uno falla, la prueba sigue con el próximo.
                IEnumerator cuerpo = null;
                try { cuerpo = paso.Value(); }
                catch (Exception ex) { Comprobar(paso.Key, false, "Excepción: " + ex.Message); }
                while (cuerpo != null)
                {
                    object actual;
                    try
                    {
                        if (!cuerpo.MoveNext()) break;
                        actual = cuerpo.Current;
                    }
                    catch (Exception ex)
                    {
                        Comprobar(paso.Key, false, "Excepción: " + ex.Message);
                        break;
                    }
                    yield return actual;
                }
            }

            // Fin: se repone la partida guardada del jugador y se vuelve al menú.
            pantallaPrueba = "fin";
            yield return null;
            registroTextos = null;
            Application.logMessageReceived -= AlRecibirLog;
            try
            {
                if (respaldo != null) File.WriteAllText(ruta, respaldo);
                else SistemaGuardado.Borrar();
            }
            catch (Exception ex) { Debug.LogWarning("[Eternauta] No se pudo reponer la partida guardada: " + ex.Message); }
            jugador.invulnerable = false;
            string informe = Path.Combine(carpetaPrueba, "INFORME_PRUEBAS.md");
            try
            {
                VolverAlMenu();
                informe = EscribirInforme(Time.realtimeSinceStartup - inicio);
            }
            catch (Exception ex) { Debug.LogError("[Eternauta] La prueba no pudo terminar el informe: " + ex); }
            int ok = 0;
            foreach (var r in resultados) if (r.ok) ok++;
            bool err01 = Err01Resuelto();
            resumenPrueba = "PRUEBA TERMINADA: " + ok + " de " + resultados.Count + " comprobaciones OK  ·  ERR-01 " +
                            (err01 ? "RESUELTO" : "PENDIENTE") + "  ·  Informe en Capturas/Pruebas";
            pruebaEnCurso = false;
            Debug.Log("[Eternauta][Prueba] " + resumenPrueba + "\n" + informe);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(informe);
#endif
        }

        bool Err01Resuelto()
        {
            return avisosFuente.Count == 0 && desbordes.Count == 0 && superposiciones.Count == 0 && textosChicos.Count == 0;
        }

        IEnumerator PasoApertura()
        {
            yield return Esperar(1.5f);
            Comprobar("La beta abre en el menú principal", estado == Estado.MenuPrincipal, "Estado: " + estado);
            Comprobar("El mundo 3D se dibuja", visual != null && visual.Imagen != null && visual.Imagen.IsCreated(),
                visual != null ? "Imagen interna " + visual.Ancho + "×" + visual.Alto : "sin MundoVisual");
            bool fuentes = fuenteAngosta != null && fuenteNormal != null &&
                           fuenteAngosta.name.Contains("Roboto") && fuenteNormal.name.Contains("Liberation");
            Comprobar("Se cargan las fuentes incluidas en el proyecto", fuentes,
                (fuenteAngosta != null ? fuenteAngosta.name : "null") + " / " + (fuenteNormal != null ? fuenteNormal.name : "null"));
            yield return Captura("menu_principal");
            origenOpciones = Estado.MenuPrincipal; estado = Estado.Opciones; selOpciones = 0;
            yield return Captura("opciones");
            origenCreditos = Estado.MenuPrincipal; estado = Estado.Creditos;
            yield return Captura("creditos");
            estado = Estado.MenuPrincipal;
            if (hayPartidaGuardada)
            {
                estado = Estado.ConfirmarNueva; selConfirmar = 0;
                yield return Captura("confirmar_nueva_partida");
                estado = Estado.MenuPrincipal;
            }
        }

        IEnumerator PasoNuevaPartida()
        {
            NuevaPartida();
            Comprobar("NUEVA PARTIDA empieza con la introducción", estado == Estado.Intro, "Estado: " + estado);
            yield return Esperar(1.2f);
            yield return Captura("intro");
            estado = Estado.Jugando;
            avisoObjetivo = 7f;
            jugador.invulnerable = true; // la prueba no mide el daño por frío
            yield return Esperar(1.2f);
            Comprobar("Primer objetivo: armar el traje", progresion.Actual != null && progresion.Actual.id == 1);
            yield return Captura("hud_objetivo_actual");
        }

        IEnumerator PasoTraje()
        {
            // Sin traje la puerta del refugio no se abre
            jugador.x = 15.5f; jugador.y = 32.45f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            yield return Esperar(0.5f);
            Comprobar("Se detecta la puerta del refugio", puertaX == 15 && puertaY == 31, "Puerta detectada: " + puertaX + "," + puertaY);
            puertaX = 15; puertaY = 31; objetivoInteraccion = null;
            Interactuar();
            Comprobar("Sin traje, la puerta del refugio no se abre", mundo.Celda(15, 31) == 'D');
            yield return Captura("puerta_sin_traje");

            yield return UsarEntidad(11, "Recoger lona", "recoger_objeto");
            Comprobar("Recoger lona", !mundo.BuscarEntidad(11).activa);
            yield return UsarEntidad(12, "Recoger alambre");
            Comprobar("Recoger alambre", !mundo.BuscarEntidad(12).activa);
            yield return UsarEntidad(13, "Recoger botiquín");
            Comprobar("Recoger botiquín", !mundo.BuscarEntidad(13).activa);
            yield return UsarEntidad(10, "Usar la mesa de trabajo");
            Comprobar("Objetivo 1 completado: traje armado", jugador.traje && progresion.Completado(1));
            yield return Captura("traje_armado");

            // Puerta: abrir, cerrar y volver a abrir
            jugador.x = 15.5f; jugador.y = 32.45f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            yield return Esperar(0.4f);
            puertaX = 15; puertaY = 31; objetivoInteraccion = null;
            Interactuar();
            Comprobar("Con traje, la puerta se abre", mundo.Celda(15, 31) == 'd');
            yield return Esperar(0.8f);
            yield return Captura("puerta_abierta");
            puertaX = 15; puertaY = 31; objetivoInteraccion = null;
            Interactuar();
            Comprobar("La puerta abierta se puede cerrar (no desaparece)", mundo.Celda(15, 31) == 'D');
            yield return Esperar(0.8f);
            yield return Captura("puerta_cerrada");
            puertaX = 15; puertaY = 31; objetivoInteraccion = null;
            Interactuar();
            Comprobar("La puerta se vuelve a abrir", mundo.Celda(15, 31) == 'd');
        }

        IEnumerator PasoComidaYRadio()
        {
            Comprobar("Segundo objetivo: buscar comida", progresion.EsActual(2));
            yield return UsarEntidad(21, "Recoger galletitas");
            yield return UsarEntidad(20, "Recoger lata de conserva", "casa_abandonada");
            Comprobar("Objetivo 2 completado: comida", progresion.Completado(2));

            yield return UsarEntidad(23, "Encender la radio");
            Comprobar("La radio se enciende", radioTiempo >= 0f);
            var lineas = new HashSet<string>();
            bool capturada = false, corte = false;
            float limite = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < limite)
            {
                foreach (var m in mensajes)
                    if (m.texto.StartsWith("RADIO: ")) lineas.Add(m.texto);
                if (!capturada && radioTiempo > 2f * ContenidoJuego.RadioIntervalo + 0.8f)
                {
                    capturada = true;
                    yield return Captura("radio_bichos");
                    continue;
                }
                if (radioTiempo < 0f)
                {
                    corte = mensajes.Count > 0 && mensajes[0].texto == ContenidoJuego.RadioCorte;
                    break;
                }
                yield return null;
            }
            Comprobar("La radio pasa todas sus líneas", lineas.Count == ContenidoJuego.Radio.Length,
                lineas.Count + " de " + ContenidoJuego.Radio.Length);
            bool mencion = false;
            foreach (var l in lineas) if (l.Contains("Obelisco") && l.Contains("atacados")) mencion = true;
            Comprobar("La radio avisa del ataque a los militares del Obelisco", mencion);
            Comprobar("La señal se corta en medio del aviso", corte);
            yield return Esperar(0.4f);
            yield return Captura("radio_corte");
        }

        IEnumerator PasoInformante()
        {
            Comprobar("Tercer objetivo: hablar con el sobreviviente", progresion.EsActual(3));
            yield return UsarEntidad(30, "Hablar con el Informante");
            Comprobar("Se abre el diálogo", estado == Estado.Dialogo, "Estado: " + estado);
            yield return Captura("dialogo");
            bool huboDecision = false;
            for (int guarda = 0; guarda < 20 && estado == Estado.Dialogo; guarda++)
            {
                if (decisionActiva)
                {
                    huboDecision = true;
                    yield return Captura("dialogo_decision");
                    ElegirDecision(true);
                }
                else AvanzarDialogo();
                yield return Esperar(0.25f);
            }
            Comprobar("Aparece la decisión de darle un medicamento", huboDecision);
            Comprobar("Objetivo 3 completado y se recibe el mapa", progresion.Completado(3) && inventario.Cantidad(9) > 0);
            Comprobar("Cuarto objetivo: llegar al Puente Pueyrredón", progresion.EsActual(4));
        }

        IEnumerator PasoInventario()
        {
            jugador.vida = 40f;
            AbrirInventario(Estado.Jugando);
            Comprobar("Se abre el inventario", estado == Estado.Inventario);
            for (int i = 0; i < 4; i++)
            {
                invCategoria = i; invObjeto = 0;
                yield return Esperar(0.6f);
                yield return Captura("inventario_" + Inventario.NombreCategoria(Inventario.Orden[i]).ToLowerInvariant().Replace(' ', '_'));
            }
            // Utilizar un recurso que cure
            int cat = -1;
            for (int i = 0; i < 4 && cat < 0; i++)
                foreach (var r in inventario.ObjetosDe(Inventario.Orden[i]))
                    if (r.curacion > 0) { cat = i; invObjeto = inventario.ObjetosDe(Inventario.Orden[i]).IndexOf(r); break; }
            if (cat >= 0)
            {
                invCategoria = cat;
                float antes = jugador.vida;
                int total = inventario.Total();
                UtilizarSeleccionado();
                Comprobar("UTILIZAR cura y descuenta el recurso", jugador.vida > antes && inventario.Total() == total - 1,
                    "Salud " + Mathf.RoundToInt(antes) + " → " + Mathf.RoundToInt(jugador.vida));
                yield return Esperar(0.4f);
                yield return Captura("inventario_utilizar");
            }
            else Comprobar("UTILIZAR cura y descuenta el recurso", false, "No había recursos curativos");
            CerrarInventario();
            Comprobar("Se cierra el inventario", estado == Estado.Jugando);
            jugador.vida = 25f;
            yield return Esperar(0.5f);
            yield return Captura("hud_salud_baja");
            jugador.vida = 100f;
        }

        IEnumerator PasoPausa()
        {
            AbrirPausa();
            Comprobar("Esc abre la pausa", estado == Estado.Pausa);
            yield return Captura("pausa");
            AbrirInventario(Estado.Pausa);
            yield return Esperar(0.3f);
            CerrarInventario();
            Comprobar("El inventario desde la pausa vuelve a la pausa", estado == Estado.Pausa);
            origenOpciones = Estado.Pausa; estado = Estado.Opciones;
            yield return Captura("pausa_opciones");
            estado = Estado.Pausa;
            estado = Estado.ConfirmarSalir; selSalir = 1;
            yield return Captura("pausa_confirmar_salir");
            estado = Estado.Pausa;
            Activar(0); // CONTINUAR
            Comprobar("CONTINUAR de la pausa vuelve al juego", estado == Estado.Jugando);
            yield return null;
        }

        IEnumerator PasoGuardado()
        {
            float x = jugador.x, y = jugador.y;
            Guardar();
            Comprobar("Se escribe el archivo de guardado (JSON)", File.Exists(SistemaGuardado.Ruta), SistemaGuardado.Ruta);
            var p = SistemaGuardado.Cargar();
            bool datos = p != null && p.traje_aislante && Mathf.Abs(p.pos_x - x) < 0.01f && Mathf.Abs(p.pos_y - y) < 0.01f &&
                         p.inventario.Exists(i => i.id_recurso == 9) &&
                         p.objetivos.Exists(o => o.id_objetivo == 3 && o.estado == "Completado");
            Comprobar("El JSON guarda posición, traje, inventario y objetivos", datos);

            AbrirPausa();
            estado = Estado.ConfirmarSalir;
            Activar(0); // SÍ: volver al menú
            Comprobar("VOLVER AL MENÚ lleva al menú principal", estado == Estado.MenuPrincipal);
            Comprobar("CONTINUAR queda habilitado", Habilitada(Estado.MenuPrincipal, 1));
            selMenu = 1;
            yield return Captura("menu_continuar_habilitado");

            // Se pierde todo lo de la memoria y se carga desde el archivo
            ReiniciarMundo();
            Activar(1); // CONTINUAR
            bool recuperada = estado == Estado.Jugando && jugador.traje && Mathf.Abs(jugador.x - x) < 0.01f &&
                              Mathf.Abs(jugador.y - y) < 0.01f && inventario.Cantidad(9) > 0 && progresion.EsActual(4) &&
                              mundo.Celda(15, 31) == 'd';
            Comprobar("CONTINUAR recupera la partida guardada", recuperada, "Estado: " + estado);
            yield return Esperar(1f);
            yield return Captura("partida_continuada");
        }

        IEnumerator PasoFinal()
        {
            jugador.x = 15.5f; jugador.y = 7.8f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            estado = Estado.Jugando;
            yield return Esperar(0.5f);
            yield return Captura("puente_pueyrredon");
            float limite = Time.realtimeSinceStartup + 10f;
            while (estado == Estado.Jugando && Time.realtimeSinceStartup < limite)
            {
                jugador.y -= 0.03f; // camina hacia el norte, por el puente
                yield return null;
            }
            Comprobar("Al cruzar la línea del puente empieza el final", estado == Estado.Final, "Estado: " + estado);
            yield return Esperar(5f);
            yield return Captura("final_obelisco");
            limite = Time.realtimeSinceStartup + 20f;
            while (estado == Estado.Final && finalFase == 0 && Time.realtimeSinceStartup < limite) yield return null;
            yield return Esperar(4.5f);
            yield return Captura("fin_del_prologo");
            limite = Time.realtimeSinceStartup + 20f;
            while (estado == Estado.Final && Time.realtimeSinceStartup < limite) yield return null;
            Comprobar("Termina en los créditos", estado == Estado.Creditos && origenCreditos == Estado.Final, "Estado: " + estado);
            Comprobar("Objetivo 4 completado y progreso 100 %", progresion.Completado(4) && progresion.PorcentajeProgreso() >= 99.9f,
                "Progreso " + Mathf.RoundToInt(progresion.PorcentajeProgreso()) + " %");
            yield return Esperar(0.5f);
            yield return Captura("creditos_final");
        }

        // ------------------------------------------------------------------
        // INFORME
        // ------------------------------------------------------------------
        string EscribirInforme(float segundos)
        {
            var sb = new StringBuilder();
            int ok = 0;
            foreach (var r in resultados) if (r.ok) ok++;
            sb.AppendLine("# Informe de pruebas: El Eternauta, Vientos de Octubre (beta " + Application.version + ", " + (Application.isEditor ? "Editor" : "ejecutable") + ")");
            sb.AppendLine();
            sb.AppendLine("- Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            sb.AppendLine("- Versión del juego: " + Application.version);
            sb.AppendLine("- Dónde se ejecutó: " + (Application.isEditor ? "Editor de Unity (modo Play)" : "ejecutable de Windows compilado (build " + Application.buildGUID + ")"));
            sb.AppendLine("- Unity: " + Application.unityVersion);
            sb.AppendLine("- Sistema: " + SystemInfo.operatingSystem + " · GPU: " + SystemInfo.graphicsDeviceName);
            sb.AppendLine("- Resolución probada: " + Screen.width + "×" + Screen.height);
            sb.AppendLine("- Duración: " + Mathf.RoundToInt(segundos) + " s");
            sb.AppendLine("- Resultado: **" + ok + " de " + resultados.Count + " comprobaciones OK**");
            sb.AppendLine();
            sb.AppendLine("## 1. Testing de la versión entregada");
            sb.AppendLine();
            sb.AppendLine("| # | Comprobación | Resultado | Detalle |");
            sb.AppendLine("|---|---|---|---|");
            for (int i = 0; i < resultados.Count; i++)
            {
                var r = resultados[i];
                sb.AppendLine("| " + (i + 1) + " | " + r.nombre + " | " + (r.ok ? "OK" : "**FALLA**") + " | " + r.detalle.Replace("|", "/") + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## 2. ERR-01: fuentes y textos");
            sb.AppendLine();
            sb.AppendLine("Estado: **" + (Err01Resuelto() ? "RESUELTO" : "PENDIENTE") + "** en " + Screen.width + "×" + Screen.height + ".");
            sb.AppendLine();
            sb.AppendLine("| Criterio | Resultado |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Avisos \"Unable to load font face\" en la consola | " + avisosFuente.Count + " |");
            sb.AppendLine("| Textos distintos revisados (en " + cuadrosAnalizados + " cuadros) | " + textosAnalizados.Count + " |");
            sb.AppendLine("| Textos que no entran en su caja | " + desbordes.Count + " |");
            sb.AppendLine("| Textos que se cruzan con otro texto | " + superposiciones.Count + " |");
            sb.AppendLine("| Textos de menos de 14 px reales | " + textosChicos.Count + " |");
            sb.AppendLine("| Tamaño de letra más chico usado | " + (tamMinimoVisto == int.MaxValue ? "-" : tamMinimoVisto + " px") + " |");
            sb.AppendLine("| Fuentes usadas | " + (fuenteAngosta != null ? fuenteAngosta.name : "-") + ", " + (fuenteNormal != null ? fuenteNormal.name : "-") + " |");
            Listar(sb, "Avisos de fuentes", avisosFuente);
            Listar(sb, "Textos que no entran", desbordes);
            Listar(sb, "Textos que se cruzan", superposiciones);
            Listar(sb, "Textos chicos", textosChicos);
            sb.AppendLine();
            sb.AppendLine("## 3. Errores en la consola durante la prueba");
            sb.AppendLine();
            sb.AppendLine(erroresConsola.Count == 0 ? "Ninguno." : erroresConsola.Count + " errores:");
            Listar(sb, null, erroresConsola);
            sb.AppendLine();
            sb.AppendLine("## 4. Capturas");
            sb.AppendLine();
            foreach (var c in capturasPrueba) sb.AppendLine("- `" + c + "`");
            sb.AppendLine();
            sb.AppendLine("La prueba usa las mismas funciones del juego (recoger, mesa de trabajo, puertas, radio, diálogo, inventario,");
            sb.AppendLine("pausa, guardado, CONTINUAR y final) y ubica al protagonista al lado de cada objeto en lugar de caminar hasta él.");
            sb.AppendLine("El recorrido caminando con teclado y mouse se prueba a mano (ver docs/beta/PRUEBAS_BETA_1.1.md).");

            string ruta = Path.Combine(carpetaPrueba, "INFORME_PRUEBAS.md");
            try { File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false)); }
            catch (Exception ex) { Debug.LogWarning("[Eternauta] No se pudo escribir el informe: " + ex.Message); }
            return ruta;
        }

        static void Listar(StringBuilder sb, string titulo, IEnumerable<string> items)
        {
            var lista = new List<string>(items);
            if (lista.Count == 0) return;
            sb.AppendLine();
            if (titulo != null) sb.AppendLine("**" + titulo + ":**");
            int n = 0;
            foreach (var s in lista)
            {
                if (++n > 40) { sb.AppendLine("- ... y " + (lista.Count - 40) + " más"); break; }
                sb.AppendLine("- " + s.Replace("\n", " ").Replace("|", "/"));
            }
        }
    }
}
