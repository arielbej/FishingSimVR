using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class SLDescargarCanva
{
    // URL de exportación PNG del diseño de Canva (caduca en unas horas)
    const string Url = "https://export-download.canva.com/4w_TU/DAHY6y4w_TU/-1/0/0001-3142906619978804524.png?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAQYCGKMUH5AO7UJ26%2F20261024%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20261024T043234Z&X-Amz-Expires=29797&X-Amz-Signature=78cb8384d333db087921f9d87418a39124b93cbaa77148fb1311cc3e3e914b10&X-Amz-SignedHeaders=host%3Bx-amz-expected-bucket-owner&response-expires=Sat%2C%2024%20Oct%202026%2012%3A49%3A11%20GMT";

    [MenuItem("Serene Lake/0. Descargar diseño de Canva")]
    public static void Descargar()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../_canva"));
        Directory.CreateDirectory(dir);
        using (var req = UnityWebRequest.Get(Url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) System.Threading.Thread.Sleep(50);
            string log = req.result + " " + req.responseCode + " " + req.downloadedBytes + " bytes";
            if (req.result == UnityWebRequest.Result.Success)
                File.WriteAllBytes(Path.Combine(dir, "menu_final.png"), req.downloadHandler.data);
            File.WriteAllText(Path.Combine(dir, "log.txt"), log);
            Debug.Log("[SereneLake] Descarga Canva: " + log);
        }
    }

    const string UrlMarcador = "https://export-download.canva.com/LLp3A/DAHY7ELLp3A/-1/0/0001-161523673484841633.jpg?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAQYCGKMUH5AO7UJ26%2F20261023%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20261023T221842Z&X-Amz-Expires=56958&X-Amz-Signature=fa8eb8626e3cdecd7bedaa0a2e68f36a6d7730f8ddfc7151c2ab6e55f1287faf&X-Amz-SignedHeaders=host%3Bx-amz-expected-bucket-owner&response-expires=Sat%2C%2024%20Oct%202026%2014%3A08%3A00%20GMT";

    [MenuItem("Serene Lake/0b. Descargar marcador de Canva")]
    public static void DescargarMarcador()
    {
        using (var req = UnityWebRequest.Get(UrlMarcador))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) System.Threading.Thread.Sleep(50);
            string log = req.result + " " + req.responseCode + " " + req.downloadedBytes + " bytes";
            if (req.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(Path.Combine(Application.dataPath, "SereneLake/Marcador/marcador_serenelake.jpg"), req.downloadHandler.data);
                UnityEditor.AssetDatabase.Refresh();
            }
            Debug.Log("[SereneLake] Descarga marcador: " + log);
        }
    }
}
