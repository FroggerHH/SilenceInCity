using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace SilenceInCity.Patch;

[HarmonyPatch] public class CreatePiece
{
    private static Piece _piece;
    private static bool done;

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
        var wood_floor_1x1 = __instance.GetPrefab("wood_floor_1x1").GetComponent<Piece>();
        var projectorOrig = guard_stone?.transform?.FindChildByName("AreaMarker")?.GetComponent<CircleProjector>();
        var newPrefab = Instantiate(projectorOrig, Vector3.zero, Quaternion.identity, parent);
        newPrefab.name = "SilenceInCityPiece";
        var silenceMono = newPrefab.gameObject.AddComponent<SilenceMono>();
        silenceMono.m_projector = newPrefab;
        var piece = newPrefab.gameObject.AddComponent<Piece>();
        var wearNTear = newPrefab.gameObject.AddComponent<WearNTear>();
        var zNetView = newPrefab.gameObject.AddComponent<ZNetView>();
        zNetView.m_syncInitialScale = false;
        zNetView.m_ghost = false;
        zNetView.m_distant = false;
        zNetView.m_persistent = true;
        zNetView.m_type = ZDO.ObjectType.Solid;
        piece.enabled = true;
        piece.m_canBeRemoved = true;
        piece.m_isUpgrade = false;
        piece.m_comfort = 0;
        piece.m_groundPiece = true;
        piece.m_allowAltGroundPlacement = true;
        piece.m_placeEffect = guard_stone.m_placeEffect;
        piece.m_category = Piece.PieceCategory.Misc;
        piece.m_resources = new[]
        {
            new Piece.Requirement()
            {
                m_amount = 1,
                m_recover = false,
                m_resItem = ZNetScene.instance.GetPrefab("SwordCheat").GetComponent<ItemDrop>()
            }
        };
        piece.m_icon = Sprite.Create(new(128, 128), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        wearNTear.m_health = 1000;
        wearNTear.m_damages.m_blunt = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_slash = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_pierce = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_chop = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_pickaxe = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_fire = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_frost = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_lightning = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_poison = HitData.DamageModifier.Ignore;
        wearNTear.m_damages.m_spirit = HitData.DamageModifier.Ignore;
        wearNTear.m_destroyedEffect = wood_floor_1x1.GetComponent<WearNTear>().m_destroyedEffect;

        Instantiate(wood_floor_1x1.transform.FindChildByName("collider"), Vector3.zero, Quaternion.identity,
            newPrefab.transform);
        Instantiate(wood_floor_1x1.transform.FindChildByName("New"), Vector3.zero, Quaternion.identity,
            newPrefab.transform);
        LoadImageFromWEB(
            @"https://regnum.ru/uploads/pictures/news/2017/02/05/regnum_picture_148624788092153_normal.png",
            sprite => piece.m_icon = sprite);

        _piece = piece;
    }

    public static void LoadImageFromWEB(string url, Action<Sprite> callback)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _)) return;

        GetPlugin().StartCoroutine(_Internal_LoadImage(url, callback));
    }

    private static IEnumerator _Internal_LoadImage(string url, Action<Sprite> callback)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.result is UnityWebRequest.Result.Success)
        {
            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture.width == 0 || texture.height == 0) yield break;
            var temp = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            temp.SetPixels(texture.GetPixels());
            temp.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = url.Split('/').Last();
            callback?.Invoke(sprite);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))] [HarmonyWrapSafe]
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    [HarmonyPostfix]
    private static void PieceManager_Patch_ObjectDBInit(ObjectDB __instance)
    {
        var pieceTable = __instance?.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>()?.m_itemData.m_shared
            .m_buildPieces;
        if (pieceTable == null || _piece == null) return;
        if (!pieceTable.m_pieces.Contains(_piece.gameObject))
            pieceTable.m_pieces.Add(_piece.gameObject);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] [HarmonyWrapSafe] [HarmonyPostfix]
    private static void Patch_ZNetSceneAwake(ZNetScene __instance)
    {
        if (!__instance.m_prefabs.Contains(_piece.gameObject))
            __instance.m_prefabs.Add(_piece.gameObject);
        var hashCode = _piece.name.GetStableHashCode();
        if (!__instance.m_namedPrefabs.ContainsKey(hashCode))
            __instance.m_namedPrefabs.Add(hashCode, _piece.gameObject);
    }
}