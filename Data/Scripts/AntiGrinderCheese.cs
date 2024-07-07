using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace SkiittzsAntiGrinderCheese.Data.Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AntiGrinderCheese : MySessionComponentBase
    {
        public bool initialized = false;

        public override void UpdateAfterSimulation()
        {
            if (!initialized)
            {
                initialized = true;
                Configuration.Load();
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(99, DamageHandler);
            }
        }

        private void DamageHandler(object target, ref MyDamageInformation DamageInfo)
        {
            if (target == null) return;
            if (DamageInfo.Type != MyStringHash.GetOrCompute("Grind")
                && DamageInfo.Type != MyStringHash.GetOrCompute("Drill")) return;

            var block = target as IMySlimBlock;
            if (block == null) return;

            var fatBlock = block.FatBlock;
            if (fatBlock is IMyTerminalBlock) return;

            var attackerEntity = DamageInfo.AttackerId != 0 ? MyAPIGateway.Entities.GetEntityById(DamageInfo.AttackerId) : null;
            if (attackerEntity == null) return;

            if (!(attackerEntity is IMyAngleGrinder || attackerEntity is IMyHandDrill)) return;
            
            var grinder = (attackerEntity as IMyAngleGrinder);
            var drill = (attackerEntity as IMyHandDrill);
            var attackingEntityId = grinder?.OwnerIdentityId ?? drill?.OwnerIdentityId;
            if (attackingEntityId == null) return;

            var owners = block.CubeGrid.BigOwners;
            if (!owners.Any() || owners.Contains(attackingEntityId.Value)) return;

            var factions = MyAPIGateway.Session.Factions;
            var targetOwner = block.CubeGrid.BigOwners[0];
            var fac1 = factions.TryGetPlayerFaction(targetOwner);
            var fac2 = factions.TryGetPlayerFaction(attackingEntityId.Value);

            if (fac1 != null && fac1.IsEveryoneNpc() && Configuration.Settings.IgnoreNpcGrids)
                return;

            if (fac1 != null && fac2 != null)
            {
                var Relationship = MyAPIGateway.Session.Factions.GetRelationBetweenFactions(fac1.FactionId, fac2.FactionId);
                if (Relationship != MyRelationsBetweenFactions.Enemies) return;
            }

            DamageInfo.Amount = 0;
        }
    }

    public static class Configuration
    {
        private static bool _isLoaded;
        public static ModSettings Settings = ModSettings.Default;
        private const string FileName = "Settings_V1.2.xml";

        public static void Load()
        {
            if (_isLoaded) return;
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(AntiGrinderCheese)))
            {
                var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(AntiGrinderCheese));
                var content = reader.ReadToEnd();
                reader.Close();

                try
                {
                    Settings = MyAPIGateway.Utilities.SerializeFromXML<ModSettings>(content);
                }
                catch
                {
                    Settings = ModSettings.Default;
                }
            }
            else
            {
                Settings = ModSettings.Default;
                Save();
            }

            _isLoaded = true;
        }

        public static void Save()
        {
            var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(AntiGrinderCheese));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Settings));
            writer.Flush();
            writer.Close();
        }
    }

    public class ModSettings
    {
        public bool IgnoreNpcGrids { get; set; }

        public static ModSettings Default => new ModSettings
        {
            IgnoreNpcGrids = true
        };
    }
}