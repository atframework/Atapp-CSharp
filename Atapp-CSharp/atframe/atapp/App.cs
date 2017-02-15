using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace atframe.atapp {
    class App {
        public enum ATBUS_ERROR_TYPE  {
            EN_ATBUS_ERR_SUCCESS = 0,

            EN_ATBUS_ERR_PARAMS = -1,
            EN_ATBUS_ERR_INNER = -2,
            EN_ATBUS_ERR_NO_DATA = -3,         // 无数据
            EN_ATBUS_ERR_BUFF_LIMIT = -4,      // 缓冲区不足
            EN_ATBUS_ERR_MALLOC = -5,          // 分配失败
            EN_ATBUS_ERR_SCHEME = -6,          // 协议错误
            EN_ATBUS_ERR_BAD_DATA = -7,        // 数据校验不通过
            EN_ATBUS_ERR_INVALID_SIZE = -8,    // 数据大小异常
            EN_ATBUS_ERR_NOT_INITED = -9,      // 未初始化
            EN_ATBUS_ERR_ALREADY_INITED = -10, // 已填充初始数据
            EN_ATBUS_ERR_ACCESS_DENY = -11,    // 不允许的操作
            EN_ATBUS_ERR_UNPACK = -12,         // 解包失败
            EN_ATBUS_ERR_PACK = -13,           // 打包失败

            EN_ATBUS_ERR_ATNODE_NOT_FOUND = -65,        // 查找不到目标节点
            EN_ATBUS_ERR_ATNODE_INVALID_ID = -66,       // 不可用的ID
            EN_ATBUS_ERR_ATNODE_NO_CONNECTION = -67,    // 无可用连接
            EN_ATBUS_ERR_ATNODE_FAULT_TOLERANT = -68,   // 超出容错值
            EN_ATBUS_ERR_ATNODE_INVALID_MSG = -69,      // 错误的消息
            EN_ATBUS_ERR_ATNODE_BUS_ID_NOT_MATCH = -70, // Bus ID不匹配
            EN_ATBUS_ERR_ATNODE_TTL = -71,              // ttl限制
            EN_ATBUS_ERR_ATNODE_MASK_CONFLICT = -72,    // 域范围错误或冲突
            EN_ATBUS_ERR_ATNODE_ID_CONFLICT = -73,      // ID冲突

            EN_ATBUS_ERR_CHANNEL_SIZE_TOO_SMALL = -101,
            EN_ATBUS_ERR_CHANNEL_BUFFER_INVALID = -102, // 缓冲区错误（已被其他模块使用或检测冲突）
            EN_ATBUS_ERR_CHANNEL_ADDR_INVALID = -103,   // 地址错误
            EN_ATBUS_ERR_CHANNEL_CLOSING = -104,        // 正在关闭

            EN_ATBUS_ERR_NODE_BAD_BLOCK_NODE_NUM = -202,  // 发现写坏的数据块 - 节点数量错误
            EN_ATBUS_ERR_NODE_BAD_BLOCK_BUFF_SIZE = -203, // 发现写坏的数据块 - 节点数量错误
            EN_ATBUS_ERR_NODE_BAD_BLOCK_WSEQ_ID = -204,   // 发现写坏的数据块 - 写操作序列错误
            EN_ATBUS_ERR_NODE_BAD_BLOCK_CSEQ_ID = -205,   // 发现写坏的数据块 - 检查操作序列错误

            EN_ATBUS_ERR_NODE_TIMEOUT = -211, // 操作超时

            EN_ATBUS_ERR_SHM_GET_FAILED = -301, // 连接共享内存出错，具体错误原因可以查看errno或类似的位置
            EN_ATBUS_ERR_SHM_NOT_FOUND = -302,  // 共享内存未找到

            EN_ATBUS_ERR_SOCK_BIND_FAILED = -401,    // 绑定地址或端口失败
            EN_ATBUS_ERR_SOCK_LISTEN_FAILED = -402,  // 监听失败
            EN_ATBUS_ERR_SOCK_CONNECT_FAILED = -403, // 连接失败

            EN_ATBUS_ERR_PIPE_BIND_FAILED = -501,    // 绑定地址或端口失败
            EN_ATBUS_ERR_PIPE_LISTEN_FAILED = -502,  // 监听失败
            EN_ATBUS_ERR_PIPE_CONNECT_FAILED = -503, // 连接失败

            EN_ATBUS_ERR_DNS_GETADDR_FAILED = -601,   // DNS解析失败
            EN_ATBUS_ERR_CONNECTION_NOT_FOUND = -602, // 找不到连接
            EN_ATBUS_ERR_WRITE_FAILED = -603,         // 底层API写失败
            EN_ATBUS_ERR_READ_FAILED = -604,          // 底层API读失败
            EN_ATBUS_ERR_EV_RUN = -605,               // 底层API事件循环失败
            EN_ATBUS_ERR_NO_LISTEN = -606,            // 尚未监听（绑定）
            EN_ATBUS_ERR_CLOSING = -607,              // 正在关闭或已关闭
        }

        // global pool
        static private readonly Dictionary<IntPtr, App> _binder_manager = new Dictionary<IntPtr, App>();
        private Dictionary<IntPtr, Module> _all_modules = new Dictionary<IntPtr, Module>();
        private IntPtr _native_app = IntPtr.Zero;

        App() {
            _native_app = libatapp_c_create();
            if (IntPtr.Zero == _native_app) {
                throw new System.OutOfMemoryException("Can not create native atgateway inner protocol v1 object");
            } else {
                lock(_binder_manager) {
                    _binder_manager.Add(_native_app, this);
                }
            }
        }

        ~App() {
            if (null != _native_app) {
                // release all modules
                foreach (KeyValuePair<IntPtr, Module> kv in _all_modules) {
                    kv.Value.Release();
                }

                libatapp_c_destroy(_native_app);
                lock (_binder_manager) {
                    _binder_manager.Remove(_native_app);
                }
            }
        }

        public IntPtr NativeApp {
            get {
                return _native_app;
            }
        }

        static public App GetApp(IntPtr key) {
            lock (_binder_manager) {
                App ret;
                return _binder_manager.TryGetValue(key, out ret) ? ret : null;
            }
        }

        public T CreateModule<T>() where T : Module, new() {
            if (IntPtr.Zero == _native_app) {
                throw new InvalidOperationException("create module failed");
            }

            T ret = Module.Create<T>(_native_app);
            _all_modules.Add(ret.NativeModule, ret);

            return ret;
        }

        public Module GetModule(IntPtr key) {
            Module ret;
            if (_all_modules.TryGetValue(key, out ret)) {
                return ret;
            }

            return null;
        }

        #region native delegate types
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_msg_fn_t(IntPtr app, IntPtr msg, IntPtr buffer, ulong buffer_length, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_send_fail_fn_t(IntPtr app, ulong src_pd, ulong dst_pd, IntPtr msg, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_connected_fn_t(IntPtr app, ulong rp, int status, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_disconnected_fn_t(IntPtr app, ulong rp, int status, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_all_module_inited_fn_t(IntPtr app, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_on_cmd_option_fn_t(IntPtr app, IntPtr buffer, IntPtr buffer_len, ulong sz, IntPtr priv_data);


        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_set_on_msg_fn(IntPtr context, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_set_on_send_fail_fn(IntPtr context, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_set_on_connected_fn(IntPtr context, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_set_on_disconnected_fn(IntPtr context, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_set_on_all_module_inited_fn(IntPtr context, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_add_cmd(IntPtr context, string cmd, IntPtr fn, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_add_option(IntPtr context, string key, IntPtr fn, string help_msg, IntPtr priv_data);

        #endregion

        #region wrapper delegate types
        /// <summary>
        /// This callback will be called when receive any message from atbus
        /// </summary>
        /// <param name="self">atapp instance</param>
        /// <param name="msg">received msg, this object will only be available in this callback</param>
        /// <param name="data">data</param>
        /// <returns></returns>
        public delegate int OnReceiveMessageFunction(App self, Message msg, byte[] data);
        public delegate int OnSendFailFunction(App self, ulong src_pd, ulong dst_pd, Message msg);
        public delegate int OnConnectedFunction(App self, ulong pd, int status);
        public delegate int OnDisconnectedFunction(App self, ulong pd, int status);
        public delegate int OnAllModuleInitedFunction(App self);
        public delegate int OnCommandOptionFunction(App self, byte[][] cmds);
        #endregion

        #region delegate implantation
        static private int libatapp_c_on_msg_fn(IntPtr app, IntPtr msg, IntPtr buffer, ulong buffer_length, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            if (null == self.OnReceiveMessage) {
                return 0;
            }

            byte[] data_buffer = null;
            if (IntPtr.Zero != buffer && buffer_length > 0) {
                data_buffer = new byte[buffer_length];
                Marshal.Copy(buffer, data_buffer, 0, (int)buffer_length);
            }
            Message m = new Message(msg);
            int ret = self.OnReceiveMessage(self, m, data_buffer);
            m.Release();

            return ret;
        }

        private OnReceiveMessageFunction _on_receive_msg_fn = null;
        private libatapp_c_on_msg_fn_t _on_receive_msg_holder = null;
        public OnReceiveMessageFunction OnReceiveMessage {
            get {
                return _on_receive_msg_fn;
            }
            set {
                if (IntPtr.Zero == _native_app) {
                    throw new InvalidOperationException("native object invalid");
                }

                _on_receive_msg_fn = value;
                IntPtr fn = IntPtr.Zero;

                if (null != _on_receive_msg_fn) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_receive_msg_holder = new libatapp_c_on_msg_fn_t(libatapp_c_on_msg_fn));
                }

                libatapp_c_set_on_msg_fn(_native_app, fn, IntPtr.Zero);
            }
        }


        static private int libatapp_c_on_send_fail_fn(IntPtr app, ulong src_pd, ulong dst_pd, IntPtr msg, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            if (null == self.OnSendFail) {
                return 0;
            }
            Message m = new Message(msg);
            int ret = self.OnSendFail(self, src_pd, dst_pd, m);
            m.Release();

            return ret;
        }

        private OnSendFailFunction _on_send_fail_fn = null;
        private libatapp_c_on_send_fail_fn_t _on_send_fail_holder = null;
        public OnSendFailFunction OnSendFail {
            get {
                return _on_send_fail_fn;
            }
            set {
                if (IntPtr.Zero == _native_app) {
                    throw new InvalidOperationException("native object invalid");
                }

                _on_send_fail_fn = value;
                IntPtr fn = IntPtr.Zero;

                if (null != _on_send_fail_fn) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_send_fail_holder = new libatapp_c_on_send_fail_fn_t(libatapp_c_on_send_fail_fn));
                }

                libatapp_c_set_on_send_fail_fn(_native_app, fn, IntPtr.Zero);
            }
        }

        static private int libatapp_c_on_connected_fn(IntPtr app, ulong ep, int status, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            if (null == self.OnConnected) {
                return 0;
            }
            return self.OnConnected(self, ep, status);
        }

        private OnConnectedFunction _on_connected_fn = null;
        private libatapp_c_on_connected_fn_t _on_connected_holder = null;
        public OnConnectedFunction OnConnected {
            get {
                return _on_connected_fn;
            }
            set {
                if (IntPtr.Zero == _native_app) {
                    throw new InvalidOperationException("native object invalid");
                }

                _on_connected_fn = value;
                IntPtr fn = IntPtr.Zero;

                if (null != _on_connected_fn) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_connected_holder = new libatapp_c_on_connected_fn_t(libatapp_c_on_connected_fn));
                }

                libatapp_c_set_on_connected_fn(_native_app, fn, IntPtr.Zero);
            }
        }

        static private int libatapp_c_on_disconnected_fn(IntPtr app, ulong ep, int status, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            if (null == self.OnDisconnected) {
                return 0;
            }
            return self.OnDisconnected(self, ep, status);
        }

        private OnDisconnectedFunction _on_disconnected_fn = null;
        private libatapp_c_on_disconnected_fn_t _on_disconnected_holder = null;
        public OnDisconnectedFunction OnDisconnected {
            get {
                return _on_disconnected_fn;
            }
            set {
                if (IntPtr.Zero == _native_app) {
                    throw new InvalidOperationException("native object invalid");
                }

                _on_disconnected_fn = value;
                IntPtr fn = IntPtr.Zero;

                if (null != _on_disconnected_fn) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_disconnected_holder = new libatapp_c_on_disconnected_fn_t(libatapp_c_on_disconnected_fn));
                }

                libatapp_c_set_on_disconnected_fn(_native_app, fn, IntPtr.Zero);
            }
        }

        static private int libatapp_c_on_all_module_inited_fn(IntPtr app, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            if (null == self.OnAllModuleInited) {
                return 0;
            }

            return self.OnAllModuleInited(self);
        }

        private OnAllModuleInitedFunction _on_all_module_inited_fn = null;
        private libatapp_c_on_all_module_inited_fn_t _on_all_module_inited_holder = null;
        public OnAllModuleInitedFunction OnAllModuleInited {
            get {
                return _on_all_module_inited_fn;
            }
            set {
                if (IntPtr.Zero == _native_app) {
                    throw new InvalidOperationException("native object invalid");
                }

                _on_all_module_inited_fn = value;
                IntPtr fn = IntPtr.Zero;

                if (null != _on_all_module_inited_fn) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_all_module_inited_holder = new libatapp_c_on_all_module_inited_fn_t(libatapp_c_on_all_module_inited_fn));
                }

                libatapp_c_set_on_all_module_inited_fn(_native_app, fn, IntPtr.Zero);
            }
        }

        static private int libatapp_c_on_cmd_fn(IntPtr app, IntPtr buffer, IntPtr buffer_len, ulong sz, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            OnCommandOptionFunction fn;
            if (self._all_cmd_fn.TryGetValue(priv_data, out fn)) {
                if (null != fn) {
                    byte[][] res = new byte[sz][];
                    int ptr_size = Marshal.SizeOf<IntPtr>();
                    int i64_size = Marshal.SizeOf<long>();
                    for (ulong i = 0; i < sz; ++i) {
                        IntPtr buf_addr = Marshal.ReadIntPtr(buffer, (int)i * ptr_size);
                        long buf_len = Marshal.ReadInt64(buffer_len, (int)i * i64_size);

                        res[i] = new byte[buf_len];
                        Marshal.Copy(buf_addr, res[i], 0, (int)buf_len);
                    }

                    return fn(self, res);
                }
            }

            return 0;
        }

        private Dictionary<IntPtr, OnCommandOptionFunction> _all_cmd_fn = new Dictionary<IntPtr, OnCommandOptionFunction>();
        private Dictionary<IntPtr, libatapp_c_on_cmd_option_fn_t> _all_cmd_holder = new Dictionary<IntPtr, libatapp_c_on_cmd_option_fn_t>();
        public void AddCustomCommand(string cmd, OnCommandOptionFunction val) {
            if (IntPtr.Zero == _native_app) {
                throw new InvalidOperationException("native object invalid");
            }

            if (null == val || 0 == cmd.Length) {
                throw new ArgumentNullException();
            }

            libatapp_c_on_cmd_option_fn_t holder = new libatapp_c_on_cmd_option_fn_t(libatapp_c_on_cmd_fn);
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(holder);
            _all_cmd_fn.Add(fn, val);
            _all_cmd_holder.Add(fn, holder);

            libatapp_c_add_cmd(_native_app, cmd, fn, fn);
        }

        static private int libatapp_c_on_option_fn(IntPtr app, IntPtr buffer, IntPtr buffer_len, ulong sz, IntPtr priv_data) {
            App self = GetApp(app);
            if (null == self) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_ATNODE_INVALID_ID;
            }

            OnCommandOptionFunction fn;
            if (self._all_option_fn.TryGetValue(priv_data, out fn)) {
                if (null != fn) {
                    byte[][] res = new byte[sz][];
                    int ptr_size = Marshal.SizeOf<IntPtr>();
                    int i64_size = Marshal.SizeOf<long>();
                    for (ulong i = 0; i < sz; ++i) {
                        IntPtr buf_addr = Marshal.ReadIntPtr(buffer, (int)i * ptr_size);
                        long buf_len = Marshal.ReadInt64(buffer_len, (int)i * i64_size);

                        res[i] = new byte[buf_len];
                        Marshal.Copy(buf_addr, res[i], 0, (int)buf_len);
                    }

                    return fn(self, res);
                }
            }

            return 0;
        }

        private Dictionary<IntPtr, OnCommandOptionFunction> _all_option_fn = new Dictionary<IntPtr, OnCommandOptionFunction>();
        private Dictionary<IntPtr, libatapp_c_on_cmd_option_fn_t> _all_option_holder = new Dictionary<IntPtr, libatapp_c_on_cmd_option_fn_t>();
        public void AddOption(string opt, OnCommandOptionFunction val, string help_msg) {
            if (IntPtr.Zero == _native_app) {
                throw new InvalidOperationException("native object invalid");
            }

            if (null == val || 0 == opt.Length) {
                throw new ArgumentNullException();
            }

            libatapp_c_on_cmd_option_fn_t holder = new libatapp_c_on_cmd_option_fn_t(libatapp_c_on_option_fn);
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(holder);
            _all_option_fn.Add(fn, val);
            _all_option_holder.Add(fn, holder);

            libatapp_c_add_option(_native_app, opt, fn, help_msg, fn);
        }
        #endregion

        #region import from dll setter
        /// <summary>
        /// create app
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr libatapp_c_create();

        /// <summary>
        /// destroy app
        /// </summary>
        /// <param name="app">app context</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_destroy(IntPtr app);


        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_run(IntPtr context, int argc, string[] argv, IntPtr priv_data);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_reload(IntPtr context);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_stop(IntPtr context);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_tick(IntPtr context);

        // =========================== basic ===========================
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong libatapp_c_get_id(IntPtr context);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern string libatapp_c_get_app_version(IntPtr context);

        // =========================== configures ===========================
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong  libatapp_c_get_configure_size(IntPtr context, string path);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong  libatapp_c_get_configure(IntPtr context, string path, out string[] out_buf, out ulong[] out_len, ulong arr_sz);

        // =========================== flags ===========================
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_is_running(IntPtr context);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_is_stoping(IntPtr context);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_is_timeout(IntPtr context);

        // =========================== bus actions ===========================
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_listen(IntPtr context, string address);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_connect(IntPtr context, string address);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_disconnect(IntPtr context, ulong app_id);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_send_data_msg(IntPtr context, ulong app_id, int type, byte[] buffer, ulong sz, int require_rsp);
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int  libatapp_c_send_custom_msg(IntPtr context, ulong app_id, byte[][] arr_buf, ulong[] arr_size, ulong arr_count);


        // ====================== utility ======================
        /// <summary>
        /// get unix timestamp in atapp
        /// </summary>
        /// <returns>unix timestamp</returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long libatapp_c_get_unix_timestamp();

        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_log_write(uint tag, uint level, string level_name, 
            string file_path, string func_name, uint line_number, string log_content);

        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_log_update();

        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint libatapp_c_log_get_level(uint tag);

        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int libatapp_c_log_check_level(uint tag, uint level);
        #endregion

        public int Run(string[] args) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_run(_native_app, args.Length, args, IntPtr.Zero);
        }

        public int Reload() {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_reload(_native_app);
        }

        public int Stop() {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_stop(_native_app);
        }

        public int Tick() {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_tick(_native_app);
        }

        public ulong AppID {
            get {
                if (IntPtr.Zero == _native_app) {
                    return 0;
                }

                return libatapp_c_get_id(_native_app);
            }
        }

        public string AppVersion {
            get {
                if (IntPtr.Zero == _native_app) {
                    return "";
                }

                return libatapp_c_get_app_version(_native_app);
            }
        }

        public string[] GetConfigure(string path) {
            if (IntPtr.Zero == _native_app) {
                return null;
            }

            ulong sz = libatapp_c_get_configure_size(_native_app, path);
            string[] ret;
            ulong[] ret_sz;
            libatapp_c_get_configure(_native_app, path, out ret, out ret_sz, sz);

            return ret;
        }

        bool IsRunning {
            get {
                if (IntPtr.Zero == _native_app) {
                    return false;
                }

                return 0 != libatapp_c_is_running(_native_app);
            }
        }

        bool IsStoping {
            get {
                if (IntPtr.Zero == _native_app) {
                    return false;
                }

                return 0 != libatapp_c_is_stoping(_native_app);
            }
        }

        bool IsTimeout {
            get {
                if (IntPtr.Zero == _native_app) {
                    return false;
                }

                return 0 != libatapp_c_is_timeout(_native_app);
            }
        }

        public int Listen(string addr) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_listen(_native_app, addr);
        }

        public int Connect(string addr) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_connect(_native_app, addr);
        }

        public int Disconnect(ulong appid) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_disconnect(_native_app, appid);
        }

        public int SendData(ulong appid, int type, byte[] data, bool require_rsp = false) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            return libatapp_c_send_data_msg(_native_app, appid, type, data, (ulong)data.Length, require_rsp? 1: 0);
        }

        public int SendCommand(ulong appid, byte[][] data) {
            if (IntPtr.Zero == _native_app) {
                return (int)ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            ulong[] arr_size = new ulong[data.Length];
            for (int i = 0; i < data.Length; ++i) {
                arr_size[i] = (ulong)data[i].Length;
            }

            return libatapp_c_send_custom_msg(_native_app, appid, data, arr_size, (ulong)data.Length);
        }

        // ====================== utility ======================
        /// <summary>
        /// 获取atapp的unix时间戳
        /// </summary>
        static public long Now {
            get {
                return libatapp_c_get_unix_timestamp();
            }
        }

        static public uint GetLogLevel(uint tag) {
            return libatapp_c_log_get_level(tag);
        }

        static public bool CheckLogLevel(uint tag, uint level) {
            return 0 != libatapp_c_log_check_level(tag, level);
        }

        static public void UpdateLog() {
            libatapp_c_log_update();
        }

        static public void WriteLog(uint tag, uint level, string level_name,
            string file_path, string func_name, uint line_number, string log_content) {
            libatapp_c_log_write(tag, level, level_name, file_path, func_name, line_number, log_content);
        }
    }
}
