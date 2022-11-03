using Pidgin;
using Robust.Shared.Utility;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.Power.Pow3r
{
    public sealed class BatteryRampPegSolver : IPowerSolver
    {
        private sealed class HeightComparer : Comparer<Network>
        {
            public static HeightComparer Instance { get; } = new();

            public override int Compare(Network? x, Network? y)
            {
                if (x!.Height == y!.Height) return 0;
                if (x!.Height > y!.Height) return 1;
                return -1;
            }
        }

        public void Tick(float frameTime, PowerState state, int parallel)
        {
            ClearLoadsAndSupplies(state);

            state.GroupedNets ??= GroupByNetworkDepth(state);
            DebugTools.Assert(state.GroupedNets.Select(x => x.Count).Sum() == state.Networks.Count);

            // Each network height layer can be run in parallel without issues.
            var opts = new ParallelOptions { MaxDegreeOfParallelism = parallel };
            foreach (var group in state.GroupedNets)
            {
                // Note that many net-layers only have a handful of networks.
                // E.g., the number of nets from lowest to heights for box and saltern are:
                // Saltern: 1477, 11, 2, 2, 3.
                // Box:     3308, 20, 1, 5.
                //
                // I have NFI what the overhead for a Parallel.ForEach is, and how it compares to computing differently
                // sized nets. Basic benchmarking shows that this is better, but maybe the highest-tier nets should just
                // be run sequentially? But then again, maybe they are 2-3 very BIG networks at the top? So maybe:
                //
                // TODO make GroupByNetworkDepth evaluate the TOTAL size of each layer (i.e. loads + chargers +
                // suppliers + discharger) Then decide based on total layer size whether its worth parallelizing that
                // layer?
                Parallel.ForEach(group, opts, net => UpdateNetwork(net, state, frameTime));
            }

            ClearBatteries(state);

            PowerSolverShared.UpdateRampPositions(frameTime, state);
        }

        private void ClearLoadsAndSupplies(PowerState state)
        {
            foreach (var load in state.Loads.Values)
            {
                if (load.Paused)
                    continue;

                load.ReceivingPower = 0;
            }

            foreach (var supply in state.Supplies.Values)
            {
                if (supply.Paused)
                    continue;

                supply.CurrentSupply = 0;
                supply.SupplyRampTarget = 0;
            }
        }

        private void UpdateNetwork(Network network, PowerState state, float frameTime)
        {
            // TODO Look at SIMD.
            // a lot of this is performing very basic math on arrays of data objects like batteries
            // this really shouldn't be hard to do.
            // except for maybe the paused/enabled guff. If its mostly false, I guess they could just be 0 multipliers?

            // Add up demand from loads.
            var demand = 0f;
            foreach (var loadId in network.Loads)
            {
                var load = state.Loads[loadId];

                if (!load.Enabled || load.Paused)
                    continue;

                DebugTools.Assert(load.DesiredPower >= 0);
                demand += load.DesiredPower;
            }

            // TODO: Consider having battery charge loads be processed "after" pass-through loads.
            // This would mean that charge rate would have no impact on throughput rate like it does currently.
            // Would require a second pass over the network, or something. Not sure.

            // Add demand from batteries
            foreach (var batteryId in network.BatteryLoads)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || !battery.CanCharge || battery.Paused)
                    continue;

                var batterySpace = (battery.Capacity - battery.CurrentStorage) * (1 / battery.Efficiency);
                batterySpace = Math.Max(0, batterySpace);
                var scaledSpace = batterySpace / frameTime;

                var chargeRate = battery.MaxChargeRate + battery.LoadingNetworkDemand / battery.Efficiency;

                battery.DesiredPower = Math.Min(chargeRate, scaledSpace);
                DebugTools.Assert(battery.DesiredPower >= 0);
                demand += battery.DesiredPower;
            }

            DebugTools.Assert(demand >= 0);

            // Add up supply in network.
            var totalSupply = 0f;
            var totalMaxSupply = 0f;
            foreach (var supplyId in network.Supplies)
            {
                var supply = state.Supplies[supplyId];
                if (!supply.Enabled || supply.Paused)
                    continue;

                var rampMax = supply.SupplyRampPosition + supply.SupplyRampTolerance;
                var effectiveSupply = Math.Min(rampMax, supply.MaxSupply);

                DebugTools.Assert(effectiveSupply >= 0);
                DebugTools.Assert(supply.MaxSupply >= 0);

                supply.AvailableSupply = effectiveSupply;
                totalSupply += effectiveSupply;
                totalMaxSupply += supply.MaxSupply;
            }

            var unmet = Math.Max(0, demand - totalSupply);
            DebugTools.Assert(totalSupply >= 0);
            DebugTools.Assert(totalMaxSupply >= 0);

            // Supplying batteries. Batteries need to go after local supplies so that local supplies are prioritized.
            // Also, it makes demand-pulling of batteries. Because all batteries will desire the unmet demand of their
            // loading network, there will be a "rush" of input current when a network powers on, before power
            // stabilizes in the network. This is fine.

            var totalBatterySupply = 0f;
            var totalMaxBatterySupply = 0f;
            if (unmet > 0)
            {
                // determine supply available from batteries
                foreach (var batteryId in network.BatterySupplies)
                {
                    var battery = state.Batteries[batteryId];
                    if (!battery.Enabled || !battery.CanDischarge || battery.Paused)
                        continue;

                    var scaledSpace = battery.CurrentStorage / frameTime;
                    var supplyCap = Math.Min(battery.MaxSupply,
                        battery.SupplyRampPosition + battery.SupplyRampTolerance);
                    var supplyAndPassthrough = supplyCap + battery.CurrentReceiving * battery.Efficiency;

                    battery.AvailableSupply = Math.Min(scaledSpace, supplyAndPassthrough);
                    battery.LoadingNetworkDemand = unmet;

                    totalBatterySupply += battery.AvailableSupply;
                    totalMaxBatterySupply += battery.MaxEffectiveSupply;
                }
            }

            network.LastCombinedSupply = totalSupply + totalBatterySupply;
            network.LastCombinedMaxSupply = totalMaxSupply + totalMaxBatterySupply;

            var met = Math.Min(demand, network.LastCombinedSupply);
            if (met == 0) 
                return;

            var supplyRatio = met / demand;

            // Distribute supply to loads.
            foreach (var loadId in network.Loads)
            {
                var load = state.Loads[loadId];
                if (!load.Enabled || load.DesiredPower == 0 || load.Paused)
                    continue;

                load.ReceivingPower = load.DesiredPower * supplyRatio;
            }

            // Distribute supply to batteries
            foreach (var batteryId in network.BatteryLoads)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || battery.DesiredPower == 0 || battery.Paused)
                    continue;

                battery.LoadingMarked = true;
                battery.CurrentReceiving = battery.DesiredPower * supplyRatio;
                battery.CurrentStorage += frameTime * battery.CurrentReceiving * battery.Efficiency;

                DebugTools.Assert(battery.CurrentStorage <= battery.Capacity || MathHelper.CloseTo(battery.CurrentStorage, battery.Capacity));
            }

            // Target output capacity for supplies
            var metSupply = Math.Min(demand, totalSupply);
            if (metSupply > 0)
            {
                var relativeSupplyOutput = metSupply / totalSupply;
                var targetRelativeSupplyOutput = Math.Min(demand, totalMaxSupply) / totalMaxSupply;

                // Apply load to supplies
                foreach (var supplyId in network.Supplies)
                {
                    var supply = state.Supplies[supplyId];
                    if (!supply.Enabled || supply.Paused)
                        continue;

                    supply.CurrentSupply = supply.AvailableSupply * relativeSupplyOutput;

                    // Supply ramp assumes all supplies ramp at the same rate. If some generators spin up very slowly, in
                    // principle the fast supplies should try over-shoot until they can settle back down. E.g., all supplies
                    // need to reach 50% capacity, but it takes the nuclear reactor 1 hour to reach that, then our lil coal
                    // furnaces should run at 100% for a while. But I guess this is good enough for now.
                    supply.SupplyRampTarget = supply.MaxSupply * targetRelativeSupplyOutput;
                }
            }
            
            if (unmet <= 0 || totalBatterySupply <= 0)
                return;

            // Target output capacity for batteries
            var relativeBatteryOutput = Math.Min(unmet, totalBatterySupply) / totalBatterySupply;

            // Apply load to supplying batteries
            foreach (var batteryId in network.BatterySupplies)
            {
                var battery = state.Batteries[batteryId];
                if (!battery.Enabled || battery.Paused)
                    continue;

                battery.SupplyingMarked = true;
                battery.CurrentSupply = battery.AvailableSupply * relativeBatteryOutput;
                battery.CurrentStorage -= frameTime * battery.CurrentSupply;
                DebugTools.Assert(battery.CurrentStorage >= 0 || MathHelper.CloseTo(battery.CurrentStorage, 0));

                // TODO calculate this properly. Currently this is only initially non-zero if the ramp tolerance is
                // non-zero then the ramp target grows as the ramp position does. Instead, batteries should just try to
                // ramp to satisfy the demand.
                battery.SupplyRampTarget = battery.CurrentSupply - battery.CurrentReceiving * battery.Efficiency;
            }
        }

        private void ClearBatteries(PowerState state)
        {
            // Clear supplying/loading on any batteries that haven't been marked by usage.
            // Because we need this data while processing ramp-pegging, we can't clear it at the start.
            foreach (var battery in state.Batteries.Values)
            {
                if (battery.Paused)
                    continue;

                if (!battery.SupplyingMarked)
                {
                    battery.CurrentSupply = 0;
                    battery.SupplyRampTarget = 0;
                    battery.LoadingNetworkDemand = 0;
                }

                if (!battery.LoadingMarked)
                {
                    battery.CurrentReceiving = 0;
                }

                battery.SupplyingMarked = false;
                battery.LoadingMarked = false;
            }
        }

        private List<List<Network>> GroupByNetworkDepth(PowerState state)
        {
            List<List<Network>> groupedNetworks = new() { new() };
            foreach (var network in state.Networks.Values)
            {
                network.Height = -1;
            }

            foreach (var network in state.Networks.Values)
            {
                if (network.Height == -1)
                    RecursivelyEstimateNetworkDepth(state, network, groupedNetworks);
            }

            return groupedNetworks;
        }

        private static void RecursivelyEstimateNetworkDepth(PowerState state, Network network, List<List<Network>> groupedNetworks)
        {
            network.Height = -2;
            var height = -1;

            foreach (var batteryId in network.BatteryLoads)
            {
                var battery = state.Batteries[batteryId];

                if (battery.LinkedNetworkDischarging == default || battery.LinkedNetworkDischarging == network.Id)
                    continue;

                var subNet = state.Networks[battery.LinkedNetworkDischarging];
                if (subNet.Height == -1)
                    RecursivelyEstimateNetworkDepth(state, subNet, groupedNetworks);
                else if (subNet.Height == -2)
                {
                    // this network is currently computing its own height (we encountered a loop).
                    continue;
                }

                height = Math.Max(subNet.Height, height);
            }

            network.Height = 1 + height;

            if (network.Height >= groupedNetworks.Count)
                groupedNetworks.Add(new() { network });
            else
                groupedNetworks[network.Height].Add(network);
        }
    }
}
