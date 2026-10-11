using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Eternauta.Beta.Herramientas
{
    // Al compilar para Windows deja, al lado del .exe, accesos para correr la prueba automática
    // en las tres resoluciones de la Etapa 12 (doble clic en cada .bat).
    public class CopiarPruebasAlBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        static readonly int[,] Resoluciones = { { 1920, 1080 }, { 1366, 768 }, { 1024, 600 } };

        public void OnPostprocessBuild(BuildReport report)
        {
            var destino = report.summary.platform;
            if (destino != BuildTarget.StandaloneWindows && destino != BuildTarget.StandaloneWindows64) return;
            string exe = report.summary.outputPath;
            string carpeta = Path.GetDirectoryName(exe);
            string nombre = Path.GetFileName(exe);
            try
            {
                for (int i = 0; i < Resoluciones.GetLength(0); i++)
                {
                    int w = Resoluciones[i, 0], h = Resoluciones[i, 1];
                    string bat = "@echo off\r\n" +
                                 "rem Prueba automática de la beta (ver docs/beta/PRUEBAS_BETA_1.1.md)\r\n" +
                                 "start \"\" \"%~dp0" + nombre + "\" -prueba -screen-fullscreen 0 -screen-width " + w + " -screen-height " + h + "\r\n";
                    File.WriteAllText(Path.Combine(carpeta, "PRUEBA_AUTOMATICA_" + w + "x" + h + ".bat"), bat, new UTF8Encoding(false));
                }
                File.WriteAllText(Path.Combine(carpeta, "LEEME_PRUEBAS.txt"),
                    "El Eternauta: Vientos de Octubre - beta " + PlayerSettings.bundleVersion + "\r\n\r\n" +
                    "Para jugar: doble clic en " + nombre + ".\r\n\r\n" +
                    "Prueba automática: doble clic en PRUEBA_AUTOMATICA_1920x1080.bat (o en las otras resoluciones).\r\n" +
                    "No toques nada mientras corre (alrededor de un minuto y medio). Al terminar, el cartel de arriba\r\n" +
                    "dice PRUEBA TERMINADA. El informe y las capturas quedan en la carpeta Capturas\\Pruebas, al lado del .exe.\r\n",
                    new UTF8Encoding(false));
                Debug.Log("[Eternauta] Se copiaron los accesos de la prueba automática en " + carpeta);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Eternauta] No se pudieron copiar los accesos de la prueba: " + e.Message);
            }
        }
    }
}
