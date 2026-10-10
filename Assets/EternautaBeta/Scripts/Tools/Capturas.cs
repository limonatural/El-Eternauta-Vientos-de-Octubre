using System;
using System.IO;
using UnityEngine;

namespace Eternauta.Beta
{
    // F12: guarda una captura PNG (con la interfaz incluida) para usar como evidencia.
    // En el Editor quedan en <carpeta del proyecto>/Capturas; en el juego compilado,
    // en la carpeta Capturas al lado del .exe.
    public static class Capturas
    {
        static string ultimo;
        static float avisoDesde, avisoHasta;

        public static string Carpeta
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas")); }
        }

        public static void Sacar(MonoBehaviour quien, string zona)
        {
            try
            {
                Directory.CreateDirectory(Carpeta);
                string sufijo = string.IsNullOrEmpty(zona) ? "menu" : Limpiar(zona);
                string nombre = "eternauta_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + sufijo + ".png";
                ScreenCapture.CaptureScreenshot(Path.Combine(Carpeta, nombre));
                ultimo = "Capturas/" + nombre;
                // El aviso aparece después, para que no salga en la propia captura.
                avisoDesde = Time.unscaledTime + 0.2f;
                avisoHasta = avisoDesde + 2.5f;
                Debug.Log("[Eternauta] Captura guardada en " + Path.Combine(Carpeta, nombre));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Eternauta] No se pudo guardar la captura: " + e.Message);
            }
        }

        static string Limpiar(string s)
        {
            s = s.ToLowerInvariant().Replace(' ', '_');
            string salida = "";
            foreach (char c in s)
            {
                switch (c)
                {
                    case 'á': salida += 'a'; break;
                    case 'é': salida += 'e'; break;
                    case 'í': salida += 'i'; break;
                    case 'ó': salida += 'o'; break;
                    case 'ú': salida += 'u'; break;
                    case 'ñ': salida += 'n'; break;
                    default: if (char.IsLetterOrDigit(c) || c == '_') salida += c; break;
                }
            }
            return salida;
        }

        // Texto del aviso "Captura guardada" mientras debe verse (lo dibuja la interfaz); null si no hay aviso.
        public static string AvisoVisible()
        {
            if (ultimo == null) return null;
            float t = Time.unscaledTime;
            if (t < avisoDesde || t > avisoHasta) return null;
            return "Captura guardada: " + ultimo;
        }
    }
}
