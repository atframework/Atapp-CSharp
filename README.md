# Atapp-CSharp
libatapp内置协议的C#接入层


+ 依赖 [libapp](https://github.com/atframework/libatapp) 
+ 需要导入[libapp](https://github.com/atframework/libatapp) 的[纯C接入层](https://github.com/atframework/libatapp/tree/master/binding/c)编译出的动态链接库

> 本项目理论上同时支持.net framework或.net core, 但是目前仅在Windows下自测过。
> 
> 另外[Atapp-CSharp/atframe/atapp/Message.cs](Atapp-CSharp/atframe/atapp/Message.cs) 中的**LIBNAME**默认设置是**atapp_c**， 
> 但是[libapp](https://github.com/atframework/libatapp) 的[纯C接入层](https://github.com/atframework/libatapp/tree/master/binding/c)编译出来再有些平台上的文件名可能是libatapp_c.dll或libatapp_c.so(或其他后缀)
> 这种情况请直接重命名编译出来的[纯C接入层](https://github.com/atframework/libatapp/tree/master/binding/c)的动态库的文件名为atapp_c.dll或atapp_c.so(或其他后缀)
