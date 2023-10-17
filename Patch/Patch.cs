using PieceManager;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace SilenceInCity.Patch;

[HarmonyPatch] public class Patch
{
    private static bool done = false;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] [HarmonyPostfix] [HarmonyWrapSafe]
    private static void Init(ZNetScene __instance)
    {
        if (done) return;
        if (SceneManager.GetActiveScene().name != "main") return;
        if (!ZNet.instance.IsServer()) return;
        var parent = new GameObject("SilenceInCity_Parent").transform;
        parent.gameObject.SetActive(false);
        DontDestroyOnLoad(parent);

        done = true;
        var guard_stone = __instance.GetPrefab("guard_stone").GetComponent<Piece>();
        var projectorOrig = guard_stone?.transform?.FindChildByName("AreaMarker")?.GetComponent<CircleProjector>();
        var newPrefab = Instantiate(projectorOrig, Vector3.zero, Quaternion.identity, parent);
        newPrefab.name = "SilenceInCityPiece";
        var piece = newPrefab.gameObject.AddComponent<Piece>();
        var zNetView = newPrefab.gameObject.AddComponent<ZNetView>();
        zNetView.m_persistent = true;
        zNetView.m_type = ZDO.ObjectType.Solid;
        piece.enabled = true;
        piece.m_isUpgrade = false;
        piece.m_comfort = 0;
        piece.m_groundPiece = true;
        piece.m_allowAltGroundPlacement = true;
        piece.m_placeEffect = guard_stone.m_placeEffect;
        LoadImageFromWEB(
            $@"https://regnum.ru/uploads/pictures/news/2017/02/05/regnum_picture_148624788092153_normal.png",
            sprite => piece.m_icon = sprite);

        var buildPiece = new BuildPiece(newPrefab.gameObject);
        buildPiece.Name
            .Russian("Text")
            .English("Text");
        buildPiece.Description
            .Russian("Text_desc")
            .English("Text_desc");
        buildPiece.Category.Set(BuildPieceCategory.All);
        buildPiece.SpecialProperties.AdminOnly = true;
        buildPiece.SpecialProperties.NoConfig = true;
        buildPiece.activeTools = new[] { "Hammer" };
    }

    public static void LoadImageFromWEB(string url, Action<Sprite> callback)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _)) return;

        GetPlugin().StartCoroutine(_Internal_LoadImage(url, callback));
    }

    private static IEnumerator _Internal_LoadImage(string url, Action<Sprite> callback)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.result is UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture.width == 0 || texture.height == 0) yield break;
            Texture2D temp = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            temp.SetPixels(texture.GetPixels());
            temp.Apply();
            var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f));
            sprite.name = url.Split('/').Last();
            callback?.Invoke(sprite);
        }
    }
}