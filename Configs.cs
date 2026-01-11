using Dusk;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalWashing
{
    public static class Configs
    {
        // Washing Machine
        public static string Blacklist => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<string>("Washing Machine | Blacklist").Value;
        public static bool PreventDespawnOnTeamWipe => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Washing Machine | Prevent Despawn On Team Wipe").Value;
        public static string TeamWipeBlacklist => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<string>("Washing Machine | Team Wipe Blacklist").Value;
        public static float WashTime => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<float>("Washing Machine | Wash Time").Value;
        public static bool UsableOnlyOnCompanyDay => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Washing Machine | Usable Only On Company Day").Value;
        public static bool CombineCoinValues => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Washing Machine | Combine Coin Values").Value;
        public static int CoinsToSpawn => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<int>("Washing Machine | Coins To Spawn").Value;

        // Coin
        public static float ThrowDistance => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<float>("Coin | Throw Distance").Value;
    }
}
