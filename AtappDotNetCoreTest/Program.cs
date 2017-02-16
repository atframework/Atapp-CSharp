using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using atframe.atapp;

namespace AtappSimpleTest {
    class Program {
        static App app = new App();

        enum LOG_LEVEL {
            DISABLED = 0, // 关闭日志
            FATAL,        // 强制输出
            ERROR,        // 错误
            WARNING,
            INFO,
            NOTICE,
            DEBUG,
        };

        static void log(LOG_LEVEL level, string content) {
            App.WriteLog(0, (uint)level, level.ToString(), "DotNetCore", "", 0, content);
        }

        class AtappSimpleTestModule : Module {
            static public int Init(Module self) {
                log(LOG_LEVEL.INFO, "AppID=" + self.Application.AppID + " " + self.Name + " init, Version: " + self.Application.AppVersion);
                return 0;
            }

            static public int Reload(Module self) {
                App my_app = self.Application;

                log(LOG_LEVEL.INFO, "Configure atapp.id=" + string.Join(" ", my_app.GetConfigure("atapp.id")) + ", listen=" + string.Join(",", my_app.GetConfigure("atapp.bus.listen")));
                log(LOG_LEVEL.INFO, self.Name + " reloaded");
                return 0;
            }

            static public int Stop(Module self) {
                log(LOG_LEVEL.INFO, self.Name + " stop");
                return 0;
            }

            static public int Timeout(Module self) {
                log(LOG_LEVEL.INFO, self.Name + " timeout");
                return 0;
            }

            static public int Tick(Module self) {
                AtappSimpleTestModule me = (AtappSimpleTestModule)self;
                long cur = App.Now / 20;
                if (cur == me.print_time) {
                    return 0;
                }

                me.print_time = cur;
                App my_app = self.Application;
                string log_content = "AppID=" + my_app.AppID + " " + me.Name + " tick. ";
                if (my_app.IsRunning) {
                    log_content += "Running...";
                }

                if (my_app.IsTimeout) {
                    log_content += "Timeout.";
                }

                if (my_app.IsStoping) {
                    log_content += "Stoping.";
                }

                log(LOG_LEVEL.INFO, log_content);
                return 0;
            }

            private long print_time = 0;
        }

        static int AppOnCommandEcho(App self, byte[][] cmds) {
            string text = "echo commander:";
            for (int i = 0; i < cmds.Length; ++i) {
                text += " " + System.Text.Encoding.UTF8.GetString(cmds[i]);
            }

            log(LOG_LEVEL.NOTICE, text);
            return 0;
        }

        static int AppOnCommandTransfer(App self, byte[][] cmds) {
            if (cmds.Length < 2) {
                log(LOG_LEVEL.ERROR, "transfer command require at least 2 parameters");
                return 0;
            }

            int type = 0;
            if (cmds.Length > 2) {
                type = int.Parse(System.Text.Encoding.UTF8.GetString(cmds[2]));
            }
            ulong dst_id = ulong.Parse(System.Text.Encoding.UTF8.GetString(cmds[0]));

            self.SendData(dst_id, type, cmds[1], false);
            return 0;
        }

        static int AppOnCommandListen(App self, byte[][] cmds) {
            if (cmds.Length < 1) {
                log(LOG_LEVEL.ERROR, "listen command require at least 1 parameters");
                return 0;
            }

            return self.Listen(System.Text.Encoding.UTF8.GetString(cmds[0]));
        }

        static int AppOnCommandConnect(App self, byte[][] cmds) {
            if (cmds.Length < 1) {
                log(LOG_LEVEL.ERROR, "connect command require at least 1 parameters");
                return 0;
            }

            return self.Connect(System.Text.Encoding.UTF8.GetString(cmds[0]));
        }

        static int AppOnOptionEcho(App self, byte[][] cmds) {
            string text = "echo option:";
            for (int i = 0; i < cmds.Length; ++i) {
                text += " " + System.Text.Encoding.UTF8.GetString(cmds[i]);
            }

            log(LOG_LEVEL.NOTICE, text);
            return 0;
        }

        static int AppOnReceiveMessage(App self, Message msg, byte[] data) {
            log(LOG_LEVEL.DEBUG, string.Format(
                "receive a message(from 0x{0:X16}, type={1}): {2}",
                msg.SrcBusID, msg.Type,
                System.Text.Encoding.UTF8.GetString(data)
            ));
            return 0;
        }

        static int AppOnSendFailFunction(App self, ulong src_pd, ulong dst_pd, Message msg) {
            log(LOG_LEVEL.ERROR, string.Format("send data from 0x{0:X16} to 0x{1:X16} failed", src_pd, dst_pd));
            return 0;
        }

        static int AppOnConnectedFunction(App self, ulong pd, int status) {
            log(LOG_LEVEL.INFO, string.Format("app 0x{0:X16} connected, status: {1}", pd, status));
            return 0;
        }

        static int AppOnDisconnectedFunction(App self, ulong pd, int status) {
            log(LOG_LEVEL.INFO, string.Format("app 0x{0:X16} disconnected, status: {1}", pd, status));
            return 0;
        }

        static int AppOnAllModuleInitedFunction(App self) {
            return 0;
        }

        static void Main(string[] args) {
            // setup module
            Module mod = app.CreateModule<AtappSimpleTestModule>();
            mod.OnInit = AtappSimpleTestModule.Init;
            mod.OnReload = AtappSimpleTestModule.Reload;
            mod.OnStop = AtappSimpleTestModule.Stop;
            mod.OnTimeout = AtappSimpleTestModule.Timeout;
            mod.OnTick = AtappSimpleTestModule.Tick;

            // setup command
            app.AddCustomCommand("echo", AppOnCommandEcho, null);
            app.AddCustomCommand("transfer", AppOnCommandTransfer,
                "transfer    <target bus id> <message> [type=0]              send a message to another atapp"
            );
            app.AddCustomCommand("listen", AppOnCommandListen,
                "listen      <listen address>                                address(for example: ipv6//:::23456)"
            );
            app.AddCustomCommand("connect", AppOnCommandConnect,
                "connect     <connect address>                               address(for example: ipv4://127.0.0.1:23456)"
            );

            // setup option
            app.AddOption("-echo", AppOnOptionEcho, "-echo [text]                           echo a message.");

            // setup handle
            app.OnReceiveMessage = AppOnReceiveMessage;
            app.OnSendFail = AppOnSendFailFunction;
            app.OnConnected = AppOnConnectedFunction;
            app.OnDisconnected = AppOnDisconnectedFunction;
            app.OnAllModuleInited = AppOnAllModuleInitedFunction;

            int exit_code = app.Run(Environment.GetCommandLineArgs());
            Environment.Exit(exit_code);
        }
    }
}
