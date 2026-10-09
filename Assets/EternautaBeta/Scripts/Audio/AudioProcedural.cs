using UnityEngine;

namespace Eternauta.Beta
{
    // Sonidos ambientales básicos (Etapa 4) generados por código: viento de la
    // tormenta, pasos en la nieve, puertas, estática de radio, latidos y un drone de menú.
    public class AudioProcedural
    {
        const int Frec = 22050;
        readonly AudioSource viento, drone, radio, efectos;
        readonly AudioClip clipPasoNieve, clipPasoMadera, clipRecoger, clipPuerta, clipMenu, clipLatido, clipError, clipTraje;
        float proximoPaso, proximoLatido;

        public AudioProcedural(GameObject host)
        {
            var rnd = new System.Random(3);
            viento = Fuente(host, Viento(rnd), true, 0f);
            drone = Fuente(host, Drone(), true, 0f);
            radio = Fuente(host, Estatica(rnd), true, 0f);
            efectos = host.AddComponent<AudioSource>();
            efectos.playOnAwake = false;

            clipPasoNieve = Ruido("paso_nieve", 0.16f, 0.35f, 0.55f, rnd);
            clipPasoMadera = Golpe("paso_madera", 0.09f, 90f, 0.35f);
            clipRecoger = Tonos("recoger", new[] { 660f, 880f }, 0.08f, 0.25f);
            clipPuerta = Golpe("puerta", 0.35f, 55f, 0.6f);
            clipMenu = Tonos("menu", new[] { 440f }, 0.04f, 0.15f);
            clipError = Tonos("error", new[] { 180f, 150f }, 0.09f, 0.25f);
            clipLatido = Golpe("latido", 0.18f, 48f, 0.7f);
            clipTraje = Tonos("traje", new[] { 330f, 440f, 554f, 660f }, 0.1f, 0.25f);
        }

        static AudioSource Fuente(GameObject host, AudioClip clip, bool loop, float vol)
        {
            var s = host.AddComponent<AudioSource>();
            s.clip = clip; s.loop = loop; s.volume = vol; s.playOnAwake = false;
            s.Play();
            return s;
        }

        static AudioClip Crear(string nombre, float[] datos)
        {
            var c = AudioClip.Create(nombre, datos.Length, 1, Frec, false);
            c.SetData(datos, 0);
            return c;
        }

        static AudioClip Viento(System.Random rnd)
        {
            int n = Frec * 6;
            var d = new float[n];
            float b = 0f, lento = 0f;
            for (int i = 0; i < n; i++)
            {
                float blanco = (float)(rnd.NextDouble() * 2 - 1);
                b = b * 0.985f + blanco * 0.015f;           // ruido marrón (grave)
                lento = lento * 0.9995f + blanco * 0.0005f;
                float t = i / (float)n;
                float rafaga = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 2f) * Mathf.Sin(t * Mathf.PI * 2f * 3f + 1f);
                d[i] = Mathf.Clamp(b * 6f * rafaga + lento * 4f, -1f, 1f);
            }
            // fundido de las puntas para que el loop no haga click
            for (int i = 0; i < 2000; i++) { float k = i / 2000f; d[i] *= k; d[n - 1 - i] *= k; }
            return Crear("viento", d);
        }

        static AudioClip Drone()
        {
            int n = Frec * 8;
            var d = new float[n];
            float[] notas = { 55f, 82.41f, 110f, 130.81f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Frec;
                float v = 0f;
                for (int k = 0; k < notas.Length; k++)
                    v += Mathf.Sin(2f * Mathf.PI * notas[k] * t) * (0.25f + 0.2f * Mathf.Sin(t * 0.785f * (k + 1)));
                d[i] = v * 0.18f;
            }
            return Crear("drone", d);
        }

        static AudioClip Estatica(System.Random rnd)
        {
            int n = Frec * 2;
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Frec;
                float voz = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Max(0f, Mathf.Sin(t * 9f)) * 0.15f;
                d[i] = (float)(rnd.NextDouble() * 2 - 1) * 0.35f + voz;
            }
            return Crear("estatica", d);
        }

        static AudioClip Ruido(string nombre, float dur, float vol, float filtro, System.Random rnd)
        {
            int n = (int)(Frec * dur);
            var d = new float[n];
            float f = 0f;
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Pow(1f - i / (float)n, 2f);
                f = f * filtro + (float)(rnd.NextDouble() * 2 - 1) * (1f - filtro);
                d[i] = f * env * vol * 3f;
            }
            return Crear(nombre, d);
        }

        static AudioClip Golpe(string nombre, float dur, float hz, float vol)
        {
            int n = (int)(Frec * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Frec;
                float env = Mathf.Exp(-t * 18f / dur * 0.1f) * (1f - i / (float)n);
                d[i] = Mathf.Sin(2f * Mathf.PI * hz * t * (1f - t * 0.8f)) * env * vol;
            }
            return Crear(nombre, d);
        }

        static AudioClip Tonos(string nombre, float[] hz, float durNota, float vol)
        {
            int porNota = (int)(Frec * durNota);
            var d = new float[porNota * hz.Length];
            for (int k = 0; k < hz.Length; k++)
                for (int i = 0; i < porNota; i++)
                {
                    float t = i / (float)Frec;
                    float env = Mathf.Min(1f, i / 200f) * (1f - i / (float)porNota);
                    float onda = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz[k] * t)) * 0.5f + Mathf.Sin(2f * Mathf.PI * hz[k] * t) * 0.5f;
                    d[k * porNota + i] = onda * env * vol;
                }
            return Crear(nombre, d);
        }

        // Llamar cada frame.
        public void Actualizar(bool enMenu, bool jugando, bool bajoTecho, bool caminando, bool corriendo, bool radioSonando, bool saludBaja, float dt)
        {
            float objViento = enMenu ? 0.25f : (jugando ? (bajoTecho ? 0.12f : 0.55f) : 0.2f);
            viento.volume = Mathf.MoveTowards(viento.volume, objViento, dt * 0.6f);
            drone.volume = Mathf.MoveTowards(drone.volume, enMenu ? 0.5f : 0f, dt * 0.5f);
            radio.volume = Mathf.MoveTowards(radio.volume, radioSonando ? 0.35f : 0f, dt * 1.5f);

            if (jugando && caminando)
            {
                proximoPaso -= dt;
                if (proximoPaso <= 0f)
                {
                    efectos.PlayOneShot(bajoTecho ? clipPasoMadera : clipPasoNieve, 0.8f);
                    proximoPaso = corriendo ? 0.3f : 0.45f;
                }
            }
            else proximoPaso = 0.1f;

            if (jugando && saludBaja)
            {
                proximoLatido -= dt;
                if (proximoLatido <= 0f)
                {
                    efectos.PlayOneShot(clipLatido, 0.9f);
                    proximoLatido = 1.1f;
                }
            }
        }

        public void Recoger() { efectos.PlayOneShot(clipRecoger, 0.8f); }
        public void Puerta() { efectos.PlayOneShot(clipPuerta, 1f); }
        public void Menu() { efectos.PlayOneShot(clipMenu, 0.6f); }
        public void Error() { efectos.PlayOneShot(clipError, 0.7f); }
        public void Traje() { efectos.PlayOneShot(clipTraje, 0.8f); }
    }
}
