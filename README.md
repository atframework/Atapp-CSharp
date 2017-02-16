# Atapp-CSharp
libatapp内置协议的C#接入层


## 依赖项
+ 依赖 [libapp][1] 
+ 需要导入[libapp][1] 的[纯C接入层][2] 编译出的动态链接库

> 本项目同时支持.net framework或.net core, 但是目前仅在Windows下自测过。
> 
> 另外[Atapp-CSharp/atframe/atapp/Message.cs](Atapp-CSharp/atframe/atapp/Message.cs) 中的**LIBNAME**默认设置是**atapp_c**， 
> 但是[libapp][1] 的[纯C接入层][2] 编译出来再有些平台上的文件名可能是libatapp_c.dll或libatapp_c.so(或其他后缀)
> 这种情况请直接重命名编译出来的[纯C接入层][2] 的动态库的文件名为atapp_c.dll或atapp_c.so(或其他后缀)

## 注意事项
1. **Atapp-CSharp**和**AtappDotNetCoreTest**的编译选项使用的是AnyCPU，**AtappSimpleTest**是x64。但是依赖的[libapp][1] 一般是指定x86或x86_64的，所以使用的时候要根据.net运行时复制相应的C++依赖库到库搜索目录中
2. [libapp][1] 还依赖[libuv][3]，所以也要复制对应架构的[libuv][3]动态库到库搜索目录
3. **AtappSimpleTest**是基于.net framework的测试项目，**AtappDotNetCoreTest**是基于.net core的测试项目
4. .net core的获取运行时调用栈的的API受限，所以暂时Log无法判定文件名、行号和函数名称
5. **AtappSimpleTest**会自动调用Bat脚本复制配置和启动脚本到生成目录，但是**AtappDotNetCoreTest**的允许环境不统一，所以需要手动把etc目录中的文件copy过去


[1]: https://github.com/atframework/libatapp
[2]: https://github.com/atframework/libatapp/tree/master/binding/c
[3]: http://libuv.org/