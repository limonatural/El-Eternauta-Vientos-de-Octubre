// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Logic/Progresion.cs.
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using TMPro;
using UnityEngine;

// TT11 - Implementar sistema de objetivos (TF05)
// GameObject: "GameManager" (o cualquier objeto vacío de la escena).
// Para agregar objetivos nuevos: sumar textos a la lista "objectives" en el Inspector.
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [SerializeField] private TMP_Text objectiveText; // TextMeshPro - Text (UI)
    [SerializeField] private string[] objectives =
    {
        "Buscar comida en la casa abandonada",
        "Hablar con el sobreviviente",
        "Llegar al refugio"
    };

    private int currentIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void CompleteCurrentObjective()
    {
        if (currentIndex >= objectives.Length) return;

        MessageUI.Instance.Show("Objetivo completado: " + objectives[currentIndex]);
        currentIndex++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentIndex < objectives.Length)
            objectiveText.text = "Objetivo: " + objectives[currentIndex];
        else
            objectiveText.text = "Todos los objetivos completados";
    }
}
