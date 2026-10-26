using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// Menú "Serene Lake": monta la escena de RA con el menú y prepara el build Android.
public static class SLConstruir
{
    const string Root = "Assets/SereneLake";
    const string ScenePath = "Assets/Scenes/MenuAR.unity";
    const string MarkerPath = Root + "/Marcador/marcador_serenelake.jpg";
    static readonly Vector2 Ref = new Vector2(1920, 1080);

    // Posición de cada botón en el diseño de Canva (px, origen arriba-izquierda)
    static readonly (string nombre, string sprite, string metodo, int top)[] Botones =
    {
        ("Boton_Minijuego",   "btn_minijuego", "Minijuego",    437),
        ("Boton_VerBarco3D",  "btn_visor3d",   "VisorBarco3D", 562),
        ("Boton_PilotarBarco","btn_pilotar",   "PilotarBarco", 687),
        ("Boton_VerTrailer",  "btn_trailer",   "VerTrailer",   812),
    };
    const int BtnLeft = 697, BtnW = 507, BtnH = 114;

    // ---------------------------------------------------------------- Android
    [MenuItem("Serene Lake/1. Configurar build Android")]
    public static void ConfigurarAndroid()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "HugoBeldaRevert";
        PlayerSettings.productName = "Serene Lake Fishing";
        PlayerSettings.SetApplicationIdentifier(android, "com.hugobeldarevert.serenelakefishing");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        // Solo horizontal
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        AssetDatabase.SaveAssets();
        Debug.Log("[SereneLake] Build Android configurado");
    }

    // ---------------------------------------------------------------- Escena
    [MenuItem("Serene Lake/2. Crear escena AR + menú")]
    public static void CrearEscena()
    {
        PrepararTexturas();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = GameObject.Find("Main Camera");
        if (cam != null) UnityEngine.Object.DestroyImmediate(cam);

        // --- Vuforia: AR Camera (el Image Target se crea en ejecución, ver SLMarcadorRuntime)
        EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera");
        // --- UI
        EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");

        var canvasGO = new GameObject("Canvas_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Ref;
        scaler.matchWidthOrHeight = 1f;

        var escanear = CrearPanelEscanear(canvasGO.transform);
        var menu = CrearPanelMenu(canvasGO.transform, out var botones);

        // --- Controlador
        var ctrlGO = new GameObject("MenuAR_Controlador");
        var ctrl = ctrlGO.AddComponent<SLMenuAR>();
        ctrl.panelEscanear = escanear;
        ctrl.panelMenu = menu;
        var acciones = ctrlGO.AddComponent<SLBotonesMenu>();
        var marc = ctrlGO.AddComponent<SLMarcadorRuntime>();
        marc.imagenMarcador = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
        marc.anchoMetros = 0.2f;
        marc.menu = ctrl;

        // Botones -> métodos (de momento solo escriben en consola)
        foreach (var (go, metodo) in botones)
        {
            var mi = typeof(SLBotonesMenu).GetMethod(metodo);
            var action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), acciones, mi);
            UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, action);
        }

        escanear.SetActive(true);
        menu.SetActive(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = ctrlGO;
        Debug.Log("[SereneLake] Escena creada en " + ScenePath);
    }

    static void PrepararTexturas()
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(MarkerPath);
        imp.isReadable = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 2048;
        foreach (var plat in new[] { "Android", "iPhone", "Standalone" })
            imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            { name = plat, overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.RGB24 });
        imp.SaveAndReimport();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/UI" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
    }

    static Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/UI/" + n + ".png");

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    static Text Texto(Transform parent, string name, string txt, int size, Color c)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = txt; t.fontSize = size; t.color = c; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        return t;
    }

    // Aviso que se ve sobre la cámara mientras se busca el marcador
    static GameObject CrearPanelEscanear(Transform parent)
    {
        var panel = Rect("Panel_Escanear", parent); Stretch(panel);

        // Marco de enfoque (4 esquinas)
        var marco = Rect("Marco", panel);
        marco.anchorMin = marco.anchorMax = new Vector2(0.5f, 0.55f);
        marco.sizeDelta = new Vector2(560, 560);
        var crema = new Color32(255, 244, 224, 255);
        foreach (var (ax, ay) in new[] { (0f, 0f), (1f, 0f), (0f, 1f), (1f, 1f) })
        {
            foreach (bool horizontal in new[] { true, false })
            {
                var l = Rect("Esquina", marco);
                l.anchorMin = l.anchorMax = l.pivot = new Vector2(ax, ay);
                l.sizeDelta = horizontal ? new Vector2(110, 14) : new Vector2(14, 110);
                l.gameObject.AddComponent<Image>().color = crema;
            }
        }

        // Banda inferior con el texto
        var banda = Rect("Banda", panel);
        banda.anchorMin = new Vector2(0, 0); banda.anchorMax = new Vector2(1, 0);
        banda.pivot = new Vector2(0.5f, 0); banda.sizeDelta = new Vector2(0, 150);
        banda.gameObject.AddComponent<Image>().color = new Color32(15, 58, 82, 200);
        var t = Texto(banda, "Texto", "Apunta la cámara al marcador para empezar", 52, crema);
        Stretch((RectTransform)t.transform);
        return panel.gameObject;
    }

    // Menú principal: fondo del diseño de Canva + 4 botones
    static GameObject CrearPanelMenu(Transform parent, out (GameObject, string)[] botones)
    {
        var panel = Rect("Panel_Menu", parent); Stretch(panel);

        // Relleno para pantallas más anchas que 16:9 (fondo ampliado y oscurecido)
        var relleno = Rect("Fondo_Relleno", panel);
        relleno.anchorMin = relleno.anchorMax = new Vector2(0.5f, 0.5f); relleno.sizeDelta = Ref;
        var ri = relleno.gameObject.AddComponent<Image>(); ri.sprite = Spr("fondo_menu"); ri.color = new Color(0.75f, 0.75f, 0.75f);
        var rf = relleno.gameObject.AddComponent<AspectRatioFitter>();
        rf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; rf.aspectRatio = Ref.x / Ref.y;

        // Fondo 16:9 completo (sin recortar el logo)
        var fondo = Rect("Fondo", panel);
        fondo.anchorMin = fondo.anchorMax = new Vector2(0.5f, 0.5f); fondo.sizeDelta = Ref;
        fondo.gameObject.AddComponent<Image>().sprite = Spr("fondo_menu");
        var ff = fondo.gameObject.AddComponent<AspectRatioFitter>();
        ff.aspectMode = AspectRatioFitter.AspectMode.FitInParent; ff.aspectRatio = Ref.x / Ref.y;

        botones = new (GameObject, string)[Botones.Length];
        for (int i = 0; i < Botones.Length; i++)
        {
            var b = Botones[i];
            var rt = Rect(b.nombre, fondo);
            // anclas en proporción del diseño -> escalan con el fondo
            rt.anchorMin = new Vector2(BtnLeft / Ref.x, 1f - (b.top + BtnH) / Ref.y);
            rt.anchorMax = new Vector2((BtnLeft + BtnW) / Ref.x, 1f - b.top / Ref.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Spr(b.sprite);
            var btn = rt.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.highlightedColor = new Color(1f, 0.97f, 0.9f);
            cb.pressedColor = new Color(0.78f, 0.74f, 0.7f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            rt.gameObject.AddComponent<SLBotonPulsar>();
            botones[i] = (rt.gameObject, b.metodo);
        }
        return panel.gameObject;
    }

    // ---------------------------------------------------------------- Build
    static string BuildDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));
    static string Apk => Path.Combine(BuildDir, "SereneLakeFishing.apk");

    [MenuItem("Serene Lake/3. Compilar APK")]
    public static void Compilar()
    {
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(BuildDir);
        EditorUserBuildSettings.buildAppBundle = false;
        var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = Apk,
            target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android
        });
        var s = report.summary;
        string msg = "Build " + s.result + " | " + s.totalErrors + " errores | " + (s.totalSize / 1048576f).ToString("0.0") + " MB | " + Apk;
        File.WriteAllText(Path.Combine(BuildDir, "build_log.txt"), msg + "\n" + string.Join("\n",
            report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content)));
        Debug.Log("[SereneLake] " + msg);
    }

    // ---------------------------------------------------------------- Móvil (USB)
    static string Adb(string args)
    {
        var p = new System.Diagnostics.Process();
        p.StartInfo.FileName = Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath, "platform-tools", "adb");
        p.StartInfo.Arguments = args; p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = p.StartInfo.RedirectStandardError = true;
        p.Start();
        string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(300000);
        return "$ adb " + args + "\n" + o + "\n";
    }

    [MenuItem("Serene Lake/4. Instalar APK en el móvil (USB)")]
    public static void Instalar()
    {
        Directory.CreateDirectory(BuildDir);
        string log = Adb("devices -l") + Adb("install -r \"" + Apk + "\"");
        if (log.Contains("Success"))
            log += Adb("shell monkey -p com.hugobeldarevert.serenelakefishing -c android.intent.category.LAUNCHER 1");
        File.WriteAllText(Path.Combine(BuildDir, "adb_log.txt"), log);
        Debug.Log("[SereneLake] " + log);
    }

    [MenuItem("Serene Lake/5. Captura de pantalla del móvil (USB)")]
    public static void Captura()
    {
        Directory.CreateDirectory(BuildDir);
        string log = Adb("shell screencap -p /sdcard/sl_captura.png") + Adb("pull /sdcard/sl_captura.png \"" + Path.Combine(BuildDir, "captura_movil.png") + "\"");
        File.WriteAllText(Path.Combine(BuildDir, "adb_log.txt"), log);
        Debug.Log("[SereneLake] " + log);
    }

    [MenuItem("Serene Lake/6. Guardar log del móvil (USB)")]
    public static void LogMovil()
    {
        Directory.CreateDirectory(BuildDir);
        File.WriteAllText(Path.Combine(BuildDir, "logcat.txt"), Adb("logcat -d"));
        Debug.Log("[SereneLake] logcat guardado");
    }
}
