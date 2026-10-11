using System.Collections.Generic;

namespace Eternauta.Beta
{
    // Inventario básico de recursos (Etapa 4) con las reglas de la Etapa 12 (láminas 7 a 7C):
    // las cantidades se guardan por objeto; los contadores de categoría y el total
    // RECURSOS del HUD se calculan sumando.
    public class Inventario
    {
        readonly Dictionary<int, int> cantidades = new Dictionary<int, int>();

        public static readonly Categoria[] Orden =
        {
            Categoria.Medicamentos, Categoria.Comida, Categoria.Materiales, Categoria.ObjetosEncontrados
        };

        public static string NombreCategoria(Categoria c)
        {
            switch (c)
            {
                case Categoria.Medicamentos: return "Medicamentos";
                case Categoria.Comida: return "Comida";
                case Categoria.Materiales: return "Materiales";
                default: return "Objetos encontrados";
            }
        }

        public void Agregar(int idRecurso, int cantidad)
        {
            int actual;
            cantidades.TryGetValue(idRecurso, out actual);
            cantidades[idRecurso] = actual + cantidad;
        }

        public bool Quitar(int idRecurso, int cantidad)
        {
            int actual;
            if (!cantidades.TryGetValue(idRecurso, out actual) || actual < cantidad) return false;
            cantidades[idRecurso] = actual - cantidad;
            return true;
        }

        public int Cantidad(int idRecurso)
        {
            int c;
            return cantidades.TryGetValue(idRecurso, out c) ? c : 0;
        }

        public int CantidadCategoria(Categoria cat)
        {
            int total = 0;
            foreach (var kv in cantidades)
            {
                var r = ContenidoJuego.Recurso(kv.Key);
                if (r != null && r.categoria == cat) total += kv.Value;
            }
            return total;
        }

        public int Total()
        {
            int total = 0;
            foreach (var kv in cantidades) total += kv.Value;
            return total;
        }

        // Objetos de una categoría con cantidad mayor a 0, en orden de ID.
        public List<RecursoDef> ObjetosDe(Categoria cat)
        {
            var lista = new List<RecursoDef>();
            foreach (var r in ContenidoJuego.Recursos.Values)
                if (r.categoria == cat && Cantidad(r.id) > 0) lista.Add(r);
            lista.Sort((a, b) => a.id.CompareTo(b.id));
            return lista;
        }

        // Saca materiales (los que haya) hasta juntar la cantidad pedida.
        public bool ConsumirMateriales(int cantidad)
        {
            if (CantidadCategoria(Categoria.Materiales) < cantidad) return false;
            foreach (var r in ObjetosDe(Categoria.Materiales))
            {
                while (cantidad > 0 && Quitar(r.id, 1)) cantidad--;
                if (cantidad == 0) break;
            }
            return true;
        }

        public List<ItemInventario> Exportar()
        {
            var lista = new List<ItemInventario>();
            foreach (var kv in cantidades)
                if (kv.Value > 0) lista.Add(new ItemInventario { id_recurso = kv.Key, cantidad = kv.Value });
            return lista;
        }

        public void Importar(List<ItemInventario> items)
        {
            cantidades.Clear();
            if (items == null) return;
            foreach (var i in items) if (i.cantidad > 0) cantidades[i.id_recurso] = i.cantidad;
        }
    }
}
