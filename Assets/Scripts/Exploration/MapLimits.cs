using UnityEngine;

// TT08 - Implementar sistema de exploración (TF02)
// GameObject: Player (junto a PlayerController)
// Limita la zona por la que el jugador puede moverse.
public class MapLimits : MonoBehaviour
{
    [SerializeField] private Vector2 minLimit = new Vector2(-10f, -6f); // esquina inferior izquierda
    [SerializeField] private Vector2 maxLimit = new Vector2(10f, 6f);   // esquina superior derecha

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minLimit.x, maxLimit.x);
        pos.y = Mathf.Clamp(pos.y, minLimit.y, maxLimit.y);
        transform.position = pos;
    }

    // Dibuja el rectángulo del mapa en la ventana Scene (solo para ayudar a configurarlo)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = (minLimit + maxLimit) / 2f;
        Vector3 size = maxLimit - minLimit;
        Gizmos.DrawWireCube(center, size);
    }
}
