using UnityEditor;
using UnityEngine;

public static class SLVistaPrevia
{
    // Alterna en el editor entre el aviso de escaneo y el menú, para poder verlo sin marcador
    [MenuItem("Serene Lake/Vista previa: alternar menú ↔ escaneo")]
    public static void Alternar()
    {
        var ctrl = Object.FindFirstObjectByType<SLMenuAR>();
        if (ctrl == null || ctrl.panelMenu == null) return;
        bool abrir = !ctrl.panelMenu.activeSelf;
        ctrl.panelMenu.SetActive(abrir);
        ctrl.panelEscanear.SetActive(!abrir);
        // Se deja marcado como modificado: guarda la escena con el aviso de escaneo visible
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);
    }
}
