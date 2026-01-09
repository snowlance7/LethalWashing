using Dusk;

namespace LethalWashing
{
    public class LethalWashingContentHandler : ContentHandler<LethalWashingContentHandler>
    {
        public class WashingMachineAssets(DuskMod mod, string filePath) : AssetBundleLoader<WashingMachineAssets>(mod, filePath) { }


        public WashingMachineAssets? WashingMachine;

        public LethalWashingContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("washingmachine", out WashingMachine, true);
        }
    }
}
/*public class DuckSongAssets(DuskMod mod, string filePath) : AssetBundleLoader<DuckSongAssets>(mod, filePath)
{
    [LoadFromBundle("DuckHolder.prefab")]
    public GameObject DuckUIPrefab { get; private set; } = null!;
}*/