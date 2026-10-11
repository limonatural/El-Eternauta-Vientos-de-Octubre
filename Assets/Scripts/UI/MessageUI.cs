// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/UI/EternautaGameUI.cs (mensajes).
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using System.Collections;
using TMPro;
using UnityEngine;

// Muestra mensajes simples en pantalla (lo usan interacción, recursos y diálogos).
// GameObject: "MessageUI" vacío en la escena. Arrastrar un TextMeshPro - Text (UI) a "messageText".
public class MessageUI : MonoBehaviour
{
    public static MessageUI Instance;

    [SerializeField] private TMP_Text messageText;

    private Coroutine current;

    void Awake()
    {
        Instance = this;
        messageText.text = "";
    }

    public void Show(string text, float seconds = 2.5f)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(ShowRoutine(text, seconds));
    }

    private IEnumerator ShowRoutine(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }
}
