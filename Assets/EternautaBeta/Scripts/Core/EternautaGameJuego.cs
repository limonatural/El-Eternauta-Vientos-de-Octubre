using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Eternauta.Beta
{
    // Sistemas agregados para la muestra anual: armas y hotbar, bichos, cajones y heladeras,
    // ascensor, muerte, mapa descubierto, cortes a negro y capítulo 2.
    public partial class EternautaGame
    {
        // ---- Estado de la partida (se guarda) ----
        int semillaPartida = 1;
        bool pistolaEncontrada;
        bool cap2Desbloqueado;
        readonly HashSet<int> bichosMuertos = new HashSet<int>();   // índices de spawn de los bichos que ya mataste en este mapa
        int bichosTotalesMapa, bichosRetirados;
        int mapaVisual = -1;

        // ---- Hotbar y combate ----
        int hotbarSel, hotbarAnterior;
        float tEnfriamiento;
        float tRecarga = -1f;
        float tAnimAtaque = -1f, duracionAtaque = 0.3f;
        float marcaImpacto;      // la mirilla se marca al acertar
        float flashDanio;        // pantalla roja al recibir una mordida
        const float TiempoRecarga = 1.25f;

        // ---- Cortes a negro (ascensor, puente, despertar) ----
        float corteT = -1f;
        string corteTexto = "";
        System.Action corteAccion;
        bool corteHecho;
        const float CorteSube = 0.55f, CorteNegro = 1.6f, CorteBaja = 0.7f;

        public float CorteAlpha
        {
            get
            {
                if (corteT < 0f) return 0f;
                if (corteT < CorteSube) return corteT / CorteSube;
                if (corteT < CorteSube + CorteNegro) return 1f;
                return 1f - (corteT - CorteSube - CorteNegro) / CorteBaja;
            }
        }

        // ---- Pablo habla solo ----
        float tFrase = 50f;

        // ---- Muerte y finales ----
        float tEscenaMuerte;
        int selMuerte, selAviso;
        string causaMuerteMostrada = "";
        float tContinuara;

        // ---- Mapa descubierto (tecla M) ----
        public const int BloqueMapa = 3;
        public class Descubierto { public int bw, bh; public bool[] v; }
        public readonly Dictionary<int, Descubierto> descubierto = new Dictionary<int, Descubierto>();
        public bool mapaSucio = true;
        int ultimoBloque = -1;

        int NuevaSemilla() { return Random.Range(1, 1000000); }

        // ------------------------------------------------------------------
        // MUNDO: crear mapa, bichos y vista
        // ------------------------------------------------------------------
        void CrearMundo(int mapa)
        {
            ContenidoJuego.MapaActual = mapa;
            mundo = new Mundo(ContenidoJuego.Datos);
            bichosMuertos.Clear();
            bichosRetirados = 0;
            GenerarBichos(semillaPartida, null);
            if (visual == null || mapaVisual != mapa) CrearVisual(); else visual.Vincular(mundo);
            mapaVisual = mapa;
            mensajes.Clear();
            zonaActual = -1;
            ultimoBloque = -1;
            mapaSucio = true;
            objetivoInteraccion = null; puertaX = puertaY = -1;
        }

        void CrearVisual()
        {
            if (visual != null) visual.Destruir();
            visual = new MundoVisual(mundo, resolucionVertical);
        }

        // Elige al azar cuántos y cuáles bichos hay (de bichosMin a bichosMax), siempre los mismos para una semilla.
        void GenerarBichos(int semilla, ICollection<int> muertos)
        {
            mundo.bichos.Clear();
            var d = mundo.datos;
            var rnd = new System.Random(semilla * 31 + d.id * 977);
            var idx = new List<int>();
            for (int i = 0; i < d.spawns.Count; i++)
            {
                if (d.salidaBajaY > 0 && d.spawns[i].y >= d.salidaBajaY) continue;   // nada en el departamento
                idx.Add(i);
            }
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int t = idx[i]; idx[i] = idx[j]; idx[j] = t;
            }
            int n = Mathf.Min(idx.Count, d.bichosMin + rnd.Next(Mathf.Max(1, d.bichosMax - d.bichosMin + 1)));
            bichosTotalesMapa = n;
            for (int k = 0; k < n; k++)
            {
                int i = idx[k];
                if (muertos != null && muertos.Contains(i)) { bichosMuertos.Add(i); continue; }
                var s = d.spawns[i];
                mundo.bichos.Add(new Bicho(i, s.x, s.y, s.interior, rnd));
            }
        }

        // Saca a los bichos que quedaron demasiado cerca de un punto (al aparecer o despertar).
        void AlejarBichos(float px, float py, float radio)
        {
            bool cambio = false;
            for (int i = mundo.bichos.Count - 1; i >= 0; i--)
            {
                var b = mundo.bichos[i];
                float dx = b.x - px, dy = b.y - py;
                if (dx * dx + dy * dy < radio * radio) { mundo.bichos.RemoveAt(i); bichosRetirados++; cambio = true; }
            }
            if (cambio) visual.RecrearBichos();
        }

        // ------------------------------------------------------------------
        // CORTES A NEGRO
        // ------------------------------------------------------------------
        void IniciarCorte(string texto, System.Action alNegro)
        {
            corteT = 0f; corteTexto = texto; corteAccion = alNegro; corteHecho = false;
        }

        void ActualizarCorte(float dt)
        {
            corteT += dt;
            if (!corteHecho && corteT >= CorteSube)
            {
                corteHecho = true;
                if (corteAccion != null) corteAccion();
            }
            if (corteT >= CorteSube + CorteNegro + CorteBaja) { corteT = -1f; corteTexto = ""; }
        }

        // ------------------------------------------------------------------
        // MAPA DESCUBIERTO
        // ------------------------------------------------------------------
        public Descubierto DescubiertoDe(int mapa)
        {
            Descubierto d;
            if (!descubierto.TryGetValue(mapa, out d))
            {
                var dm = ContenidoJuego.DatosDe(mapa);
                d = new Descubierto { bw = (dm.ancho + BloqueMapa - 1) / BloqueMapa, bh = (dm.alto + BloqueMapa - 1) / BloqueMapa };
                d.v = new bool[d.bw * d.bh];
                descubierto[mapa] = d;
            }
            return d;
        }

        void Descubrir(float px, float py)
        {
            int cx = Mathf.FloorToInt(px / BloqueMapa), cy = Mathf.FloorToInt(py / BloqueMapa);
            int clave = cy * 4096 + cx;
            if (clave == ultimoBloque) return;
            ultimoBloque = clave;
            var d = DescubiertoDe(mundo.datos.id);
            const int r = 4;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r + 1) continue;
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= d.bw || y >= d.bh) continue;
                    if (!d.v[y * d.bw + x]) { d.v[y * d.bw + x] = true; mapaSucio = true; }
                }
        }

        void DescubrirTodo(int mapa)
        {
            var d = DescubiertoDe(mapa);
            for (int i = 0; i < d.v.Length; i++) d.v[i] = true;
            mapaSucio = true;
        }

        void AbrirMapa()
        {
            estado = Estado.Mapa;
            audioJuego.Menu();
        }

        void ActualizarMapa()
        {
            if (Entrada.Pulsada(Tecla.Escape) || Entrada.Pulsada(Tecla.M) || Entrada.Pulsada(Tecla.Tab) || Entrada.Pulsada(Tecla.I))
            {
                estado = Estado.Jugando;
                audioJuego.Menu();
            }
        }

        // ------------------------------------------------------------------
        // HOTBAR Y ARMAS
        // ------------------------------------------------------------------
        RecursoDef Equipado()
        {
            int id = inventario.hotbar[hotbarSel];
            return id != 0 ? ContenidoJuego.Recurso(id) : null;
        }

        void SeleccionarRanura(int n)
        {
            n = ((n % 4) + 4) % 4;
            if (n == hotbarSel) return;
            hotbarAnterior = hotbarSel;
            hotbarSel = n;
            tRecarga = -1f; tAnimAtaque = -1f;
            audioJuego.Menu();
        }

        // Pone el objeto en una ranura libre (o en la elegida si están todas ocupadas).
        void AutoAsignar(int idRecurso)
        {
            for (int i = 0; i < 4; i++) if (inventario.hotbar[i] == idRecurso) return;
            for (int i = 0; i < 4; i++)
                if (inventario.hotbar[i] == 0) { inventario.hotbar[i] = idRecurso; return; }
        }

        void ActualizarHotbar(float dt)
        {
            if (Entrada.Pulsada(Tecla.Uno)) SeleccionarRanura(0);
            else if (Entrada.Pulsada(Tecla.Dos)) SeleccionarRanura(1);
            else if (Entrada.Pulsada(Tecla.Tres)) SeleccionarRanura(2);
            else if (Entrada.Pulsada(Tecla.Cuatro)) SeleccionarRanura(3);
            else if (Entrada.Pulsada(Tecla.Q)) SeleccionarRanura(hotbarAnterior);
            int rueda = Entrada.Rueda();
            if (rueda > 0) SeleccionarRanura(hotbarSel - 1);
            else if (rueda < 0) SeleccionarRanura(hotbarSel + 1);

            var r = Equipado();
            visual.manoSprite = r != null ? r.sprite : null;
            visual.manoTipo = r != null ? r.arma : TipoArma.Ninguna;

            tEnfriamiento = Mathf.Max(0f, tEnfriamiento - dt);
            marcaImpacto = Mathf.Max(0f, marcaImpacto - dt);
            flashDanio = Mathf.Max(0f, flashDanio - dt * 1.6f);
            if (tAnimAtaque >= 0f) { tAnimAtaque += dt / duracionAtaque; if (tAnimAtaque >= 1f) tAnimAtaque = -1f; }

            bool pistola = r != null && r.arma == TipoArma.Pistola;
            if (tRecarga >= 0f)
            {
                if (!pistola) tRecarga = -1f;
                else { tRecarga += dt; if (tRecarga >= TiempoRecarga) TerminarRecarga(); }
            }
            visual.animGolpe = tAnimAtaque;
            visual.animRecarga = tRecarga >= 0f ? tRecarga / TiempoRecarga : -1f;

            if (Entrada.Pulsada(Tecla.R))
            {
                if (pistola) IntentarRecargar();
                else if (inventario.Cantidad(ContenidoJuego.IdPistola) > 0) Mostrar("Equipá la Glock 19 en la hotbar para recargarla.", 2.5f);
            }

            if (tEnfriamiento > 0f || tRecarga >= 0f) return;
            if (r == null)
            {
                if (Entrada.ClicPulsado()) Atacar(ContenidoJuego.Arma(0));
            }
            else if (r.arma == TipoArma.Pistola)
            {
                if (Entrada.ClicPulsado()) Atacar(ContenidoJuego.Arma(r.id));
            }
            else if (r.arma == TipoArma.CuerpoACuerpo)
            {
                if (Entrada.ClicMantenido()) Atacar(ContenidoJuego.Arma(r.id));
            }
            else if (Entrada.ClicPulsado())
            {
                tEnfriamiento = 0.35f;
                if (r.id == ContenidoJuego.IdMapa) AbrirMapa();
                else if (r.curacion > 0) UsarObjeto(r);
            }
        }

        void IntentarRecargar()
        {
            if (tRecarga >= 0f) return;
            if (jugador.cargador >= ContenidoJuego.CargadorCapacidad) { Mostrar("El cargador ya está lleno.", 1.5f); return; }
            if (inventario.Cantidad(ContenidoJuego.IdMunicion) <= 0) { audioJuego.Error(); Mostrar("No te queda munición de repuesto.", 2.5f); return; }
            tRecarga = 0f;
            audioJuego.Recarga();
        }

        void TerminarRecarga()
        {
            int falta = ContenidoJuego.CargadorCapacidad - jugador.cargador;
            int toma = Mathf.Min(falta, inventario.Cantidad(ContenidoJuego.IdMunicion));
            if (toma > 0) { inventario.Quitar(ContenidoJuego.IdMunicion, toma); jugador.cargador += toma; }
            tRecarga = -1f;
        }

        void Atacar(ArmaDef a)
        {
            float dirX = Mathf.Cos(jugador.angulo), dirY = Mathf.Sin(jugador.angulo);
            tEnfriamiento = a.enfriamiento;
            duracionAtaque = Mathf.Max(0.25f, a.enfriamiento);
            Bicho mejor = null;
            float mejorD = 999f;

            if (a.tipo == TipoArma.Pistola)
            {
                if (jugador.cargador <= 0)
                {
                    tEnfriamiento = 0.35f;
                    audioJuego.ClickVacio();
                    Mostrar(inventario.Cantidad(ContenidoJuego.IdMunicion) > 0 ? "Cargador vacío. Recargá con R." : "Sin balas. Buscá munición en cajones.", 2f);
                    return;
                }
                jugador.cargador--;
                tAnimAtaque = 0f;
                visual.fogonazoT = 0.07f;
                audioJuego.Disparo();
                foreach (var b in mundo.bichos) b.Oir(jugador.x, jugador.y, 16f);
                foreach (var b in mundo.bichos)
                {
                    if (!b.vivo) continue;
                    float dx = b.x - jugador.x, dy = b.y - jugador.y;
                    float along = dx * dirX + dy * dirY;
                    if (along < 0.2f || along > a.alcance) continue;
                    float perp = Mathf.Abs(dx * dirY - dy * dirX);
                    if (perp > Bicho.Radio + 0.14f) continue;
                    if (along < mejorD && mundo.LineaLibre(jugador.x, jugador.y, b.x, b.y)) { mejor = b; mejorD = along; }
                }
            }
            else
            {
                tAnimAtaque = 0f;
                audioJuego.GolpeAire();
                foreach (var b in mundo.bichos)
                {
                    if (!b.vivo) continue;
                    float dx = b.x - jugador.x, dy = b.y - jugador.y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > a.alcance + Bicho.Radio || d < 0.001f) continue;
                    if ((dx * dirX + dy * dirY) / d < 0.55f) continue;
                    if (d < mejorD && mundo.LineaLibre(jugador.x, jugador.y, b.x, b.y)) { mejor = b; mejorD = d; }
                }
            }

            if (mejor != null)
            {
                mejor.Danar(a.danio, dirX, dirY, a.empuje, mundo);
                marcaImpacto = 0.18f;
                audioJuego.GolpeBicho();
                if (!mejor.vivo)
                {
                    bichosMuertos.Add(mejor.indice);
                    audioJuego.BichoMuere();
                }
            }
        }

        void UsarObjeto(RecursoDef r)
        {
            if (jugador.vida >= 100f) { Mostrar("Ya estás con la salud al máximo.", 2f); return; }
            if (!inventario.Quitar(r.id, 1)) return;
            jugador.Curar(r.curacion);
            inventario.LimpiarHotbar();
            audioJuego.Recoger();
            Mostrar("Usaste: " + r.nombre + " (+" + r.curacion + " de salud)", 2.5f);
        }

        // ------------------------------------------------------------------
        // BICHOS
        // ------------------------------------------------------------------
        void ActualizarBichos(float dt)
        {
            foreach (var b in mundo.bichos)
            {
                if (b.vivo)
                {
                    float dx = b.x - jugador.x, dy = b.y - jugador.y;
                    if (dx * dx + dy * dy > 48f * 48f) continue;   // lejos: no se simulan
                }
                if (b.Actualizar(mundo, jugador, dt))
                {
                    jugador.Herir(ContenidoJuego.DanioBicho, "Un cascarudo gigante te mordió hasta matarte.");
                    flashDanio = 1f;
                    audioJuego.Herido();
                    audioJuego.BichoGrita();
                }
            }
        }

        int MuertosEnMapa() { return bichosMuertos.Count; }
        int MuertosNecesarios() { return Mathf.Min(8, Mathf.Max(1, bichosTotalesMapa - bichosRetirados)); }

        // Texto extra del objetivo actual (por ejemplo el avance de "Ayudar a los soldados").
        public string ProgresoObjetivo()
        {
            if (progresion.EsActual(7)) return " (" + Mathf.Min(MuertosEnMapa(), MuertosNecesarios()) + "/" + MuertosNecesarios() + ")";
            return "";
        }

        // ------------------------------------------------------------------
        // CAJONES Y HELADERAS
        // ------------------------------------------------------------------
        void AbrirContenedor(Entidad e)
        {
            bool heladera = e.def.tipo == TipoEntidad.Heladera;
            if (e.abierto)
            {
                audioJuego.Error();
                Mostrar(heladera ? "Ya revisaste esta heladera." : "Ya revisaste este cajón. Está vacío.", 2f);
                return;
            }
            e.abierto = true;
            audioJuego.Cajon();
            var botin = Botin.Abrir(e.def, semillaPartida);
            if (botin.Count == 0)
            {
                Mostrar(heladera ? "La heladera está vacía. Solo hielo y escarcha." : "El cajón está vacío.", 2.5f);
            }
            else
            {
                var sb = new StringBuilder("Encontraste: ");
                for (int i = 0; i < botin.Count; i++)
                {
                    var r = ContenidoJuego.Recurso(botin[i][0]);
                    if (r == null) continue;
                    inventario.Agregar(r.id, botin[i][1]);
                    if (i > 0) sb.Append(", ");
                    sb.Append(r.nombre);
                    if (botin[i][1] > 1) sb.Append(" ×").Append(botin[i][1]);
                    AlObtener(r);
                }
                destello = 0.5f;
                Mostrar(sb.ToString(), 4f);
            }
            if (e.def.completaObjetivo != 0 && progresion.EsActual(e.def.completaObjetivo)) CompletarObjetivo(e.def.completaObjetivo);
            RevisarObjetivosPendientes();
            if (progresion.EsActual(1) && inventario.CantidadCategoria(Categoria.Materiales) >= 2)
                Mostrar("Ya tenés materiales suficientes. Usá la mesa de trabajo del taller.", 3.5f);
        }

        // Efectos de conseguir un objeto: la pistola se recupera con el cargador lleno y las armas se ponen en la hotbar.
        void AlObtener(RecursoDef r)
        {
            if (r.id == ContenidoJuego.IdPistola && !pistolaEncontrada)
            {
                pistolaEncontrada = true;
                jugador.cargador = ContenidoJuego.CargadorCapacidad;
                progresion.Activar(11);
                Mostrar("Recuperaste tu Glock 19 reglamentaria. Clic izquierdo dispara y R recarga.", 5f);
            }
            if (r.arma != TipoArma.Ninguna || r.id == ContenidoJuego.IdMapa) AutoAsignar(r.id);
        }

        // La búsqueda de comida se da por cumplida aunque hayas revisado la heladera antes de que fuera el objetivo.
        void RevisarObjetivosPendientes()
        {
            if (progresion.EsActual(2))
                foreach (var e in mundo.entidades)
                    if (e.def.completaObjetivo == 2 && e.abierto) { CompletarObjetivo(2); break; }
        }

        // ------------------------------------------------------------------
        // ASCENSOR, CUADRO
        // ------------------------------------------------------------------
        void UsarAscensor(Entidad e)
        {
            var d = mundo.datos;
            bool arriba = e.y >= d.salidaBajaY;       // el del departamento baja; el del hall sube
            if (arriba)
            {
                if (!jugador.traje)
                {
                    audioJuego.Error();
                    Mostrar("Afuera la nieve mata. Necesitás el traje aislante para salir.", 3f);
                    return;
                }
                audioJuego.Ding();
                IniciarCorte("Bajás en el ascensor...", () =>
                {
                    jugador.x = d.hallX; jugador.y = d.hallY; jugador.angulo = 0f; jugador.inclinacion = 0f;
                    Descubrir(jugador.x, jugador.y);
                });
            }
            else
            {
                audioJuego.Ding();
                IniciarCorte("Subís a tu departamento...", () =>
                {
                    jugador.x = 119.3f; jugador.y = 101f; jugador.angulo = Mathf.PI; jugador.inclinacion = 0f;
                });
            }
        }

        // ------------------------------------------------------------------
        // PABLO HABLA SOLO
        // ------------------------------------------------------------------
        void ActualizarFrases(float dt)
        {
            tFrase -= dt;
            if (tFrase > 0f) return;
            tFrase = Random.Range(55f, 110f);
            if (mensajes.Count > 0) return;
            foreach (var b in mundo.bichos)
            {
                if (!b.vivo || b.tPersecucion <= 0f) continue;
                tFrase = 15f;       // no mientras lo persiguen
                return;
            }
            string frase = ContenidoJuego.FrasesFamilia[Random.Range(0, ContenidoJuego.FrasesFamilia.Length)];
            Mostrar("Pablo: «" + frase + "»", 4.5f);
        }

        // ------------------------------------------------------------------
        // MUERTE
        // ------------------------------------------------------------------
        void Morir()
        {
            estado = Estado.Muerte;
            tEscenaMuerte = 0f;
            selMuerte = 0;
            causaMuerteMostrada = string.IsNullOrEmpty(jugador.causaMuerte) ? "Perdiste toda tu salud." : jugador.causaMuerte;
            tRecarga = -1f; tAnimAtaque = -1f;
            audioJuego.Muerte();
            mensajes.Clear();
        }

        void ActualizarMuerte(float dt)
        {
            tEscenaMuerte += dt;
            if (tEscenaMuerte < 1.5f) return;
            bool arriba = Entrada.Pulsada(Tecla.W) || Entrada.Pulsada(Tecla.Arriba);
            bool abajo = Entrada.Pulsada(Tecla.S) || Entrada.Pulsada(Tecla.Abajo);
            if (arriba || abajo) { selMuerte = 1 - selMuerte; audioJuego.Menu(); }
            if (Entrada.Pulsada(Tecla.Uno)) ActivarMuerte(0);
            else if (Entrada.Pulsada(Tecla.Dos)) ActivarMuerte(1);
            else if (Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio)) ActivarMuerte(selMuerte);
        }

        void ActivarMuerte(int i)
        {
            audioJuego.Menu();
            if (i == 1) { VolverAlMenu(); return; }
            Despertar();
        }

        // Reaparece en la cama (o al inicio del tramo en el mapa del puente) con la salud al máximo.
        void Despertar()
        {
            var d = mundo.datos;
            bool cama = d.id == 1;
            estado = Estado.Jugando;
            jugador.vida = 100f; jugador.exposicion = 0f; jugador.causaMuerte = "";
            jugador.tIntentaNieve = 0f;
            flashDanio = 0f;
            IniciarCorte(cama ? "Despertás sobresaltado en tu cama...\nFue solo un mal sueño." : "Todo se apaga... y volvés a abrir los ojos al comienzo del tramo.\nParece que fue un mal sueño.", () =>
            {
                jugador.x = d.camaX; jugador.y = d.camaY; jugador.angulo = -Mathf.PI / 2f; jugador.inclinacion = 0f;
                // los bichos vuelven a sus lugares (los que mataste siguen muertos)
                bichosRetirados = 0;
                GenerarBichos(semillaPartida, new List<int>(bichosMuertos));
                visual.RecrearBichos();
                AlejarBichos(jugador.x, jugador.y, 16f);
            });
        }

        // ------------------------------------------------------------------
        // CAPÍTULO 2
        // ------------------------------------------------------------------
        // Cruzar el puente: Avellaneda -> Puente Pueyrredón y Av. 9 de Julio.
        void CruzarPuente()
        {
            IniciarCorte("Cruzás el Puente Pueyrredón.\nAvellaneda queda atrás.", () =>
            {
                CrearMundo(2);
                var d = mundo.datos;
                jugador.x = d.inicioX; jugador.y = d.inicioY; jugador.angulo = d.inicioAngulo * Mathf.Deg2Rad; jugador.inclinacion = 0f;
                jugador.bajoTecho = false;
                progresion.Activar(8);
                inventario.Agregar(ContenidoJuego.IdMunicion, 10);
                Descubrir(jugador.x, jugador.y);
                AlejarBichos(jugador.x, jugador.y, 20f);
                Mostrar("Encontrás un morral militar: Munición 9 mm ×10", 4f);
                Mostrar("Los soldados caídos y el humo... algo grande pasó acá.", 4f);
                Guardar();
            });
        }

        // Empieza el capítulo 2 desde cero (menú) con lo que Pablo ya tendría al terminar el prólogo.
        void NuevoCapitulo2()
        {
            semillaPartida = NuevaSemilla();
            ReiniciarMundo(1, semillaPartida);
            jugador.traje = true;
            visual.conTraje = true;
            while (progresion.indiceObjetivo < 4) progresion.indiceObjetivo++;
            foreach (int ev in new[] { 1, 3, 4, 5, 6, 7, 11 }) progresion.Activar(ev);
            inventario.Agregar(ContenidoJuego.IdPistola, 1);
            inventario.Agregar(ContenidoJuego.IdMunicion, 5);
            inventario.Agregar(ContenidoJuego.IdCuchillo, 1);
            inventario.Agregar(ContenidoJuego.IdMapa, 1);
            inventario.Agregar(1, 1);
            inventario.Agregar(4, 1);
            inventario.hotbar[0] = ContenidoJuego.IdPistola;
            inventario.hotbar[1] = ContenidoJuego.IdCuchillo;
            inventario.hotbar[2] = 1;
            inventario.hotbar[3] = ContenidoJuego.IdMapa;
            hotbarSel = 0;
            jugador.cargador = ContenidoJuego.CargadorCapacidad;
            pistolaEncontrada = true;
            jugador.x = 134.5f; jugador.y = 26.5f; jugador.angulo = -Mathf.PI / 2f;
            DescubrirTodo(1);
            Descubrir(jugador.x, jugador.y);
            AlejarBichos(jugador.x, jugador.y, 14f);
            partidaEnCurso = true;
            estado = Estado.Jugando;
            avisoObjetivo = 7f;
            Mostrar("CAPÍTULO 2 — La voz en la radio", 4f);
            Guardar();
        }

        // Después del final del prólogo: se sigue en el mismo mundo.
        void SeguirCapitulo2()
        {
            visual.ocultarMuroNorte = false;
            visual.visibilidadObelisco = 0.3f;
            fundido = 0f;
            jugador.x = 134.5f; jugador.y = 26.5f; jugador.angulo = -Mathf.PI / 2f; jugador.inclinacion = 0f;
            estado = Estado.Jugando;
            avisoObjetivo = 7f;
            Mostrar("CAPÍTULO 2 — La voz en la radio", 4f);
            Guardar();
        }

        void FinCapitulo2()
        {
            Guardar();
            estado = Estado.Continuara;
            tContinuara = 0f;
            mensajes.Clear();
        }

        void ActualizarContinuara(float dt)
        {
            tContinuara += dt;
            if (tContinuara > 3f && (Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio) || Entrada.Pulsada(Tecla.Escape)))
                VolverAlMenu();
        }

        void ActualizarAviso()
        {
            if (Entrada.Pulsada(Tecla.W) || Entrada.Pulsada(Tecla.S) || Entrada.Pulsada(Tecla.Arriba) || Entrada.Pulsada(Tecla.Abajo)) { selAviso = 1 - selAviso; audioJuego.Menu(); }
            if (Entrada.Pulsada(Tecla.Enter) || Entrada.Pulsada(Tecla.E) || Entrada.Pulsada(Tecla.Espacio))
            {
                audioJuego.Menu();
                if (selAviso == 0) SeguirCapitulo2(); else VolverAlMenu();
            }
        }
    }
}
