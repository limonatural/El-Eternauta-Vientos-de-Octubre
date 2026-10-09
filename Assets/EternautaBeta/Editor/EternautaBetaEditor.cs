using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eternauta.Beta.Herramientas
{
    // Menú "Eternauta" en la barra superior de Unity.
    // La primera vez que se abre el proyecto también abre la escena de la beta.
    [InitializeOnLoad]
    public static class EternautaBetaEditor
    {
        public const string RutaEscena = "Assets/EternautaBeta/Scenes/Eternauta_Beta.unity";

        static EternautaBetaEditor()
        {
            EditorApplication.delayCall += AlIniciar;
        }

        static void AlIniciar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            AsegurarEnBuild();
            string clave = "EternautaBeta_escena_abierta_" + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetBool(clave, false)) return;
            EditorPrefs.SetBool(clave, true);
            if (!File.Exists(RutaEscena)) CrearEscena();
            else if (EditorSceneManager.GetActiveScene().path != RutaEscena) EditorSceneManager.OpenScene(RutaEscena);
        }

        [MenuItem("Eternauta/Jugar beta", false, 0)]
        static void Jugar()
        {
            if (!AbrirEscena()) return;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Eternauta/Abrir escena Beta", false, 1)]
        static void MenuAbrir() { AbrirEscena(); }

        static bool AbrirEscena()
        {
            if (!File.Exists(RutaEscena)) CrearEscena();
            if (EditorSceneManager.GetActiveScene().path == RutaEscena) return true;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(RutaEscena);
            return true;
        }

        [MenuItem("Eternauta/Crear escena Beta (reparar)", false, 2)]
        static void CrearEscena()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            c.cullingMask = 0;
            cam.AddComponent<AudioListener>();
            new GameObject("EternautaGame").AddComponent<EternautaGame>();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AsegurarEnBuild();
            Debug.Log("[Eternauta] Escena creada en " + RutaEscena);
        }

        [MenuItem("Eternauta/Abrir carpeta de capturas", false, 20)]
        static void CarpetaCapturas()
        {
            Directory.CreateDirectory(Capturas.Carpeta);
            EditorUtility.RevealInFinder(Capturas.Carpeta + Path.DirectorySeparatorChar);
        }

        [MenuItem("Eternauta/Abrir carpeta de la partida guardada", false, 21)]
        static void CarpetaPartida()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath + Path.DirectorySeparatorChar);
        }

        [MenuItem("Eternauta/Borrar partida guardada", false, 22)]
        static void BorrarPartida()
        {
            SistemaGuardado.Borrar();
            Debug.Log("[Eternauta] Partida guardada borrada.");
        }

        static void AsegurarEnBuild()
        {
            var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (lista.Exists(s => s.path == RutaEscena)) return;
            lista.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
