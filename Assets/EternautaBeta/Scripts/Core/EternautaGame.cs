using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    public enum Estado
    {
        MenuPrincipal, ConfirmarNueva, Opciones, Creditos, Intro,
        Jugando, Dialogo, Pausa, ConfirmarSalir, Inventario, Final,
        Mapa, Muerte, Capitulo2Aviso, Continuara
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
            cap2Desbloqueado = PlayerPrefs.GetInt("eternauta_cap2", 0) == 1;
            ReiniciarMundo(1, NuevaSemilla());
            hayPartidaGuardada = SistemaGuardado.ExistePartida();
        }

        void OnDestroy()
        {
            if (visual != null) visual.Destruir();
        }

        // Mundo y estado nuevos en el mapa indicado (nueva partida, cargar, volver al menú).
        void ReiniciarMundo(int mapa, int semilla)
        {
            semillaPartida = semilla;
            CrearMundo(mapa);
            jugador = new Jugador { x = ContenidoJuego.InicioX, y = ContenidoJuego.InicioY, angulo = ContenidoJuego.InicioAngulo * Mathf.Deg2Rad };
            inventario = new Inventario();
            progresion = new Progresion();
            visual.conTraje = false;
            visual.ocultarMuroNorte = false;
            visual.manoSprite = null;
            pistolaEncontrada = false;
            hotbarSel = hotbarAnterior = 0;
            tRecarga = -1f; tAnimAtaque = -1f; tEnfriamiento = 0f;
            corteT = -1f; flashDanio = 0f; fundido = 0f;
            tFrase = Random.Range(40f, 80f);
            descubierto.Clear();
            radioTiempo = -1f;
            Descubrir(jugador.x, jugador.y);
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
                case Estado.Mapa: ActualizarMapa(); break;
                case Estado.Muerte: ActualizarMuerte(dt); break;
                case Estado.Capitulo2Aviso: ActualizarAviso(); break;
                case Estado.Continuara: ActualizarContinuara(dt); break;
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
            bool pausado = estado == Estado.Pausa || estado == Estado.Inventario || estado == Estado.ConfirmarSalir || estado == Estado.Mapa ||
                           estado == Estado.Muerte || (estado == Estado.Opciones && origenOpciones == Estado.Pausa);
            float dtMundo = pausado ? 0f : dt;
            destello = Mathf.MoveTowards(destello, 0f, dt * 3f);
            if (juegoVisible || estado == Estado.Final || estado == Estado.Capitulo2Aviso || estado == Estado.Continuara)
            {
                visual.mostrarManos = estado != Estado.Final && estado != Estado.Capitulo2Aviso && !modoFoto;
                escarcha = jugador.bajoTecho ? jugador.exposicion * 0.3f : Mathf.Clamp01(jugador.exposicion * 0.9f + (jugador.vida <= 0f ? 0.5f : 0f));
                alertaRoja = jugador.Segmentos() <= 3 && estado == Estado.Jugando && !modoFoto ? 0.25f + 0.2f * Mathf.Sin(Time.time * 3f) : 0f;
                if (estado != Estado.Final && estado != Estado.Capitulo2Aviso && estado != Estado.Continuara) fundido = 0f;
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
                visual.Actualizar(dt, 75.5f, 60f, anguloAtraccion, 6f, 0f, 0f, null, -1, -1);
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
                else if (o.id == 6) { if (mundo.datos.id == 1) CruzarPuente(); else CompletarObjetivo(6); }
                else if (o.id == 7) { for (int i = 0; i < MuertosNecesarios(); i++) bichosMuertos.Add(1000 + i); CompletarObjetivo(7); }
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
            if (mundo.datos.id != 1) return;
            if (!jugador.traje) DarTraje();
            while (progresion.Actual != null && progresion.Actual.id < 4) progresion.indiceObjetivo++;
            AbrirTodasLasPuertas();
            var d = mundo.datos;
            jugador.x = d.llegadaX; jugador.y = d.llegadaY + 9f; jugador.angulo = -90f * Mathf.Deg2Rad; jugador.inclinacion = 0f;
            if (estado != Estado.Jugando) estado = Estado.Jugando;
            Mostrar("DEV: saltaste al acceso del Puente Pueyrredón", 2f);
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
            if (corteT >= 0f) { ActualizarCorte(dt); return; }
            if (Entrada.Pulsada(Tecla.Escape)) { AbrirPausa(); return; }
            if (Entrada.Pulsada(Tecla.Tab) || Entrada.Pulsada(Tecla.I)) { AbrirInventario(Estado.Jugando); return; }
            if (Entrada.Pulsada(Tecla.M)) { AbrirMapa(); return; }

            float avance = 0f, lateral = 0f, giro = 0f;
            if (Entrada.Mantenida(Tecla.W) || Entrada.Mantenida(Tecla.Arriba)) avance += 1f;
            if (Entrada.Mantenida(Tecla.S) || Entrada.Mantenida(Tecla.Abajo)) avance -= 1f;
            if (Entrada.Mantenida(Tecla.D)) lateral += 1f;
            if (Entrada.Mantenida(Tecla.A)) lateral -= 1f;
            if (Entrada.Mantenida(Tecla.Derecha)) giro += Jugador.VelGiro * dt;
            if (Entrada.Mantenida(Tecla.Izquierda)) giro -= Jugador.VelGiro * dt;
            giro += Entrada.MouseX() * sensibilidadMouse;
            // Mirar arriba / abajo: mouse o Re Pág / Av Pág (R es recargar)
            float mirar = Entrada.MouseY() * sensibilidadMouse * Mathf.Rad2Deg * (invertirMouseY ? -1f : 1f);
            if (Entrada.Mantenida(Tecla.RePag)) mirar += 75f * dt;
            if (Entrada.Mantenida(Tecla.AvPag)) mirar -= 75f * dt;

            bool estabaAdentro = jugador.bajoTecho;
            jugador.factorNieve = mundo.datos.id == 2 ? 0.5f : 1f;
            jugador.Actualizar(mundo, avance, lateral, giro, mirar, Entrada.Mantenida(Tecla.Shift), dt);

            ampBalanceo = Mathf.MoveTowards(ampBalanceo, jugador.caminando ? 1f : 0f, dt * 4f);
            if (jugador.caminando) faseBalanceo += dt * (Entrada.Mantenida(Tecla.Shift) ? 9f : 6.5f);

            // Muro de nieve: si insistís un rato contra el límite del mapa, el protagonista lo comenta
            if (jugador.caminando && mundo.CercaMuroNieve(jugador.x, jugador.y, Jugador.Radio + 0.3f)) jugador.tIntentaNieve += dt;
            else jugador.tIntentaNieve = Mathf.Max(0f, jugador.tIntentaNieve - dt * 2f);
            if (jugador.tIntentaNieve > 2.2f && avisoBloqueo <= 0f)
            {
                Mostrar("Parece que no se puede cruzar, está tapado de nieve.", 3.5f);
                avisoBloqueo = 8f;
                jugador.tIntentaNieve = 0f;
            }

            Descubrir(jugador.x, jugador.y);

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
            }

            // Bichos y armas
            ActualizarBichos(dt);
            ActualizarHotbar(dt);
            ActualizarFrases(dt);

            // Llegadas: acceso al puente en Avellaneda, fin del puente en Capital
            var dm = mundo.datos;
            float lx = jugador.x - dm.llegadaX, ly = jugador.y - dm.llegadaY;
            if (lx * lx + ly * ly < dm.llegadaRadio * dm.llegadaRadio)
            {
                if (dm.id == 1 && progresion.EsActual(4)) { IniciarFinal(); return; }
                if (dm.id == 1 && progresion.EsActual(6)) { CruzarPuente(); return; }
                if (dm.id == 2 && progresion.EsActual(6))
                {
                    CompletarObjetivo(6);
                    progresion.Activar(9);
                    Mostrar("Av. 9 de Julio. Los soldados pelean contra los bichos: ayudalos.", 4f);
                }
                if (dm.id == 1 && progresion.EsActual(5) && avisoBloqueo <= 0f)
                {
                    Mostrar("Antes de cruzar, buscá la radio de la casa abandonada.", 3.5f);
                    avisoBloqueo = 10f;
                }
            }
            if (progresion.EsActual(7) && MuertosEnMapa() >= MuertosNecesarios())
            {
                CompletarObjetivo(7);
                Mostrar("Los soldados te hacen señas: hablá con el sargento.", 4f);
            }

            BuscarInteraccion();
            if (HayInteraccion && (Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio) || Entrada.Pulsada(Tecla.Enter)))
                Interactuar();

            if (jugador.vida <= 0f) { Morir(); return; }
            if (jugador.vida < 25f && avisoBloqueo <= 0f && jugador.exposicion > 0.2f && !jugador.bajoTecho)
            {
                Mostrar("Te estás congelando. Usá un medicamento o buscá un techo.", 3f);
                avisoBloqueo = 6f;
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
                    AlObtener(r);
                    if (e.def.completaObjetivo != 0) CompletarObjetivo(e.def.completaObjetivo);
                    break;
                }
                case TipoEntidad.Cajon:
                case TipoEntidad.Heladera:
                    AbrirContenedor(e);
                    break;
                case TipoEntidad.Ascensor:
                    UsarAscensor(e);
                    break;
                case TipoEntidad.Cuadro:
                    audioJuego.Menu();
                    Mostrar("Pablo: «" + (e.def.texto ?? "Mi familia.") + "»", 5f);
                    break;
                case TipoEntidad.MesaTrabajo:
                    if (jugador.traje) { Mostrar("Ya armaste el traje aislante.", 2f); break; }
                    if (inventario.CantidadCategoria(Categoria.Materiales) < 2)
                    {
                        audioJuego.Error();
                        Mostrar("Para el traje aislante necesitás 2 materiales (tenés " + inventario.CantidadCategoria(Categoria.Materiales) + "). Revisá los cajones.", 3.5f);
                        break;
                    }
                    inventario.ConsumirMateriales(2);
                    DarTraje();
                    audioJuego.Traje();
                    Mostrar("Armaste el traje aislante. Ahora podés salir con el ascensor.", 3.5f);
                    CompletarObjetivo(1);
                    break;
                case TipoEntidad.Radio:
                    UsarRadio();
                    break;
                case TipoEntidad.Personaje:
                    IniciarDialogo(e);
                    break;
            }
        }

        void UsarRadio()
        {
            if (radioTiempo >= 0f)
            {
                // saltear la transmisión
                radioTiempo = -1f;
                mensajes.RemoveAll(m => m.texto.StartsWith("RADIO: "));
                audioJuego.Menu();
                Mostrar("Apagaste la radio.", 2f);
                return;
            }
            if (!progresion.EsActual(5) && !progresion.Completado(5))
            {
                audioJuego.Error();
                Mostrar("Una radio antigua. Solo se oye estática... por ahora.", 3f);
                return;
            }
            progresion.Activar(2);
            radioTiempo = 0f;
            if (progresion.EsActual(5)) CompletarObjetivo(5);
            Mostrar("Pablo: «Se escucha algo... (E para apagar la radio)»", 3f);
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
            RevisarObjetivosPendientes();
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
        bool dialogoSargento;

        void IniciarDialogo(Entidad e)
        {
            dialogo.Clear();
            dialogoIdx = 0;
            decisionActiva = false;
            dialogoCompletaObjetivo = false;
            dialogoSargento = e.def.sprite == "soldado";
            if (dialogoSargento)
            {
                if (progresion.EsActual(8))
                {
                    foreach (var l in ContenidoJuego.DialogoSargento) dialogo.Add(l);
                    dialogoCompletaObjetivo = true;
                }
                else if (progresion.Completado(8)) dialogo.Add("@EL SARGENTO|Seguí buscando, soldado. Y cuidate la espalda.");
                else dialogo.Add("@EL SARGENTO|¡Agachate! Primero ayudanos con esos bichos y después hablamos.");
            }
            else if (progresion.Completado(3))
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
            if (dialogoCompletaObjetivo && !dialogoSargento && dialogo.Count == 4)
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
                if (dialogoSargento)
                {
                    inventario.Agregar(ContenidoJuego.IdMunicion, 15);
                    Mostrar("El sargento te da munición: Munición 9 mm ×15", 3f);
                    CompletarObjetivo(8);
                    FinCapitulo2();
                }
                else CompletarObjetivo(3);
            }
        }

        public void ElegirDecision(bool dar)
        {
            decisionActiva = false;
            audioJuego.Menu();
            if (dar)
            {
                inventario.Quitar(1, 1);
                inventario.LimpiarHotbar();
                progresion.Activar(5);
                dialogo.Add(ContenidoJuego.RespuestaDar);
            }
            else dialogo.Add(ContenidoJuego.RespuestaNoDar);
            inventario.Agregar(ContenidoJuego.IdMapa, 1);
            AutoAsignar(ContenidoJuego.IdMapa);
            Mostrar("Recibiste: Mapa de Avellaneda (tecla M, o equipalo en la hotbar)", 4f);
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
            int nCat = Inventario.Orden.Length;
            if (Entrada.Pulsada(Tecla.W) || Entrada.Pulsada(Tecla.Arriba)) { invCategoria = (invCategoria + nCat - 1) % nCat; invObjeto = 0; audioJuego.Menu(); }
            if (Entrada.Pulsada(Tecla.S) || Entrada.Pulsada(Tecla.Abajo)) { invCategoria = (invCategoria + 1) % nCat; invObjeto = 0; audioJuego.Menu(); }
            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count > 0)
            {
                if (Entrada.Pulsada(Tecla.D) || Entrada.Pulsada(Tecla.Derecha)) { invObjeto = (invObjeto + 1) % lista.Count; audioJuego.Menu(); }
                if (Entrada.Pulsada(Tecla.A) || Entrada.Pulsada(Tecla.Izquierda)) { invObjeto = (invObjeto + lista.Count - 1) % lista.Count; audioJuego.Menu(); }
                invObjeto = Mathf.Clamp(invObjeto, 0, lista.Count - 1);
                if (Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Enter)) UtilizarSeleccionado();
                if (Entrada.Pulsada(Tecla.Uno)) AsignarRanuraSeleccionado(0);
                if (Entrada.Pulsada(Tecla.Dos)) AsignarRanuraSeleccionado(1);
                if (Entrada.Pulsada(Tecla.Tres)) AsignarRanuraSeleccionado(2);
                if (Entrada.Pulsada(Tecla.Cuatro)) AsignarRanuraSeleccionado(3);
            }
            if (Entrada.Pulsada(Tecla.Escape) || Entrada.Pulsada(Tecla.Tab) || Entrada.Pulsada(Tecla.I)) CerrarInventario();
        }

        // ¿Se puede poner en la hotbar? Armas, comida, medicinas y el mapa (la munición se carga con R).
        static bool Equipable(RecursoDef r)
        {
            return r.arma != TipoArma.Ninguna || r.curacion > 0 || r.id == ContenidoJuego.IdMapa;
        }

        void AsignarRanuraSeleccionado(int ranura)
        {
            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count == 0) return;
            var r = lista[Mathf.Clamp(invObjeto, 0, lista.Count - 1)];
            if (!Equipable(r)) { audioJuego.Error(); Mostrar("Eso no se puede poner en la hotbar.", 2f); return; }
            inventario.AsignarHotbar(ranura, r.id);
            audioJuego.Recoger();
            Mostrar(r.nombre + " → ranura " + (ranura + 1), 2f);
        }

        public void UtilizarSeleccionado()
        {
            var lista = inventario.ObjetosDe(Inventario.Orden[invCategoria]);
            if (lista.Count == 0) return;
            var r = lista[Mathf.Clamp(invObjeto, 0, lista.Count - 1)];
            if (r.curacion <= 0)
            {
                // armas y mapa: se equipan en la ranura elegida
                if (Equipable(r))
                {
                    inventario.AsignarHotbar(hotbarSel, r.id);
                    audioJuego.Recoger();
                    Mostrar(r.nombre + " → ranura " + (hotbarSel + 1) + ". Con 1 a 4 elegís otra ranura.", 2.5f);
                }
                else audioJuego.Error();
                return;
            }
            if (jugador.vida >= 100f) { audioJuego.Error(); Mostrar("Ya estás con la salud al máximo.", 2f); return; }
            inventario.Quitar(r.id, 1);
            inventario.LimpiarHotbar();
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
            cap2Desbloqueado = true;
            PlayerPrefs.SetInt("eternauta_cap2", 1);
            PlayerPrefs.Save();
            Guardar();
            estado = Estado.Final;
            finalFase = 0;
            finalTiempo = 0f;
            finalAnguloInicial = jugador.angulo;
            finalInclinacionInicial = jugador.inclinacion;
            mensajes.Clear();
            tRecarga = -1f; tAnimAtaque = -1f;
        }

        void ActualizarFinal(float dt)
        {
            finalTiempo += dt;
            if (modoDesarrollador && Entrada.Pulsada(Tecla.Enter)) finalTiempo += 4f;
            var dm = mundo.datos;
            if (finalFase == 0)
            {
                // El protagonista se detiene y observa el Obelisco a la distancia
                visual.ocultarMuroNorte = true;
                float t = Mathf.Clamp01(finalTiempo / 4f);
                float objetivo = -Mathf.PI / 2f;
                float delta = Mathf.DeltaAngle(finalAnguloInicial * Mathf.Rad2Deg, objetivo * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                jugador.angulo = finalAnguloInicial + delta * Mathf.SmoothStep(0f, 1f, t);
                jugador.inclinacion = Mathf.Lerp(finalInclinacionInicial, 7f, Mathf.SmoothStep(0f, 1f, t));
                jugador.x = Mathf.MoveTowards(jugador.x, dm.llegadaX, dt * 0.5f);
                if (jugador.y > dm.llegadaY + 0.2f) jugador.y -= dt * 0.15f;
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
                // En lugar de los créditos se desbloquea el capítulo 2
                estado = Estado.Capitulo2Aviso;
                selAviso = 0;
                fundido = 1f;
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
                id_capitulo = progresion.indiceObjetivo >= 4 ? 2 : 1,
                pos_x = jugador.x, pos_y = jugador.y,
                angulo = jugador.angulo * Mathf.Rad2Deg,
                inclinacion = jugador.inclinacion,
                traje_aislante = jugador.traje,
                inventario = inventario.Exportar(),
                objetivos = progresion.ExportarObjetivos(),
                eventos = progresion.ExportarEventos(),
                mapa_id = mundo.datos.id,
                semilla = semillaPartida,
                cargador = jugador.cargador,
                hotbar = (int[])inventario.hotbar.Clone(),
                hotbar_sel = hotbarSel,
                pistola_encontrada = pistolaEncontrada,
            };
            foreach (var e in mundo.entidades)
            {
                if (!e.activa) p.entidades_retiradas.Add(e.def.id);
                if (e.abierto) p.contenedores_abiertos.Add(e.def.id);
            }
            p.puertas_abiertas.AddRange(mundo.puertasAbiertas);
            p.bichos_muertos.AddRange(bichosMuertos);
            foreach (var kv in descubierto)
            {
                var d = kv.Value;
                for (int y = 0; y < d.bh; y++)
                {
                    var fila = new System.Text.StringBuilder(kv.Key + ":");
                    for (int x = 0; x < d.bw; x++) fila.Append(d.v[y * d.bw + x] ? '1' : '0');
                    p.mapa_descubierto.Add(fila.ToString());
                }
            }
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
            semillaPartida = p.semilla != 0 ? p.semilla : NuevaSemilla();
            ReiniciarMundo(p.mapa_id == 2 ? 2 : 1, semillaPartida);
            // los bichos que ya mataste siguen muertos
            GenerarBichos(semillaPartida, p.bichos_muertos);
            visual.RecrearBichos();
            jugador.x = p.pos_x; jugador.y = p.pos_y;
            jugador.angulo = p.angulo * Mathf.Deg2Rad;
            jugador.inclinacion = Mathf.Clamp(p.inclinacion, -Jugador.InclinacionMax, Jugador.InclinacionMax);
            jugador.vida = Mathf.Clamp(p.vida, 1, 100);
            jugador.cargador = Mathf.Clamp(p.cargador, 0, ContenidoJuego.CargadorCapacidad);
            pistolaEncontrada = p.pistola_encontrada;
            if (p.traje_aislante) DarTraje();
            inventario.Importar(p.inventario);
            if (p.hotbar != null) for (int i = 0; i < 4 && i < p.hotbar.Length; i++) inventario.hotbar[i] = p.hotbar[i];
            inventario.LimpiarHotbar();
            hotbarSel = Mathf.Clamp(p.hotbar_sel, 0, 3);
            progresion.Importar(p.objetivos, p.eventos);
            foreach (int id in p.entidades_retiradas) { var e = mundo.BuscarEntidad(id); if (e != null) e.activa = false; }
            foreach (int id in p.contenedores_abiertos) { var e = mundo.BuscarEntidad(id); if (e != null) { e.abierto = true; e.abrirT = 1f; } }
            foreach (var clave in p.puertas_abiertas)
            {
                var partes = clave.Split(',');
                int x, y;
                if (partes.Length == 2 && int.TryParse(partes[0], out x) && int.TryParse(partes[1], out y)) mundo.AbrirPuerta(x, y);
            }
            descubierto.Clear();
            foreach (var fila in p.mapa_descubierto)
            {
                int dos = fila.IndexOf(':');
                int idMapa;
                if (dos <= 0 || !int.TryParse(fila.Substring(0, dos), out idMapa)) continue;
                var d = DescubiertoDe(idMapa);
                // las filas de un mismo mapa vienen en orden: la primera libre es la siguiente
                int y = 0;
                while (y < d.bh && FilaCargada(d, y)) y++;
                for (int x = 0; x < d.bw && dos + 1 + x < fila.Length; x++) d.v[y * d.bw + x] = fila[dos + 1 + x] == '1';
                if (y < d.bh) filaMarcada[d] = y + 1;
            }
            filaMarcada.Clear();
            mapaSucio = true;
            cap2Desbloqueado = cap2Desbloqueado || PlayerPrefs.GetInt("eternauta_cap2", 0) == 1;
            Descubrir(jugador.x, jugador.y);
            return true;
        }

        readonly Dictionary<Descubierto, int> filaMarcada = new Dictionary<Descubierto, int>();
        bool FilaCargada(Descubierto d, int y)
        {
            int n;
            return filaMarcada.TryGetValue(d, out n) && y < n;
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
