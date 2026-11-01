using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Actualiza la escena MenuAR al diseño nuevo de Canva:
///  - el menú se abre directamente (se quita el escaneo del marcador para el menú)
///  - 4 botones: Ver barco 3D, Pilotar barco, Ver tráiler, Instrucciones (Minijuego quitado)
///  - botón Salir (píldora "× SALIR" arriba a la derecha, como en Canva)
///  - pantalla de Instrucciones con botón Volver
/// No toca el tráiler ni el resto de la escena.
public static class SLMenuInstrucciones
{
    const string ScenePath = "Assets/AR/ScenesAR/MiniJuegoScenes/MenuAR.unity";
    const string UI = "Assets/AR/SereneLake/UI/";
    static readonly Vector2 Ref = new Vector2(1920, 1080);

    // Rectángulos en píxeles del diseño (x, y desde arriba-izquierda, ancho, alto)
    static readonly (string nombre, string sprite, string metodo, Rect r)[] BotonesMenu =
    {
        ("Boton_VerBarco3D",    "btn_visor3d",       "VisorBarco3D",       new Rect(697, 434, 507, 114)),
        ("Boton_PilotarBarco",  "btn_pilotar",       "PilotarBarco",       new Rect(697, 549, 507, 114)),
        ("Boton_VerTrailer",    "btn_trailer",       "VerTrailer",         new Rect(697, 664, 507, 114)),
        ("Boton_Instrucciones", "btn_instrucciones", "AbrirInstrucciones", new Rect(697, 779, 507, 114)),
        ("Boton_Salir",         "btn_salir",         "Salir",              new Rect(1620, 40, 260, 109)),
    };
    static readonly Rect RectVolver = new Rect(747, 972, 427, 104);

    [MenuItem("Serene Lake/Actualizar menú: sin escaneo + Instrucciones + Salir")]
    public static void Aplicar()
    {
        PrepararSprites();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var menuAR = UnityEngine.Object.FindFirstObjectByType<SLMenuAR>(FindObjectsInactive.Include);
        var acciones = UnityEngine.Object.FindFirstObjectByType<SLBotonesMenu>(FindObjectsInactive.Include);
        if (menuAR == null || acciones == null || menuAR.panelMenu == null)
        { Debug.LogError("[SereneLake] No encuentro SLMenuAR / SLBotonesMenu / Panel_Menu"); return; }

        // 1) Menú sin escaneo: el marcador ya no abre el menú
        menuAR.abrirMenuAlIniciar = true;
        foreach (var m in menuAR.GetComponents<SLMarcadorRuntime>()) UnityEngine.Object.DestroyImmediate(m, true);
        if (menuAR.panelEscanear != null) { UnityEngine.Object.DestroyImmediate(menuAR.panelEscanear); menuAR.panelEscanear = null; }
        EditorUtility.SetDirty(menuAR);

        // 2) Botones del menú (se reutilizan los que existen y se crean los que faltan)
        var panelMenu = menuAR.panelMenu.transform;
        var fondoMenu = panelMenu.Find("Fondo");
        foreach (var b in BotonesMenu)
        {
            var t = fondoMenu.Find(b.nombre);
            var go = t != null ? t.gameObject : CrearBoton(b.nombre, fondoMenu);
            Colocar((RectTransform)go.transform, b.r);
            go.GetComponent<Image>().sprite = Spr(b.sprite);
            Conectar(go.GetComponent<Button>(), acciones, b.metodo);
        }
        // Minijuego ya no está en el menú
        var mini = fondoMenu.Find("Boton_Minijuego");
        if (mini != null) UnityEngine.Object.DestroyImmediate(mini.gameObject);
        // Fondo nuevo (sin botones) en el relleno y en el fondo
        foreach (var n in new[] { "Fondo", "Fondo_Relleno" })
        { var f = panelMenu.Find(n); if (f != null) f.GetComponent<Image>().sprite = Spr("fondo_menu"); }

        // 3) Pantalla de instrucciones (copia de la estructura del menú)
        var viejo = panelMenu.parent.Find("Panel_Instrucciones");
        if (viejo != null) UnityEngine.Object.DestroyImmediate(viejo.gameObject);
        var panelInstr = CrearPanelFondo("Panel_Instrucciones", panelMenu.parent, "fondo_instrucciones", out var fondoInstr);
        panelInstr.transform.SetSiblingIndex(panelMenu.GetSiblingIndex() + 1);
        var volver = CrearBoton("Boton_Volver", fondoInstr);
        Colocar((RectTransform)volver.transform, RectVolver);
        volver.GetComponent<Image>().sprite = Spr("btn_volver");
        Conectar(volver.GetComponent<Button>(), acciones, "CerrarInstrucciones");
        panelInstr.SetActive(false);

        acciones.panelMenu = menuAR.panelMenu;
        acciones.panelInstrucciones = panelInstr;
        EditorUtility.SetDirty(acciones);

        menuAR.panelMenu.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SereneLake] MenuAR actualizado");
    }

    // ------------------------------------------------------------------
    static Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(UI + n + ".png");

    static void PrepararSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UI.TrimEnd('/') }))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (ti.textureType == TextureImporterType.Sprite && ti.alphaIsTransparency) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
    }

    static void Colocar(RectTransform rt, Rect r)
    {
        rt.anchorMin = new Vector2(r.x / Ref.x, 1f - (r.y + r.height) / Ref.y);
        rt.anchorMax = new Vector2((r.x + r.width) / Ref.x, 1f - r.y / Ref.y);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static GameObject CrearBoton(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        var btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1f, 0.97f, 0.9f);
        cb.pressedColor = new Color(0.78f, 0.74f, 0.7f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        go.AddComponent<SLBotonPulsar>();
        return go;
    }

    static void Conectar(Button b, SLBotonesMenu acciones, string metodo)
    {
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        var mi = typeof(SLBotonesMenu).GetMethod(metodo);
        UnityEventTools.AddPersistentListener(b.onClick, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), acciones, mi));
        EditorUtility.SetDirty(b);
    }

    static GameObject CrearPanelFondo(string nombre, Transform padre, string sprite, out Transform fondo)
    {
        var panel = new GameObject(nombre, typeof(RectTransform));
        panel.transform.SetParent(padre, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;

        var relleno = new GameObject("Fondo_Relleno", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        relleno.transform.SetParent(panel.transform, false);
        ((RectTransform)relleno.transform).sizeDelta = Ref;
        relleno.GetComponent<Image>().sprite = Spr(sprite);
        relleno.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f);
        var rf = relleno.GetComponent<AspectRatioFitter>();
        rf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; rf.aspectRatio = Ref.x / Ref.y;

        var f = new GameObject("Fondo", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        f.transform.SetParent(panel.transform, false);
        ((RectTransform)f.transform).sizeDelta = Ref;
        f.GetComponent<Image>().sprite = Spr(sprite);
        var ff = f.GetComponent<AspectRatioFitter>();
        ff.aspectMode = AspectRatioFitter.AspectMode.FitInParent; ff.aspectRatio = Ref.x / Ref.y;
        fondo = f.transform;
        return panel;
    }
    // ------------------------------------------------------------------ Build + móvil
    static string BuildDir => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Builds"));
    static string Apk => System.IO.Path.Combine(BuildDir, "FishingSimVR_AR.apk");

    /// Compila con las escenas activas de Build Settings (menú, pilotar barco, visor 3D...)
    [MenuItem("Serene Lake/Compilar APK (escenas de Build Settings)")]
    public static void Compilar()
    {
        System.IO.Directory.CreateDirectory(BuildDir);
        var escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        EditorUserBuildSettings.buildAppBundle = false;
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        { scenes = escenas, locationPathName = Apk, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android });
        var s = r.summary;
        string msg = "Build " + s.result + " | " + s.totalErrors + " errores | " + string.Join(", ", escenas) + "\n" + string.Join("\n",
            r.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content));
        System.IO.File.WriteAllText(System.IO.Path.Combine(BuildDir, "build_log.txt"), msg);
        Debug.Log("[SereneLake] " + msg);
    }

    static string Adb(string args)
    {
        var p = new System.Diagnostics.Process();
        p.StartInfo.FileName = System.IO.Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath, "platform-tools", "adb");
        p.StartInfo.Arguments = args; p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = p.StartInfo.RedirectStandardError = true;
        p.Start();
        string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(300000);
        return "$ adb " + args + "\n" + o + "\n";
    }

    [MenuItem("Serene Lake/Instalar y abrir en el móvil (USB)")]
    public static void Instalar()
    {
        System.IO.Directory.CreateDirectory(BuildDir);
        string id = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        string log = Adb("devices -l") + Adb("install -r \"" + Apk + "\"");
        if (log.Contains("Success")) log += Adb("shell monkey -p " + id + " -c android.intent.category.LAUNCHER 1");
        System.IO.File.WriteAllText(System.IO.Path.Combine(BuildDir, "adb_log.txt"), log);
        Debug.Log("[SereneLake] " + log);
    }
}
