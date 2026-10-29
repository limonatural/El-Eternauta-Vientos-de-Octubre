using System.Collections.Generic;

namespace Eternauta.Beta
{
    // Contenido fijo del juego (Etapa 13: Capítulo, Escenario, Objeto, Personaje,
    // Diálogo, EventoNarrativo, Objetivo y Recurso). No cambia al jugar.
    // Los IDs son los mismos que se guardan en la partida (archivo JSON).
    // Para la beta está escrito en C# para que no haga falta crear assets a mano;
    // más adelante se puede pasar a ScriptableObjects como propone la Etapa 14.

    public enum Categoria { Medicamentos, Comida, Materiales, ObjetosEncontrados, Armas }

    public enum TipoArma { Ninguna, Pistola, CuerpoACuerpo }

    public class RecursoDef
    {
        public int id;
        public string nombre;
        public string tipo;
        public string utilidad;
        public string descripcion;
        public Categoria categoria;
        public int curacion;      // cuánta salud recupera al UTILIZAR (0 = no se puede utilizar)
        public string sprite;
        public TipoArma arma;     // Pistola / CuerpoACuerpo = se puede equipar en la hotbar
    }

    // Arma equipable (balance: pistola > cuchillo > bate > palo; un cascarudo tiene 100 de vida).
    public class ArmaDef
    {
        public int idRecurso;
        public string nombre;
        public TipoArma tipo;
        public float danio;          // por golpe o disparo
        public float enfriamiento;   // segundos entre usos
        public float alcance;        // celdas (1 celda = 2 m)
        public float empuje;         // cuánto retrocede el bicho al recibir el golpe
    }

    public class Destino
    {
        public int mapa;
        public float x, y;
        public Destino(int mapa, float x, float y) { this.mapa = mapa; this.x = x; this.y = y; }
    }

    public class ObjetivoDef
    {
        public int id;
        public string titulo;
        public string descripcion;
        public int idCapitulo;
        public int idEscenario;
        public int idEvento;
        public string zona;
        public string destino;
        public List<Destino> marcas = new List<Destino>(); // puntos que se marcan en el mapa (tecla M)
    }

    public class EscenarioDef
    {
        public int id;
        public string nombre;
        public string ubicacion;
        public int idCapitulo;
        public string descripcion;
        // Rectángulo en celdas del mapa (x0, y0) - (x1, y1), inclusivo.
        public int x0, y0, x1, y1;
    }

    public class PersonajeDef
    {
        public int id;
        public string nombre;
        public string tipo;
        public int idEscenario;
    }

    public class DialogoDef
    {
        public int id;
        public int idPersonaje;
        public string texto;
        public int orden;
    }

    public class EventoDef
    {
        public int id;
        public string nombre;
        public string descripcion;
        public int idCapitulo;
        public int idPersonaje;
    }

    public enum TipoEntidad { Decoracion, Recurso, Personaje, Radio, MesaTrabajo, Cajon, Heladera, Ascensor, Cuadro }

    public class EntidadDef
    {
        public int id;
        public string sprite;
        public float x, y;
        public TipoEntidad tipo;
        public int idRecurso;
        public int cantidad = 1;
        public bool solido;
        public float radio = 0.3f;
        public float angulo;         // giro del modelo 3D en grados (0 = el frente mira al sur)
        public int completaObjetivo; // id de objetivo que se completa al recogerlo (0 = ninguno)
        public string nombre;
        public string tabla;         // contenedores: tabla de botín al azar (ver Botin)
        public string fijo;          // contenedores: contenido fijo "idxCantidad,idxCantidad"
        public string texto;         // cuadros: lo que piensa el protagonista al mirarlo
    }

    public class LuzDef { public float x, y, h, r, g, b, radio; }
    public class SpawnBicho { public float x, y; public bool interior; }
    public class LugarMapa { public string nombre; public float x, y; public int x0, y0, x1, y1; }

    // Todo lo que define un mapa: grilla, objetos, luces y lugares (lo genera tools/generar_mapas.py).
    public class DatosMapa
    {
        public int id;
        public string nombre;
        public int ancho, alto;
        public string[] filas;
        public string[] zonaFilas;
        public string zonaClaves;
        public int[] zonaIds;
        public readonly List<EntidadDef> entidades = new List<EntidadDef>();
        public readonly List<EscenarioDef> escenarios = new List<EscenarioDef>();
        public readonly List<Postal> postales = new List<Postal>();
        public readonly List<LuzDef> luces = new List<LuzDef>();
        public readonly List<SpawnBicho> spawns = new List<SpawnBicho>();
        public readonly List<LugarMapa> lugares = new List<LugarMapa>();
        public float inicioX, inicioY, inicioAngulo;
        public float camaX, camaY;        // donde reaparece el protagonista (la cama)
        public float hallX, hallY;        // donde sale del ascensor
        public float llegadaX, llegadaY, llegadaRadio; // acceso al puente / fin del tramo
        public float obeliscoX, obeliscoY;
        public bool hayObelisco, hayPuente;
        public int puenteX0, puenteX1, puenteY0, puenteY1;
        public int salidaBajaY;           // filas desde esta en adelante no son parte del mapa visible (departamento)
        public int bichosMin, bichosMax;
    }

    // Lugares predefinidos para sacar capturas (modo desarrollador, F4).
    public class Postal
    {
        public string nombre;
        public float x, y, angulo;
    }

    public static class ContenidoJuego
    {
        public const string NombreJuego = "EL ETERNAUTA";
        public const string Subtitulo = "VIENTOS DE OCTUBRE";
        public const string FraseFinal = "“Avellaneda ya no es la misma.”";

        // Créditos (Etapa 12, lámina 8D). Editar acá si cambia el equipo.
        public static readonly string[] Creditos =
        {
            "Kevin Tanta — Project Manager",
            "Juan Rocabado — Analista Funcional",
            "Mendoza — Programador",
            "Thomas Raineiri — Marketing",
            "Olivia Escobar — Marketing",
        };

        public static readonly string[] Intro =
        {
            "AVELLANEDA. OCTUBRE.",
            "Hace días que la nieve cae sin parar.\nQuien la toca, muere.",
            "Sobreviviste encerrado en tu departamento, a pasos de Plaza Alsina.\nPero la comida se terminó.",
            "Para salir vas a necesitar un traje aislante.",
        };

        // Los mapas están en MapasGenerados.cs (tools/generar_mapas.py). Hay dos:
        // 1 = Avellaneda (prólogo y comienzo del capítulo 2), 2 = Puente Pueyrredón y Av. 9 de Julio.
        // Leyenda de la grilla (cada carácter es una celda):
        //  .  calle nevada   :  vereda   p  plaza   ,  piso de madera   ;  baldosas   =  asfalto del puente
        //  D / d  puerta cerrada / abierta     N  muro de nieve (límite del mapa)     ~  río
        //  T U V Y  edificios altos     H  casa     B  vereda y baranda del puente     C  pilar del puente
        //  m b c i e a y r u g f K  fachadas de los lugares donde se puede entrar
        static DatosMapa avellaneda, puente;
        public static int MapaActual = 1;

        public static DatosMapa Datos { get { return DatosDe(MapaActual); } }

        public static DatosMapa DatosDe(int id)
        {
            if (id == 2) return puente ?? (puente = MapasGenerados.Puente());
            return avellaneda ?? (avellaneda = MapasGenerados.Avellaneda());
        }

        public static string[] Mapa { get { return Datos.filas; } }
        public static List<EntidadDef> Entidades { get { return Datos.entidades; } }
        public static List<EscenarioDef> Escenarios { get { return Datos.escenarios; } }
        public static List<Postal> Postales { get { return Datos.postales; } }
        public static float InicioX { get { return Datos.inicioX; } }
        public static float InicioY { get { return Datos.inicioY; } }
        public static float InicioAngulo { get { return Datos.inicioAngulo; } }

        // Armas y balance del combate
        public const int IdMunicion = 10, IdPistola = 11, IdCuchillo = 12, IdPalo = 13, IdBate = 14, IdMapa = 9;
        public const int CargadorCapacidad = 15;       // Glock 19
        public const float VidaBicho = 100f;           // 3 disparos de pistola
        public const float DanioBicho = 20f;           // 20 % de la salud por mordida
        public static readonly Dictionary<int, ArmaDef> Armas = new Dictionary<int, ArmaDef>
        {
            { 0, new ArmaDef { idRecurso = 0, nombre = "Puños", tipo = TipoArma.CuerpoACuerpo, danio = 8f, enfriamiento = 0.6f, alcance = 1.0f, empuje = 0.1f } },
            { IdPistola, new ArmaDef { idRecurso = IdPistola, nombre = "Glock 19", tipo = TipoArma.Pistola, danio = 34f, enfriamiento = 0.28f, alcance = 16f, empuje = 0.15f } },
            { IdCuchillo, new ArmaDef { idRecurso = IdCuchillo, nombre = "Cuchillo", tipo = TipoArma.CuerpoACuerpo, danio = 26f, enfriamiento = 0.45f, alcance = 1.3f, empuje = 0.12f } },
            { IdBate, new ArmaDef { idRecurso = IdBate, nombre = "Bate de béisbol", tipo = TipoArma.CuerpoACuerpo, danio = 28f, enfriamiento = 0.9f, alcance = 1.6f, empuje = 0.55f } },
            { IdPalo, new ArmaDef { idRecurso = IdPalo, nombre = "Palo de escoba", tipo = TipoArma.CuerpoACuerpo, danio = 14f, enfriamiento = 0.55f, alcance = 1.6f, empuje = 0.3f } },
        };

        public static ArmaDef Arma(int idRecurso)
        {
            ArmaDef a;
            return Armas.TryGetValue(idRecurso, out a) ? a : Armas[0];
        }

        public static readonly string[] FrasesFamilia =
        {
            "Celeste... Mateo... Patricio... tienen que estar bien.",
            "Hace días que no los veo. Los voy a encontrar.",
            "Mateo decía que la nieve era linda. Si la viera ahora...",
            "Patricio todavía duerme con la luz prendida. ¿Tendrá luz donde esté?",
            "Celeste, aguantá. Ya voy.",
            "Si les pasó algo... no. No voy a pensar en eso.",
            "Lo que daría por escuchar la risa de los chicos otra vez.",
            "Extraño a mi familia. Muchísimo.",
        };

        public static readonly Dictionary<int, RecursoDef> Recursos = new Dictionary<int, RecursoDef>
        {
            { 1, new RecursoDef { id = 1, nombre = "Medicamento", tipo = "Curación", utilidad = "Recuperar vida", categoria = Categoria.Medicamentos, curacion = 35, sprite = "botiquin",
                descripcion = "Botiquín con vendas y analgésicos. Recupera 35 de salud." } },
            { 2, new RecursoDef { id = 2, nombre = "Lata de conservas", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 15, sprite = "lata",
                descripcion = "Arvejas en lata. Todavía se pueden comer. Recupera 15 de salud." } },
            { 3, new RecursoDef { id = 3, nombre = "Paquete de galletitas", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 10, sprite = "galletitas",
                descripcion = "Galletitas de agua, un poco húmedas. Recupera 10 de salud." } },
            { 4, new RecursoDef { id = 4, nombre = "Choripán congelado", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 20, sprite = "choripan",
                descripcion = "Alguien lo dejó en la parrilla. Recupera 20 de salud." } },
            { 5, new RecursoDef { id = 5, nombre = "Lona impermeable", tipo = "Material", utilidad = "Fabricar el traje", categoria = Categoria.Materiales, sprite = "lona",
                descripcion = "Lona gruesa de una carpa. Sirve para el traje aislante." } },
            { 6, new RecursoDef { id = 6, nombre = "Alambre y cinta", tipo = "Material", utilidad = "Fabricar el traje", categoria = Categoria.Materiales, sprite = "alambre",
                descripcion = "Para unir las piezas del traje aislante." } },
            { 7, new RecursoDef { id = 7, nombre = "Chatarra", tipo = "Material", utilidad = "Reparaciones", categoria = Categoria.Materiales, sprite = "chatarra",
                descripcion = "Piezas de metal. Pueden servir más adelante." } },
            { 8, new RecursoDef { id = 8, nombre = "Foto familiar", tipo = "Objeto", utilidad = "Recuerdo", categoria = Categoria.ObjetosEncontrados, sprite = "foto",
                descripcion = "Una foto de antes de la nevada. No sirve para sobrevivir, pero ayuda." } },
            { 9, new RecursoDef { id = 9, nombre = "Mapa de Avellaneda", tipo = "Objeto", utilidad = "Orientación", categoria = Categoria.ObjetosEncontrados, sprite = "mapa",
                descripcion = "Marcado a mano. Con la tecla M (o equipado en la hotbar) se ve toda la zona y los lugares donde entrar." } },
            { 10, new RecursoDef { id = 10, nombre = "Munición 9 mm", tipo = "Munición", utilidad = "Recargar la Glock 19", categoria = Categoria.Armas, sprite = "municion",
                descripcion = "Balas 9 mm para tu arma reglamentaria. Se cargan con R." } },
            { 11, new RecursoDef { id = 11, nombre = "Glock 19", tipo = "Arma de fuego", utilidad = "Disparar (clic izquierdo)", categoria = Categoria.Armas, sprite = "glock", arma = TipoArma.Pistola,
                descripcion = "Tu arma reglamentaria de policía. Cargador de 15. Daño alto: un cascarudo cae con 3 disparos." } },
            { 12, new RecursoDef { id = 12, nombre = "Cuchillo", tipo = "Cuerpo a cuerpo", utilidad = "Atacar (clic izquierdo)", categoria = Categoria.Armas, sprite = "cuchillo", arma = TipoArma.CuerpoACuerpo,
                descripcion = "Cuchillo de cocina. Rápido y filoso: un cascarudo cae con 4 golpes." } },
            { 13, new RecursoDef { id = 13, nombre = "Palo de escoba", tipo = "Cuerpo a cuerpo", utilidad = "Atacar (clic izquierdo)", categoria = Categoria.Armas, sprite = "palo", arma = TipoArma.CuerpoACuerpo,
                descripcion = "Largo pero flojo. Sirve para ganar distancia. Hacen falta unos 8 golpes." } },
            { 14, new RecursoDef { id = 14, nombre = "Bate de béisbol", tipo = "Cuerpo a cuerpo", utilidad = "Atacar (clic izquierdo)", categoria = Categoria.Armas, sprite = "bate", arma = TipoArma.CuerpoACuerpo,
                descripcion = "Pesado y lento, pero empuja al bicho hacia atrás. Un cascarudo cae con 4 golpes." } },
            { 15, new RecursoDef { id = 15, nombre = "Carne congelada", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 5, sprite = "carne",
                descripcion = "Un trozo de carne dura como piedra. Es lo peor que se puede comer. Recupera 5 de salud." } },
            { 16, new RecursoDef { id = 16, nombre = "Hamburguesa congelada", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 22, sprite = "hamburguesa",
                descripcion = "Quedó en la cocina del local. Recupera 22 de salud." } },
            { 17, new RecursoDef { id = 17, nombre = "Gaseosa", tipo = "Bebida", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 8, sprite = "gaseosa",
                descripcion = "Una gaseosa helada. Recupera 8 de salud." } },
        };

        // Objetivos del prólogo (1 a 4) y del capítulo 2 (5 a 8). Hay uno activo a la vez;
        // conseguir el arma es un pedido opcional que queda en segundo plano (ver EternautaGame).
        public static readonly List<ObjetivoDef> Objetivos = new List<ObjetivoDef>
        {
            new ObjetivoDef { id = 1, titulo = "Armar el traje aislante", descripcion = "Abrí los cajones del departamento, reuní 2 materiales y usá la mesa de trabajo del taller.",
                idCapitulo = 1, idEscenario = 3, idEvento = 3, zona = "Departamento", destino = "Taller",
                marcas = { new Destino(1, 117.2f, 108.3f) } },
            new ObjetivoDef { id = 2, titulo = "Buscar comida en el barrio", descripcion = "Abrí heladeras y cajones: el McDonald's, el Burger King y el kiosco son los más cercanos.",
                idCapitulo = 1, idEscenario = 2, idEvento = 4, zona = "Avellaneda", destino = "Comercios del barrio",
                marcas = { new Destino(1, 66.5f, 8.2f), new Destino(1, 82.5f, 8.2f), new Destino(1, 50.5f, 75.5f) } },
            new ObjetivoDef { id = 3, titulo = "Hablar con el sobreviviente", descripcion = "Hay una luz encendida en el kiosco, junto a la Plaza Alsina.",
                idCapitulo = 1, idEscenario = 5, idEvento = 1, zona = "Plaza Alsina", destino = "Kiosco",
                marcas = { new Destino(1, 50.5f, 75.5f) } },
            new ObjetivoDef { id = 4, titulo = "Llegar al Puente Pueyrredón", descripcion = "Seguí la Av. Mitre y la calle Berutti hasta el acceso al puente, en el extremo norte.",
                idCapitulo = 1, idEscenario = 1, idEvento = 6, zona = "Avellaneda", destino = "Acceso al puente",
                marcas = { new Destino(1, 131.5f, 5.2f) } },
            new ObjetivoDef { id = 5, titulo = "Encontrar la radio", descripcion = "Una casa abandonada junto al acceso al puente tiene una radio antigua. Quizás diga qué pasa del otro lado.",
                idCapitulo = 2, idEscenario = 2, idEvento = 2, zona = "Avellaneda", destino = "Casa abandonada",
                marcas = { new Destino(1, 131.5f, 8.5f) } },
            new ObjetivoDef { id = 6, titulo = "Cruzar el Puente Pueyrredón", descripcion = "Cruzá el puente hacia Capital: ahí resisten los militares y quizás haya noticias de tu familia.",
                idCapitulo = 2, idEscenario = 1, idEvento = 8, zona = "Avellaneda", destino = "Puente Pueyrredón",
                marcas = { new Destino(1, 131.5f, 5.2f), new Destino(2, 28f, 62f) } },
            new ObjetivoDef { id = 7, titulo = "Ayudar a los soldados", descripcion = "En la Av. 9 de Julio los militares pelean contra los bichos. Derrotá a los que quedan.",
                idCapitulo = 2, idEscenario = 3, idEvento = 9, zona = "Av. 9 de Julio", destino = "Obelisco",
                marcas = { new Destino(2, 27.5f, 38f) } },
            new ObjetivoDef { id = 8, titulo = "Hablar con el sargento", descripcion = "Contale al sargento que buscás a tu familia.",
                idCapitulo = 2, idEscenario = 3, idEvento = 10, zona = "Av. 9 de Julio", destino = "Sargento",
                marcas = { new Destino(2, 28f, 43.6f) } },
        };

        public static readonly PersonajeDef Informante = new PersonajeDef { id = 1, nombre = "El Informante", tipo = "Secundario", idEscenario = 5 };
        public static readonly PersonajeDef Sargento = new PersonajeDef { id = 2, nombre = "El Sargento", tipo = "Secundario", idEscenario = 3 };

        public static readonly List<DialogoDef> DialogosInformante = new List<DialogoDef>
        {
            new DialogoDef { id = 1, idPersonaje = 1, orden = 1, texto = "¿Quién anda ahí? ...Tranquilo, no tengo nada. Estoy herido." },
            new DialogoDef { id = 2, idPersonaje = 1, orden = 2, texto = "La nevada me agarró cruzando la plaza. Unos copos nomás... y mirá cómo quedé." },
            new DialogoDef { id = 3, idPersonaje = 1, orden = 3, texto = "Escuché que hay grupos organizados del otro lado del Riachuelo. Y ojo con los bichos: andan por todo el barrio." },
            new DialogoDef { id = 4, idPersonaje = 1, orden = 4, texto = "Los militares están en Capital. Dicen que resisten cerca del Obelisco." },
            // orden 5: decisión (darle un medicamento o no)
            new DialogoDef { id = 5, idPersonaje = 1, orden = 6, texto = "Seguí la Av. Mitre y la calle Berutti hasta el extremo norte: ahí empieza el acceso al Puente Pueyrredón. Y no te saques el traje por nada del mundo." },
        };

        public const string DecisionPregunta = "El informante mira tu botiquín. Tiene las manos heladas.";
        public const string DecisionDar = "Darle un medicamento";
        public const string DecisionNoDar = "Guardarlo para vos";
        public const string RespuestaDar = "Gracias, pibe... Tomá, es lo único que tengo. Un mapa del barrio.";
        public const string RespuestaNoDar = "Entiendo. Cada uno se las arregla como puede. Tomá igual, el mapa te va a servir más a vos que a mí.";
        public const string RespuestaSinMedicamento = "No tenés nada para darme, ya sé. Está bien. Llevate el mapa.";

        // Transmisión de la radio antigua (capítulo 2). La última línea se corta en la mitad.
        public static readonly string[] Radio =
        {
            "...kssshh... a quien esté escuchando... habla el Puesto de Mando del Obelisco... el puente Pueyrredón sigue en pie...",
            "...los militares resistimos en la 9 de Julio... los bichos avanzan desde el sur... repito... desde el sur...",
            "...civiles: crucen por el puente y no se detengan... los vamos a evacuar...",
            "...Pablo... ¿me escuchás?... Pablo, soy yo... estamos bien... los chicos están conmigo... estamos en—",
        };
        public const float RadioIntervalo = 4.4f;     // segundos entre líneas
        public const float RadioUltimaLinea = 2.4f;   // la última se corta antes de terminar
        public const string RadioCorte = "La señal se cortó de golpe. Esa voz... era Celeste.";

        // Diálogo del sargento (capítulo 2). "@NOMBRE|texto" cambia el que habla.
        public static readonly string[] DialogoSargento =
        {
            "@EL SARGENTO|¡Alto ahí! ... Un civil vivo. Esos bichos casi nos pasan por encima, y vos los ayudaste a frenar.",
            "@PABLO|Soy Pablo. Vengo de Avellaneda. Busco a mi familia: mi esposa Celeste y mis hijos, Mateo y Patricio.",
            "@EL SARGENTO|Mucha gente cruzó hacia el Obelisco antes de que cayera el perímetro. No llevamos listas... lo lamento.",
            "@PABLO|Escuché la voz de mi esposa por la radio. Tengo que seguir buscando.",
            "@EL SARGENTO|Entonces tomá munición. Si los encontrás, que Dios los guíe. Nosotros sostenemos la avenida.",
        };

        public static readonly List<EventoDef> Eventos = new List<EventoDef>
        {
            new EventoDef { id = 1, nombre = "Aparición del sobreviviente", descripcion = "El jugador encuentra a un sobreviviente", idCapitulo = 1, idPersonaje = 1 },
            new EventoDef { id = 2, nombre = "Transmisión de radio", descripcion = "La radio antigua capta la voz de Celeste y el aviso de los militares del Obelisco", idCapitulo = 2 },
            new EventoDef { id = 3, nombre = "Traje aislante", descripcion = "El protagonista arma su traje aislante", idCapitulo = 1 },
            new EventoDef { id = 4, nombre = "Salida del departamento", descripcion = "El protagonista sale a la nieve por primera vez", idCapitulo = 1 },
            new EventoDef { id = 5, nombre = "Ayudar al informante", descripcion = "El jugador le da un medicamento al informante", idCapitulo = 1, idPersonaje = 1 },
            new EventoDef { id = 6, nombre = "Llegada al acceso del puente", descripcion = "El protagonista llega al acceso del puente", idCapitulo = 1 },
            new EventoDef { id = 7, nombre = "El Obelisco a lo lejos", descripcion = "Secuencia final del prólogo", idCapitulo = 1 },
            new EventoDef { id = 8, nombre = "Cruce del puente", descripcion = "El protagonista cruza el Puente Pueyrredón", idCapitulo = 2 },
            new EventoDef { id = 9, nombre = "Batalla en la 9 de Julio", descripcion = "El protagonista ayuda a los soldados", idCapitulo = 2 },
            new EventoDef { id = 10, nombre = "El sargento", descripcion = "Conversación con el sargento: fin del capítulo 2", idCapitulo = 2, idPersonaje = 2 },
            new EventoDef { id = 11, nombre = "Arma encontrada", descripcion = "El protagonista recupera su arma reglamentaria", idCapitulo = 1 },
        };

        public static RecursoDef Recurso(int id)
        {
            RecursoDef r;
            return Recursos.TryGetValue(id, out r) ? r : null;
        }

        // Zona (calle, plaza o lugar) en la que está el punto; sirve para el cartel de zona y las capturas.
        public static EscenarioDef EscenarioEn(float x, float y)
        {
            var d = Datos;
            int cx = (int)x, cy = (int)y;
            if (cx >= 0 && cy >= 0 && cy < d.zonaFilas.Length && cx < d.zonaFilas[cy].Length)
            {
                int k = d.zonaClaves.IndexOf(d.zonaFilas[cy][cx]);
                if (k >= 0 && k < d.zonaIds.Length)
                {
                    var e = EscenarioPorId(d, d.zonaIds[k]);
                    if (e != null) return e;
                }
            }
            return d.escenarios[0];
        }

        static EscenarioDef EscenarioPorId(DatosMapa d, int id)
        {
            foreach (var e in d.escenarios) if (e.id == id) return e;
            return null;
        }

        public static EscenarioDef EscenarioPorId(int id) { return EscenarioPorId(Datos, id); }
    }
}
