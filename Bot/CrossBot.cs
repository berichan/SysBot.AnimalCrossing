using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHSE.Core;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    public sealed class CrossBot : SwitchRoutineExecutor<CrossBotConfig>
    {
        public readonly ConcurrentQueue<ItemRequest> Injections = new();
        public readonly ArrivalLog Arrivals = new();
        public readonly PocketInjectorAsync PocketInjector;
        public bool CleanRequested { private get; set; }
        public string DodoCode { get; set; } = "No code set yet.";
        public uint Offset { get; set; } = (uint)OffsetHelper.InventoryOffset;
        public string LastArrival { get; set; } = string.Empty;

        public readonly DropBotState State;

        public CrossBot(CrossBotConfig cfg) : base(cfg)
        {
            State = new DropBotState(cfg.DropConfig);
            PocketInjector = new PocketInjectorAsync(Connection, Offset);
        }

        public override void SoftStop() => Config.AcceptingCommands = false;

        protected override async Task MainLoop(CancellationToken token)
        {
            // Disconnect our virtual controller; will reconnect once we send a button command after a request.
            LogUtil.LogInfo("Detaching controller on startup as first interaction.", Config.IP);
            await Connection.SendAsync(SwitchCommand.DetachController(), token).ConfigureAwait(false);
            await Task.Delay(200, token).ConfigureAwait(false);

            // Auto get offset
            Offset = await PlayerOffsetHelper.GetCurrentPlayerOffset(Connection, (uint)OffsetHelper.InventoryOffset, (uint)OffsetHelper.PlayerSize, token).ConfigureAwait(false);

            // Validate inventory offset.
            LogUtil.LogInfo("Checking inventory offset for validity.", Config.IP);
            var valid = await GetIsPlayerInventoryValid(Offset, token).ConfigureAwait(false);
            if (!valid)
            {
                LogUtil.LogInfo($"Inventory read from {Offset} (0x{Offset:X8}) does not appear to be valid.", Config.IP);
                if (Config.RequireValidInventoryMetadata)
                {
                    LogUtil.LogInfo("Exiting!", Config.IP);
                    return;
                }
            }
            PocketInjector.WriteOffset = Offset;

            // Prefetch first dodo
            await UpdateDodo(CancellationToken.None).ConfigureAwait(false);

            // Don't add the last arrival while the bot wasn't running to the log
            LastArrival = await GetArriver(token).ConfigureAwait(false);

            LogUtil.LogInfo("Successfully connected to bot. Starting main loop!", Config.IP);
            while (!token.IsCancellationRequested)
                await DropLoop(token).ConfigureAwait(false);
        }

        private async Task DropLoop(CancellationToken token)
        {
            // Check for new users (even if not accepting commands)
            var newArriver = await GetArriver(token).ConfigureAwait(false);
            if (newArriver != LastArrival)
            {
                LastArrival = newArriver;
                var arrival = Arrivals.UpdateLog(LastArrival);
                LogUtil.LogInfo($"A visitor is arriving: {arrival}", Config.IP);
            }

            if (!Config.AcceptingCommands)
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                return;
            }

            if (Injections.TryDequeue(out var item))
            {
                var count = await DropItems(item, token).ConfigureAwait(false);
                State.AfterDrop(count);
            }
            else if ((State.CleanRequired && State.Config.AutoClean) || CleanRequested)
            {
                await CleanUp(State.Config.PickupCount, token).ConfigureAwait(false);
                State.AfterClean();
                CleanRequested = false;
            }
            else
            {
                State.StillIdle();
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task<bool> GetIsPlayerInventoryValid(uint playerOfs, CancellationToken token)
        {
            var (ofs, len) = InventoryValidator.GetOffsetLength(playerOfs);
            var inventory = await Connection.ReadBytesAsync(ofs, len, token).ConfigureAwait(false);

            return InventoryValidator.ValidateItemBinary(inventory);
        }

        private async Task<int> DropItems(ItemRequest drop, CancellationToken token)
        {
            int dropped = 0;
            bool first = true;
            foreach (var item in drop.Items)
            {
                await DropItem(item, first, token).ConfigureAwait(false);
                first = false;
                dropped++;
            }
            return dropped;
        }

        private async Task DropItem(Item item, bool first, CancellationToken token)
        {
            // Exit out of any menus.
            if (first)
            {
                for (int i = 0; i < 3; i++)
                    await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);
            }

            var itemName = GameInfo.Strings.GetItemName(item);
            LogUtil.LogInfo($"Injecting Item: {item.DisplayItemId:X4} ({itemName}).", Config.IP);

            // Inject item.
            if (Config.InjectToEntireInventory)
            {
                var items = PocketInjectorAsync.GetEmptyInventory(40);
                foreach (var i in items)
                    i.CopyFrom(item);
                await PocketInjector?.Write(items, token);
            }
            else
            {
                var data = item.ToBytesClass();
                var poke = SwitchCommand.Poke(Offset, data);
                await Connection.SendAsync(poke, token).ConfigureAwait(false);
                await Task.Delay(0_300, token).ConfigureAwait(false);
            }

            // Open player inventory and open the currently selected item slot -- assumed to be the config offset.
            await Click(SwitchButton.X, 1_100, token).ConfigureAwait(false);
            await Click(SwitchButton.A, 0_500, token).ConfigureAwait(false);

            // Navigate down to the "drop item" option.
            var downCount = item.GetItemDropOption();
            for (int i = 0; i < downCount; i++)
                await Click(SwitchButton.DDOWN, 0_400, token).ConfigureAwait(false);

            // Drop item, close menu.
            await Click(SwitchButton.A, 0_400, token).ConfigureAwait(false);
            await Click(SwitchButton.X, 0_400, token).ConfigureAwait(false);

            // Exit out of any menus (fail-safe)
            for (int i = 0; i < 2; i++)
                await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);
        }

        public async Task UpdateDodo(CancellationToken token)
        {
            var offset = (uint)OffsetHelper.DodoOffset;
            LogUtil.LogInfo($"Attempting to update Dodo code from offset {offset}. Last Dodo code was: {DodoCode}", Config.IP);
            DodoCode = await FetchDodo(offset, token);
        }

        public async Task<string> GetArriver(CancellationToken token) => await GetLastArrival((uint)OffsetHelper.ArriverNameLocAddress, token);

        private async Task CleanUp(int count, CancellationToken token)
        {
            LogUtil.LogInfo("Picking up leftover items during idle time.", Config.IP);

            // Exit out of any menus.
            for (int i = 0; i < 3; i++)
                await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);

            // Pick up and delete.
            for (int i = 0; i < count; i++)
            {
                await Click(SwitchButton.Y, 2_000, token).ConfigureAwait(false);
                var poke = SwitchCommand.Poke(Offset, Item.NONE.ToBytes());
                await Connection.SendAsync(poke, token).ConfigureAwait(false);
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task<string> FetchDodo(uint offset, CancellationToken token)
        {	
            var data = await Connection.ReadBytesAsync(offset, 5, token).ConfigureAwait(false);
            var dodo = Encoding.UTF8.GetString(data, 0, 5);
            LogUtil.LogInfo($"Fetched Dodo code: {dodo}.", Config.IP);
            return dodo;
        }

        private async Task<string> GetLastArrival(uint offset, CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(offset, 0xC, token).ConfigureAwait(false);
            var name = Encoding.Unicode.GetString(data).TrimEnd();
            return name;
        }
    }
}
