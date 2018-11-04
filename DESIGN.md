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
2. 每个 Scheduler 由 2 个以上的实例来保证稳定性, 组成一个 Swarm Sharding Node, SSN 通过 SignalR 组成集群, 每个 SSN 启动后把信息添加到数据库, 并定时更新心跳. 每个 SSN 定时扫描注册表, 如果发现掉线的节点(分布式锁)则触发迁移
3. 每触发一个任务在数据库中给对应的 Scheduler 增加 1 次计数
4. SSN 还可以创建、删除、修改、触发, 同时也是对外的连接中心, 管理分布式客户端的触发, 创建任务的时候, 查询各 Scheduler 的触发计数, 取最小的一个 Scheduler 调用其Quartz数据库接口添加任务(负载算法可调整)
5. 当 SSN 触发一个任务时, 通过任务信息归属和分片信息等分发到对应的客户端
6. 当某个 SSN 只有一个实例时, 提示警告信息, 当 SSN 节点完全当掉时, 集群中的其它 SSN 应该把此节点的所有任务迁移到其它节点或者备用节点

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
       |        JOBS          |  |        NODES          |  |      CLIENTS         |  |   CLIENT_JOBS   |
       +----------------------+  +-----------------------+  +----------------------+  +-----------------+
       |  TriggerType         |  |  NodeId               |  |  Name                |  |   ClientId      |
       |  PermformerType      |  |  SchedName            |  |  Group               |  |   ClassName     |
       |  UserId              |  |  Provider             |  |  ConnectionId        |  |   CreationTime  |
       |  Name                |  |  TriggerTimes         |  |  Ip                  |  +-----------------+
       |  Group               |  |  LastModificationTime |  |  Os                  |
       |  SsnId               |  |  CreationTime         |  |  CoreCount           |  
       |  Description         |  |  ConnectString        |  |  Memory              |
       |  Owner               |  +-----------------------+  |  CreationTime        |
       |  Load                |                             |  LastModificationTime|
       |  Sharding            |                             |  IsConnected         |
       |  ShardingParameters  |                             |  UseId               |
       |  StartAt             |                             +----------------------+
       |  EndAt               |
       |  AllowConcurrent     |
       |  CreationTime        |                             
       |  LastModificationTime|                             
       +----------------------+                             
       |  ExecutorType        |                                                                       
       |  Cron                |      
       |  CallbackHost        |  
       |  App                 |  
       |  AppArguments        |  
       +----------------------+  

实际上以上设计已经完全不关心SSN是同一个数据库还是不同的数据库了       
              
##### SSN 注册: ISwarmCluster

1. SSN 从配置文件读取信息: SchedName, NodeId 更新心跳时间到数据库, 更新条件为 SchedName, NodeId
2. 如果更新影响行数为 0, 则插入一条新的记录, 初始 TriggerTimes 为 0 
2. 循环执行

##### SSN 触发任务

1. 判断任务是否在 Swarm JOBS 中存在, 如果不存在, 删除此任务
2. 增加一次触发计算
3. 通过 PerformerFactory 创建接口执行对应的触发任务

##### 创建任务逻辑

1. 数据验证
2. 查询最低负载结点, 如果没有提示没有可用节点
3. 通过配置在节点中的 Provider, ConnectionString, SchedName, NodeId 创建 IScheduler 对象
4. 通过 IScheduler 添加任务
5. 添加任务到 Swarm 数据库的 JOBS 表中

##### 删除任务逻辑

1. 从 Swarm JOBS 表中查找到任务所在节点
2. 通过配置在节点中的 Provider, ConnectionString, SchedName, NodeId 创建 IScheduler 对象
3. 通过 IScheduler 删除任务
4. 删除 Swarm 数据库中的任务信息

##### SSN 健康检查

1. 获取表锁
2. 根据心跳时间取得掉线节点
3. 迁移数据

##### 任务执行类型

1. 依赖接口的, 客户端扫描固定目录下的　DLL, 加载并反射得到所有实现　Job　接口的类, 注册类型到服务端， 服务端触发任务后可匹配到类型后发送到正常的客户端
2. 不依赖接口的, 用户自己通过　Client 的 Group　来配置,　一切由用户自己控制,　配置出错可由 Sharding　平台查看日志