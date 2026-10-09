using System.Collections.Generic;

namespace Eternauta.Beta
{
    // Contenido fijo del juego (Etapa 13: Capítulo, Escenario, Objeto, Personaje,
    // Diálogo, EventoNarrativo, Objetivo y Recurso). No cambia al jugar.
    // Los IDs son los mismos que se guardan en la partida (archivo JSON).
    // Para la beta está escrito en C# para que no haga falta crear assets a mano;
    // más adelante se puede pasar a ScriptableObjects como propone la Etapa 14.

    public enum Categoria { Medicamentos, Comida, Materiales, ObjetosEncontrados }

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

    public enum TipoEntidad { Decoracion, Recurso, Personaje, Radio, MesaTrabajo }

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
        public int completaObjetivo; // id de objetivo que se completa al recogerlo (0 = ninguno)
        public string nombre;
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
            "Sobreviviste encerrado en tu casa, frente a Plaza Alsina.\nPero la comida se terminó.",
            "Para salir vas a necesitar un traje aislante.",
        };

        // Leyenda del mapa (cada carácter es una celda):
        //  #  ladrillo         H  casa / revoque     P  persiana metálica
        //  K  cartel almacén   W  ventana tapiada    C  cartel del puente
        //  B  baranda puente   D  puerta (se abre con E)
        //  .  calle nevada     =  asfalto del puente
        //  ,  piso de madera (interior)   ;  baldosas (interior)
        // Norte = arriba. El Obelisco está hacia el norte, del otro lado del Riachuelo.
        public static readonly string[] Mapa =
        {
            "###########BBBBBBBBB###########", // 0  fin del puente
            "###########B=======B###########", // 1
            "###########B=======B###########", // 2
            "###########B=======B###########", // 3
            "###########B=======B###########", // 4
            "###########B=======B###########", // 5
            "###########B=======B###########", // 6
            "###########B=======B###########", // 7
            "#######WWWCB=======BCPPP#######", // 8  entrada al puente
            "######H.................P######", // 9  Av. Mitre
            "######H.................P######", // 10
            "######W.................W######", // 11
            "######H.................P######", // 12
            "#######.................#######", // 13
            "######H.................W######", // 14
            "######H.................P######", // 15
            "#HHHHHH.................PPPPPP#", // 16
            "#.............................#", // 17 Plaza Alsina
            "#HHHHHHHH.............PPPPPPPP#", // 18
            "#H,,,H,,H.............P;;;;;;P#", // 19 casa abandonada (oeste) / almacén (este)
            "#H,,,H,,H.............K;;;;;;P#", // 20
            "#H,,,,,,H.............D;;;;;;P#", // 21
            "#H,,,,,,D.............P;;;;;;P#", // 22
            "#H,,,,,,H.............K;;;;;;P#", // 23
            "#H,,,,,,H.............P;;;;;;P#", // 24
            "#H,,,,,,H.............PPPPPPPP#", // 25
            "#HHHHHHHH.....................#", // 26
            "#.............................#", // 27
            "#.............................#", // 28
            "#.............................#", // 29
            "#.............................#", // 30
            "#########HHHHHHDHHHHHH#########", // 31 refugio (puerta norte)
            "#########H,,,,,,,,,,,H#########", // 32
            "#########H,,,,,,,,,,,H#########", // 33
            "#########H,,,,,,,,,,,H#########", // 34
            "#########HHH,HHHHH,HHH#########", // 35
            "#########H,,,,,H,,,,,H#########", // 36 taller (oeste) / dormitorio (este)
            "#########H,,,,,H,,,,,H#########", // 37
            "#########H,,,,,H,,,,,H#########", // 38
            "#########H,,,,,H,,,,,H#########", // 39
            "###############################", // 40
        };

        public const float InicioX = 15.5f, InicioY = 33.6f;
        public const float InicioAngulo = -90f; // mirando al norte

        public const float LimiteNortePuente = 9.0f;   // no se cruza antes de tiempo
        public const float LineaFinal = 6.2f;          // fin de la demo

        public static readonly Dictionary<int, RecursoDef> Recursos = new Dictionary<int, RecursoDef>
        {
            { 1, new RecursoDef { id = 1, nombre = "Medicamento", tipo = "Curación", utilidad = "Recuperar vida", categoria = Categoria.Medicamentos, curacion = 35, sprite = "botiquin",
                descripcion = "Botiquín con vendas y analgésicos. Recupera 35 de salud." } },
            { 2, new RecursoDef { id = 2, nombre = "Lata de conservas", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 15, sprite = "lata",
                descripcion = "Arvejas en lata. Todavía se pueden comer. Recupera 15 de salud." } },
            { 3, new RecursoDef { id = 3, nombre = "Paquete de galletitas", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 10, sprite = "galletitas",
                descripcion = "Galletitas de agua, un poco húmedas. Recupera 10 de salud." } },
            { 4, new RecursoDef { id = 4, nombre = "Choripán congelado", tipo = "Alimento", utilidad = "Recuperar energía", categoria = Categoria.Comida, curacion = 20, sprite = "choripan",
                descripcion = "Alguien lo dejó en la parrilla del almacén. Recupera 20 de salud." } },
            { 5, new RecursoDef { id = 5, nombre = "Lona impermeable", tipo = "Material", utilidad = "Fabricar el traje", categoria = Categoria.Materiales, sprite = "lona",
                descripcion = "Lona gruesa de una carpa. Sirve para el traje aislante." } },
            { 6, new RecursoDef { id = 6, nombre = "Alambre y cinta", tipo = "Material", utilidad = "Fabricar el traje", categoria = Categoria.Materiales, sprite = "alambre",
                descripcion = "Para unir las piezas del traje aislante." } },
            { 7, new RecursoDef { id = 7, nombre = "Chatarra", tipo = "Material", utilidad = "Reparaciones", categoria = Categoria.Materiales, sprite = "chatarra",
                descripcion = "Piezas de metal. Pueden servir más adelante." } },
            { 8, new RecursoDef { id = 8, nombre = "Foto familiar", tipo = "Objeto", utilidad = "Recuerdo", categoria = Categoria.ObjetosEncontrados, sprite = "foto",
                descripcion = "Una foto de antes de la nevada. No sirve para sobrevivir, pero ayuda." } },
            { 9, new RecursoDef { id = 9, nombre = "Mapa de Avellaneda", tipo = "Objeto", utilidad = "Orientación", categoria = Categoria.ObjetosEncontrados, sprite = "mapa",
                descripcion = "Marcado a mano: la Av. Mitre lleva al Puente Pueyrredón." } },
        };

        public static readonly List<ObjetivoDef> Objetivos = new List<ObjetivoDef>
        {
            new ObjetivoDef { id = 1, titulo = "Armar el traje aislante", descripcion = "Reuní 2 materiales y usá la mesa de trabajo del taller.",
                idCapitulo = 1, idEscenario = 3, idEvento = 3, zona = "Refugio", destino = "Taller del refugio" },
            new ObjetivoDef { id = 2, titulo = "Buscar comida en la casa abandonada", descripcion = "Cruzá Plaza Alsina hasta la casa abandonada del lado oeste.",
                idCapitulo = 1, idEscenario = 2, idEvento = 4, zona = "Avellaneda", destino = "Casa abandonada" },
            new ObjetivoDef { id = 3, titulo = "Hablar con el sobreviviente", descripcion = "Hay una luz encendida en el almacén de la plaza.",
                idCapitulo = 1, idEscenario = 5, idEvento = 1, zona = "Plaza Alsina", destino = "Almacén" },
            new ObjetivoDef { id = 4, titulo = "Llegar al Puente Pueyrredón", descripcion = "Encontrar una ruta hacia Capital.",
                idCapitulo = 1, idEscenario = 1, idEvento = 6, zona = "Avellaneda", destino = "Puente Pueyrredón" },
        };

        public static readonly List<EscenarioDef> Escenarios = new List<EscenarioDef>
        {
            new EscenarioDef { id = 3, nombre = "Refugio", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "Casa del protagonista. Calidez frágil en el caos.", x0 = 9, y0 = 31, x1 = 21, y1 = 40 },
            new EscenarioDef { id = 2, nombre = "Casa abandonada", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "Vivienda donde se encuentra la radio antigua.", x0 = 1, y0 = 18, x1 = 8, y1 = 26 },
            new EscenarioDef { id = 5, nombre = "Almacén", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "Almacén de barrio donde se refugió el informante.", x0 = 22, y0 = 18, x1 = 29, y1 = 25 },
            new EscenarioDef { id = 1, nombre = "Puente Pueyrredón", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "Puente cubierto de nieve que conecta con Capital.", x0 = 0, y0 = 0, x1 = 30, y1 = 8 },
            new EscenarioDef { id = 6, nombre = "Avenida Mitre", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "Calles abandonadas, autos volcados y señales caídas.", x0 = 0, y0 = 9, x1 = 30, y1 = 16 },
            new EscenarioDef { id = 4, nombre = "Plaza Alsina", ubicacion = "Avellaneda", idCapitulo = 1, descripcion = "La plaza, bajo metros de nieve.", x0 = 0, y0 = 17, x1 = 30, y1 = 30 },
        };

        public static readonly PersonajeDef Informante = new PersonajeDef { id = 1, nombre = "El Informante", tipo = "Secundario", idEscenario = 5 };

        public static readonly List<DialogoDef> DialogosInformante = new List<DialogoDef>
        {
            new DialogoDef { id = 1, idPersonaje = 1, orden = 1, texto = "¿Quién anda ahí? ...Tranquilo, no tengo nada. Estoy herido." },
            new DialogoDef { id = 2, idPersonaje = 1, orden = 2, texto = "La nevada me agarró cruzando la plaza. Unos copos nomás... y mirá cómo quedé." },
            new DialogoDef { id = 3, idPersonaje = 1, orden = 3, texto = "Escuché en la radio que hay grupos organizados del otro lado del Riachuelo." },
            new DialogoDef { id = 4, idPersonaje = 1, orden = 4, texto = "Los militares están en Capital. Dicen que resisten cerca del Obelisco." },
            // orden 5: decisión (darle un medicamento o no)
            new DialogoDef { id = 5, idPersonaje = 1, orden = 6, texto = "Seguí la Av. Mitre hasta el Puente Pueyrredón. Y no te saques el traje por nada del mundo." },
        };

        public const string DecisionPregunta = "El informante mira tu botiquín. Tiene las manos heladas.";
        public const string DecisionDar = "Darle un medicamento";
        public const string DecisionNoDar = "Guardarlo para vos";
        public const string RespuestaDar = "Gracias, pibe... Tomá, es lo único que tengo. Un mapa del barrio.";
        public const string RespuestaNoDar = "Entiendo. Cada uno se las arregla como puede.";
        public const string RespuestaSinMedicamento = "No tenés nada para darme, ya sé. Está bien.";

        public static readonly string[] Radio =
        {
            "...kssshh... a todos los sobrevivientes... no salgan sin protección...",
            "...grupos organizados... del otro lado del Riachuelo... kssshh...",
            "...repito: eviten todo contacto con la nieve... ...kssshhhhh",
        };

        public static readonly List<EventoDef> Eventos = new List<EventoDef>
        {
            new EventoDef { id = 1, nombre = "Aparición del sobreviviente", descripcion = "El jugador encuentra a un sobreviviente", idCapitulo = 1, idPersonaje = 1 },
            new EventoDef { id = 2, nombre = "Transmisión de radio", descripcion = "La radio antigua capta una transmisión", idCapitulo = 1 },
            new EventoDef { id = 3, nombre = "Traje aislante", descripcion = "El protagonista arma su traje aislante", idCapitulo = 1 },
            new EventoDef { id = 4, nombre = "Salida del refugio", descripcion = "El protagonista sale a la nieve por primera vez", idCapitulo = 1 },
            new EventoDef { id = 5, nombre = "Ayudar al informante", descripcion = "El jugador le da un medicamento al informante", idCapitulo = 1, idPersonaje = 1 },
            new EventoDef { id = 6, nombre = "Llegada al Puente Pueyrredón", descripcion = "El protagonista llega al puente", idCapitulo = 1 },
            new EventoDef { id = 7, nombre = "El Obelisco a lo lejos", descripcion = "Secuencia final del prólogo", idCapitulo = 1 },
        };

        public static readonly List<EntidadDef> Entidades = new List<EntidadDef>
        {
            // --- Refugio ---
            new EntidadDef { id = 10, sprite = "mesa_trabajo", x = 11.5f, y = 37.4f, tipo = TipoEntidad.MesaTrabajo, solido = true, radio = 0.45f, nombre = "Mesa de trabajo" },
            new EntidadDef { id = 11, sprite = "lona", x = 13.6f, y = 38.6f, tipo = TipoEntidad.Recurso, idRecurso = 5 },
            new EntidadDef { id = 12, sprite = "alambre", x = 19.5f, y = 38.6f, tipo = TipoEntidad.Recurso, idRecurso = 6 },
            new EntidadDef { id = 13, sprite = "botiquin", x = 19.5f, y = 36.5f, tipo = TipoEntidad.Recurso, idRecurso = 1 },
            new EntidadDef { id = 14, sprite = "cama", x = 16.8f, y = 38.3f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.5f },
            new EntidadDef { id = 15, sprite = "lampara", x = 10.6f, y = 32.6f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 16, sprite = "foto", x = 20.4f, y = 32.6f, tipo = TipoEntidad.Recurso, idRecurso = 8 },
            new EntidadDef { id = 17, sprite = "mesa", x = 12.3f, y = 33.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.45f },

            // --- Plaza Alsina ---
            new EntidadDef { id = 40, sprite = "monumento", x = 15.5f, y = 23.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.6f },
            new EntidadDef { id = 41, sprite = "arbol", x = 10.5f, y = 19.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 42, sprite = "arbol", x = 20.5f, y = 19.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 43, sprite = "arbol", x = 10.0f, y = 28.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 44, sprite = "arbol", x = 25.5f, y = 28.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 45, sprite = "arbol", x = 4.5f, y = 28.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 46, sprite = "arbol", x = 4.0f, y = 17.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 47, sprite = "arbol", x = 26.5f, y = 17.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },
            new EntidadDef { id = 48, sprite = "banco", x = 12.6f, y = 23.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.4f },
            new EntidadDef { id = 49, sprite = "banco", x = 18.4f, y = 23.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.4f },
            new EntidadDef { id = 50, sprite = "farola", x = 11.0f, y = 26.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 51, sprite = "farola", x = 20.0f, y = 21.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 52, sprite = "auto_nevado", x = 24.5f, y = 29.3f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.7f },
            new EntidadDef { id = 53, sprite = "cartel_plaza", x = 13.4f, y = 29.6f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 54, sprite = "arbol", x = 17.5f, y = 27.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.25f },

            // --- Casa abandonada ---
            new EntidadDef { id = 20, sprite = "lata", x = 3.4f, y = 19.6f, tipo = TipoEntidad.Recurso, idRecurso = 2, completaObjetivo = 2 },
            new EntidadDef { id = 21, sprite = "galletitas", x = 6.5f, y = 24.6f, tipo = TipoEntidad.Recurso, idRecurso = 3 },
            new EntidadDef { id = 22, sprite = "chatarra", x = 2.5f, y = 22.6f, tipo = TipoEntidad.Recurso, idRecurso = 7 },
            new EntidadDef { id = 23, sprite = "radio", x = 2.6f, y = 25.3f, tipo = TipoEntidad.Radio, solido = true, radio = 0.35f, nombre = "Radio antigua" },
            new EntidadDef { id = 24, sprite = "silla_rota", x = 6.6f, y = 20.4f, tipo = TipoEntidad.Decoracion },

            // --- Almacén ---
            new EntidadDef { id = 30, sprite = "informante", x = 27.4f, y = 22.4f, tipo = TipoEntidad.Personaje, solido = true, radio = 0.35f, nombre = "El Informante" },
            new EntidadDef { id = 31, sprite = "choripan", x = 24.4f, y = 23.9f, tipo = TipoEntidad.Recurso, idRecurso = 4 },
            new EntidadDef { id = 32, sprite = "botiquin", x = 28.4f, y = 19.6f, tipo = TipoEntidad.Recurso, idRecurso = 1 },
            new EntidadDef { id = 33, sprite = "estanteria", x = 25.5f, y = 19.35f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.4f },
            new EntidadDef { id = 34, sprite = "vela", x = 26.4f, y = 21.7f, tipo = TipoEntidad.Decoracion },
            new EntidadDef { id = 35, sprite = "estanteria", x = 23.4f, y = 19.35f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.4f },

            // --- Avenida Mitre ---
            new EntidadDef { id = 60, sprite = "auto_volcado", x = 9.6f, y = 12.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.8f },
            new EntidadDef { id = 61, sprite = "auto_nevado", x = 19.6f, y = 10.4f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.8f },
            new EntidadDef { id = 62, sprite = "auto_nevado", x = 14.2f, y = 14.6f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.8f },
            new EntidadDef { id = 63, sprite = "semaforo", x = 22.6f, y = 13.5f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.2f },
            new EntidadDef { id = 64, sprite = "cartel_caido", x = 12.5f, y = 10.0f, tipo = TipoEntidad.Decoracion },
            new EntidadDef { id = 65, sprite = "cartel_avenida", x = 7.6f, y = 15.4f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 66, sprite = "farola", x = 16.8f, y = 12.2f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 67, sprite = "auto_volcado", x = 21.0f, y = 15.2f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.8f },

            // --- Puente Pueyrredón ---
            new EntidadDef { id = 70, sprite = "farola", x = 12.3f, y = 6.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 71, sprite = "farola", x = 18.7f, y = 6.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 72, sprite = "auto_volcado", x = 17.4f, y = 2.7f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.75f },
            new EntidadDef { id = 73, sprite = "farola", x = 12.3f, y = 2.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
            new EntidadDef { id = 74, sprite = "farola", x = 18.7f, y = 2.0f, tipo = TipoEntidad.Decoracion, solido = true, radio = 0.15f },
        };

        // Lugares para capturas (modo desarrollador, F4). Ángulo en grados: -90 = norte.
        public static readonly List<Postal> Postales = new List<Postal>
        {
            new Postal { nombre = "Refugio - living", x = 15.5f, y = 33.6f, angulo = 200f },
            new Postal { nombre = "Refugio - taller", x = 13.4f, y = 36.4f, angulo = 145f },
            new Postal { nombre = "Plaza Alsina", x = 15.5f, y = 30.2f, angulo = -90f },
            new Postal { nombre = "Casa abandonada", x = 6.6f, y = 22.4f, angulo = 165f },
            new Postal { nombre = "Almacén - informante", x = 24.0f, y = 21.6f, angulo = 15f },
            new Postal { nombre = "Avenida Mitre", x = 17.6f, y = 17.2f, angulo = -100f },
            new Postal { nombre = "Entrada al puente", x = 15.5f, y = 10.2f, angulo = -90f },
            new Postal { nombre = "Puente Pueyrredón", x = 15.5f, y = 7.6f, angulo = -90f },
        };

        public static RecursoDef Recurso(int id)
        {
            RecursoDef r;
            return Recursos.TryGetValue(id, out r) ? r : null;
        }

        public static EscenarioDef EscenarioEn(float x, float y)
        {
            int cx = (int)x, cy = (int)y;
            foreach (var e in Escenarios)
                if (cx >= e.x0 && cx <= e.x1 && cy >= e.y0 && cy <= e.y1) return e;
            return Escenarios[Escenarios.Count - 1];
        }

        public static EscenarioDef EscenarioPorId(int id)
        {
            foreach (var e in Escenarios) if (e.id == id) return e;
            return null;
        }
    }
}
