using NHSE.Core;
using SysBot.Base;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.AnimalCrossing
{
    public static class PlayerOffsetHelper
    {
        public static async Task<uint> GetCurrentPlayerOffset(SwitchConnectionAsync connection, uint rootInventoryOffset, uint playerSize, CancellationToken token)
        {
            var names = await FetchPlayerNames(connection, rootInventoryOffset, playerSize, token).ConfigureAwait(false);
            LogUtil.LogText($"Found the following players on your island: {string.Join(", ", names)}");
            return rootInventoryOffset + (playerSize * ((uint)names.Length - 1));
        }

        public static async Task<string[]> FetchPlayerNames(SwitchConnectionAsync connection, uint rootInventoryOffset, uint playerSize, CancellationToken token)
        {
            List<string> toRet = new List<string>();
            for (int i = 0; i < 8; ++i)
            {
                ulong address = OffsetHelper.getPlayerIdAddress(rootInventoryOffset) - 0xB8 + 0x20 + (playerSize * (ulong)i);
                byte[] pName = await connection.ReadBytesAsync((uint)address, 20, token).ConfigureAwait(false);
                if (!isZeroArray(pName))
                {
                    string name = StringUtil.GetString(pName, 0, 10);
                    toRet.Add(name);
                }
            }

            return toRet.ToArray();
        }

        private static bool isZeroArray(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; ++i)
                if (bytes[i] != 0)
                    return false;
            return true;
        }
    }
}
