// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Logic/Inventario.cs.
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using TMPro;
using UnityEngine;

// TT10 - Implementar sistema de recolección de recursos (TF04)
// GameObject: "GameManager" (o cualquier objeto vacío de la escena).
// Lleva la cuenta de los recursos y la muestra en pantalla.
public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance;

    [SerializeField] private TMP_Text counterText; // TextMeshPro - Text (UI)

    private int food;
    private int medicine;
    private int materials;

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void Add(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Food: food += amount; break;
            case ResourceType.Medicine: medicine += amount; break;
            case ResourceType.Materials: materials += amount; break;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        counterText.text = "Comida: " + food + "\nMedicinas: " + medicine + "\nMateriales: " + materials;
    }
}
