using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    public class Entidad
    {
        public EntidadDef def;
        public bool activa = true;   // false = ya se recogió
        public float x, y;
    }

    // Mapa en grilla (Etapa 4: mapa reducido y cerrado de Avellaneda) y objetos del escenario.
    // El mapa sigue siendo una grilla para la lógica (choques, zonas, puertas); lo que se ve
    // se arma en 3D a partir de esta grilla (ver GeneradorNivel).
    public class Mundo
    {
        public readonly int ancho, alto;
        readonly char[] celdas;
        public readonly List<Entidad> entidades = new List<Entidad>();
        public readonly HashSet<string> puertasAbiertas = new HashSet<string>();

        // Marco de las puertas: el paso libre es más angosto que la celda.
        public const float MarcoPuerta = 0.13f;

        public Mundo()
        {
            var mapa = ContenidoJuego.Mapa;
            alto = mapa.Length;
            ancho = 0;
            foreach (var fila in mapa) ancho = Mathf.Max(ancho, fila.Length);
            celdas = new char[ancho * alto];
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                    celdas[y * ancho + x] = x < mapa[y].Length ? mapa[y][x] : '#';

            foreach (var def in ContenidoJuego.Entidades)
                entidades.Add(new Entidad { def = def, x = def.x, y = def.y });
        }

        public char Celda(int x, int y)
        {
            if (x < 0 || y < 0 || x >= ancho || y >= alto) return '#';
            return celdas[y * ancho + x];
        }

        public static bool EsPared(char c)
        {
            return c != '.' && c != '=' && c != ',' && c != ';' && c != 'd';
        }

        public static bool EsPuerta(char c) { return c == 'D' || c == 'd'; }

        public bool Solida(int x, int y) { return EsPared(Celda(x, y)); }

        // Choque con un punto: las puertas abiertas dejan pasar solo por el hueco (sin atravesar el marco).
        public bool SolidaPunto(float px, float py)
        {
            int cx = Mathf.FloorToInt(px), cy = Mathf.FloorToInt(py);
            char c = Celda(cx, cy);
            if (EsPared(c)) return true;
            if (c != 'd') return false;
            float lateral = LateralEnX(cx, cy) ? px - cx : py - cy;
            return lateral < MarcoPuerta || lateral > 1f - MarcoPuerta;
        }

        // true si las paredes que rodean la puerta están a los lados en X (la puerta se cruza moviéndose en Y).
        public bool LateralEnX(int x, int y)
        {
            return EsPared(Celda(x - 1, y)) && !EsPuerta(Celda(x - 1, y)) && EsPared(Celda(x + 1, y)) && !EsPuerta(Celda(x + 1, y));
        }

        // Las celdas con techo no muestran cielo y protegen de la nieve.
        public bool Interior(int x, int y)
        {
            char c = Celda(x, y);
            return c == ',' || c == ';' || c == 'd' || c == 'D';
        }

        public bool Interior(float x, float y) { return Interior(Mathf.FloorToInt(x), Mathf.FloorToInt(y)); }

        public static string ClavePuerta(int x, int y) { return x + "," + y; }

        public void AbrirPuerta(int x, int y)
        {
            if (Celda(x, y) != 'D') return;
            celdas[y * ancho + x] = 'd';
            puertasAbiertas.Add(ClavePuerta(x, y));
        }

        public void CerrarPuerta(int x, int y)
        {
            if (Celda(x, y) != 'd') return;
            celdas[y * ancho + x] = 'D';
            puertasAbiertas.Remove(ClavePuerta(x, y));
        }

        public Entidad BuscarEntidad(int id)
        {
            foreach (var e in entidades) if (e.def.id == id) return e;
            return null;
        }
    }
}
