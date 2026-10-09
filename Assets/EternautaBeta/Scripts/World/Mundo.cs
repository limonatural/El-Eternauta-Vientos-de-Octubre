using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    public class Entidad
    {
        public EntidadDef def;
        public SpriteDef sprite;
        public bool activa = true;   // false = ya se recogió
        public float x, y;
        public float dist;           // distancia a la cámara (para ordenar al dibujar)
    }

    // Mapa en grilla (Etapa 4: mapa reducido y cerrado de Avellaneda) y objetos del escenario.
    public class Mundo
    {
        public readonly int ancho, alto;
        readonly char[] celdas;
        public readonly List<Entidad> entidades = new List<Entidad>();
        public readonly HashSet<string> puertasAbiertas = new HashSet<string>();

        public Mundo(Dictionary<string, SpriteDef> sprites)
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
            {
                SpriteDef s;
                if (!sprites.TryGetValue(def.sprite, out s))
                {
                    Debug.LogWarning("[Eternauta] Falta el sprite " + def.sprite);
                    continue;
                }
                entidades.Add(new Entidad { def = def, sprite = s, x = def.x, y = def.y });
            }
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

        public bool Solida(int x, int y) { return EsPared(Celda(x, y)); }

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

        public Entidad BuscarEntidad(int id)
        {
            foreach (var e in entidades) if (e.def.id == id) return e;
            return null;
        }
    }
}
