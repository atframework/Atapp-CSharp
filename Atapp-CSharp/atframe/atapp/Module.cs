using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace atframe.atapp {
    class Module {
        private IntPtr _native_module = IntPtr.Zero;
        public IntPtr NativeModule {
            get {
                return _native_module;
            }
        }

        private Module() {}

        static public T Create<T>(IntPtr app_native) where T : Module, new() {
            if (IntPtr.Zero == app_native) {
                throw new InvalidOperationException("create module failed");
            }

            T ret = new T();
            IntPtr mod = Module.libatapp_c_module_create(app_native, ret.GetType().Name);
            if (IntPtr.Zero == mod) {
                throw new InvalidOperationException("create module failed");
            }
            ret._native_module = mod;
            return ret;
        }

        public void Release() {
            _native_module = IntPtr.Zero;
        }


        #region native delegate types
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_module_on_init_fn_t(IntPtr mod, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_module_on_reload_fn_t(IntPtr mod, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_module_on_stop_fn_t(IntPtr mod, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_module_on_timeout_fn_t(IntPtr mod, IntPtr priv_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int libatapp_c_module_on_tick_fn_t(IntPtr mod, IntPtr priv_data);
        #endregion

        #region import from dll setter
        /// <summary>
        /// get message cmd in header
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="mod_name"> module name</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libatapp_c_module_create(IntPtr context, string mod_name);

        /// <summary>
        /// get module name
        /// </summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern string libatapp_c_module_get_name(IntPtr mod);

        /// <summary>
        /// get module app context
        /// </summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr libatapp_c_module_get_context(IntPtr mod);

        /// <summary>
        /// set module event callback: on init
        /// </summary>
        /// <param name="mod">module</param>
        /// <param name="fn">callback function(return by Marshal.GetFunctionPointerForDelegate)</param>
        /// <param name="priv_data">private data</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_module_set_on_init(IntPtr mod, IntPtr fn, IntPtr priv_data);

        /// <summary>
        /// set module event callback: on reload
        /// </summary>
        /// <param name="mod">module</param>
        /// <param name="fn">callback function(return by Marshal.GetFunctionPointerForDelegate)</param>
        /// <param name="priv_data">private data</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_module_set_on_reload(IntPtr mod, IntPtr fn, IntPtr priv_data);

        /// <summary>
        /// set module event callback: on stop
        /// </summary>
        /// <param name="mod">module</param>
        /// <param name="fn">callback function(return by Marshal.GetFunctionPointerForDelegate)</param>
        /// <param name="priv_data">private data</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_module_set_on_stop(IntPtr mod, IntPtr fn, IntPtr priv_data);

        /// <summary>
        /// set module event callback: on timeout
        /// </summary>
        /// <param name="mod">module</param>
        /// <param name="fn">callback function(return by Marshal.GetFunctionPointerForDelegate)</param>
        /// <param name="priv_data">private data</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_module_set_on_timeout(IntPtr mod, IntPtr fn, IntPtr priv_data);

        /// <summary>
        /// set module event callback: on tick
        /// </summary>
        /// <param name="mod">module</param>
        /// <param name="fn">callback function(return by Marshal.GetFunctionPointerForDelegate)</param>
        /// <param name="priv_data">private data</param>
        /// <returns></returns>
        [DllImport(Message.LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void libatapp_c_module_set_on_tick(IntPtr mod, IntPtr fn, IntPtr priv_data);
        #endregion

        public string Name {
            get {
                if (IntPtr.Zero == _native_module) {
                    return "";
                }

                return libatapp_c_module_get_name(_native_module);
            }
        }

        public App Application {
            get {
                if (IntPtr.Zero == _native_module) {
                    return null;
                }

                IntPtr app_ptr = libatapp_c_module_get_context(_native_module);
                if (null == app_ptr || IntPtr.Zero == app_ptr) {
                    return null;
                }

                return App.GetApp(app_ptr);
            }
        }


        #region wrapper delegate types
        public delegate int EventFunction(Module self);
        #endregion

        class EventCallbackRefGroup<TC> where TC: class {
            public TC native = null;
            public EventFunction cs = null;
        }


        static int _on_init_call(IntPtr mod, IntPtr priv_data) {
            App app = App.GetApp(priv_data);
            if (null == priv_data) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_BAD_DATA;
            }

            Module m = app.GetModule(mod);
            if (null == m) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            if (null == m._on_init.cs) {
                return 0;
            }

            return m._on_init.cs(m);
        }

        private EventCallbackRefGroup<libatapp_c_module_on_init_fn_t> _on_init = new EventCallbackRefGroup<libatapp_c_module_on_init_fn_t>();
        public EventFunction OnInit {
            get {
                return _on_init.cs;
            }
            set {
                if (IntPtr.Zero == _native_module) {
                    throw new InvalidOperationException("Module released");
                }

                _on_init.cs = value;
                IntPtr fn;
                if (null != value) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_init.native = new libatapp_c_module_on_init_fn_t(_on_init_call));
                } else {
                    fn = IntPtr.Zero;
                }

                libatapp_c_module_set_on_init(_native_module, fn, Application.NativeApp);
            }
        }

        static int _on_reload_call(IntPtr mod, IntPtr priv_data) {
            App app = App.GetApp(priv_data);
            if (null == priv_data) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_BAD_DATA;
            }

            Module m = app.GetModule(mod);
            if (null == m) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            if (null == m._on_reload.cs) {
                return 0;
            }

            return m._on_reload.cs(m);
        }

        private EventCallbackRefGroup<libatapp_c_module_on_reload_fn_t> _on_reload = new EventCallbackRefGroup<libatapp_c_module_on_reload_fn_t>();
        public EventFunction OnReload {
            get {
                return _on_reload.cs;
            }
            set {
                if (IntPtr.Zero == _native_module) {
                    throw new InvalidOperationException("Module released");
                }

                _on_reload.cs = value;
                IntPtr fn;
                if (null != value) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_reload.native = new libatapp_c_module_on_reload_fn_t(_on_reload_call));
                } else {
                    fn = IntPtr.Zero;
                }

                libatapp_c_module_set_on_reload(_native_module, fn, Application.NativeApp);
            }
        }

        static int _on_stop_call(IntPtr mod, IntPtr priv_data) {
            App app = App.GetApp(priv_data);
            if (null == priv_data) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_BAD_DATA;
            }

            Module m = app.GetModule(mod);
            if (null == m) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            if (null == m._on_stop.cs) {
                return 0;
            }

            return m._on_stop.cs(m);
        }

        private EventCallbackRefGroup<libatapp_c_module_on_stop_fn_t> _on_stop = new EventCallbackRefGroup<libatapp_c_module_on_stop_fn_t>();
        public EventFunction OnStop {
            get {
                return _on_stop.cs;
            }
            set {
                if (IntPtr.Zero == _native_module) {
                    throw new InvalidOperationException("Module released");
                }

                _on_stop.cs = value;
                IntPtr fn;
                if (null != value) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_stop.native = new libatapp_c_module_on_stop_fn_t(_on_stop_call));
                } else {
                    fn = IntPtr.Zero;
                }

                libatapp_c_module_set_on_stop(_native_module, fn, Application.NativeApp);
            }
        }

        static int _on_timeout_call(IntPtr mod, IntPtr priv_data) {
            App app = App.GetApp(priv_data);
            if (null == priv_data) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_BAD_DATA;
            }

            Module m = app.GetModule(mod);
            if (null == m) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            if (null == m._on_timeout.cs) {
                return 0;
            }

            return m._on_timeout.cs(m);
        }

        private EventCallbackRefGroup<libatapp_c_module_on_timeout_fn_t> _on_timeout = new EventCallbackRefGroup<libatapp_c_module_on_timeout_fn_t>();
        public EventFunction OnTimeout {
            get {
                return _on_timeout.cs;
            }
            set {
                if (IntPtr.Zero == _native_module) {
                    throw new InvalidOperationException("Module released");
                }

                _on_timeout.cs = value;
                IntPtr fn;
                if (null != value) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_timeout.native = new libatapp_c_module_on_timeout_fn_t(_on_timeout_call));
                } else {
                    fn = IntPtr.Zero;
                }

                libatapp_c_module_set_on_timeout(_native_module, fn, Application.NativeApp);
            }
        }

        static int _on_tick_call(IntPtr mod, IntPtr priv_data) {
            App app = App.GetApp(priv_data);
            if (null == priv_data) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_BAD_DATA;
            }

            Module m = app.GetModule(mod);
            if (null == m) {
                return (int)App.ATBUS_ERROR_TYPE.EN_ATBUS_ERR_NOT_INITED;
            }

            if (null == m._on_tick.cs) {
                return 0;
            }

            return m._on_tick.cs(m);
        }

        private EventCallbackRefGroup<libatapp_c_module_on_tick_fn_t> _on_tick = new EventCallbackRefGroup<libatapp_c_module_on_tick_fn_t>();
        public EventFunction OnTick {
            get {
                return _on_tick.cs;
            }
            set {
                if (IntPtr.Zero == _native_module) {
                    throw new InvalidOperationException("Module released");
                }

                _on_tick.cs = value;
                IntPtr fn;
                if (null != value) {
                    fn = Marshal.GetFunctionPointerForDelegate(_on_tick.native = new libatapp_c_module_on_tick_fn_t(_on_tick_call));
                } else {
                    fn = IntPtr.Zero;
                }

                libatapp_c_module_set_on_tick(_native_module, fn, Application.NativeApp);
            }
        }
    }
}
