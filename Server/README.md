##GameServerCsharp

使用Actor模式，参考 Skynet 设计的c#服务器

使用c# 的 async await 关键字，以及 c# synchronizeContext 技术, Task 轻量级线程

实现Actor 之间的通信

实现Actor 的调度执行


##协议使用 protobuff

http://git.oschina.net/liyonghelpme/protobufferUnity

通过build.py 生成协议c#文件，将其放入到服务器工程的 packages 目录，从而在服务器使用pb 协议

##客户端和服务器共用了一些数据报文处理代码

服务器通过ServerMsgReader 解析协议

