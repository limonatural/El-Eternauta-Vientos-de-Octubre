using System.Collections.Generic;

namespace Eternauta.Beta
{
    // Sistema de objetivos (RF08), progresión de la historia (RF09) y eventos narrativos (RF10).
    // Un solo objetivo activo a la vez (Etapa 12, lámina 6D).
    public class Progresion
    {
        public int indiceObjetivo; // posición en ContenidoJuego.Objetivos; == Count cuando no queda ninguno
        readonly HashSet<int> eventos = new HashSet<int>();

        public ObjetivoDef Actual
        {
            get { return indiceObjetivo < ContenidoJuego.Objetivos.Count ? ContenidoJuego.Objetivos[indiceObjetivo] : null; }
        }

        public bool EsActual(int idObjetivo) { return Actual != null && Actual.id == idObjetivo; }

        public bool Completado(int idObjetivo)
        {
            for (int i = 0; i < indiceObjetivo && i < ContenidoJuego.Objetivos.Count; i++)
                if (ContenidoJuego.Objetivos[i].id == idObjetivo) return true;
            return false;
        }

        // Devuelve el objetivo completado (o null si no era el actual).
        public ObjetivoDef Completar(int idObjetivo)
        {
            if (!EsActual(idObjetivo)) return null;
            var o = Actual;
            indiceObjetivo++;
            return o;
        }

        public bool Activar(int idEvento) { return eventos.Add(idEvento); }
        public bool Activado(int idEvento) { return eventos.Contains(idEvento); }

        public float PorcentajeProgreso()
        {
            return 100f * indiceObjetivo / ContenidoJuego.Objetivos.Count;
        }

        public List<EstadoObjetivo> ExportarObjetivos()
        {
            var lista = new List<EstadoObjetivo>();
            for (int i = 0; i < ContenidoJuego.Objetivos.Count; i++)
            {
                string estado = i < indiceObjetivo ? "Completado" : (i == indiceObjetivo ? "En progreso" : "Pendiente");
                lista.Add(new EstadoObjetivo { id_objetivo = ContenidoJuego.Objetivos[i].id, estado = estado });
            }
            return lista;
        }

        public List<EstadoEvento> ExportarEventos()
        {
            var lista = new List<EstadoEvento>();
            foreach (var e in ContenidoJuego.Eventos)
                lista.Add(new EstadoEvento { id_evento = e.id, activado = eventos.Contains(e.id) });
            return lista;
        }

        public void Importar(List<EstadoObjetivo> objetivos, List<EstadoEvento> evs)
        {
            indiceObjetivo = 0;
            eventos.Clear();
            if (objetivos != null)
                for (int i = 0; i < ContenidoJuego.Objetivos.Count; i++)
                {
                    var o = objetivos.Find(x => x.id_objetivo == ContenidoJuego.Objetivos[i].id);
                    if (o != null && o.estado == "Completado") indiceObjetivo = i + 1;
                }
            if (evs != null) foreach (var e in evs) if (e.activado) eventos.Add(e.id_evento);
        }
    }
}
