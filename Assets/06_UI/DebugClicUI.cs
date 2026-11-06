using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DebugClicUI : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        var datos = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        var resultados = new List<RaycastResult>();
        EventSystem.current.RaycastAll(datos, resultados);

        if (resultados.Count == 0) { Debug.Log("DebugClicUI: el clic no toca ninguna UI"); return; }

        Debug.Log("DebugClicUI: el clic toca primero -> " + resultados[0].gameObject.name, resultados[0].gameObject);
        for (int i = 1; i < resultados.Count; i++)
            Debug.Log("   debajo: " + resultados[i].gameObject.name);
    }
}