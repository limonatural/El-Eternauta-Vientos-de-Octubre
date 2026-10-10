using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Parte visible del juego en 3D real: arma los objetos de Unity (mallas, materiales,
    // cámara en baja resolución, mano con linterna, nieve, puertas, objetos que giran)
    // a partir del Mundo. La lógica sigue en Mundo/Jugador; esta clase solo dibuja.
    public class MundoVisual
    {
        public const int CapaVistaPrevia = 30;     // capa sin nombre para el inventario 3D
        public const float AltoOjos = 0.8f;

        // Lo controla el juego
        public bool conTraje;
        public bool mostrarManos = true;
        public float visibilidadObelisco = 0.3f;
        public bool radioEncendida;
        public bool mostrarVistaPrevia;
        public float campoVisual = 58f;            // vertical, en grados

        public RenderTexture Imagen { get; private set; }
        public RenderTexture ImagenVistaPrevia { get; private set; }
        public int Alto { get; private set; }
        public int Ancho { get; private set; }

        Mundo mundo;
        readonly GameObject raiz;
        readonly Camera camara;
        readonly Shader shader;
        readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();
        Material matModelos, matObelisco, matCielo;
        Transform cielo;
        readonly GeneradorNivel generador;

        class EntidadVisual
        {
            public Entidad e;
            public GameObject go;
            public Transform cuerpo;
            public Renderer[] renderers;
            public readonly Dictionary<string, Transform> partes = new Dictionary<string, Transform>();
            public Color luz;
            public bool interior;
        }
        readonly List<EntidadVisual> entidades = new List<EntidadVisual>();

        class PuertaVisual { public PuertaInfo info; public Transform hoja; public Renderer render; public float angulo; }
        readonly List<PuertaVisual> puertas = new List<PuertaVisual>();

        readonly List<Transform> bengalas = new List<Transform>(), fogonazos = new List<Transform>();
        GameObject manoSin, manoCon;
        Transform mano;

        // Nieve
        const int Copos = 600;
        readonly Vector3[] copos = new Vector3[Copos];
        readonly float[] velCopo = new float[Copos];
        Mesh mallaNieve;
        readonly Vector3[] vertNieve = new Vector3[Copos * 4];
        readonly System.Random rnd = new System.Random(5);

        // Vista previa del inventario
        Camera camPrevia;
        Transform raizPrevia;
        readonly Dictionary<string, GameObject> modelosPrevia = new Dictionary<string, GameObject>();
        string previaActual;

        MaterialPropertyBlock bloque;
        static readonly int IdColor = Shader.PropertyToID("_Color"), IdResaltado = Shader.PropertyToID("_Resaltado"), IdInterior = Shader.PropertyToID("_Interior");

        public MundoVisual(Mundo m, int altoImagen)
        {
            mundo = m;
            Alto = altoImagen;
            bloque = new MaterialPropertyBlock();
            shader = Resources.Load<Shader>("EternautaRetro");
            if (shader == null) shader = Shader.Find("Eternauta/Retro");
            if (shader == null)
            {
                Debug.LogError("[Eternauta] No se encontró el shader Eternauta/Retro (Assets/EternautaBeta/Resources/EternautaRetro.shader).");
                shader = Shader.Find("Unlit/Texture");
            }

            raiz = new GameObject("Eternauta_Mundo3D");
            generador = new GeneradorNivel(mundo);
            generador.Generar();

            foreach (var kv in GeneradorNivel.Texturas()) texturas[kv.Key] = ATextura(kv.Value.px, kv.Value.w, kv.Value.h, true, kv.Key);
            var at = Modelos.atlas;
            var texAtlas = new Texture2D(at.tam, at.tam, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "modelos" };
            texAtlas.SetPixels32(at.px);
            texAtlas.Apply(false, true);
            texturas["modelos"] = texAtlas;
            matModelos = Material("modelos");

            // Escenario estático
            var escenario = new GameObject("Escenario").transform;
            escenario.SetParent(raiz.transform, false);
            foreach (var kv in generador.mallas) CrearObjeto(kv.Key, kv.Value, Material(kv.Key), escenario);

            // Obelisco (material propio: su niebla cambia en el final)
            matObelisco = new Material(matModelos) { name = "Obelisco" };
            var ob = CrearObjeto("Obelisco", generador.obelisco, matObelisco, raiz.transform);
            ob.transform.localPosition = GeneradorNivel.PosObelisco;

            // Cielo
            matCielo = new Material(Material("cielo")) { name = "Cielo" };
            matCielo.SetFloat("_NieblaMul", 0f);
            matCielo.renderQueue = 1000;
            cielo = CrearObjeto("Cielo", generador.cielo, matCielo, raiz.transform).transform;

            // Luces del Obelisco (bengalas rojas y disparos a lo lejos)
            foreach (var p in generador.bengalas) bengalas.Add(Luz(p, new Color32(255, 60, 40, 255), 0.25f));
            foreach (var p in generador.fogonazos) fogonazos.Add(Luz(p, new Color32(255, 230, 150, 255), 0.18f));

            CrearEntidades();
            CrearPuertas();

            // Cámara del mundo (dibuja en una imagen chica que después se agranda)
            var goCam = new GameObject("Camara3D");
            goCam.transform.SetParent(raiz.transform, false);
            camara = goCam.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.48f, 0.52f);
            camara.nearClipPlane = 0.02f;
            camara.farClipPlane = 320f;
            camara.cullingMask = ~(1 << CapaVistaPrevia);
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.depth = -10;
            camara.fieldOfView = campoVisual;
            AjustarAspecto(16f / 9f);

            // Mano con linterna (pegada a la cámara)
            mano = new GameObject("Mano").transform;
            mano.SetParent(camara.transform, false);
            mano.localPosition = new Vector3(0.11f, -0.105f, 0.17f);
            mano.localRotation = Quaternion.Euler(-4f, -6f, 0f);
            manoSin = CrearObjeto("ManoSinTraje", Modelos.Mano(false), matModelos, mano);
            manoCon = CrearObjeto("ManoConTraje", Modelos.Mano(true), matModelos, mano);
            manoCon.SetActive(false);

            CrearNieve();
            CrearVistaPrevia();
            AplicarGlobales(1f, 0.3f);
        }

        // ------------------------------------------------------------------
        // CONSTRUCCIÓN
        // ------------------------------------------------------------------
        static Texture2D ATextura(Color32[] px, int w, int h, bool invertir, string nombre)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = nombre };
            if (invertir)
            {
                var sub = new Color32[px.Length];
                for (int y = 0; y < h; y++) System.Array.Copy(px, y * w, sub, (h - 1 - y) * w, w);
                px = sub;
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        Material Material(string clave)
        {
            Material m;
            if (materiales.TryGetValue(clave, out m)) return m;
            m = new Material(shader) { name = "Eternauta_" + clave };
            Texture2D t;
            if (texturas.TryGetValue(clave, out t)) m.mainTexture = t;
            materiales[clave] = m;
            return m;
        }

        public static Mesh AMalla(ConstructorMalla b, string nombre)
        {
            var m = new Mesh { name = nombre };
            if (b.v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(b.v);
            m.SetUVs(0, b.uv);
            m.SetUVs(1, b.uv2);
            m.SetColors(b.col);
            m.SetTriangles(b.tri, 0);
            m.RecalculateBounds();
            return m;
        }

        static GameObject CrearObjeto(string nombre, ConstructorMalla b, Material mat, Transform padre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = AMalla(b, nombre);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        Transform Luz(Vector3 pos, Color32 color, float tam)
        {
            var b = Modelos.Nueva();
            b.brillo = 1f;
            b.Caja(Vector3.zero, new Vector3(tam, tam, tam), color);
            var go = CrearObjeto("Luz", b, matObelisco, raiz.transform);
            go.transform.localPosition = pos;
            return go.transform;
        }

        void CrearEntidades()
        {
            var padre = new GameObject("Objetos").transform;
            padre.SetParent(raiz.transform, false);
            foreach (var e in mundo.entidades)
            {
                var ev = new EntidadVisual { e = e };
                ev.go = new GameObject(e.def.sprite + "_" + e.def.id);
                ev.go.transform.SetParent(padre, false);
                ev.go.transform.localPosition = new Vector3(e.x, 0, -e.y);
                ev.go.transform.localRotation = Quaternion.Euler(0, e.def.angulo, 0);
                var modelo = Modelos.Crear(e.def.sprite, e.def.id);
                ev.cuerpo = new GameObject("Cuerpo").transform;
                ev.cuerpo.SetParent(ev.go.transform, false);
                CrearObjeto("Malla", modelo.malla, matModelos, ev.cuerpo);
                foreach (var p in modelo.partes)
                {
                    var t = CrearObjeto(p.nombre, p.malla, matModelos, ev.cuerpo).transform;
                    t.localPosition = p.pivote;
                    ev.partes[p.nombre] = t;
                }
                if (e.def.tipo == TipoEntidad.Recurso)
                {
                    // sombra redonda debajo del objeto que gira
                    var s = Modelos.Nueva();
                    s.sombrear = false;
                    s.Cilindro(Vector3.zero, 0.17f, 0.17f, 0.004f, 10, new Color32(70, 74, 82, 255));
                    CrearObjeto("Sombra", s, matModelos, ev.go.transform).transform.localPosition = new Vector3(0, 0.003f, 0);
                }
                ev.interior = mundo.Interior(e.x, e.y);
                ev.luz = generador.LuzEn(e.x, e.y, 0.45f);
                ev.renderers = ev.go.GetComponentsInChildren<Renderer>();
                entidades.Add(ev);
            }
        }

        void CrearPuertas()
        {
            foreach (var info in generador.puertas)
            {
                var pv = new PuertaVisual { info = info };
                var bisagra = new GameObject("Puerta_" + info.x + "_" + info.y).transform;
                bisagra.SetParent(raiz.transform, false);
                bisagra.localPosition = info.bisagra;
                var go = CrearObjeto("Hoja", Modelos.HojaPuerta(info.ancho, GeneradorNivel.AltoPuerta - 0.01f, 0.05f), matModelos, bisagra);
                pv.hoja = bisagra;
                pv.render = go.GetComponent<Renderer>();
                pv.angulo = mundo.Celda(info.x, info.y) == 'd' ? info.anguloAbierta : info.anguloCerrada;
                bisagra.localRotation = Quaternion.Euler(0, pv.angulo, 0);
                puertas.Add(pv);
            }
        }

        void CrearNieve()
        {
            mallaNieve = new Mesh { name = "Nieve" };
            mallaNieve.MarkDynamic();
            var uv = new Vector2[Copos * 4];
            var uv2 = new Vector2[Copos * 4];
            var col = new Color32[Copos * 4];
            var tri = new int[Copos * 6];
            var r = Modelos.Ruido;
            for (int i = 0; i < Copos; i++)
            {
                for (int k = 0; k < 4; k++) { uv[i * 4 + k] = r.UV(0.5f, 0.5f); uv2[i * 4 + k] = new Vector2(0, 1); col[i * 4 + k] = new Color32(236, 240, 248, 255); }
                tri[i * 6 + 0] = i * 4; tri[i * 6 + 1] = i * 4 + 1; tri[i * 6 + 2] = i * 4 + 2;
                tri[i * 6 + 3] = i * 4; tri[i * 6 + 4] = i * 4 + 2; tri[i * 6 + 5] = i * 4 + 3;
                copos[i] = new Vector3(9999f, 0, 0);
                velCopo[i] = 0.35f + (float)rnd.NextDouble() * 0.45f;
            }
            mallaNieve.vertices = vertNieve;
            mallaNieve.uv = uv;
            mallaNieve.uv2 = uv2;
            mallaNieve.colors32 = col;
            mallaNieve.triangles = tri;
            mallaNieve.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
            var go = new GameObject("NieveCayendo");
            go.transform.SetParent(raiz.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mallaNieve;
            var mr = go.AddComponent<MeshRenderer>();
            var matNieve = new Material(matModelos) { name = "Nieve" };
            matNieve.SetFloat("_Cull", 0f);
            matNieve.SetFloat("_NieblaMul", 0.6f);
            mr.sharedMaterial = matNieve;
        }

        void CrearVistaPrevia()
        {
            raizPrevia = new GameObject("VistaPreviaInventario").transform;
            raizPrevia.SetParent(raiz.transform, false);
            raizPrevia.localPosition = new Vector3(0, -500f, 0);
            var goCam = new GameObject("CamaraInventario");
            goCam.transform.SetParent(raizPrevia, false);
            goCam.transform.localPosition = new Vector3(0, 0.12f, -0.62f);
            goCam.transform.localRotation = Quaternion.Euler(12f, 0, 0);
            camPrevia = goCam.AddComponent<Camera>();
            camPrevia.clearFlags = CameraClearFlags.SolidColor;
            camPrevia.backgroundColor = new Color(0x17 / 255f, 0x1B / 255f, 0x1F / 255f);
            camPrevia.cullingMask = 1 << CapaVistaPrevia;
            camPrevia.fieldOfView = 40f;
            camPrevia.nearClipPlane = 0.05f;
            camPrevia.farClipPlane = 5f;
            camPrevia.allowHDR = false;
            camPrevia.allowMSAA = false;
            camPrevia.depth = -20;
            ImagenVistaPrevia = new RenderTexture(160, 160, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "VistaPrevia" };
            camPrevia.targetTexture = ImagenVistaPrevia;
            camPrevia.enabled = false;
        }

        // Muestra en el inventario el modelo 3D del recurso, girando.
        public void ElegirVistaPrevia(string sprite)
        {
            if (sprite == previaActual) return;
            previaActual = sprite;
            foreach (var kv in modelosPrevia) kv.Value.SetActive(kv.Key == sprite);
            if (sprite == null || modelosPrevia.ContainsKey(sprite)) return;
            var modelo = Modelos.Crear(sprite, 1);
            var go = new GameObject("Previa_" + sprite);
            go.transform.SetParent(raizPrevia, false);
            var b = modelo.malla;
            foreach (var p in modelo.partes) { b.Push(); b.Mover(p.pivote); b.Unir(p.malla); b.Pop(); }
            Vector3 min, max;
            b.Limites(out min, out max);
            float tam = Mathf.Max(max.x - min.x, Mathf.Max(max.y - min.y, max.z - min.z));
            var hijo = CrearObjeto("Malla", b, matModelos, go.transform);
            float esc = 0.42f / Mathf.Max(0.01f, tam);
            hijo.transform.localScale = Vector3.one * esc;
            hijo.transform.localPosition = -new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f) * esc;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CapaVistaPrevia;
            modelosPrevia[sprite] = go;
        }

        // ------------------------------------------------------------------
        // POR CUADRO
        // ------------------------------------------------------------------
        public void AjustarAspecto(float aspecto)
        {
            int ancho = Mathf.Clamp(Mathf.RoundToInt(Alto * aspecto), 240, 720);
            if (Imagen != null && ancho == Ancho) return;
            Ancho = ancho;
            if (Imagen != null) { camara.targetTexture = null; Imagen.Release(); Object.Destroy(Imagen); }
            Imagen = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "Eternauta_Imagen" };
            Imagen.Create();
            camara.targetTexture = Imagen;
            camara.aspect = Ancho / (float)Alto;
        }

        void AplicarGlobales(float densidad, float linterna)
        {
            Shader.SetGlobalColor("_EtNieblaColor", new Color(114 / 255f, 122 / 255f, 134 / 255f, 1f));
            Shader.SetGlobalFloat("_EtNieblaDensidad", 0.05f * densidad);
            Shader.SetGlobalFloat("_EtLinterna", linterna);
            Shader.SetGlobalFloat("_EtNiveles", 31f);
            Shader.SetGlobalVector("_EtNorte", new Vector4(-10f, 40f, 0.3f, 0f));
        }

        float linternaActual = 0.3f;

        // x, y = posición en la grilla; angulo = radianes (-PI/2 = norte); inclinacion = grados (+ = arriba).
        public void Actualizar(float dt, float x, float y, float angulo, float inclinacion, float balanceo, float faseBalanceo,
                               Entidad resaltada, int puertaX, int puertaY)
        {
            float t = Time.time;
            camara.fieldOfView = campoVisual;

            // Cámara
            float yaw = Mathf.Atan2(Mathf.Cos(angulo), -Mathf.Sin(angulo)) * Mathf.Rad2Deg;
            float bob = Mathf.Sin(faseBalanceo * 2f) * 0.018f * balanceo;
            camara.transform.localPosition = new Vector3(x, AltoOjos + bob, -y);
            camara.transform.localRotation = Quaternion.Euler(-inclinacion, yaw, 0f);
            cielo.localPosition = new Vector3(x, 0, -y);

            // Mano
            mano.gameObject.SetActive(mostrarManos);
            manoCon.SetActive(conTraje);
            manoSin.SetActive(!conTraje);
            mano.localPosition = new Vector3(0.11f + Mathf.Cos(faseBalanceo) * 0.006f * balanceo, -0.105f - Mathf.Abs(Mathf.Sin(faseBalanceo)) * 0.008f * balanceo, 0.17f);

            // Linterna más fuerte bajo techo
            bool adentro = mundo.Interior(x, y);
            linternaActual = Mathf.MoveTowards(linternaActual, adentro ? 0.85f : 0.3f, dt * 1.5f);
            AplicarGlobales(1f, mostrarManos ? linternaActual : 0.15f);

            // Obelisco: más nítido en la secuencia final
            matObelisco.SetFloat("_NieblaMul", Mathf.Lerp(0.5f, 0.07f, Mathf.Clamp01((visibilidadObelisco - 0.3f) / 0.7f)));

            // Objetos
            foreach (var ev in entidades)
            {
                bool activa = ev.e.activa;
                if (ev.go.activeSelf != activa) ev.go.SetActive(activa);
                if (!activa) continue;
                var def = ev.e.def;
                if (def.tipo == TipoEntidad.Recurso)
                {
                    // gira sobre su eje y flota, como los objetos para recoger de Minecraft
                    ev.cuerpo.localPosition = new Vector3(0, 0.1f + Mathf.Sin(t * 2.2f + def.id) * 0.035f, 0);
                    ev.cuerpo.localRotation = Quaternion.Euler(0, t * 95f + def.id * 37f, 0);
                }
                Transform p;
                if (ev.partes.TryGetValue("llama", out p))
                {
                    float f = 0.85f + 0.25f * Mathf.PerlinNoise(t * 7f, def.id);
                    p.localScale = new Vector3(1f, f, 1f);
                }
                if (ev.partes.TryGetValue("ambar", out p)) p.gameObject.SetActive(Mathf.Repeat(t, 1.2f) < 0.6f);
                if (ev.partes.TryGetValue("dial", out p)) p.gameObject.SetActive(radioEncendida || Mathf.Repeat(t, 2.5f) < 0.08f);
                if (ev.partes.TryGetValue("torso", out p)) p.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.7f) * 0.018f, 1f + Mathf.Sin(t * 1.7f) * 0.025f);
                if (ev.partes.TryGetValue("cabeza", out p))
                {
                    // el Informante mira al jugador cuando está cerca
                    var local = ev.go.transform.InverseTransformPoint(new Vector3(x, AltoOjos, -y));
                    float giro = 0f;
                    if (local.sqrMagnitude < 25f) giro = Mathf.Clamp(Mathf.Atan2(-local.x, -local.z) * Mathf.Rad2Deg, -65f, 65f);
                    float actual = p.localEulerAngles.y;
                    p.localRotation = Quaternion.Euler(4f + Mathf.Sin(t * 0.9f) * 2f, Mathf.MoveTowardsAngle(actual, giro, dt * 120f), 0);
                }
                float res = ev.e == resaltada ? 0.3f + 0.2f * Mathf.Sin(t * 6f) : 0f;
                bloque.Clear();
                bloque.SetColor(IdColor, ev.luz);
                bloque.SetFloat(IdResaltado, res);
                bloque.SetFloat(IdInterior, ev.interior ? 1f : 0f);
                foreach (var r in ev.renderers) r.SetPropertyBlock(bloque);
            }

            // Puertas: giran hacia abierta o cerrada
            foreach (var pv in puertas)
            {
                bool abierta = mundo.Celda(pv.info.x, pv.info.y) == 'd';
                float objetivo = abierta ? pv.info.anguloAbierta : pv.info.anguloCerrada;
                pv.angulo = Mathf.MoveTowardsAngle(pv.angulo, objetivo, dt * 240f);
                pv.hoja.localRotation = Quaternion.Euler(0, pv.angulo, 0);
                bool marcada = pv.info.x == puertaX && pv.info.y == puertaY;
                bloque.Clear();
                bloque.SetColor(IdColor, generador.LuzEn(pv.info.x + 0.5f, pv.info.y + 0.5f, 0.5f));
                bloque.SetFloat(IdResaltado, marcada ? 0.3f + 0.2f * Mathf.Sin(t * 6f) : 0f);
                pv.render.SetPropertyBlock(bloque);
            }

            // Luces del Obelisco
            for (int i = 0; i < bengalas.Count; i++) bengalas[i].gameObject.SetActive(Mathf.PerlinNoise(t * 1.3f, i * 3.1f) > 0.35f);
            for (int i = 0; i < fogonazos.Count; i++) fogonazos[i].gameObject.SetActive(Mathf.PerlinNoise(t * 9f, i * 5.7f) > 0.72f);

            ActualizarNieve(dt, x, y);

            // Vista previa del inventario
            camPrevia.enabled = mostrarVistaPrevia;
            if (mostrarVistaPrevia) raizPrevia.localRotation = Quaternion.Euler(0, t * 60f, 0);
        }

        void ActualizarNieve(float dt, float px, float py)
        {
            float viento = Mathf.Sin(Time.time * 0.3f) * 0.25f + 0.35f;
            Vector3 der = camara.transform.right * 0.014f, arr = camara.transform.up * 0.014f;
            float cx = px, cz = -py;
            for (int i = 0; i < Copos; i++)
            {
                var c = copos[i];
                if (Mathf.Abs(c.x - cx) > 7f || Mathf.Abs(c.z - cz) > 7f || c.y < 0f)
                {
                    c = new Vector3(cx + (float)(rnd.NextDouble() * 14 - 7), c.y < 0f ? 2.6f : (float)rnd.NextDouble() * 2.6f, cz + (float)(rnd.NextDouble() * 14 - 7));
                }
                c.y -= velCopo[i] * dt;
                c.x += viento * dt;
                c.z += Mathf.Sin(Time.time * 1.7f + i) * 0.08f * dt;
                copos[i] = c;
                int gx = Mathf.FloorToInt(c.x), gy = Mathf.FloorToInt(-c.z);
                bool oculto = gy >= 0 && (mundo.Interior(gx, gy) || mundo.Solida(gx, gy));
                Vector3 p = oculto ? new Vector3(0, -900f, 0) : c;
                vertNieve[i * 4 + 0] = p - der - arr;
                vertNieve[i * 4 + 1] = p - der + arr;
                vertNieve[i * 4 + 2] = p + der + arr;
                vertNieve[i * 4 + 3] = p + der - arr;
            }
            mallaNieve.vertices = vertNieve;
        }

        // Nueva partida: el mapa es el mismo, solo cambian los objetos y las puertas.
        public void Vincular(Mundo m)
        {
            mundo = m;
            foreach (var ev in entidades)
            {
                var nueva = m.BuscarEntidad(ev.e.def.id);
                if (nueva != null) ev.e = nueva;
            }
            foreach (var pv in puertas)
            {
                pv.angulo = m.Celda(pv.info.x, pv.info.y) == 'd' ? pv.info.anguloAbierta : pv.info.anguloCerrada;
                pv.hoja.localRotation = Quaternion.Euler(0, pv.angulo, 0);
            }
        }

        public void Destruir()
        {
            if (Imagen != null) { Imagen.Release(); Object.Destroy(Imagen); }
            if (ImagenVistaPrevia != null) { ImagenVistaPrevia.Release(); Object.Destroy(ImagenVistaPrevia); }
            Object.Destroy(raiz);
        }
    }
}
