using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    public enum Estado
    {
        MenuPrincipal, ConfirmarNueva, Opciones, Creditos, Intro,
        Jugando, Dialogo, Pausa, ConfirmarSalir, Inventario, Final
    }

    // Punto de entrada de la beta. Va en un GameObject vacío de la escena
    // "Eternauta_Beta" (o se crea solo desde el menú Eternauta > Crear escena Beta).
    // Arma todo por código: cámara, mundo, gráficos, audio e interfaz.
    public partial class EternautaGame : MonoBehaviour
    {
        [Header("Gráficos")]
        [Tooltip("Filas de la imagen interna (baja resolución estilo retro). 200 = Doom original.")]
        [Range(160, 480)] public int resolucionVertical = 240;
        [Tooltip("Campo visual vertical en grados.")]
        [Range(45f, 80f)] public float campoVisual = 58f;

        [Header("Controles")]
        public float sensibilidadMouse = 0.0035f;
        [Tooltip("Invertir el eje vertical del mouse al mirar arriba / abajo.")]
        public bool invertirMouseY = false;

        // Sistemas
        MundoVisual visual;
        Mundo mundo;
        Jugador jugador;
        Inventario inventario;
        Progresion progresion;
        AudioProcedural audioJuego;

        // Efectos de pantalla (se dibujan encima de la imagen 3D)
        float escarcha, alertaRoja, destello, fundido;
        bool efectoVHS = true;
        float ampBalanceo, faseBalanceo;

        public Estado estado = Estado.MenuPrincipal;
        Estado origenOpciones, origenCreditos, origenInventario;
        bool partidaEnCurso;
        bool hayPartidaGuardada;

        // Interacción
        Entidad objetivoInteraccion;
        int puertaX = -1, puertaY = -1;
        bool HayInteraccion { get { return objetivoInteraccion != null || puertaX >= 0; } }

        // Mensajes
        class Mensaje { public string texto; public float tiempo; }
        readonly List<Mensaje> mensajes = new List<Mensaje>();
        float avisoObjetivo;      // segundos que queda visible el panel "OBJETIVO ACTUAL"
        float avisoZona;
        string nombreZona = "";
        int zonaActual = -1;
        float avisoBloqueo;

        // Diálogo
        readonly List<string> dialogo = new List<string>();
        int dialogoIdx;
        bool decisionActiva;
        bool dialogoCompletaObjetivo;

        // Radio
        float radioTiempo = -1f;

        // Intro y final
        int introIdx;
        float introTiempo;
        int finalFase;
        float finalTiempo, finalAnguloInicial, finalInclinacionInicial;

        // Herramientas
        bool modoDesarrollador, modoFoto;
        int postalIdx = -1;
        float fps;

        // Menú de atracción (fondo del menú principal)
        float anguloAtraccion;

        // Opciones
        int volumen = 70;
        bool pantallaCompleta = true;
        int resolucionIdx;
        public static readonly Vector2Int[] Resoluciones = { new Vector2Int(1920, 1080), new Vector2Int(1366, 768), new Vector2Int(1024, 600) };

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (Camera.main == null)
            {
                var cam = new GameObject("Camara").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.cullingMask = 0;
                cam.gameObject.AddComponent<AudioListener>();
            }
            else if (FindAnyObjectByType<AudioListener>() == null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            audioJuego = new AudioProcedural(gameObject);
            CargarOpciones();
            ReiniciarMundo();
            visual = new MundoVisual(mundo, resolucionVertical);
            hayPartidaGuardada = SistemaGuardado.ExistePartida();
        }

        void OnDestroy()
        {
            if (visual != null) visual.Destruir();
        }

        void ReiniciarMundo()
        {
            mundo = new Mundo();
            if (visual != null) visual.Vincular(mundo);
            jugador = new Jugador { x = ContenidoJuego.InicioX, y = ContenidoJuego.InicioY, angulo = ContenidoJuego.InicioAngulo * Mathf.Deg2Rad };
            inventario = new Inventario();
            progresion = new Progresion();
            if (visual != null) visual.conTraje = false;
            mensajes.Clear();
            zonaActual = -1;
            radioTiempo = -1f;
        }

        // ------------------------------------------------------------------
        // BUCLE PRINCIPAL
        // ------------------------------------------------------------------
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.05f);

            TeclasGlobales();
            mouseMovido = Entrada.MouseMovido();

            switch (estado)
            {
                case Estado.MenuPrincipal: case Estado.ConfirmarNueva: case Estado.Opciones:
                case Estado.Creditos: case Estado.Pausa: case Estado.ConfirmarSalir:
                    NavegarMenu(); break;
                case Estado.Intro: ActualizarIntro(dt); break;
                case Estado.Jugando: ActualizarJuego(dt); break;
                case Estado.Dialogo: ActualizarDialogo(); break;
                case Estado.Inventario: ActualizarInventario(); break;
                case Estado.Final: ActualizarFinal(dt); break;
            }

            // Los mensajes se muestran de a uno; solo corre el reloj del primero de la cola.
            if (mensajes.Count > 0)
            {
                mensajes[0].tiempo -= dt;
                if (mensajes[0].tiempo <= 0f) mensajes.RemoveAt(0);
            }
            avisoObjetivo -= dt; avisoZona -= dt; avisoBloqueo -= dt;
            ActualizarRadio(dt);

            bool juegoVisible = partidaEnCurso && estado != Estado.MenuPrincipal && estado != Estado.ConfirmarNueva &&
                                !(estado == Estado.Opciones && origenOpciones == Estado.MenuPrincipal) &&
                                !(estado == Estado.Creditos && origenCreditos != Estado.Final) && estado != Estado.Intro;

            Cursor.lockState = estado == Estado.Jugando ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = estado != Estado.Jugando;

            // Dibujar el mundo (cámara 3D en baja resolución)
            visual.AjustarAspecto(Screen.width / (float)Mathf.Max(1, Screen.height));
            visual.campoVisual = campoVisual;
            visual.mostrarVistaPrevia = estado == Estado.Inventario;
            visual.radioEncendida = radioTiempo >= 0f;
            bool pausado = estado == Estado.Pausa || estado == Estado.Inventario || estado == Estado.ConfirmarSalir ||
                           (estado == Estado.Opciones && origenOpciones == Estado.Pausa);
            float dtMundo = pausado ? 0f : dt;
            destello = Mathf.MoveTowards(destello, 0f, dt * 3f);
            if (juegoVisible || estado == Estado.Final)
            {
                visual.mostrarManos = estado != Estado.Final && !modoFoto;
                escarcha = jugador.bajoTecho ? jugador.exposicion * 0.3f : Mathf.Clamp01(jugador.exposicion * 0.9f + (jugador.vida <= 0f ? 0.5f : 0f));
                alertaRoja = jugador.Segmentos() <= 3 && !modoFoto ? 0.25f + 0.2f * Mathf.Sin(Time.time * 3f) : 0f;
                if (estado != Estado.Final) fundido = 0f;
                visual.Actualizar(dtMundo, jugador.x, jugador.y, jugador.angulo, jugador.inclinacion, ampBalanceo, faseBalanceo,
                    estado == Estado.Jugando ? objetivoInteraccion : null, estado == Estado.Jugando ? puertaX : -1, puertaY);
            }
            else
            {
                // Fondo del menú: la plaza nevada girando despacio
                anguloAtraccion += dt * 0.12f;
                visual.mostrarManos = false;
                escarcha = 0.15f; alertaRoja = 0f; destello = 0f; fundido = 0f;
                visual.visibilidadObelisco = 0.3f;
                visual.Actualizar(dt, 15.5f, 27.5f, anguloAtraccion, 6f, 0f, 0f, null, -1, -1);
            }

            audioJuego.Actualizar(!juegoVisible && estado != Estado.Final, estado == Estado.Jugando, jugador.bajoTecho,
                jugador.caminando, Entrada.Mantenida(Tecla.Shift), radioTiempo >= 0f, jugador.Segmentos() <= 3, dt);
        }

        void TeclasGlobales()
        {
            if (Entrada.Pulsada(Tecla.F12)) Capturas.Sacar(this, nombreZona);
            if (Entrada.Pulsada(Tecla.F11)) modoFoto = !modoFoto;
            if (Entrada.Pulsada(Tecla.F9))
            {
                modoDesarrollador = !modoDesarrollador;
                Mostrar(modoDesarrollador ? "Modo desarrollador activado (F9)" : "Modo desarrollador desactivado", 2f);
            }
            if (!modoDesarrollador || !partidaEnCurso) return;

            if (Entrada.Pulsada(Tecla.F2))
            {
                jugador.invulnerable = !jugador.invulnerable;
                Mostrar(jugador.invulnerable ? "DEV: invulnerable" : "DEV: invulnerable desactivado", 2f);
            }
            if (Entrada.Pulsada(Tecla.F3) && progresion.Actual != null)
            {
                var o = progresion.Actual;
                if (o.id == 1) DarTraje();
                if (o.id == 3) { progresion.Activar(1); }
                if (o.id == 4) { IrAlPuente(); }
                else CompletarObjetivo(o.id);
            }
            if (Entrada.Pulsada(Tecla.F4))
            {
                postalIdx = (postalIdx + 1) % ContenidoJuego.Postales.Count;
                var p = ContenidoJuego.Postales[postalIdx];
                jugador.x = p.x; jugador.y = p.y; jugador.angulo = p.angulo * Mathf.Deg2Rad; jugador.inclinacion = 0f;
                AbrirTodasLasPuertas();
                if (!jugador.traje) DarTraje();
                Mostrar("DEV: " + p.nombre, 2f);
            }
            if (Entrada.Pulsada(Tecla.F5))
            {
                foreach (var r in ContenidoJuego.Recursos.Values) inventario.Agregar(r.id, 1);
                Mostrar("DEV: +1 de cada recurso", 2f);
            }
            if (Entrada.Pulsada(Tecla.F6))
            {
                jugador.vida = jugador.vida > 50f ? 25f : (jugador.vida > 0f ? 0f : 100f);
                Mostrar("DEV: salud " + Mathf.RoundToInt(jugador.vida), 2f);
            }
            if (Entrada.Pulsada(Tecla.F7)) IrAlPuente();
            if (Entrada.Pulsada(Tecla.F8))
            {
                efectoVHS = !efectoVHS;
                Mostrar(efectoVHS ? "DEV: efecto VHS activado" : "DEV: efecto VHS desactivado", 2f);
            }
        }

        void IrAlPuente()
        {
            if (!jugador.traje) DarTraje();
            while (progresion.Actual != null && progresion.Actual.id != 4) progresion.indiceObjetivo++;
            AbrirTodasLasPuertas();
            jugador.x = 15.5f; jugador.y = 7.5f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            if (estado != Estado.Jugando) estado = Estado.Jugando;
            Mostrar("DEV: saltaste al Puente Pueyrredón", 2f);
        }

        void AbrirTodasLasPuertas()
        {
            for (int y = 0; y < mundo.alto; y++)
                for (int x = 0; x < mundo.ancho; x++)
                    if (mundo.Celda(x, y) == 'D') mundo.AbrirPuerta(x, y);
        }

        // ------------------------------------------------------------------
        // JUEGO
        // ------------------------------------------------------------------
        void ActualizarJuego(float dt)
        {
            if (Entrada.Pulsada(Tecla.Escape)) { AbrirPausa(); return; }
            if (Entrada.Pulsada(Tecla.Tab) || Entrada.Pulsada(Tecla.I)) { AbrirInventario(Estado.Jugando); return; }

            float avance = 0f, lateral = 0f, giro = 0f;
            if (Entrada.Mantenida(Tecla.W) || Entrada.Mantenida(Tecla.Arriba)) avance += 1f;
            if (Entrada.Mantenida(Tecla.S) || Entrada.Mantenida(Tecla.Abajo)) avance -= 1f;
            if (Entrada.Mantenida(Tecla.D)) lateral += 1f;
            if (Entrada.Mantenida(Tecla.A)) lateral -= 1f;
            if (Entrada.Mantenida(Tecla.Derecha)) giro += Jugador.VelGiro * dt;
            if (Entrada.Mantenida(Tecla.Izquierda)) giro -= Jugador.VelGiro * dt;
            giro += Entrada.MouseX() * sensibilidadMouse;
            // Mirar arriba / abajo: mouse, R / F o Re Pág / Av Pág
            float mirar = Entrada.MouseY() * sensibilidadMouse * Mathf.Rad2Deg * (invertirMouseY ? -1f : 1f);
            if (Entrada.Mantenida(Tecla.R) || Entrada.Mantenida(Tecla.RePag)) mirar += 75f * dt;
            if (Entrada.Mantenida(Tecla.F) || Entrada.Mantenida(Tecla.AvPag)) mirar -= 75f * dt;

            var obj = progresion.Actual;
            float limite = (obj == null || obj.id == 4) ? -1f : ContenidoJuego.LimiteNortePuente;
            bool estabaAdentro = jugador.bajoTecho;
            jugador.Actualizar(mundo, avance, lateral, giro, mirar, Entrada.Mantenida(Tecla.Shift), dt, limite);

            ampBalanceo = Mathf.MoveTowards(ampBalanceo, jugador.caminando ? 1f : 0f, dt * 4f);
            if (jugador.caminando) faseBalanceo += dt * (Entrada.Mantenida(Tecla.Shift) ? 9f : 6.5f);

            if (jugador.bloqueadoNorte && avisoBloqueo <= 0f)
            {
                Mostrar("Sin saber qué hay del otro lado, cruzar es un suicidio. Primero buscá información.", 3f);
                avisoBloqueo = 4f;
            }

            // Eventos por posición
            if (estabaAdentro && !jugador.bajoTecho && !progresion.Activado(4))
            {
                progresion.Activar(4);
                Mostrar("La nieve cae sin parar. Con el traje puesto, no te detengas.", 4f);
            }
            var esc = ContenidoJuego.EscenarioEn(jugador.x, jugador.y);
            if (esc.id != zonaActual)
            {
                zonaActual = esc.id;
                nombreZona = esc.nombre;
                avisoZona = 2.5f;
                if (esc.id == 1 && progresion.Activar(6))
                    Mostrar("El Puente Pueyrredón. Del otro lado del Riachuelo, Capital.", 4f);
            }
            if (obj != null && obj.id == 4 && jugador.y < ContenidoJuego.LineaFinal) { IniciarFinal(); return; }

            BuscarInteraccion();
            if (HayInteraccion && (Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio) || Entrada.Pulsada(Tecla.Enter)))
                Interactuar();

            if (jugador.vida <= 0f && avisoBloqueo <= 0f)
            {
                Mostrar("Estás congelándote. Usá un medicamento o buscá un techo.", 3f);
                avisoBloqueo = 5f;
            }
        }

        void BuscarInteraccion()
        {
            objetivoInteraccion = null;
            puertaX = puertaY = -1;
            float dirX = Mathf.Cos(jugador.angulo), dirY = Mathf.Sin(jugador.angulo);
            float mejor = 1.45f;
            foreach (var e in mundo.entidades)
            {
                if (!e.activa || e.def.tipo == TipoEntidad.Decoracion) continue;
                float dx = e.x - jugador.x, dy = e.y - jugador.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > mejor || d < 0.001f) continue;
                if ((dx * dirX + dy * dirY) / d < 0.55f) continue;
                mejor = d;
                objetivoInteraccion = e;
            }
            if (objetivoInteraccion != null) return;
            // Puertas: abiertas o cerradas, siempre se pueden usar (no si estás parado en el marco)
            int px0 = Mathf.FloorToInt(jugador.x), py0 = Mathf.FloorToInt(jugador.y);
            foreach (float alcance in new[] { 0.3f, 0.6f, 0.9f, 1.25f })
            {
                int cx = Mathf.FloorToInt(jugador.x + dirX * alcance), cy = Mathf.FloorToInt(jugador.y + dirY * alcance);
                char c = mundo.Celda(cx, cy);
                if (Mundo.EsPuerta(c))
                {
                    if (cx == px0 && cy == py0) continue;
                    puertaX = cx; puertaY = cy; return;
                }
                if (Mundo.EsPared(c)) return;
            }
        }

        void Interactuar()
        {
            if (puertaX >= 0)
            {
                if (mundo.Celda(puertaX, puertaY) == 'd')
                {
                    // Cerrar: solo si el protagonista no quedó en el marco
                    if (TocaCelda(puertaX, puertaY))
                    {
                        audioJuego.Error();
                        Mostrar("Salí del marco de la puerta para poder cerrarla.", 2f);
                        return;
                    }
                    mundo.CerrarPuerta(puertaX, puertaY);
                    audioJuego.Puerta();
                    return;
                }
                bool esRefugio = puertaX == 15 && puertaY == 31;
                if (esRefugio && !jugador.traje)
                {
                    audioJuego.Error();
                    Mostrar("Afuera la nieve mata. Necesitás el traje aislante para salir.", 3f);
                    return;
                }
                mundo.AbrirPuerta(puertaX, puertaY);
                audioJuego.Puerta();
                return;
            }

            var e = objetivoInteraccion;
            switch (e.def.tipo)
            {
                case TipoEntidad.Recurso:
                {
                    var r = ContenidoJuego.Recurso(e.def.idRecurso);
                    inventario.Agregar(r.id, e.def.cantidad);
                    e.activa = false;
                    destello = 1f;
                    audioJuego.Recoger();
                    Mostrar("Recogiste: " + r.nombre + " (+" + e.def.cantidad + ")", 2.5f);
                    if (e.def.completaObjetivo != 0) CompletarObjetivo(e.def.completaObjetivo);
                    if (progresion.EsActual(1) && inventario.CantidadCategoria(Categoria.Materiales) >= 2)
                        Mostrar("Ya tenés materiales suficientes. Usá la mesa de trabajo del taller.", 3.5f);
                    break;
                }
                case TipoEntidad.MesaTrabajo:
                    if (jugador.traje) { Mostrar("Ya armaste el traje aislante.", 2f); break; }
                    if (inventario.CantidadCategoria(Categoria.Materiales) < 2)
                    {
                        audioJuego.Error();
                        Mostrar("Para el traje aislante necesitás 2 materiales (tenés " + inventario.CantidadCategoria(Categoria.Materiales) + ").", 3f);
                        break;
                    }
                    inventario.ConsumirMateriales(2);
                    DarTraje();
                    audioJuego.Traje();
                    Mostrar("Armaste el traje aislante. Ahora podés salir a la nieve.", 3.5f);
                    CompletarObjetivo(1);
                    break;
                case TipoEntidad.Radio:
                    progresion.Activar(2);
                    radioTiempo = 0f;
                    break;
                case TipoEntidad.Personaje:
                    IniciarDialogo();
                    break;
            }
        }

        bool TocaCelda(int cx, int cy)
        {
            float r = Jugador.Radio + 0.02f;
            return jugador.x + r > cx && jugador.x - r < cx + 1 && jugador.y + r > cy && jugador.y - r < cy + 1;
        }

        void DarTraje()
        {
            jugador.traje = true;
            if (visual != null) visual.conTraje = true;
            progresion.Activar(3);
        }

        void CompletarObjetivo(int id)
        {
            var o = progresion.Completar(id);
            if (o == null) return;
            Mostrar("Objetivo completado: " + o.titulo, 3f);
            if (progresion.Actual != null) avisoObjetivo = 6f;
            Guardar();
        }

        void ActualizarRadio(float dt)
        {
            if (radioTiempo < 0f) return;
            float antes = radioTiempo;
            radioTiempo += dt;
            for (int i = 0; i < ContenidoJuego.Radio.Length; i++)
            {
                float t = i * ContenidoJuego.RadioIntervalo;
                if (antes <= t && radioTiempo > t)
                {
                    // las líneas se reemplazan entre sí: la última se corta de golpe
                    mensajes.RemoveAll(m => m.texto.StartsWith("RADIO: "));
                    Mostrar("RADIO: " + ContenidoJuego.Radio[i], ContenidoJuego.RadioIntervalo - 0.15f, true);
                }
            }
            float fin = (ContenidoJuego.Radio.Length - 1) * ContenidoJuego.RadioIntervalo + ContenidoJuego.RadioUltimaLinea;
            if (radioTiempo > fin)
            {
                // Se corta la señal en plena advertencia
                radioTiempo = -1f;
                mensajes.RemoveAll(m => m.texto.StartsWith("RADIO: "));
                audioJuego.Error();
                Mostrar(ContenidoJuego.RadioCorte, 4f, true);
            }
        }

        public void Mostrar(string texto, float segundos, bool alFrente = false)
        {
            var m = new Mensaje { texto = texto, tiempo = segundos };
            if (alFrente) mensajes.Insert(0, m); else mensajes.Add(m);
            while (mensajes.Count > 4) mensajes.RemoveAt(mensajes.Count - 1);
        }

        // ------------------------------------------------------------------
        // DIÁLOGOS (RF07)
        // ------------------------------------------------------------------
        void IniciarDialogo()
        {
            dialogo.Clear();
            dialogoIdx = 0;
            decisionActiva = false;
            dialogoCompletaObjetivo = false;
            if (progresion.Completado(3))
            {
                dialogo.Add(ContenidoJuego.DialogosInformante[ContenidoJuego.DialogosInformante.Count - 1].texto);
            }
            else if (!progresion.EsActual(3))
            {
                dialogo.Add("Estás pálido. Primero conseguí algo para comer... después hablamos. Yo no me muevo de acá.");
            }
            else
            {
                progresion.Activar(1);
                for (int i = 0; i < 4; i++) dialogo.Add(ContenidoJuego.DialogosInformante[i].texto);
                dialogoCompletaObjetivo = true;
            }
            estado = Estado.Dialogo;
        }

        void ActualizarDialogo()
        {
            if (decisionActiva)
            {
                if (Entrada.Pulsada(Tecla.Uno)) ElegirDecision(true);
                else if (Entrada.Pulsada(Tecla.Dos)) ElegirDecision(false);
                return;
            }
            if (Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.Espacio))
                AvanzarDialogo();
            if (Entrada.Pulsada(Tecla.Escape) && !dialogoCompletaObjetivo) estado = Estado.Jugando;
        }

        void AvanzarDialogo()
        {
            audioJuego.Menu();
            dialogoIdx++;
            if (dialogoIdx < dialogo.Count) return;
            if (dialogoCompletaObjetivo && dialogo.Count == 4)
            {
                // Decisión simple (Etapa 4): darle o no un medicamento
                if (inventario.Cantidad(1) > 0) { decisionActiva = true; dialogoIdx = dialogo.Count - 1; return; }
                dialogo.Add(ContenidoJuego.RespuestaSinMedicamento);
                dialogo.Add(ContenidoJuego.DialogosInformante[4].texto);
                return;
            }
            estado = Estado.Jugando;
            if (dialogoCompletaObjetivo)
            {
                dialogoCompletaObjetivo = false;
                CompletarObjetivo(3);
            }
        }

        public void ElegirDecision(bool dar)
        {
            decisionActiva = false;
            audioJuego.Menu();
            if (dar)
            {
                inventario.Quitar(1, 1);
                inventario.Agregar(9, 1);
                progresion.Activar(5);
                dialogo.Add(ContenidoJuego.RespuestaDar);
                Mostrar("Recibiste: Mapa de Avellaneda", 3f);
            }
            else dialogo.Add(ContenidoJuego.RespuestaNoDar);
            dialogo.Add(ContenidoJuego.DialogosInformante[4].texto);
            dialogoIdx = dialogo.Count - 2;
        }

        // ------------------------------------------------------------------
        // INVENTARIO (Etapa 12, láminas 7 a 7C)
        // ------------------------------------------------------------------
        int invCategoria, invObjeto;

        void AbrirInventario(Estado origen)
        {
            origenInventario = origen;
            estado = Estado.Inventario;
            invObjeto = 0;
            audioJuego.Menu();
        }

        void ActualizarInventario()
        {
            if (Entrada.Pulsada(Tecla.W) || Entrada.Pulsada(Tecla.Arriba)) { invCategoria = (invCategoria + 3) % 4; invObjeto = 0; audioJuego.Menu(); }
            if (Entrada.Pulsada(Tecla.S) || Entrada.Pulsada(Tecla.Abajo)) { invCategoria = (invCategoria + 1) % 4; invObjeto = 0; audioJuego.Menu(); }
            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count > 0)
            {
                if (Entrada.Pulsada(Tecla.D) || Entrada.Pulsada(Tecla.Derecha)) { invObjeto = (invObjeto + 1) % lista.Count; audioJuego.Menu(); }
                if (Entrada.Pulsada(Tecla.A) || Entrada.Pulsada(Tecla.Izquierda)) { invObjeto = (invObjeto + lista.Count - 1) % lista.Count; audioJuego.Menu(); }
                invObjeto = Mathf.Clamp(invObjeto, 0, lista.Count - 1);
                if (Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Enter)) UtilizarSeleccionado();
            }
            if (Entrada.Pulsada(Tecla.Escape) || Entrada.Pulsada(Tecla.Tab) || Entrada.Pulsada(Tecla.I)) CerrarInventario();
        }

        public void UtilizarSeleccionado()
        {
            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count == 0) return;
            var r = lista[Mathf.Clamp(invObjeto, 0, lista.Count - 1)];
            if (r.curacion <= 0) { audioJuego.Error(); return; }
            inventario.Quitar(r.id, 1);
            jugador.Curar(r.curacion);
            audioJuego.Recoger();
            Mostrar("Usaste: " + r.nombre + " (+" + r.curacion + " de salud)", 2.5f);
        }

        void CerrarInventario()
        {
            estado = origenInventario == Estado.Pausa ? Estado.Pausa : Estado.Jugando;
            audioJuego.Menu();
        }

        // ------------------------------------------------------------------
        // INTRO Y SECUENCIA FINAL (RF13, RF14 / Etapa 12, lámina 8E)
        // ------------------------------------------------------------------
        void ActualizarIntro(float dt)
        {
            introTiempo += dt;
            bool saltar = Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio);
            if (introTiempo > 4.5f || saltar)
            {
                introIdx++;
                introTiempo = 0f;
                if (introIdx >= ContenidoJuego.Intro.Length || Entrada.Pulsada(Tecla.Escape))
                {
                    estado = Estado.Jugando;
                    avisoObjetivo = 7f;
                }
            }
            if (Entrada.Pulsada(Tecla.Escape)) { estado = Estado.Jugando; avisoObjetivo = 7f; }
        }

        void IniciarFinal()
        {
            CompletarObjetivo(4);
            progresion.Activar(7);
            Guardar();
            estado = Estado.Final;
            finalFase = 0;
            finalTiempo = 0f;
            finalAnguloInicial = jugador.angulo;
            finalInclinacionInicial = jugador.inclinacion;
            mensajes.Clear();
        }

        void ActualizarFinal(float dt)
        {
            finalTiempo += dt;
            if (modoDesarrollador && Entrada.Pulsada(Tecla.Enter)) finalTiempo += 4f;
            if (finalFase == 0)
            {
                // El protagonista se detiene y observa el Obelisco a la distancia
                float t = Mathf.Clamp01(finalTiempo / 4f);
                float objetivo = -Mathf.PI / 2f;
                float delta = Mathf.DeltaAngle(finalAnguloInicial * Mathf.Rad2Deg, objetivo * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                jugador.angulo = finalAnguloInicial + delta * Mathf.SmoothStep(0f, 1f, t);
                jugador.inclinacion = Mathf.Lerp(finalInclinacionInicial, 7f, Mathf.SmoothStep(0f, 1f, t));
                jugador.x = Mathf.MoveTowards(jugador.x, 15.5f, dt * 0.5f);
                if (jugador.y > 5.4f) jugador.y -= dt * 0.15f;
                ampBalanceo = 0f;
                visual.visibilidadObelisco = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(finalTiempo / 6f));
                fundido = Mathf.Clamp01((finalTiempo - 9f) / 2.5f);
                if (finalTiempo > 11.5f) { finalFase = 1; finalTiempo = 0f; }
            }
            else if (finalFase == 1)
            {
                fundido = 1f;
                if (finalTiempo > 7.5f) { finalFase = 2; finalTiempo = 0f; }
            }
            else
            {
                origenCreditos = Estado.Final;
                estado = Estado.Creditos;
                selCreditos = 0;
                partidaEnCurso = false;
                visual.visibilidadObelisco = 0.3f;
            }
        }

        // ------------------------------------------------------------------
        // GUARDADO (Etapas 13 y 14)
        // ------------------------------------------------------------------
        void Guardar()
        {
            var p = new PartidaGuardada
            {
                vida = Mathf.RoundToInt(jugador.vida),
                progreso = progresion.PorcentajeProgreso(),
                id_escenario_actual = ContenidoJuego.EscenarioEn(jugador.x, jugador.y).id,
                pos_x = jugador.x, pos_y = jugador.y,
                angulo = jugador.angulo * Mathf.Rad2Deg,
                inclinacion = jugador.inclinacion,
                traje_aislante = jugador.traje,
                inventario = inventario.Exportar(),
                objetivos = progresion.ExportarObjetivos(),
                eventos = progresion.ExportarEventos(),
            };
            foreach (var e in mundo.entidades) if (!e.activa) p.entidades_retiradas.Add(e.def.id);
            p.puertas_abiertas.AddRange(mundo.puertasAbiertas);
            try
            {
                SistemaGuardado.Guardar(p);
                hayPartidaGuardada = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Eternauta] No se pudo guardar: " + ex.Message);
            }
        }

        bool CargarPartida()
        {
            var p = SistemaGuardado.Cargar();
            if (p == null) return false;
            ReiniciarMundo();
            jugador.x = p.pos_x; jugador.y = p.pos_y;
            jugador.angulo = p.angulo * Mathf.Deg2Rad;
            jugador.inclinacion = Mathf.Clamp(p.inclinacion, -Jugador.InclinacionMax, Jugador.InclinacionMax);
            jugador.vida = Mathf.Clamp(p.vida, 0, 100);
            if (p.traje_aislante) DarTraje();
            inventario.Importar(p.inventario);
            progresion.Importar(p.objetivos, p.eventos);
            foreach (int id in p.entidades_retiradas) { var e = mundo.BuscarEntidad(id); if (e != null) e.activa = false; }
            foreach (var clave in p.puertas_abiertas)
            {
                var partes = clave.Split(',');
                int x, y;
                if (partes.Length == 2 && int.TryParse(partes[0], out x) && int.TryParse(partes[1], out y)) mundo.AbrirPuerta(x, y);
            }
            return true;
        }

        // ------------------------------------------------------------------
        // OPCIONES (Etapa 12, lámina 8C)
        // ------------------------------------------------------------------
        void CargarOpciones()
        {
            volumen = PlayerPrefs.GetInt("eternauta_volumen", 70);
            pantallaCompleta = PlayerPrefs.GetInt("eternauta_pantalla_completa", Screen.fullScreen ? 1 : 0) == 1;
            resolucionIdx = Mathf.Clamp(PlayerPrefs.GetInt("eternauta_resolucion", 0), 0, Resoluciones.Length - 1);
            AudioListener.volume = volumen / 100f;
        }

        void AplicarOpciones()
        {
            AudioListener.volume = volumen / 100f;
            PlayerPrefs.SetInt("eternauta_volumen", volumen);
            PlayerPrefs.SetInt("eternauta_pantalla_completa", pantallaCompleta ? 1 : 0);
            PlayerPrefs.SetInt("eternauta_resolucion", resolucionIdx);
            PlayerPrefs.Save();
#if !UNITY_EDITOR
            var r = Resoluciones[resolucionIdx];
            Screen.SetResolution(r.x, r.y, pantallaCompleta ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
        }

        // Para la herramienta de capturas.
        public string NombreZonaActual { get { return nombreZona; } }
    }
}
