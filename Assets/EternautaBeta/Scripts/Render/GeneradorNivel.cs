using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Datos de una puerta 3D (la hoja gira sobre la bisagra).
    public class PuertaInfo
    {
        public int x, y;
        public Vector3 bisagra;
        public float anguloCerrada, anguloAbierta; // giro en Y (grados)
        public float ancho;
    }

    // Arma el escenario 3D a partir de la grilla del mapa:
    // paredes con altura real, pisos, techos, fachadas de los lugares, muros de nieve (límite),
    // el Riachuelo con el Puente Pueyrredón y, según el mapa, la Capital o la Av. 9 de Julio.
    // Ejes: X = columna del mapa, Z = -fila (el norte del mapa es +Z), Y = altura.
    // Las mallas se parten en bloques de 32 x 32 celdas para que Unity descarte los que no se ven.
    public class GeneradorNivel
    {
        public const float AltoInterior = 1.4f;   // techo de las habitaciones (2,8 m)
        public const float AltoBase = 1.3f;       // planta baja de las fachadas
        public const float AltoPuerta = 1.05f;
        public const float NivelAgua = -3.0f;
        public const float AltoTechos = 2.6f;     // techo de las casas visto desde afuera
        public const float OrillaNorte = 30f;     // costa norte del Riachuelo (Z) vista desde Avellaneda
        public const int Bloque = 32;
        public const string ClaveMuroNorte = "@muroN";

        public readonly Dictionary<string, ConstructorMalla> mallas = new Dictionary<string, ConstructorMalla>();
        public ConstructorMalla obelisco;
        public ConstructorMalla cielo;
        public readonly List<PuertaInfo> puertas = new List<PuertaInfo>();
        public readonly List<Vector3> bengalas = new List<Vector3>();   // luces rojas cerca del Obelisco
        public readonly List<Vector3> fogonazos = new List<Vector3>();  // disparos a lo lejos
        public readonly HashSet<int> horneadas = new HashSet<int>();    // decoración pegada a la malla estática
        public Vector3 posObelisco;
        public bool hayObelisco;

        readonly Mundo mundo;
        readonly DatosMapa datos;
        readonly System.Random rnd = new System.Random(1234);
        readonly Vector3 obeliscoLocal = new Vector3(15.5f, 0f, 122f);
        string chunk = "";

        public GeneradorNivel(Mundo m) { mundo = m; datos = m.datos; }

        public static string ClaveBase(string clave)
        {
            int i = clave.IndexOf('@');
            return i < 0 ? clave : clave.Substring(0, i);
        }

        // Texturas que usa el escenario (se repiten: 1 celda = 1 repetición).
        public static Dictionary<string, Lienzo> Texturas()
        {
            var t = new Dictionary<string, Lienzo>
            {
                { "ladrillo", ArteProcedural.Ladrillo(true, 1) },
                { "ladrillo_liso", ArteProcedural.LadrilloLiso(31) },
                { "ladrillo_int", ArteProcedural.Ladrillo(false, 2) },
                { "revoque", ArteProcedural.Revoque(3) },
                { "fachada_beige", ArteProcedural.FachadaAlta(21, new Color32(150, 138, 118, 255)) },
                { "fachada_gris", ArteProcedural.FachadaAlta(22, new Color32(118, 124, 130, 255)) },
                { "fachada_ocre", ArteProcedural.FachadaAlta(23, new Color32(156, 118, 88, 255)) },
                { "fachada_blanca", ArteProcedural.FachadaAlta(24, new Color32(176, 174, 166, 255)) },
                { "empapelado", ArteProcedural.Empapelado(4) },
                { "persiana", ArteProcedural.Persiana(5) },
                { "azulejos", ArteProcedural.Azulejos(6) },
                { "ventana", ArteProcedural.VentanaTapiada(8) },
                { "hormigon", ArteProcedural.Hormigon(9) },
                { "hormigon_osc", ArteProcedural.Hormigon(10, 92) },
                { "nieve", ArteProcedural.PisoNieve(12) },
                { "asfalto", ArteProcedural.PisoAsfalto(13) },
                { "madera", ArteProcedural.PisoMadera(14) },
                { "baldosas", ArteProcedural.PisoBaldosas(15) },
                { "techo", ArteProcedural.Techo(16) },
                { "vereda", ArteProcedural.Vereda(17) },
                { "agua", ArteProcedural.Agua(18) },
                { "acero", ArteProcedural.Acero(19) },
                { "cielo", ArteProcedural.Cielo() },
                { "muro_nieve", ArteLugares.MuroNieve(41) },
                { "plaza", ArteLugares.Plaza(42) },
            };
            foreach (char c in "mbicieayruKgf")
            {
                string k = "loc_" + c;
                if (!t.ContainsKey(k)) t[k] = ArteLugares.Frente(c);
            }
            return t;
        }

        ConstructorMalla M(string clave)
        {
            string k = clave + chunk;
            ConstructorMalla m;
            if (!mallas.TryGetValue(k, out m))
            {
                m = clave == "modelos" ? Modelos.Nueva() : new ConstructorMalla();
                mallas[k] = m;
            }
            return m;
        }

        // ------------------------------------------------------------------
        // LUZ HORNEADA
        // ------------------------------------------------------------------
        public Vector3 LuzInterior(Vector3 p)
        {
            Vector3 l = new Vector3(0.40f, 0.42f, 0.46f);
            foreach (var luz in datos.luces)
            {
                float dx = p.x - luz.x, dy = p.y - luz.h, dz = p.z + luz.y;
                float r2 = luz.radio * luz.radio;
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 >= r2) continue;
                float k = 1f - Mathf.Sqrt(d2) / luz.radio;
                float f = k * k * 1.6f;
                l.x += luz.r * f; l.y += luz.g * f; l.z += luz.b * f;
            }
            return l;
        }

        public static Vector3 LuzExterior(Vector3 p)
        {
            float k = 0.86f + 0.14f * Mathf.Clamp01(p.y / 1.6f);
            return new Vector3(k, k, k * 1.03f);
        }

        // Luz para un objeto completo (se aplica como tinte al modelo).
        public Color LuzEn(float x, float y, float altura)
        {
            var p = new Vector3(x, altura, -y);
            var l = mundo.Interior(x, y) ? LuzInterior(p) : LuzExterior(p) * 1.06f;
            return new Color(l.x, l.y, l.z, 1f);
        }

        // ------------------------------------------------------------------
        // CARAS
        // ------------------------------------------------------------------
        // Rectángulo origen + U*s + V*t. Se da vuelta solo si hace falta para mirar hacia "normal".
        // Después de dar vuelta, U apunta a la derecha de quien mira la cara: u0 = borde izquierdo.
        static void Cara(ConstructorMalla m, Vector3 origen, Vector3 U, Vector3 V, Vector3 normal, float u0, float u1, float v0, float v1)
        {
            if (Vector3.Dot(Vector3.Cross(V, U), normal) < 0f) { origen += U; U = -U; }
            m.QuadUV(origen, origen + V, origen + V + U, origen + U, ConstructorMalla.Blanco,
                new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
        }

        struct Estilo
        {
            public string baseTex, altaTex, interiorTex;
            public float alto;
            public int runIdx, runLen;   // posición dentro de la fachada de un local (runLen 0 = textura común)
        }

        static bool EsLugar(char c) { return "mbicieayruKgf".IndexOf(c) >= 0; }

        static Estilo EstiloDe(char c)
        {
            switch (c)
            {
                case 'H': return new Estilo { baseTex = "revoque", altaTex = "fachada_beige", interiorTex = "empapelado", alto = AltoTechos };
                case 'T': return new Estilo { baseTex = "ladrillo", altaTex = "fachada_beige", interiorTex = "empapelado", alto = 4.4f };
                case 'U': return new Estilo { baseTex = "persiana", altaTex = "fachada_gris", interiorTex = "empapelado", alto = 5.2f };
                case 'V': return new Estilo { baseTex = "revoque", altaTex = "fachada_ocre", interiorTex = "empapelado", alto = 3.8f };
                case 'Y': return new Estilo { baseTex = "ladrillo", altaTex = "fachada_blanca", interiorTex = "empapelado", alto = 6.0f };
                case '#': case 'N': return new Estilo { baseTex = "muro_nieve", altaTex = "muro_nieve", interiorTex = "muro_nieve", alto = 2.3f };
                case 'C': return new Estilo { baseTex = "hormigon", altaTex = "hormigon", interiorTex = "hormigon", alto = 1.7f };
                case 'm': case 'b': case 'y': case 'f': case 'g': case 'K': case 'e':
                    return new Estilo { baseTex = "revoque", altaTex = "fachada_blanca", interiorTex = "azulejos", alto = 3.0f };
                case 'c': return new Estilo { baseTex = "ladrillo", altaTex = "ladrillo_liso", interiorTex = "ladrillo_int", alto = 3.0f };
                case 'i': case 'u': case 'a': case 'r':
                    return new Estilo { baseTex = "revoque", altaTex = "fachada_gris", interiorTex = "empapelado", alto = 3.0f };
                default: return new Estilo { baseTex = "ladrillo", altaTex = "ladrillo_liso", interiorTex = "ladrillo_int", alto = 3.2f };
            }
        }

        // Pared exterior por tramos: planta baja (textura con nieve al pie) y pisos de arriba.
        void ParedExterior(Vector3 origen, Vector3 U, Vector3 normal, float y0, float y1, Estilo e, float u0, float u1)
        {
            float corte = Mathf.Min(AltoBase, e.alto);
            if (y0 < corte)
            {
                float a = y0, b = Mathf.Min(y1, corte);
                var m = M(e.baseTex);
                m.interior = 0f; m.luzExtra = LuzExterior;
                float bu0 = u0, bu1 = u1;
                if (e.runLen > 0) { bu0 = (e.runIdx + u0) / e.runLen; bu1 = (e.runIdx + u1) / e.runLen; }
                Cara(m, origen + Vector3.up * a, U, Vector3.up * (b - a), normal, bu0, bu1, 1f - b / corte, 1f - a / corte);
            }
            if (y1 > corte)
            {
                float a = Mathf.Max(y0, corte), b = y1;
                var m = M(e.altaTex);
                m.interior = 0f; m.luzExtra = LuzExterior;
                Cara(m, origen + Vector3.up * a, U, Vector3.up * (b - a), normal, u0, u1, -(b - corte), -(a - corte));
            }
        }

        void ParedInterior(Vector3 origen, Vector3 U, Vector3 normal, float y0, float y1, Estilo e, float u0, float u1)
        {
            var m = M(e.interiorTex);
            m.interior = 1f; m.luzExtra = LuzInterior;
            Cara(m, origen + Vector3.up * y0, U, Vector3.up * (y1 - y0), normal, u0, u1, 1f - y1 / AltoInterior, 1f - y0 / AltoInterior);
        }

        static readonly int[] DX = { -1, 1, 0, 0 }, DY = { 0, 0, -1, 1 };

        // Borde de la celda (cx, cy) hacia la dirección d: origen, eje U (largo 1) y normal.
        static void Borde(int cx, int cy, int d, out Vector3 origen, out Vector3 U, out Vector3 normal)
        {
            switch (d)
            {
                case 0: origen = new Vector3(cx, 0, -cy); U = new Vector3(0, 0, -1); normal = Vector3.left; break;      // oeste
                case 1: origen = new Vector3(cx + 1, 0, -cy); U = new Vector3(0, 0, -1); normal = Vector3.right; break; // este
                case 2: origen = new Vector3(cx, 0, -cy); U = Vector3.right; normal = Vector3.forward; break;           // norte
                default: origen = new Vector3(cx, 0, -cy - 1); U = Vector3.right; normal = Vector3.back; break;        // sur
            }
        }

        static int CaraDe(Vector3 normal)
        {
            if (normal.x < -0.5f) return 0;
            if (normal.x > 0.5f) return 1;
            return normal.z > 0f ? 2 : 3;
        }

        char CeldaExt(int x, int y) { return mundo.Celda(x, y); }

        static bool Caminable(char c)
        {
            return c == '.' || c == '=' || c == ',' || c == ';' || c == 'd' || c == ':' || c == 'p';
        }

        // ¿La celda (x,y) pertenece a la misma fachada (mismo local) vista desde la cara d?
        bool EnFachada(int x, int y, int d, char letra)
        {
            char c = mundo.Celda(x, y);
            if (c != letra && !Mundo.EsPuerta(c)) return false;
            int nx = x + DX[d], ny = y + DY[d];
            return Caminable(mundo.Celda(nx, ny)) && !mundo.Interior(nx, ny);
        }

        // Estilo de la cara d de un local: si el tramo de pared tiene la puerta, lleva el cartel y las vidrieras.
        Estilo EstiloLugar(char letra, int cx, int cy, int d)
        {
            var e = EstiloDe(letra);
            int ax = d < 2 ? 0 : 1, ay = d < 2 ? 1 : 0;
            int x0 = cx, y0 = cy, x1 = cx, y1 = cy;
            while (EnFachada(x0 - ax, y0 - ay, d, letra)) { x0 -= ax; y0 -= ay; }
            while (EnFachada(x1 + ax, y1 + ay, d, letra)) { x1 += ax; y1 += ay; }
            int largo = (x1 - x0) + (y1 - y0) + 1;
            bool puerta = false;
            for (int k = 0; k < largo; k++) if (Mundo.EsPuerta(mundo.Celda(x0 + ax * k, y0 + ay * k))) puerta = true;
            if (!puerta || largo < 3) return e;
            int pos = (cx - x0) + (cy - y0);
            e.runIdx = (d == 0 || d == 3) ? pos : largo - 1 - pos;
            e.runLen = largo;
            e.baseTex = "loc_" + letra;
            return e;
        }

        // ------------------------------------------------------------------
        // GENERAR
        // ------------------------------------------------------------------
        public void Generar()
        {
            Modelos.Inicializar();
            GenerarGrilla();
            GenerarPuertas();
            if (datos.id == 1) GenerarExteriorAvellaneda();
            else GenerarExteriorPuente();
            HornearDecoracion();
            GenerarCielo();
        }

        // Desplaza en X todo lo que genera "gen" (reutiliza el código del puente y la Capital para cualquier x).
        void Desplazado(float dx, System.Action gen)
        {
            var antes = new Dictionary<ConstructorMalla, int>();
            foreach (var m in mallas.Values) antes[m] = m.v.Count;
            int nb = bengalas.Count, nf = fogonazos.Count;
            gen();
            foreach (var kv in mallas)
            {
                int i0;
                antes.TryGetValue(kv.Value, out i0);
                for (int i = i0; i < kv.Value.v.Count; i++) kv.Value.v[i] += new Vector3(dx, 0, 0);
            }
            for (int i = nb; i < bengalas.Count; i++) bengalas[i] += new Vector3(dx, 0, 0);
            for (int i = nf; i < fogonazos.Count; i++) fogonazos[i] += new Vector3(dx, 0, 0);
        }

        string TexPiso(char c)
        {
            switch (c)
            {
                case '.': return "nieve";
                case ':': return "vereda";
                case 'p': return "plaza";
                case '=': return "asfalto";
                case ';': return "baldosas";
                default: return "madera";
            }
        }

        bool EnZonaMuroNorte(int cx, int cy)
        {
            return datos.id == 1 && cy <= 1 && Mathf.Abs(cx + 0.5f - datos.llegadaX) <= 7.5f;
        }

        void GenerarGrilla()
        {
            for (int cy = 0; cy < mundo.alto; cy++)
            {
                // pisos al aire libre: se juntan en tiras a lo largo de la fila
                int cx = 0;
                while (cx < mundo.ancho)
                {
                    char c = mundo.Celda(cx, cy);
                    bool exteriorPiso = (c == '.' || c == ':' || c == 'p' || c == '=') && !mundo.Interior(cx, cy);
                    if (!exteriorPiso) { cx++; continue; }
                    int fin = cx;
                    while (fin + 1 < mundo.ancho && mundo.Celda(fin + 1, cy) == c && (fin + 1) / Bloque == cx / Bloque) fin++;
                    chunk = "@" + (cx / Bloque) + "_" + (cy / Bloque);
                    var m = M(TexPiso(c));
                    m.interior = 0f; m.luzExtra = LuzExterior;
                    int n = fin - cx + 1;
                    Cara(m, new Vector3(cx, 0, -cy - 1), Vector3.right * n, Vector3.forward, Vector3.up, 0, n, 0, 1);
                    if (cy <= 7 && datos.id == 2 && false) { }
                    cx = fin + 1;
                }

                for (cx = 0; cx < mundo.ancho; cx++)
                {
                    char c = mundo.Celda(cx, cy);
                    chunk = EnZonaMuroNorte(cx, cy) ? ClaveMuroNorte : "@" + (cx / Bloque) + "_" + (cy / Bloque);
                    bool interior = mundo.Interior(cx, cy);
                    var p0 = new Vector3(cx, 0, -cy - 1);

                    if (Caminable(c) || c == 'D')
                    {
                        if (c == '.' || c == ':' || c == 'p' || c == '=')
                        {
                            if (datos.hayPuente && (c == '=') && cy >= datos.puenteY0 && cy <= datos.puenteY1) DebajoPuente(cx, cy);
                            continue;   // ya está en las tiras
                        }
                        var m = M(TexPiso(c));
                        m.interior = interior ? 1f : 0f;
                        m.luzExtra = interior ? (System.Func<Vector3, Vector3>)LuzInterior : LuzExterior;
                        Cara(m, p0, Vector3.right, Vector3.forward, Vector3.up, 0, 1, 0, 1);
                        if (interior)
                        {
                            var t = M("techo");
                            t.interior = 1f; t.luzExtra = LuzInterior;
                            Cara(t, p0 + Vector3.up * AltoInterior, Vector3.right, Vector3.forward, Vector3.down, 0, 1, 0, 1);
                        }
                        continue;
                    }

                    if (c == '~') continue;
                    if (c == 'B') { Baranda(cx, cy); continue; }

                    // Piso debajo de los muros de nieve que se pueden sacar (cinemática del puente)
                    if (EnZonaMuroNorte(cx, cy))
                    {
                        var pm = M("nieve"); pm.interior = 0f; pm.luzExtra = LuzExterior;
                        Cara(pm, p0, Vector3.right, Vector3.forward, Vector3.up, 0, 1, 0, 1);
                    }

                    // Pared
                    var e0 = EstiloDe(c);
                    bool lugar = EsLugar(c);
                    for (int d = 0; d < 4; d++)
                    {
                        char n = CeldaExt(cx + DX[d], cy + DY[d]);
                        if (!Caminable(n) && n != '~') continue;
                        Vector3 origen, U, normal;
                        Borde(cx, cy, d, out origen, out U, out normal);
                        if (mundo.Interior(cx + DX[d], cy + DY[d]))
                            ParedInterior(origen, U, normal, 0f, AltoInterior, e0, 0, 1);
                        else
                        {
                            var e = lugar ? EstiloLugar(c, cx, cy, d) : e0;
                            ParedExterior(origen, U, normal, 0f, e.alto, e, 0, 1);
                            if (n == '.' && c != '#' && c != 'N') MonticuloNieve(origen, U, normal);
                            if (n == '~') Murallon(origen, U, normal);
                        }
                    }
                    // Costados altos si la pared vecina es más baja (p. ej. pilar 'C' junto a una casa)
                    for (int d = 0; d < 4; d++)
                    {
                        char n = CeldaExt(cx + DX[d], cy + DY[d]);
                        if (Caminable(n) || n == '~' || n == 'B' || Mundo.EsPuerta(n)) continue;
                        float altoVecino = EstiloDe(n).alto;
                        if (altoVecino >= e0.alto) continue;
                        Vector3 origen, U, normal;
                        Borde(cx, cy, d, out origen, out U, out normal);
                        ParedExterior(origen, U, normal, altoVecino, e0.alto, e0, 0, 1);
                    }
                }
            }
            chunk = "";
        }

        // Pared de hormigón que baja hasta el agua.
        void Murallon(Vector3 origen, Vector3 U, Vector3 normal)
        {
            var h = M("hormigon_osc");
            h.interior = 0f; h.luzExtra = LuzExterior;
            Cara(h, origen + Vector3.up * (NivelAgua - 0.2f), U, Vector3.up * (-NivelAgua + 0.2f), normal, 0, 1, 0, 3.2f);
        }

        void MonticuloNieve(Vector3 origen, Vector3 U, Vector3 normal)
        {
            var m = M("modelos");
            m.interior = 0f; m.luzExtra = LuzExterior;
            float h0 = 0.06f + (float)rnd.NextDouble() * 0.1f, h1 = 0.06f + (float)rnd.NextDouble() * 0.1f;
            float f0 = 0.16f + (float)rnd.NextDouble() * 0.12f, f1 = 0.16f + (float)rnd.NextDouble() * 0.12f;
            Vector3 a = origen, b = origen + U;
            // pendiente desde la pared hasta el piso
            var p00 = a + normal * f0; var p01 = a + Vector3.up * h0;
            var p10 = b + normal * f1; var p11 = b + Vector3.up * h1;
            Vector3 n = (normal + Vector3.up).normalized;
            if (Vector3.Dot(Vector3.Cross(p01 - p00, p10 - p00), n) >= 0) m.Quad(p00, p01, p11, p10, ConstructorMalla.ColNieve);
            else m.Quad(p10, p11, p01, p00, ConstructorMalla.ColNieve);
        }

        // Vereda del puente con la baranda del lado del río.
        void Baranda(int cx, int cy)
        {
            var p0 = new Vector3(cx, 0, -cy - 1);
            const float alto = 0.08f;
            var v = M("vereda");
            v.interior = 0f; v.luzExtra = LuzExterior;
            Cara(v, p0 + Vector3.up * alto, Vector3.right, Vector3.forward, Vector3.up, 0, 1, 0, 1);
            for (int d = 0; d < 4; d++)
            {
                char n = CeldaExt(cx + DX[d], cy + DY[d]);
                Vector3 origen, U, normal;
                Borde(cx, cy, d, out origen, out U, out normal);
                if (n == '=' || n == '.' || n == ':')
                {
                    var h = M("hormigon"); h.interior = 0f; h.luzExtra = LuzExterior;
                    Cara(h, origen, U, Vector3.up * alto, normal, 0, 1, 0, alto);
                }
                else if (n == '~')
                {
                    var h = M("hormigon"); h.interior = 0f; h.luzExtra = LuzExterior;
                    Cara(h, origen + Vector3.down * 0.7f, U, Vector3.up * 0.78f, normal, 0, 1, 0, 0.78f);
                    var a = origen - normal * 0.1f + Vector3.up * alto;
                    Rejas(a, a + U, true);
                }
            }
            if (datos.hayPuente) DebajoPuente(cx, cy);
        }

        // Baranda de hierro entre dos puntos (a la altura de la vereda).
        void Rejas(Vector3 a, Vector3 b, bool conNieve)
        {
            var m = M("modelos");
            m.interior = 0f; m.luzExtra = LuzExterior;
            var hierro = ConstructorMalla.Col(70, 84, 98);
            float largo = Vector3.Distance(a, b);
            int tramos = Mathf.Max(1, Mathf.RoundToInt(largo / 0.5f));
            for (int i = 0; i <= tramos; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)tramos);
                m.Barra(p, p + Vector3.up * 0.56f, 0.05f, hierro);
            }
            m.Barra(a + Vector3.up * 0.56f, b + Vector3.up * 0.56f, 0.06f, hierro);
            m.Barra(a + Vector3.up * 0.3f, b + Vector3.up * 0.3f, 0.035f, hierro);
            m.Barra(a + Vector3.up * 0.05f, b + Vector3.up * 0.05f, 0.035f, hierro);
            for (int i = 0; i < tramos; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)tramos);
                var q = Vector3.Lerp(a, b, (i + 1) / (float)tramos);
                m.Barra(p + Vector3.up * 0.05f, q + Vector3.up * 0.3f, 0.02f, hierro);
                m.Barra(q + Vector3.up * 0.05f, p + Vector3.up * 0.3f, 0.02f, hierro);
                m.Barra(p + Vector3.up * 0.3f, q + Vector3.up * 0.56f, 0.02f, hierro);
                m.Barra(q + Vector3.up * 0.3f, p + Vector3.up * 0.56f, 0.02f, hierro);
            }
            if (conNieve) m.Caja(Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.6f, new Vector3(Mathf.Abs(b.x - a.x) + 0.07f, 0.025f, Mathf.Abs(b.z - a.z) + 0.07f), ConstructorMalla.ColNieve);
        }

        // Losa del puente vista desde abajo.
        void DebajoPuente(int cx, int cy)
        {
            var h = M("hormigon_osc");
            h.interior = 0f; h.luzExtra = LuzExterior;
            Cara(h, new Vector3(cx, -0.7f, -cy - 1), Vector3.right, Vector3.forward, Vector3.down, 0, 1, 0, 1);
        }

        // ------------------------------------------------------------------
        // PUERTAS: marco, dintel y bisagra
        // ------------------------------------------------------------------
        void GenerarPuertas()
        {
            for (int cy = 0; cy < mundo.alto; cy++)
                for (int cx = 0; cx < mundo.ancho; cx++)
                {
                    if (!Mundo.EsPuerta(mundo.Celda(cx, cy))) continue;
                    bool latX = mundo.LateralEnX(cx, cy);
                    // vecinos en la dirección de paso
                    int ax = latX ? cx : cx - 1, ay = latX ? cy - 1 : cy;
                    int bx = latX ? cx : cx + 1, by = latX ? cy + 1 : cy;
                    bool aInterior = mundo.Interior(ax, ay);
                    // exterior = lado sin techo (si los dos tienen techo, el primero)
                    bool exteriorEsA = !aInterior || mundo.Interior(bx, by);
                    if (aInterior && !mundo.Interior(bx, by)) exteriorEsA = false;

                    Vector3 L = latX ? Vector3.right : new Vector3(0, 0, -1);
                    Vector3 baseLat = latX ? new Vector3(cx, 0, 0) : new Vector3(0, 0, -cy);
                    Vector3 dirPaso; // del exterior al interior
                    Vector3 caraExt;
                    if (latX)
                    {
                        dirPaso = exteriorEsA ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
                        caraExt = exteriorEsA ? new Vector3(0, 0, -cy) : new Vector3(0, 0, -cy - 1);
                    }
                    else
                    {
                        dirPaso = exteriorEsA ? Vector3.right : Vector3.left;
                        caraExt = exteriorEsA ? new Vector3(cx, 0, 0) : new Vector3(cx + 1, 0, 0);
                    }
                    Vector3 o = baseLat + caraExt; // esquina: lateral 0, profundidad 0, altura 0

                    char vecinoPared = latX ? mundo.Celda(cx - 1, cy) : mundo.Celda(cx, cy - 1);
                    var e = EstiloDe(vecinoPared);
                    if (EsLugar(vecinoPared)) e = EstiloLugar(vecinoPared, cx, cy, CaraDe(-dirPaso));
                    chunk = "@" + (cx / Bloque) + "_" + (cy / Bloque);
                    float M0 = Mundo.MarcoPuerta, M1 = 1f - Mundo.MarcoPuerta;

                    // Cara exterior (mira hacia -dirPaso)
                    Vector3 nExt = -dirPaso;
                    ParedExterior(o, L * M0, nExt, 0f, e.alto, e, 0f, M0);
                    ParedExterior(o + L * M1, L * M0, nExt, 0f, e.alto, e, M1, 1f);
                    ParedExterior(o + L * M0, L * (M1 - M0), nExt, AltoPuerta, e.alto, e, M0, M1);
                    // Cara interior (del lado de adentro, a 1 de profundidad)
                    Vector3 oi = o + dirPaso;
                    e.runLen = 0;
                    ParedInterior(oi, L * M0, dirPaso, 0f, AltoInterior, e, 0f, M0);
                    ParedInterior(oi + L * M1, L * M0, dirPaso, 0f, AltoInterior, e, M1, 1f);
                    ParedInterior(oi + L * M0, L * (M1 - M0), dirPaso, AltoPuerta, AltoInterior, e, M0, M1);
                    // Jambas y dintel (dentro del vano)
                    var mj = M(e.interiorTex);
                    mj.interior = 1f; mj.luzExtra = LuzInterior;
                    Cara(mj, o + L * M0, dirPaso, Vector3.up * AltoPuerta, L, 0, 1, 0.25f, 1f);
                    Cara(mj, o + L * M1, dirPaso, Vector3.up * AltoPuerta, -L, 0, 1, 0.25f, 1f);
                    Cara(mj, o + L * M0 + Vector3.up * AltoPuerta, L * (M1 - M0), dirPaso, Vector3.down, 0, 1, 0, 0.2f);
                    // Marco de madera
                    var mm = M("modelos");
                    mm.interior = 0f; mm.luzExtra = LuzExterior;
                    var madera = ConstructorMalla.Col(78, 54, 36);
                    Vector3 f = o - dirPaso * 0.02f;
                    mm.Barra(f + L * (M0 - 0.03f), f + L * (M0 - 0.03f) + Vector3.up * (AltoPuerta + 0.03f), 0.06f, madera);
                    mm.Barra(f + L * (M1 + 0.03f), f + L * (M1 + 0.03f) + Vector3.up * (AltoPuerta + 0.03f), 0.06f, madera);
                    mm.Barra(f + L * (M0 - 0.06f) + Vector3.up * (AltoPuerta + 0.03f), f + L * (M1 + 0.06f) + Vector3.up * (AltoPuerta + 0.03f), 0.06f, madera);

                    // Hoja: bisagra del lado lateral 0, a 0,12 de la cara exterior; abre hacia adentro.
                    var info = new PuertaInfo
                    {
                        x = cx, y = cy, ancho = M1 - M0,
                        bisagra = o + L * M0 + dirPaso * 0.12f,
                        anguloCerrada = Yaw(L), anguloAbierta = Yaw(dirPaso),
                    };
                    puertas.Add(info);
                }
        }

        // Giro en Y (grados) que lleva el eje +X local hacia la dirección d.
        static float Yaw(Vector3 d) { return Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg; }

        static Color32 Var(Color32 c, int d) { return ConstructorMalla.Col(c.r + d, c.g + d, c.b + d); }

        // ------------------------------------------------------------------
        // RIACHUELO Y COSTAS
        // ------------------------------------------------------------------
        // Agua entre las dos costas (z0 = costa sur, z1 = costa norte), con murallones y hielo flotando.
        void GenerarRio(float x0, float x1, float z0, float z1, bool murallones)
        {
            chunk = "";
            var agua = M("agua");
            agua.interior = 0f; agua.luzExtra = p => new Vector3(0.8f, 0.82f, 0.86f);
            for (float x = x0; x < x1; x += 10f)
                Cara(agua, new Vector3(x, NivelAgua, z0), Vector3.right * 10f, Vector3.forward * (z1 - z0), Vector3.up, 0, 10f / 4f, 0, (z1 - z0) / 4f);
            if (murallones)
            {
                var h = M("hormigon_osc");
                h.interior = 0f; h.luzExtra = LuzExterior;
                for (float x = x0; x < x1; x += 10f)
                {
                    Cara(h, new Vector3(x, NivelAgua - 0.2f, z0), Vector3.right * 10f, Vector3.up * (-NivelAgua + 0.2f), Vector3.forward, 0, 10, 0, 3.2f);
                    Cara(h, new Vector3(x, NivelAgua - 0.2f, z1), Vector3.right * 10f, Vector3.up * (-NivelAgua + 0.2f), Vector3.back, 0, 10, 0, 3.2f);
                }
            }
            var mod = M("modelos");
            mod.interior = 0f; mod.luzExtra = LuzExterior;
            for (int i = 0; i < 70; i++)
            {
                float x = x0 + 40f + (float)rnd.NextDouble() * (x1 - x0 - 80f);
                float z = z0 + 1f + (float)rnd.NextDouble() * (z1 - z0 - 2f);
                if (x > datos.puenteX0 - 2f && x < datos.puenteX1 + 3f && datos.id == 2) continue;
                mod.Push(); mod.Mover(x, NivelAgua + 0.02f, z); mod.RotarY((float)rnd.NextDouble() * 360f);
                mod.Caja(Vector3.zero, new Vector3(0.6f + (float)rnd.NextDouble() * 1.6f, 0.08f, 0.5f + (float)rnd.NextDouble() * 1.2f), ConstructorMalla.Col(206, 214, 224));
                mod.Pop();
            }
        }

        // ------------------------------------------------------------------
        // PUENTE PUEYRREDÓN: arco de acero, pilas y vigas (la calzada puede venir de la grilla)
        // ------------------------------------------------------------------
        // xOeste..xEste = ancho de la losa; z0..z1 = tramo sobre el agua (z0 < z1).
        void GenerarPuente(float xOeste, float xEste, float z0, float z1, bool conCalzada)
        {
            chunk = "";
            float largo = z1 - z0;
            float xc = (xOeste + xEste) * 0.5f;
            if (conCalzada)
            {
                var asf = M("asfalto"); asf.interior = 0f; asf.luzExtra = LuzExterior;
                for (float z = z0; z < z1; z += 1f)
                    for (float x = xOeste + 1f; x < xEste - 1f; x += 1f)
                        Cara(asf, new Vector3(x, 0, z), Vector3.right, Vector3.forward, Vector3.up, 0, 1, 0, 1);
                var ver = M("vereda"); ver.interior = 0f; ver.luzExtra = LuzExterior;
                var hor = M("hormigon"); hor.interior = 0f; hor.luzExtra = LuzExterior;
                var hos = M("hormigon_osc"); hos.interior = 0f; hos.luzExtra = LuzExterior;
                foreach (var xv in new[] { xOeste, xEste - 1f })
                {
                    Cara(ver, new Vector3(xv, 0.08f, z0), Vector3.right, Vector3.forward * largo, Vector3.up, 0, 1, 0, largo);
                    bool oeste = xv < xc;
                    Cara(hor, new Vector3(oeste ? xv + 1f : xv, 0, z0), Vector3.forward * largo, Vector3.up * 0.08f, oeste ? Vector3.right : Vector3.left, 0, largo, 0, 0.08f);
                    Cara(hor, new Vector3(oeste ? xv : xv + 1f, -0.7f, z0), Vector3.forward * largo, Vector3.up * 0.78f, oeste ? Vector3.left : Vector3.right, 0, largo, 0, 0.78f);
                    float xr = oeste ? xv + 0.1f : xv + 0.9f;
                    for (float z = z0; z < z1; z += 2f) Rejas(new Vector3(xr, 0.08f, z), new Vector3(xr, 0.08f, z + 2f), true);
                }
                Cara(hos, new Vector3(xOeste, -0.7f, z0), Vector3.right * (xEste - xOeste), Vector3.forward * largo, Vector3.down, 0, xEste - xOeste, 0, largo);
            }

            var mod = M("modelos"); mod.interior = 0f; mod.luzExtra = LuzExterior;
            var acero = ConstructorMalla.Col(78, 94, 96);
            var aceroOsc = ConstructorMalla.Col(56, 66, 68);
            // vigas debajo de la losa
            foreach (var x in new[] { xc - 3f, xc, xc + 3f })
                mod.Caja(new Vector3(x, -1.0f, (z0 + z1) * 0.5f), new Vector3(0.4f, 0.6f, largo + 2f), ConstructorMalla.Col(110, 108, 104));
            // pilas en el agua
            foreach (var z in new[] { z0 + 3f, z0 + 11f, z0 + 19f, z0 + 27f })
            {
                if (z > z1 - 1f) continue;
                mod.Caja(new Vector3(xc, (NivelAgua - 1.3f) * 0.5f - 0.4f, z), new Vector3(xEste - xOeste - 0.6f, 0.5f, 1.4f), ConstructorMalla.Col(104, 102, 98));
                foreach (var x in new[] { xc - 2.9f, xc + 2.9f })
                    mod.Cilindro(new Vector3(x, NivelAgua - 0.4f, z), 0.55f, 0.48f, -NivelAgua - 0.6f, 8, ConstructorMalla.Col(112, 110, 104));
            }
            // Arco de acero (puente de arco con tensores) sobre la calzada
            float za = z0 + 2f, zb = z1 - 2f, altoArco = 4.6f;
            int seg = 18;
            foreach (var x in new[] { xOeste + 0.15f, xEste - 0.15f })
            {
                Vector3 prev = new Vector3(x, 0.08f, za);
                for (int i = 1; i <= seg; i++)
                {
                    float t = i / (float)seg;
                    var p = new Vector3(x, 0.08f + Mathf.Sin(t * Mathf.PI) * altoArco, Mathf.Lerp(za, zb, t));
                    mod.Barra(prev, p, 0.28f, acero);
                    prev = p;
                }
                for (int i = 1; i < 13; i++)
                {
                    float t = i / 13f;
                    float h = Mathf.Sin(t * Mathf.PI) * altoArco;
                    var b = new Vector3(x, 0.6f, Mathf.Lerp(za, zb, t));
                    mod.Barra(b, b + Vector3.up * (h - 0.6f), 0.05f, aceroOsc);
                }
                mod.Barra(new Vector3(x, 0.45f, za - 0.4f), new Vector3(x, 0.45f, zb + 0.4f), 0.22f, aceroOsc);
                for (int i = 2; i < seg - 1; i++)
                {
                    float t = (i + 0.5f) / seg;
                    var p = new Vector3(x, 0.08f + Mathf.Sin(t * Mathf.PI) * altoArco + 0.16f, Mathf.Lerp(za, zb, t));
                    if (Mathf.Abs(t - 0.5f) < 0.3f) mod.Caja(p, new Vector3(0.24f, 0.05f, (zb - za) / seg), ConstructorMalla.ColNieve);
                }
            }
            for (int i = 4; i <= 14; i += 2)
            {
                float t = i / (float)seg;
                float h = 0.08f + Mathf.Sin(t * Mathf.PI) * altoArco;
                float z = Mathf.Lerp(za, zb, t);
                mod.Barra(new Vector3(xOeste + 0.15f, h, z), new Vector3(xEste - 0.15f, h, z), 0.14f, acero);
                if (i < 14)
                {
                    float t2 = (i + 2) / (float)seg;
                    float h2 = 0.08f + Mathf.Sin(t2 * Mathf.PI) * altoArco;
                    mod.Barra(new Vector3(xOeste + 0.15f, h, z), new Vector3(xEste - 0.15f, h2, Mathf.Lerp(za, zb, t2)), 0.07f, aceroOsc);
                }
            }
            if (conCalzada)
            {
                for (float z = z0 + 4f; z < z1; z += 7f)
                {
                    Hornear("farola", 70 + (int)z, xOeste + 0.5f, -z, -90f, false);
                    Hornear("farola", 90 + (int)z, xEste - 0.5f, -(z + 3.5f), 90f, false);
                }
                Hornear("auto_nevado", 501, xc - 1.9f, -(z0 + 9f), 82f, false);
                Hornear("auto_volcado", 502, xc + 2.1f, -(z0 + 17f), 105f, false);
                Hornear("auto_nevado", 503, xc - 1.3f, -(z0 + 24.5f), 95f, false);
            }
        }

        // Coloca un modelo (decoración) pegado a la malla estática, con la luz del lugar donde está.
        // y = fila de la grilla (el eje Z del mundo es -y).
        void Hornear(string nombre, int semilla, float x, float y, float giro, bool interior)
        {
            var mod = M("modelos");
            var modelo = Modelos.Crear(nombre, semilla);
            int i0 = mod.v.Count;
            mod.Push();
            mod.Mover(new Vector3(x, 0, -y));
            mod.RotarY(giro);
            mod.Unir(modelo.malla);
            foreach (var p in modelo.partes) { mod.Push(); mod.Mover(p.pivote); mod.Unir(p.malla); mod.Pop(); }
            mod.Pop();
            for (int i = i0; i < mod.v.Count; i++)
            {
                var u2 = mod.uv2[i];
                if (u2.y > 0.5f) continue;                      // lo que brilla no se oscurece
                var l = interior ? LuzInterior(mod.v[i]) : LuzExterior(mod.v[i]) * 1.06f;
                var c = mod.col[i];
                mod.col[i] = new Color32(Lienzo.B((int)(c.r * l.x)), Lienzo.B((int)(c.g * l.y)), Lienzo.B((int)(c.b * l.z)), 255);
                mod.uv2[i] = new Vector2(interior ? 1f : 0f, u2.y);
            }
        }

        // Pega a la malla todos los objetos de decoración que no se mueven ni se interactúan.
        void HornearDecoracion()
        {
            foreach (var e in mundo.entidades)
            {
                var def = e.def;
                if (def.tipo != TipoEntidad.Decoracion || def.sprite == "vela" || def.sprite == "semaforo") continue;
                chunk = "@" + (Mathf.FloorToInt(e.x) / Bloque) + "_" + (Mathf.FloorToInt(e.y) / Bloque);
                Hornear(def.sprite, def.id, e.x, e.y, def.angulo, mundo.Interior(e.x, e.y));
                horneadas.Add(def.id);
            }
            chunk = "";
        }

        // Lado de Avellaneda: el río, el puente y la Capital con el Obelisco al fondo, todo al norte del muro.
        void GenerarExteriorAvellaneda()
        {
            float dx = datos.llegadaX - 15.5f;
            posObelisco = obeliscoLocal + new Vector3(dx, 0, 0);
            hayObelisco = true;
            Desplazado(dx, () =>
            {
                GenerarRio(-170f, 200f, 0f, OrillaNorte, true);
                GenerarPuente(11f, 20f, 0f, OrillaNorte, true);
                GenerarCapital();
            });
            GenerarObelisco();
            GenerarHorizonteAvellaneda();
        }

        // Mapa del capítulo 2: el río alrededor del puente y la avenida con el Obelisco en el centro.
        void GenerarExteriorPuente()
        {
            float z0 = -(datos.puenteY1 + 1), z1 = -datos.puenteY0;
            GenerarRio(-120f, 180f, z0, z1, false);
            // costas: murallones de las orillas de la ciudad
            var h = M("hormigon_osc"); h.interior = 0f; h.luzExtra = LuzExterior;
            foreach (var par in new[] { new Vector2(z0, 1f), new Vector2(z1, -1f) })
            {
                Vector3 nrm = par.y > 0 ? Vector3.forward : Vector3.back;
                Cara(h, new Vector3(0, NivelAgua - 0.2f, par.x), Vector3.right * datos.ancho, Vector3.up * (-NivelAgua + 0.2f), nrm, 0, datos.ancho, 0, 3.2f);
            }
            GenerarPuente(datos.puenteX0, datos.puenteX1 + 1f, z0, z1, false);
            posObelisco = new Vector3(datos.obeliscoX, 0, -datos.obeliscoY);
            hayObelisco = true;
            GenerarObelisco();
            // luces rojas y disparos de la batalla alrededor de la plaza del Obelisco
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                fogonazos.Add(posObelisco + new Vector3(Mathf.Cos(a) * (6f + (float)rnd.NextDouble() * 6f), 0.5f + (float)rnd.NextDouble() * 1.2f, Mathf.Sin(a) * (6f + (float)rnd.NextDouble() * 6f)));
            }
            bengalas.Add(posObelisco + new Vector3(-10f, 2.5f, -12f));
            bengalas.Add(posObelisco + new Vector3(12f, 3.5f, -4f));
            GenerarHorizontePuente();
        }

        // Coloca un modelo del juego dentro de la malla estática (decorado lejano).
        void PonerModelo(string nombre, Vector3 pos, float giro, int semilla)
        {
            var mod = M("modelos");
            var modelo = Modelos.Crear(nombre, semilla);
            mod.Push();
            mod.Mover(pos);
            mod.RotarY(giro);
            mod.Unir(modelo.malla);
            foreach (var p in modelo.partes)
            {
                mod.Push(); mod.Mover(p.pivote); mod.Unir(p.malla); mod.Pop();
            }
            mod.Pop();
        }

        // Edificio de caja con fachadas texturadas y techo nevado.
        void Edificio(Vector3 centroBase, float ancho, float prof, float alto, bool simple)
        {
            string[] altas = { "fachada_beige", "fachada_gris", "fachada_ocre", "fachada_blanca", "ladrillo_liso" };
            string[] bajas = { "persiana", "revoque", "ventana", "persiana" };
            string alta = altas[rnd.Next(altas.Length)];
            var e = new Estilo { baseTex = bajas[rnd.Next(bajas.Length)], altaTex = alta, interiorTex = alta, alto = alto };
            if (simple) e.baseTex = alta;
            float x0 = centroBase.x - ancho * 0.5f, x1 = centroBase.x + ancho * 0.5f;
            float z0 = centroBase.z - prof * 0.5f, z1 = centroBase.z + prof * 0.5f;
            var y = centroBase.y;
            // caras: sur (-Z), norte (+Z), oeste, este
            ParedLarga(new Vector3(x0, y, z0), Vector3.right, ancho, Vector3.back, e);
            ParedLarga(new Vector3(x1, y, z1), Vector3.left, ancho, Vector3.forward, e);
            ParedLarga(new Vector3(x0, y, z1), Vector3.back, prof, Vector3.left, e);
            ParedLarga(new Vector3(x1, y, z0), Vector3.forward, prof, Vector3.right, e);
            var t = M("nieve"); t.interior = 0f; t.luzExtra = LuzExterior;
            Cara(t, new Vector3(x0, y + alto, z0), Vector3.right * ancho, Vector3.forward * prof, Vector3.up, 0, ancho / 2f, 0, prof / 2f);
            var mod = M("modelos");
            mod.Caja(new Vector3(centroBase.x, y + alto + 0.06f, centroBase.z), new Vector3(ancho + 0.12f, 0.12f, prof + 0.12f), ConstructorMalla.Col(150, 146, 140));
        }

        // Pared de varias celdas de largo (se arma de a 1 unidad para que la textura se repita bien).
        void ParedLarga(Vector3 inicio, Vector3 dir, float largo, Vector3 normal, Estilo e)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(largo));
            float paso = largo / n;
            for (int i = 0; i < n; i++)
                ParedExterior(inicio + dir * (i * paso), dir * paso, normal, inicio.y, inicio.y + e.alto, e, 0, 1);
        }

        // ------------------------------------------------------------------
        // CAPITAL: la avenida del otro lado del puente y el Obelisco al fondo
        // ------------------------------------------------------------------
        void GenerarCapital()
        {
            float zIni = OrillaNorte, zFin = 230f;
            // Piso nevado de toda la costa norte
            var nieve = M("nieve"); nieve.interior = 0f; nieve.luzExtra = LuzExterior;
            for (float x = -170f; x < 200f; x += 10f)
                for (float z = zIni; z < zFin; z += 10f)
                {
                    if (x >= 0f && x < 30f && z < 100f) continue; // la avenida tiene su propio piso
                    Cara(nieve, new Vector3(x, 0, z), Vector3.right * 10f, Vector3.forward * 10f, Vector3.up, 0, 5, 0, 5);
                }
            // Avenida: calzada, veredas y franja de nieve hasta los edificios
            var asf = M("asfalto"); asf.interior = 0f; asf.luzExtra = LuzExterior;
            var ver = M("vereda"); ver.interior = 0f; ver.luzExtra = LuzExterior;
            var hor = M("hormigon"); hor.interior = 0f; hor.luzExtra = LuzExterior;
            for (float z = zIni; z < 100f; z += 2f)
            {
                for (float x = 9f; x < 22f; x += 1f) Cara(asf, new Vector3(x, 0, z), Vector3.right, Vector3.forward * 2f, Vector3.up, 0, 1, 0, 2);
                Cara(nieve, new Vector3(0, 0, z), Vector3.right * 6.5f, Vector3.forward * 2f, Vector3.up, 0, 3.25f, 0, 1);
                Cara(nieve, new Vector3(24.5f, 0, z), Vector3.right * 5.5f, Vector3.forward * 2f, Vector3.up, 0, 2.75f, 0, 1);
                Cara(ver, new Vector3(6.5f, 0.08f, z), Vector3.right * 2.5f, Vector3.forward * 2f, Vector3.up, 0, 2.5f, 0, 2);
                Cara(ver, new Vector3(22f, 0.08f, z), Vector3.right * 2.5f, Vector3.forward * 2f, Vector3.up, 0, 2.5f, 0, 2);
                Cara(hor, new Vector3(9f, 0, z), Vector3.forward * 2f, Vector3.up * 0.08f, Vector3.right, 0, 2, 0, 0.08f);
                Cara(hor, new Vector3(22f, 0, z), Vector3.forward * 2f, Vector3.up * 0.08f, Vector3.left, 0, 2, 0, 0.08f);
            }
            // Edificios a los dos lados de la avenida, con calles transversales
            float[] cortes = { 52f, 74f };
            foreach (var lado in new[] { -1, 1 })
            {
                float z = zIni + 3f;
                while (z < 98f)
                {
                    bool enCalle = false;
                    foreach (var c in cortes) if (z > c - 3f && z < c + 3f) { z = c + 3f; enCalle = true; }
                    if (enCalle) continue;
                    float frente = 5f + (float)rnd.NextDouble() * 6f;
                    float limite = 98f;
                    foreach (var c in cortes) if (z < c - 3f) { limite = Mathf.Min(limite, c - 3f); }
                    frente = Mathf.Min(frente, limite - z);
                    if (frente < 2f) { z = limite; continue; }
                    float alto = 3.5f + (float)rnd.NextDouble() * 7f;
                    float prof = 8f + (float)rnd.NextDouble() * 6f;
                    float xc = lado < 0 ? 6f - prof * 0.5f : 25f + prof * 0.5f;
                    Edificio(new Vector3(xc, 0, z + frente * 0.5f), prof, frente - 0.15f, alto, false);
                    z += frente;
                }
            }
            // Costanera norte: edificios a lo largo del río
            for (float x = -160f; x < 190f;)
            {
                float ancho = 6f + (float)rnd.NextDouble() * 10f;
                if (x + ancho > 0f && x < 31f) { x = 31f; continue; }
                Edificio(new Vector3(x + ancho * 0.5f, 0, OrillaNorte + 5f + (float)rnd.NextDouble() * 4f), ancho - 0.8f, 6f, 3f + (float)rnd.NextDouble() * 8f, true);
                x += ancho;
            }
            // Autos abandonados, farolas y semáforos en la avenida
            float[] zs = { 38f, 46f, 60f, 67f, 82f, 92f };
            for (int i = 0; i < zs.Length; i++)
            {
                float x = 10.5f + (float)rnd.NextDouble() * 10f;
                PonerModelo(rnd.Next(4) == 0 ? "auto_volcado" : "auto_nevado", new Vector3(x, 0, zs[i]), 80f + (float)rnd.NextDouble() * 30f, 600 + i);
            }
            for (float z = 36f; z < 96f; z += 9f)
            {
                PonerModelo("farola", new Vector3(8.5f, 0.08f, z), -90f, 700 + (int)z);
                PonerModelo("farola", new Vector3(22.5f, 0.08f, z + 4.5f), 90f, 800 + (int)z);
            }
            foreach (var c in cortes)
            {
                PonerModelo("semaforo", new Vector3(8.2f, 0.08f, c - 3.5f), 180f, 900 + (int)c);
                PonerModelo("semaforo", new Vector3(22.8f, 0.08f, c + 3.5f), 0f, 950 + (int)c);
            }

            // Plaza del Obelisco: vallas, bolsas de arena y camiones del Ejército
            var mod = M("modelos"); mod.interior = 0f; mod.luzExtra = LuzExterior;
            Color32 arena = ConstructorMalla.Col(150, 136, 100), oliva = ConstructorMalla.Col(72, 82, 56);
            var o = obeliscoLocal;
            for (int i = 0; i < 22; i++)
            {
                float a = i / 22f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 270f)) < 25f) continue; // abertura hacia la avenida
                var p = o + new Vector3(Mathf.Cos(a) * 9f, 0, Mathf.Sin(a) * 9f);
                mod.Push(); mod.Mover(p); mod.RotarY(-a * Mathf.Rad2Deg + 90f);
                mod.Caja(new Vector3(0, 0.12f, 0), new Vector3(2.4f, 0.24f, 0.5f), arena);
                mod.Caja(new Vector3(0, 0.34f, 0), new Vector3(2.2f, 0.2f, 0.45f), Var(arena, -10));
                mod.Caja(new Vector3(0, 0.46f, 0), new Vector3(2.0f, 0.04f, 0.42f), ConstructorMalla.ColNieve);
                mod.Pop();
            }
            foreach (var cam in new[] { new Vector3(-6f, 0, -6f), new Vector3(6.5f, 0, 2f), new Vector3(-3f, 0, 7f) })
            {
                var p = o + cam;
                mod.Push(); mod.Mover(p); mod.RotarY((float)rnd.NextDouble() * 360f);
                mod.Caja(new Vector3(0, 0.55f, 0), new Vector3(3.2f, 0.7f, 1.2f), oliva);
                mod.Caja(new Vector3(1.9f, 0.5f, 0), new Vector3(0.9f, 0.8f, 1.15f), Var(oliva, -8));
                mod.Caja(new Vector3(0, 1.05f, 0), new Vector3(3.0f, 0.3f, 1.25f), ConstructorMalla.Col(90, 100, 70));
                foreach (var wx in new[] { -1.0f, 0.8f, 2.0f })
                    foreach (var wz in new[] { -0.58f, 0.58f })
                        mod.CilindroZ(new Vector3(wx, 0.25f, wz), 0.25f, 0.15f, 8, ConstructorMalla.Col(24, 26, 30));
                mod.Pop();
                bengalas.Add(p + new Vector3(0, 1.4f, 0));
            }
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                fogonazos.Add(o + new Vector3(Mathf.Cos(a) * (6f + (float)rnd.NextDouble() * 6f), 0.5f + (float)rnd.NextDouble() * 1.2f, Mathf.Sin(a) * (6f + (float)rnd.NextDouble() * 6f)));
            }
            bengalas.Add(o + new Vector3(-10f, 2.5f, -12f));
            bengalas.Add(o + new Vector3(12f, 3.5f, -4f));

            // Edificios alrededor de la plaza del Obelisco
            for (float x = -40f; x < 70f;)
            {
                float ancho = 7f + (float)rnd.NextDouble() * 9f;
                Edificio(new Vector3(x + ancho * 0.5f, 0, obeliscoLocal.z + 30f + (float)rnd.NextDouble() * 6f), ancho - 0.8f, 10f, 6f + (float)rnd.NextDouble() * 10f, true);
                x += ancho;
            }
            foreach (var lado in new[] { -1, 1 })
                for (float z = obeliscoLocal.z - 14f; z < obeliscoLocal.z + 26f;)
                {
                    float frente = 7f + (float)rnd.NextDouble() * 8f;
                    float xc = lado < 0 ? -10f : 41f;
                    Edificio(new Vector3(xc, 0, z + frente * 0.5f), 10f, frente - 0.8f, 5f + (float)rnd.NextDouble() * 9f, true);
                    z += frente;
                }
        }

        void GenerarObelisco()
        {
            obelisco = Modelos.Nueva();
            var b = obelisco;
            b.luzExtra = LuzExterior;
            var piedra = ConstructorMalla.Col(206, 204, 194);
            float baseA = 3.4f, topeA = 2.0f, alto = 32f, punta = 2.6f;
            // fuste: cuatro caras trapezoidales
            Vector3[] b0 = { new Vector3(-baseA / 2, 0, -baseA / 2), new Vector3(baseA / 2, 0, -baseA / 2), new Vector3(baseA / 2, 0, baseA / 2), new Vector3(-baseA / 2, 0, baseA / 2) };
            Vector3[] t0 = { new Vector3(-topeA / 2, alto, -topeA / 2), new Vector3(topeA / 2, alto, -topeA / 2), new Vector3(topeA / 2, alto, topeA / 2), new Vector3(-topeA / 2, alto, topeA / 2) };
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                b.Quad(b0[i], t0[i], t0[j], b0[j], piedra);
            }
            var cima = new Vector3(0, alto + punta, 0);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                b.Tri(t0[i], cima, t0[j], piedra);
            }
            // ventanita cerca de la punta (en las 4 caras)
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                var centro = Vector3.Lerp(Vector3.Lerp(b0[i], b0[j], 0.5f), Vector3.Lerp(t0[i], t0[j], 0.5f), 0.95f);
                var hacia = (centro - new Vector3(0, centro.y, 0)).normalized;
                b.Caja(centro + hacia * 0.02f, new Vector3(Mathf.Abs(hacia.z) * 0.3f + 0.04f, 0.45f, Mathf.Abs(hacia.x) * 0.3f + 0.04f), ConstructorMalla.Col(40, 44, 50));
            }
            // nieve en la base
            b.Cilindro(new Vector3(0, 0, 0), 3.0f, 2.0f, 0.25f, 10, ConstructorMalla.ColNieve);
            b.Caja(new Vector3(0, 0.12f, 0), new Vector3(baseA + 0.3f, 0.24f, baseA + 0.3f), ConstructorMalla.Col(170, 168, 160));
        }

        // Ciudad lejana alrededor del mapa (siluetas en la niebla). Se ve por encima de los muros de nieve.
        void Horizonte(Vector3 centro, float rMin, float rMax, int cantidad, System.Func<Vector3, bool> descartar)
        {
            chunk = "";
            var mod = M("modelos"); mod.interior = 0f; mod.luzExtra = LuzExterior;
            var r0 = new System.Random(77);
            for (int i = 0; i < cantidad; i++)
            {
                float a = (float)r0.NextDouble() * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float r = rMin + (float)r0.NextDouble() * (rMax - rMin);
                var p = centro + dir * r;
                if (descartar(p)) continue;
                float w = 5f + (float)r0.NextDouble() * 10f, d = 5f + (float)r0.NextDouble() * 10f, h = 5f + (float)r0.NextDouble() * 14f;
                var gris = (byte)(96 + r0.Next(40));
                mod.Push(); mod.Mover(p); mod.RotarY((float)r0.NextDouble() * 90f);
                mod.Caja(new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), new Color32(gris, gris, (byte)(gris + 6), 255));
                mod.Caja(new Vector3(0, h + 0.1f, 0), new Vector3(w + 0.1f, 0.2f, d + 0.1f), ConstructorMalla.ColNieve);
                mod.Pop();
            }
        }

        void GenerarHorizonteAvellaneda()
        {
            Horizonte(new Vector3(datos.ancho * 0.5f, 0, -datos.alto * 0.5f), 100f, 190f, 190, p => p.z > 1f);
        }

        void GenerarHorizontePuente()
        {
            Horizonte(new Vector3(datos.ancho * 0.5f, 0, -datos.alto * 0.5f), 70f, 170f, 150, p => p.x > -4f && p.x < datos.ancho + 4f && p.z > -datos.alto - 4f && p.z < 4f);
        }

        // Cúpula del cielo (la mueve la cámara para que siempre quede alrededor).
        void GenerarCielo()
        {
            cielo = new ConstructorMalla { sombrear = false };
            const int lados = 24;
            float r = 230f;
            float[] alturas = { -60f, 0f, 70f, 150f };
            float[] radios = { r, r, r * 0.92f, r * 0.55f };
            float[] vs = { 0f, 0.02f, 0.5f, 1f };
            for (int k = 0; k < alturas.Length - 1; k++)
                for (int i = 0; i < lados; i++)
                {
                    float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
                    var p00 = new Vector3(Mathf.Cos(a0) * radios[k], alturas[k], Mathf.Sin(a0) * radios[k]);
                    var p10 = new Vector3(Mathf.Cos(a1) * radios[k], alturas[k], Mathf.Sin(a1) * radios[k]);
                    var p01 = new Vector3(Mathf.Cos(a0) * radios[k + 1], alturas[k + 1], Mathf.Sin(a0) * radios[k + 1]);
                    var p11 = new Vector3(Mathf.Cos(a1) * radios[k + 1], alturas[k + 1], Mathf.Sin(a1) * radios[k + 1]);
                    float u0 = i * 3f / lados, u1 = (i + 1) * 3f / lados;
                    // caras hacia adentro
                    cielo.QuadUV(p10, p11, p01, p00, new Color32(128, 128, 128, 255), new Vector2(u1, vs[k]), new Vector2(u1, vs[k + 1]), new Vector2(u0, vs[k + 1]), new Vector2(u0, vs[k]));
                }
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
                var p0 = new Vector3(Mathf.Cos(a0) * radios[3], alturas[3], Mathf.Sin(a0) * radios[3]);
                var p1 = new Vector3(Mathf.Cos(a1) * radios[3], alturas[3], Mathf.Sin(a1) * radios[3]);
                int idx = cielo.v.Count;
                cielo.v.Add(p0); cielo.v.Add(new Vector3(0, 170f, 0)); cielo.v.Add(p1);
                cielo.uv.Add(new Vector2(0.5f, 1f)); cielo.uv.Add(new Vector2(0.5f, 1f)); cielo.uv.Add(new Vector2(0.5f, 1f));
                for (int k = 0; k < 3; k++) { cielo.uv2.Add(Vector2.zero); cielo.col.Add(new Color32(128, 128, 128, 255)); }
                cielo.tri.Add(idx); cielo.tri.Add(idx + 1); cielo.tri.Add(idx + 2);
            }
        }
    }
}
