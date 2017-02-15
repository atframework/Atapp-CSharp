using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace atframe.atapp {
    /// <summary>
    /// 维护消息结构，这个类型只被App内部创建
    /// </summary>
    public class Message {
        public enum ATBUS_CMD {
            INVALID = 0,
            // 数据协议
            DATA_TRANSFER_REQ = 1,
            DATA_TRANSFER_RSP,
            CUSTOM_CMD_REQ,

            // 控制协议
            NODE_SYNC_REQ = 9,
            NODE_SYNC_RSP,
            NODE_REG_REQ,
            NODE_REG_RSP,
            NODE_CONN_SYN,
            NODE_PING,
            NODE_PONG,

            MAX
        }

        private IntPtr _native_message = IntPtr.Zero;
        public IntPtr NativeMessage {
            get {
                return _native_message;
            }
        }

        public Message(IntPtr native) {
            _native_message = native;
        }

        public void Release() {
            _native_message = IntPtr.Zero;
        }

#if !UNITY_EDITOR && UNITY_IPHONE
        public const string LIBNAME = "__Internal";
#else
        public const string LIBNAME = "atapp_c";
#endif

        #region import from dll setter
        /// <summary>
        /// get message cmd in header
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_msg_get_cmd(IntPtr msg);

        /// <summary>
        /// get message type in header
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_msg_get_type(IntPtr msg);

        /// <summary>
        /// get message ret in header
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_msg_get_ret(IntPtr msg);

        /// <summary>
        /// get message sequence in header
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint libatapp_c_msg_get_sequence(IntPtr msg);

        /// <summary>
        /// get message src_bus_id in header
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong libatapp_c_msg_get_src_bus_id(IntPtr msg);

        /// <summary>
        /// get message source bus id in forward message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong libatapp_c_msg_get_forward_from(IntPtr msg);

        /// <summary>
        /// get message dest but id in forward message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong libatapp_c_msg_get_forward_to(IntPtr msg);
        #endregion

        #region reader

        public ATBUS_CMD Cmd {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return ATBUS_CMD.INVALID;
                }

                return (ATBUS_CMD)libatapp_c_msg_get_cmd(NativeMessage);
            }
        }

        public int Type {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_type(NativeMessage);
            }
        }

        public int ReturnCode {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_ret(NativeMessage);
            }
        }

        public uint Sequence {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_sequence(NativeMessage);
            }
        }

        /// <summary>
        /// 这个Message的来源
        /// </summary>
        public ulong SrcBusID {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_src_bus_id(NativeMessage);
            }
        }

        /// <summary>
        /// 获取数据转发消息的最初发送者
        /// </summary>
        public ulong ForwardFrom {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_forward_from(NativeMessage);
            }
        }

        /// <summary>
        /// 获取数据转发消息的最终发送目标
        /// </summary>
        public ulong ForwardTo {
            get {
                if (IntPtr.Zero == NativeMessage) {
                    return 0;
                }

                return libatapp_c_msg_get_forward_to(NativeMessage);
            }
        }

        #endregion
    }
}
