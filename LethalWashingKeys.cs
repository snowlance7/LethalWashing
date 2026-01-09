using Dawn;

namespace LethalWashing
{
    public static class LethalWashingKeys
    {
        public const string Namespace = "lethalwashing";

        internal static NamespacedKey LastVersion = NamespacedKey.From(Namespace, "last_version");

        public static readonly NamespacedKey<DawnUnlockableItemInfo> WashingMachine = NamespacedKey<DawnUnlockableItemInfo>.From("lethalwashing", "washingmachine");
        public static readonly NamespacedKey<DawnItemInfo> Coin = NamespacedKey<DawnItemInfo>.From("lethalwashing", "coin");
    }
}
