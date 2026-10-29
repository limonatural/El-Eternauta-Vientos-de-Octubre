using System.Collections.Generic;

namespace Eternauta.Beta
{
    // Qué hay dentro de cajones y heladeras. Cada contenedor tiene una tabla (o un contenido fijo)
    // y su contenido se decide con una semilla propia: siempre es el mismo en la misma partida.
    public static class Botin
    {
        class Fila { public int id, peso, min, max; public Fila(int id, int peso, int min, int max) { this.id = id; this.peso = peso; this.min = min; this.max = max; } }

        static Fila F(int id, int peso, int min = 1, int max = 1) { return new Fila(id, peso, min, max); }

        // IDs: 1 medicamento, 2 lata, 3 galletitas, 4 choripán, 5 lona, 6 alambre, 7 chatarra, 8 foto,
        //      10 munición, 12 cuchillo, 13 palo, 15 carne congelada, 16 hamburguesa, 17 gaseosa
        static readonly Dictionary<string, Fila[]> Tablas = new Dictionary<string, Fila[]>
        {
            { "cajon", new[] { F(1, 3), F(3, 3), F(7, 4), F(5, 1), F(6, 2), F(8, 1), F(10, 3, 3, 8), F(12, 1), F(2, 2) } },
            { "cajon_cocina", new[] { F(2, 4), F(3, 3), F(12, 2), F(17, 3), F(4, 2), F(1, 1) } },
            { "oficina", new[] { F(7, 3), F(8, 2), F(3, 3), F(1, 2), F(10, 2, 3, 6), F(6, 2) } },
            { "ferreteria", new[] { F(7, 4), F(6, 4), F(5, 3), F(13, 1), F(12, 1), F(10, 1, 3, 6) } },
            { "botica", new[] { F(1, 8, 1, 2), F(3, 2), F(6, 1), F(8, 1) } },
            { "kiosco", new[] { F(3, 4), F(17, 4), F(2, 2), F(1, 1), F(4, 1) } },
            { "alacena", new[] { F(2, 4), F(3, 3), F(17, 3), F(4, 1) } },
            { "ejercito", new[] { F(10, 8, 5, 12), F(1, 3), F(2, 2), F(6, 1) } },
            { "sala", new[] { F(8, 2), F(7, 2), F(3, 2) } },
            // heladeras: solo comida (la carne congelada es lo peor que se puede comer)
            { "heladera", new[] { F(15, 4), F(17, 3), F(16, 2), F(4, 2), F(2, 1) } },
        };

        // Devuelve pares (id recurso, cantidad). Contenido fijo "idxCant,idxCant" o 1 a 3 cosas de la tabla.
        public static List<int[]> Abrir(EntidadDef def, int semilla)
        {
            var lista = new List<int[]>();
            if (!string.IsNullOrEmpty(def.fijo))
            {
                foreach (var par in def.fijo.Split(','))
                {
                    var p = par.Split('x');
                    int id, cant;
                    if (p.Length == 2 && int.TryParse(p[0], out id) && int.TryParse(p[1], out cant)) Sumar(lista, id, cant);
                }
                return lista;
            }
            Fila[] tabla;
            if (string.IsNullOrEmpty(def.tabla) || !Tablas.TryGetValue(def.tabla, out tabla)) return lista;
            var rnd = new System.Random(semilla * 7919 + def.id * 104729);
            int objetos = 1 + rnd.Next(3);                       // 1 a 3 objetos
            if (def.tabla == "heladera") objetos = 1 + rnd.Next(2);
            int total = 0;
            foreach (var f in tabla) total += f.peso;
            for (int i = 0; i < objetos; i++)
            {
                int t = rnd.Next(total);
                foreach (var f in tabla)
                {
                    if (t < f.peso) { Sumar(lista, f.id, f.min + rnd.Next(f.max - f.min + 1)); break; }
                    t -= f.peso;
                }
            }
            return lista;
        }

        static void Sumar(List<int[]> lista, int id, int cant)
        {
            foreach (var p in lista) if (p[0] == id) { p[1] += cant; return; }
            lista.Add(new[] { id, cant });
        }
    }
}
