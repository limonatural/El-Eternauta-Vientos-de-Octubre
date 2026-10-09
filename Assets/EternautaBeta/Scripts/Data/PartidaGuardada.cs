using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Eternauta.Beta
{
    // Estado y progreso de la partida (Etapa 13: Jugador, Partida, Inventario,
    // PartidaObjetivo y PartidaEvento). Se guarda en un archivo JSON local (Etapa 14).
    [Serializable]
    public class PartidaGuardada
    {
        public int version = 1;

        // JUGADOR
        public int id_jugador = 1;
        public string nombre_jugador = "Jugador";

        // PARTIDA
        public int id_partida = 1;
        public string fecha_guardado;
        public float progreso;            // 0 a 100 %
        public int vida;
        public int id_escenario_actual;
        public int id_capitulo = 1;
        public float pos_x, pos_y, angulo;
        public bool traje_aislante;

        // INVENTARIO (PartidaRecurso)
        public List<ItemInventario> inventario = new List<ItemInventario>();
        // PARTIDAOBJETIVO
        public List<EstadoObjetivo> objetivos = new List<EstadoObjetivo>();
        // PARTIDAEVENTO
        public List<EstadoEvento> eventos = new List<EstadoEvento>();

        // Estado del mundo necesario para retomar la partida
        public List<int> entidades_retiradas = new List<int>();
        public List<string> puertas_abiertas = new List<string>();
    }

    [Serializable]
    public class ItemInventario
    {
        public int id_recurso;
        public int cantidad;
    }

    [Serializable]
    public class EstadoObjetivo
    {
        public int id_objetivo;
        public string estado; // "Pendiente", "En progreso", "Completado"
    }

    [Serializable]
    public class EstadoEvento
    {
        public int id_evento;
        public bool activado;
    }

    public static class SistemaGuardado
    {
        public static string Ruta
        {
            get { return Path.Combine(Application.persistentDataPath, "eternauta_partida.json"); }
        }

        public static bool ExistePartida()
        {
            try { return File.Exists(Ruta) && Cargar() != null; }
            catch { return false; }
        }

        public static void Guardar(PartidaGuardada p)
        {
            p.fecha_guardado = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            File.WriteAllText(Ruta, JsonUtility.ToJson(p, true));
        }

        public static PartidaGuardada Cargar()
        {
            if (!File.Exists(Ruta)) return null;
            try
            {
                var p = JsonUtility.FromJson<PartidaGuardada>(File.ReadAllText(Ruta));
                return (p != null && p.version >= 1) ? p : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Eternauta] No se pudo leer la partida guardada: " + e.Message);
                return null;
            }
        }

        public static void Borrar()
        {
            if (File.Exists(Ruta)) File.Delete(Ruta);
        }
    }
}
