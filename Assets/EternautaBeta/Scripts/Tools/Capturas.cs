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

        public static void DibujarAviso(float anchoUI, float altoUI, EternautaGame juego)
        {
            if (ultimo == null || Event.current.type != EventType.Repaint) return;
            float t = Time.unscaledTime;
            if (t < avisoDesde || t > avisoHasta) return;
            var r = new Rect(anchoUI - 760, altoUI - 90, 720, 50);
            var antes = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
            var st = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = new Color(0.906f, 0.925f, 0.937f);
            GUI.Label(r, "Captura guardada: " + ultimo, st);
        }
    }
}
