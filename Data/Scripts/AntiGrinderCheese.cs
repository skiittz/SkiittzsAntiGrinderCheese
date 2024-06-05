using System.Linq;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace SkiittzsAntiGrinderCheese
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
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(99, DamageHandler);
            }
        }

        private void DamageHandler(object target, ref MyDamageInformation DamageInfo)
        {
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

            DamageInfo.Amount = 0;
        }
    }
}