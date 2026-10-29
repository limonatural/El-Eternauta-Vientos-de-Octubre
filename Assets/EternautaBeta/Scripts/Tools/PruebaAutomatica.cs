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
    // compilado, abriendo el .exe con el argumento -prueba. Recorre el prólogo y el capítulo 2 usando
    // las mismas funciones del juego (cajones, mesa de trabajo, ascensor, combate, mapa, hotbar, radio,
    // diálogos, inventario, pausa, muerte, guardado, CONTINUAR y final), saca una captura de cada pantalla y escribe un informe
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
                new KeyValuePair<string, Func<IEnumerator>>("Departamento: pistola, cajones y traje", PasoDepartamento),
                new KeyValuePair<string, Func<IEnumerator>>("Ascensor y salida al exterior", PasoAscensor),
                new KeyValuePair<string, Func<IEnumerator>>("Comida e informante", PasoComidaYInformante),
                new KeyValuePair<string, Func<IEnumerator>>("Combate contra los bichos", PasoCombate),
                new KeyValuePair<string, Func<IEnumerator>>("Mapa, hotbar e inventario", PasoMapaHotbarInventario),
                new KeyValuePair<string, Func<IEnumerator>>("Pausa", PasoPausa),
                new KeyValuePair<string, Func<IEnumerator>>("Muerte y despertar", PasoMuerte),
                new KeyValuePair<string, Func<IEnumerator>>("Guardado y CONTINUAR", PasoGuardado),
                new KeyValuePair<string, Func<IEnumerator>>("Final del prólogo y capítulo 2 desbloqueado", PasoFinal),
                new KeyValuePair<string, Func<IEnumerator>>("Capítulo 2: radio, puente y sargento", PasoCapitulo2),
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
            Comprobar("El juego empieza en el departamento", jugador.y >= mundo.datos.salidaBajaY, "y = " + jugador.y);
            Comprobar("Hay entre 4 y 10 bichos en Avellaneda", bichosTotalesMapa >= 4 && bichosTotalesMapa <= 10, "Bichos: " + bichosTotalesMapa);
            yield return Captura("hud_objetivo_actual");
        }

        IEnumerator PasoDepartamento()
        {
            yield return UsarEntidad(100, "Mirar el cuadro familiar", "cuadro_familiar");
            Comprobar("El cuadro nombra a la familia", mensajes.Count > 0 && mensajes[mensajes.Count - 1].texto.Contains("Celeste"));
            yield return UsarEntidad(111, "Abrir el cajón de la mesa de luz", "cajon_pistola");
            Comprobar("La Glock 19 está en el cajón con 15 + 5 balas",
                inventario.Cantidad(ContenidoJuego.IdPistola) == 1 && jugador.cargador == 15 && inventario.Cantidad(ContenidoJuego.IdMunicion) == 5,
                "Cargador " + jugador.cargador + ", repuesto " + inventario.Cantidad(ContenidoJuego.IdMunicion));
            Comprobar("La pistola queda en la hotbar", inventario.hotbar[0] == ContenidoJuego.IdPistola);
            yield return Esperar(0.4f);
            yield return Captura("hud_pistola");
            yield return UsarEntidad(111, "Revisar el cajón otra vez");
            Comprobar("Un cajón revisado no da nada más", inventario.Cantidad(ContenidoJuego.IdPistola) == 1);

            yield return UsarEntidad(114, "Abrir el cajón del taller");
            yield return UsarEntidad(115, "Abrir el cajón de herramientas");
            Comprobar("Hay materiales para el traje", inventario.CantidadCategoria(Categoria.Materiales) >= 2);

            yield return UsarEntidad(109, "Ascensor sin traje");
            Comprobar("Sin traje el ascensor no sale al exterior", corteT < 0f);

            yield return UsarEntidad(116, "Usar la mesa de trabajo");
            Comprobar("Objetivo 1 completado: traje armado", jugador.traje && progresion.Completado(1));
            yield return Captura("traje_armado");
        }

        IEnumerator PasoAscensor()
        {
            yield return UsarEntidad(109, "Usar el ascensor");
            yield return Esperar(0.6f);
            yield return Captura("corte_ascensor");
            yield return Esperar(3.2f);
            var d = mundo.datos;
            Comprobar("El ascensor lleva al hall del edificio", corteT < 0f && Mathf.Abs(jugador.x - d.hallX) < 1f && Mathf.Abs(jugador.y - d.hallY) < 1f,
                "Posición " + jugador.x.ToString("0.0") + ", " + jugador.y.ToString("0.0"));
            yield return Captura("hall_edificio");
        }

        IEnumerator PasoComidaYInformante()
        {
            Comprobar("Segundo objetivo: buscar comida", progresion.EsActual(2));
            yield return UsarEntidad(230, "Abrir la heladera del kiosco");
            Comprobar("Objetivo 2 completado: comida", progresion.Completado(2));
            yield return Captura("heladera_abierta");

            Comprobar("Tercer objetivo: hablar con el sobreviviente", progresion.EsActual(3));
            yield return UsarEntidad(229, "Hablar con el Informante");
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
            Comprobar("Objetivo 3 completado y se recibe el mapa", progresion.Completado(3) && inventario.Cantidad(ContenidoJuego.IdMapa) > 0);
            Comprobar("Cuarto objetivo: llegar al Puente Pueyrredón", progresion.EsActual(4));
        }

        IEnumerator PasoCombate()
        {
            jugador.x = 75.5f; jugador.y = 56f; jugador.angulo = Mathf.PI / 2f; jugador.inclinacion = 0f;
            SeleccionarRanura(0);
            tEnfriamiento = 0f;
            yield return Esperar(0.3f);
            var b = new Bicho(900, 75.5f, 60f, false, new System.Random(5));
            mundo.bichos.Add(b);
            visual.RecrearBichos();
            yield return Esperar(0.3f);
            yield return Captura("bicho_enfrente");
            int antes = jugador.cargador;
            for (int i = 0; i < 3; i++)
            {
                tEnfriamiento = 0f;
                b.x = 75.5f; b.y = 59f;     // sigue de frente al caño
                Atacar(ContenidoJuego.Arma(ContenidoJuego.IdPistola));
                yield return Esperar(0.1f);
            }
            Comprobar("Tres disparos matan a un cascarudo", !b.vivo, "Vida restante " + Mathf.RoundToInt(b.vida));
            Comprobar("Cada disparo gasta una bala", jugador.cargador == antes - 3, "Cargador " + jugador.cargador);
            yield return Captura("bicho_muerto");

            inventario.Agregar(ContenidoJuego.IdCuchillo, 1);
            inventario.hotbar[1] = ContenidoJuego.IdCuchillo;
            SeleccionarRanura(1);
            var b2 = new Bicho(901, 75.5f, 57.2f, false, new System.Random(6));
            mundo.bichos.Add(b2);
            visual.RecrearBichos();
            int golpes = 0;
            while (b2.vivo && golpes < 10)
            {
                tEnfriamiento = 0f;
                b2.x = 75.5f; b2.y = 57.2f;
                Atacar(ContenidoJuego.Arma(ContenidoJuego.IdCuchillo));
                golpes++;
                yield return null;
            }
            Comprobar("El cuchillo necesita 4 golpes (no 3)", golpes == 4, "Golpes: " + golpes);

            inventario.Agregar(ContenidoJuego.IdMunicion, 20);
            SeleccionarRanura(0);
            jugador.cargador = 3;
            IntentarRecargar();
            yield return Esperar(TiempoRecarga + 0.5f);
            Comprobar("R recarga el cargador", jugador.cargador == ContenidoJuego.CargadorCapacidad, "Cargador " + jugador.cargador);

            jugador.invulnerable = false;
            float v = jugador.vida;
            jugador.Herir(ContenidoJuego.DanioBicho, "prueba");
            Comprobar("Una mordida quita el 20 % de la salud", Mathf.Abs((v - jugador.vida) - 20f) < 0.01f, "Salud " + Mathf.RoundToInt(v) + " → " + Mathf.RoundToInt(jugador.vida));
            jugador.vida = 100f;
            jugador.invulnerable = true;

            float t0 = Time.realtimeSinceStartup;
            yield return Esperar(0.2f);
            // el cadáver se va solo a los 15 segundos
            b.tMuerte = Bicho.TiempoCadaver + 1f;
            yield return Esperar(0.3f);
            Comprobar("El cadáver desaparece a los 15 segundos", b.tMuerte > Bicho.TiempoCadaver);
        }

        IEnumerator PasoMapaHotbarInventario()
        {
            AbrirMapa();
            Comprobar("La tecla M abre el mapa", estado == Estado.Mapa);
            yield return Esperar(0.4f);
            yield return Captura("mapa");
            estado = Estado.Jugando;
            Comprobar("El mapa descubierto tiene zonas vistas", DescubiertoDe(1).v.Length > 0 && System.Array.IndexOf(DescubiertoDe(1).v, true) >= 0);

            AbrirInventario(Estado.Jugando);
            Comprobar("Se abre el inventario", estado == Estado.Inventario);
            for (int i = 0; i < Inventario.Orden.Length; i++)
            {
                invCategoria = i; invObjeto = 0;
                yield return Esperar(0.6f);
                yield return Captura("inventario_" + Inventario.NombreCategoria(Inventario.Orden[i]).ToLowerInvariant().Replace(' ', '_'));
            }
            // asignar el mapa a la ranura 4
            for (int i = 0; i < Inventario.Orden.Length; i++)
                foreach (var r in inventario.ObjetosDe(Inventario.Orden[i]))
                    if (r.id == ContenidoJuego.IdMapa) { invCategoria = i; invObjeto = inventario.ObjetosDe(Inventario.Orden[i]).IndexOf(r); }
            AsignarRanuraSeleccionado(3);
            Comprobar("El mapa se puede poner en la hotbar", inventario.hotbar[3] == ContenidoJuego.IdMapa);
            CerrarInventario();
            SeleccionarRanura(3);
            yield return Esperar(0.4f);
            yield return Captura("hotbar_mapa_en_mano");
            SeleccionarRanura(0);

            jugador.vida = 40f;
            inventario.Agregar(15, 1);
            inventario.hotbar[2] = 15;
            SeleccionarRanura(2);
            tEnfriamiento = 0f;
            UsarObjeto(ContenidoJuego.Recurso(15));
            Comprobar("Comer el objeto equipado cura", jugador.vida > 40f, "Salud " + Mathf.RoundToInt(jugador.vida));
            SeleccionarRanura(0);
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
            estado = Estado.Jugando;
            yield return null;
        }

        IEnumerator PasoMuerte()
        {
            jugador.invulnerable = false;
            jugador.Herir(100f, "Un cascarudo gigante te mordió hasta matarte.");
            yield return Esperar(0.3f);
            Comprobar("Con la salud en 0 aparece la pantalla de muerte", estado == Estado.Muerte, "Estado: " + estado);
            yield return Esperar(2f);
            yield return Captura("pantalla_muerte");
            ActivarMuerte(0);
            yield return Esperar(0.8f);
            yield return Captura("corte_mal_sueno");
            yield return Esperar(3.2f);
            var d = mundo.datos;
            Comprobar("Despertar deja al protagonista en la cama con salud completa",
                estado == Estado.Jugando && jugador.vida >= 99f && Mathf.Abs(jugador.x - d.camaX) < 0.5f && Mathf.Abs(jugador.y - d.camaY) < 0.5f,
                "Estado " + estado + ", salud " + Mathf.RoundToInt(jugador.vida));
            jugador.invulnerable = true;
        }

        IEnumerator PasoGuardado()
        {
            float x = jugador.x, y = jugador.y;
            Guardar();
            Comprobar("Se escribe el archivo de guardado (JSON)", File.Exists(SistemaGuardado.Ruta), SistemaGuardado.Ruta);
            var p = SistemaGuardado.Cargar();
            bool datos = p != null && p.traje_aislante && Mathf.Abs(p.pos_x - x) < 0.01f && Mathf.Abs(p.pos_y - y) < 0.01f &&
                         p.inventario.Exists(i => i.id_recurso == ContenidoJuego.IdMapa) && p.pistola_encontrada && p.contenedores_abiertos.Count > 0 &&
                         p.objetivos.Exists(o => o.id_objetivo == 3 && o.estado == "Completado");
            Comprobar("El JSON guarda posición, traje, inventario, contenedores y objetivos", datos);

            AbrirPausa();
            estado = Estado.ConfirmarSalir;
            Activar(0); // SÍ: volver al menú
            Comprobar("VOLVER AL MENÚ lleva al menú principal", estado == Estado.MenuPrincipal);
            Comprobar("CONTINUAR queda habilitado", Habilitada(Estado.MenuPrincipal, 1));
            selMenu = 1;
            yield return Captura("menu_continuar_habilitado");

            // Se pierde todo lo de la memoria y se carga desde el archivo
            ReiniciarMundo(1, NuevaSemilla());
            Activar(1); // CONTINUAR
            bool recuperada = estado == Estado.Jugando && jugador.traje && Mathf.Abs(jugador.x - x) < 0.01f &&
                              Mathf.Abs(jugador.y - y) < 0.01f && inventario.Cantidad(ContenidoJuego.IdMapa) > 0 && progresion.EsActual(4) &&
                              pistolaEncontrada && mundo.BuscarEntidad(111).abierto;
            Comprobar("CONTINUAR recupera la partida guardada", recuperada, "Estado: " + estado);
            jugador.invulnerable = true;
            yield return Esperar(1f);
            yield return Captura("partida_continuada");
        }

        IEnumerator PasoFinal()
        {
            var d = mundo.datos;
            jugador.x = d.llegadaX; jugador.y = d.llegadaY + 3f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            estado = Estado.Jugando;
            yield return Esperar(0.5f);
            yield return Captura("acceso_puente");
            float limite = Time.realtimeSinceStartup + 6f;
            while (estado == Estado.Jugando && Time.realtimeSinceStartup < limite)
            {
                jugador.y -= 0.03f; // camina hacia el norte
                yield return null;
            }
            Comprobar("Al llegar al acceso del puente empieza el final", estado == Estado.Final, "Estado: " + estado);
            yield return Esperar(5f);
            yield return Captura("final_obelisco");
            limite = Time.realtimeSinceStartup + 40f;
            while (estado == Estado.Final && Time.realtimeSinceStartup < limite) yield return null;
            Comprobar("En lugar de los créditos se desbloquea el capítulo 2", estado == Estado.Capitulo2Aviso && cap2Desbloqueado, "Estado: " + estado);
            Comprobar("Objetivo 4 completado", progresion.Completado(4));
            yield return Esperar(0.5f);
            yield return Captura("capitulo2_desbloqueado");
            estado = Estado.Creditos; origenCreditos = Estado.MenuPrincipal;
            yield return Captura("creditos_fin_de_beta");
            estado = Estado.Capitulo2Aviso;
            SeguirCapitulo2();
            Comprobar("Sigue el capítulo 2: encontrar la radio", progresion.EsActual(5) && estado == Estado.Jugando);
        }

        IEnumerator PasoCapitulo2()
        {
            yield return UsarEntidad(212, "Encender la radio", "radio");
            Comprobar("La radio se enciende y completa el objetivo", radioTiempo >= 0f && progresion.Completado(5));
            yield return Esperar(6f);
            yield return Captura("radio_voz");
            var e = mundo.BuscarEntidad(212);
            objetivoInteraccion = e; puertaX = puertaY = -1;
            Interactuar();
            Comprobar("La transmisión se puede saltear", radioTiempo < 0f);
            Comprobar("Sexto objetivo: cruzar el puente", progresion.EsActual(6));

            var d = mundo.datos;
            jugador.x = d.llegadaX; jugador.y = d.llegadaY + 1f; jugador.angulo = -90f * Mathf.Deg2Rad;
            yield return Esperar(0.8f);
            yield return Captura("corte_puente");
            yield return Esperar(4f);
            Comprobar("Cruzar el puente lleva al mapa de la 9 de Julio", mundo.datos.id == 2 && ContenidoJuego.MapaActual == 2, "Mapa " + mundo.datos.id);
            Comprobar("Hay entre 12 y 16 bichos del lado de Capital", bichosTotalesMapa >= 12 && bichosTotalesMapa <= 16, "Bichos: " + bichosTotalesMapa);
            jugador.invulnerable = true;
            yield return Captura("puente_pueyrredon");

            d = mundo.datos;
            jugador.x = d.llegadaX; jugador.y = d.llegadaY; jugador.angulo = -90f * Mathf.Deg2Rad;
            yield return Esperar(0.8f);
            Comprobar("Al final del puente se completa el cruce", progresion.Completado(6) && progresion.EsActual(7));
            yield return Captura("av_9_de_julio");
            for (int i = 0; i < MuertosNecesarios(); i++) bichosMuertos.Add(2000 + i);
            yield return Esperar(0.5f);
            Comprobar("Derrotar a los bichos completa 'Ayudar a los soldados'", progresion.Completado(7) && progresion.EsActual(8));

            yield return UsarEntidad(1310, "Hablar con el sargento");
            Comprobar("Se abre el diálogo con el sargento", estado == Estado.Dialogo);
            yield return Captura("dialogo_sargento");
            for (int guarda = 0; guarda < 20 && estado == Estado.Dialogo; guarda++)
            {
                AvanzarDialogo();
                yield return Esperar(0.2f);
            }
            Comprobar("Termina el capítulo 2 con CONTINUARÁ", estado == Estado.Continuara && progresion.Completado(8), "Estado: " + estado);
            yield return Esperar(4.5f);
            yield return Captura("continuara");
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
            sb.AppendLine("La prueba usa las mismas funciones del juego (cajones, mesa de trabajo, ascensor, combate, mapa, hotbar, radio,");
            sb.AppendLine("diálogos, inventario, pausa, muerte, guardado, CONTINUAR y final) y ubica al protagonista al lado de cada objeto en lugar de caminar hasta él.");
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
