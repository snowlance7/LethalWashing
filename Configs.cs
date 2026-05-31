using Dusk;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalWashing
{
    public static class Configs
    {
        // Washing Machine
        public static string Blacklist => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<string>("Blacklist").Value;
        public static bool PreventDespawnOnTeamWipe => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Prevent Despawn On Team Wipe").Value;
        public static string TeamWipeBlacklist => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<string>("Team Wipe Blacklist").Value;
        public static float WashTime => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<float>("Wash Time").Value;
        public static bool UsableOnlyOnCompanyDay => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Usable Only On Company Day").Value;
        public static bool CombineCoinValues => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Combine Coin Values").Value;
        public static int CoinsToSpawn => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<int>("Coins To Spawn").Value;
    }
}
