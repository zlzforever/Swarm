#### Quartz 定时系统的基本原理

```
触发规划表

|  job1, 2018-11-01 17:36:00 |
|----------------------------|
|  job2, 2018-11-01 18:36:00 |
|----------------------------|
|  job3, 2018-11-01 18:37:00 |
|----------------------------|
|  job4, 2018-11-01 19:30:00 |
+----------------------------+

循环查询规划表以SQL SERVER举例:

1. 取得原子锁
2. 查询需要触发的 Trigger: SELECT TOP {maxCount} FROM TRIGGERS WHERE NextFireTime <=noLaterThan AND NextFireTime >= @noEarlierThan AND State = StateWaiting , 其中 noLaterThan 是当前时间 和 noEarlierThan 是当前时间加扫描频率, 在 Quartz中可以配置
3. 计算每一个 Trigger 的下一次触发时间, 更新 State 和下一次触发时间到数据库
4. 释放原子锁
5. 触发所有任务

```
那么其实这一类任务触发系统的问题也就很明显了, 在单位扫描时间内如果不能处理完所有的任务, 就会造成 miss fire. 其中 {maxCount} 的值很关键, 默认是 1, 可以根据需求调整可以大大提高性能.

#### Quartz 集群解决了什么问题?

Quartz 的集群模式是通过 SchedulerName 来标识同一组集群, 即在上面的第二步查找时添加 SchedulerName= @SchedulerName 这样的限制条件, 同时分布式锁的实现也是针对 SchedulerName 来做的, 下图是 Quartz 的 LOCKS 表

SCHED_NAME |    LOCK_NAME    |
-----------| ----------------|
SCHEDULER1 |  TRIGGER_ACCESS |

可以知道 Quartz 的集群是侧重任务端，把任务触发到不同的 Quartz 实例， 保证任务的执行是它的目的。 因此, Quartz 的默认集群模式并不能解决如果任务量过多、锁的竞争导致的 miss fire 问题

#### 是否真的需要考虑触发性能？

我不知道在真实场景中触发时间的分布, 假设我们的性能指标是每天调度100万次，触发时间是平均的则每秒要触发约 12 个任务, 这个数据量即便是使用了分布式锁相信也是能够轻轻松松负荷的. 如果要做到如阿里云 SchedulerX 所说的每天调度10亿次, 每秒要查询和更新11000+行, 这个量级我觉得高性能硬件数据库也不是不能完成. 但是这样并没有压榨掉数据库的极限性能, 毕竟是依靠一个原子锁

#### 解决思路

1. 直接依赖 Quartz 默认支持的多个 Scheduler, 如对一个数据库默认分配 5 个 Scheduler:

```
Scheduler1, Scheduler2, Scheduler3, Scheduler4, Scheduler5
```
2. 每个 Scheduler 由 2 个以上的实例来保证稳定性, 组成一个 Swarm Sharding Node, SSN 自动注册到 Sharding 模块
3. 实例每触发一个任务在数据库中给对应的 Scheduler 增加 1 次计数, Sharding 定时请求计算信息到以检查节点的健康状态
4. Sharding 模块用于创建、删除、修改、触发, 同时也是对外的连接中心, 管理分布式客户端的触发, 创建任务的时候, 查询各 Scheduler 的触发计数, 取最小的一个 Scheduler 调用其 WebApi 添加任务
5. 当 SSN 触发一个任务时, 通过消息接口传送到 Sharding 模块, Sharding 通过任务信息归属和分片信息等分发到对应的客户端
6. 当某个 SSN 只有一个实例时, Sharding 模块提示警告信息, 当 SSN 节点完全当掉时, Sharding 模块应该把此节点的所有任务迁移到其它节点或者备用节点

```
  +------------------+      +------------+        +------------------+      +------------+ 
  |  Swarm Server 1  +------>            |        |  Swarm Server 3  +------>            |
  +------------------+      |            |        +------------------+      |            | 
                            |   Quartz   |                                  |   Quartz   |
  +------------------+      | Scheduler 1|        +------------------+      | Scheduler 2|
  |  Swarm Server 2  +------>            |        |  Swarm Server 4  +------>            |
  +------------------+      +------------+        +------------------+      +------------+

                            +----------------------------------+
                            |             Swarm DB             |
                            +----------------------------------+

--------------------------------------SHARDING MODULE---------------------------------------
                                             |
                                       HTTP, WebSocket
                                             |
               +-------------------+---------+---------+-------------------+
               |                   |                   |                   | 
       +-------v--------+  +-------v--------+  +-------v--------+  +-------v--------+
       | Swarm Client 1 |  | Swarm Client 2 |  | Swarm Client 3 |  | Swarm Client 4 |
       +----------------+  +----------------+  +----------------+  +----------------+

 ```

       

       +----------------------+  +-----------------------+  +----------------------+  +-----------------+
       |        JOBS          |  |        SSNS           |  |      CLIENTS         |  |   CLIENT_JOBS   |
       +----------------------+  +-----------------------+  +----------------------+  +-----------------+
       |  TriggerType         |  |  SsnId                |  |  Name                |  |   ClientId      |
       |  PermformerType      |  |  SchedulerName        |  |  Group               |  |   ClassName     |
       |  UserId              |  |  InstanceId           |  |  ConnectionId        |  |   CreationTime  |
       |  Name                |  |  TriggerTimes         |  |  Ip                  |  +-----------------+
       |  Group               |  |  LastModificationTime |  |  Os                  |
       |  SsnId               |  |  CreationTime         |  |  CoreCount           |  
       |  Description         |  |  Host                 |  |  Memory              |
       |  Owner               |  +-----------------------+  |  CreationTime        |
       |  CreationTime        |                             |  LastModificationTime|
       |  LastModificationTime|                             |  IsConnected         |
       +----------------------+                             |  UseId               |
       |  ExecutorType        |                             +----------------------+      
       |  Load                |  
       |  Sharding            |  
       |  ShardingParameters  |  
       |  AllowConcurrent     |  
       |  Cron                |  
       |  StartAt             |  
       |  EndAt               |  
       |  CallbackHost        |  
       |  App                 |  
       |  AppArguments        |  
       +----------------------+  

实际上以上设计已经完全不关心SSN是同一个数据库还是不同的数据库了       
              
##### SSN 注册

1. SSN 从配置文件只读取信息: SchedulerName, 数据库连接串, Sharding地址等
2. 启动应用后尝试连接 Sharding, 连接成功后启动 Quartz

##### SSN 触发任务

1. 触发任务后, 触发信息推到 Sharding 中, 前期可直接用 SignalR,　后期可以考虑使用消息队列

##### 创建任务逻辑

1. 添加任务到 JOBS 表中
2. 查询哪个 SSN 是负载最小的, 调用接口添加任务, 如果添加失败则从 JOBS 表中删除并返回创建失败的信息给前端

##### SSN 健康检查

1. Sharding 每隔 5 秒请求一次已经注册的 SSN, SSN 返回它当前已经触发的任务总次数、集群实例数, 如果请求失败标识 SSN 掉线, 掉线一定时间后触发迁移, 修改 JOBS中任务对应的 SsnId

##### Sharding 任务推送

１. Sharding 收到触发信息后,　检测 SsnId 是否对应(发生迁移后原 SSN有可能恢复),　如果对应按不同的推送类型推送,　如果不对应,　推后消息让　SSN 下线

##### 任务执行类型

1. 依赖接口的, 客户端扫描固定目录下的　DLL, 加载并反射得到所有实现　Job　接口的类, 注册类型到服务端， 服务端触发任务后可匹配到类型后发送到正常的客户端
2. 不依赖接口的, 用户自己通过　Client 的 Group　来配置,　一切由用户自己控制,　配置出错可由 Sharding　平台查看日志