using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SloppyContextActions.Editor
{
    internal static class AsepriteRunningInstance
    {
#if UNITY_EDITOR_WIN
        private const uint AppCommandClientOnly = 0x00000010;
        private const int CodePageWinUnicode = 1200;
        private const uint ExecuteTransaction = 0x00004050;
        private const uint TimeoutMilliseconds = 2000;

        private static readonly DdeCallback Callback = OnDdeCallback;
#endif

        public static bool TryOpen(string absolutePath)
        {
#if UNITY_EDITOR_WIN
            uint instanceId = 0;
            IntPtr service = IntPtr.Zero;
            IntPtr topic = IntPtr.Zero;
            IntPtr conversation = IntPtr.Zero;

            try
            {
                if (DdeInitialize(ref instanceId, Callback, AppCommandClientOnly, 0) != 0)
                    return false;

                service = DdeCreateStringHandle(instanceId, "Aseprite", CodePageWinUnicode);
                topic = DdeCreateStringHandle(instanceId, "system", CodePageWinUnicode);
                if (service == IntPtr.Zero || topic == IntPtr.Zero) return false;

                conversation = DdeConnect(instanceId, service, topic, IntPtr.Zero);
                if (conversation == IntPtr.Zero) return false;

                string command = $"[open(\"{absolutePath}\")]";
                byte[] payload = Encoding.Unicode.GetBytes(command + '\0');
                IntPtr result = DdeClientTransaction(
                    payload,
                    (uint)payload.Length,
                    conversation,
                    IntPtr.Zero,
                    0,
                    ExecuteTransaction,
                    TimeoutMilliseconds,
                    out _);

                return result != IntPtr.Zero;
            }
            finally
            {
                if (conversation != IntPtr.Zero) DdeDisconnect(conversation);
                if (service != IntPtr.Zero) DdeFreeStringHandle(instanceId, service);
                if (topic != IntPtr.Zero) DdeFreeStringHandle(instanceId, topic);
                if (instanceId != 0) DdeUninitialize(instanceId);
            }
#else
            return false;
#endif
        }

#if UNITY_EDITOR_WIN
        private delegate IntPtr DdeCallback(
            uint transactionType,
            uint dataFormat,
            IntPtr conversation,
            IntPtr stringHandle1,
            IntPtr stringHandle2,
            IntPtr data,
            UIntPtr data1,
            UIntPtr data2);

        private static IntPtr OnDdeCallback(
            uint transactionType,
            uint dataFormat,
            IntPtr conversation,
            IntPtr stringHandle1,
            IntPtr stringHandle2,
            IntPtr data,
            UIntPtr data1,
            UIntPtr data2)
        {
            return IntPtr.Zero;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DdeInitialize(
            ref uint instanceId,
            DdeCallback callback,
            uint command,
            uint reserved);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DdeCreateStringHandle(
            uint instanceId,
            string value,
            int codePage);

        [DllImport("user32.dll")]
        private static extern IntPtr DdeConnect(
            uint instanceId,
            IntPtr service,
            IntPtr topic,
            IntPtr context);

        [DllImport("user32.dll")]
        private static extern IntPtr DdeClientTransaction(
            byte[] data,
            uint dataLength,
            IntPtr conversation,
            IntPtr item,
            uint dataFormat,
            uint transactionType,
            uint timeout,
            out uint result);

        [DllImport("user32.dll")]
        private static extern bool DdeDisconnect(IntPtr conversation);

        [DllImport("user32.dll")]
        private static extern bool DdeFreeStringHandle(uint instanceId, IntPtr stringHandle);

        [DllImport("user32.dll")]
        private static extern bool DdeUninitialize(uint instanceId);
#endif
    }
}
