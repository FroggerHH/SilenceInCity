using BepInEx;
using BepInEx.Configuration;

namespace SilenceInCity;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModName = "SilenceInCity",
        ModAuthor = "Frogger",
        ModVersion = "1.0.0",
        ModGUID = $"com.{ModAuthor}.{ModName}";

    internal static ConfigEntry<float> range;


    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGUID);
        range = config("General", "Range", 15f, new ConfigDescription("", new AcceptableValueRange<float>(2, 45)));
        OnConfigurationChanged += () =>
        {
            Debug("Configuration changed");
            var prefab = ZNetScene.instance?.GetPrefab("SilenceInCityPiece");
            if (!prefab) return;
            prefab.GetComponent<CircleProjector>().m_radius = range.Value;
        };
    }
}