# SWARM

Swarm is a distributed scheduled job framework, based on Quartz.

## DESGIN

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
 ##### SWARM SHARDING NODE

+ 2 or more Swarm Server as a quartz cluster node to make sure triggers will be performed.
+ Via ISharding to choose a Scheduler to create/update/delete job

 ##### EXECUTE JOB TYPE

 + Process: Client receive message from Swarm server, then start a process to execute the job 
 + Reflection: Swarm client load all DLLs from job directory, create a job instance and invoke the Task Handler(JobContext context) method

 ##### TRIGGER TYPE

+ Cron,
+ Simple,
+ DailyTimeInterval,
+ CalendarInterval

##### PERFOM TYPE

+ WebSocket
+ HTTP
+ Kafka
+ Other

## WHY SWARM?

Quartz is a great scheduler framework, but if we have millions or more jobs or trigger very frequent, one database can't load this scenario. So we want to implement a distributed scheduler system can scheudler millions jobs, millions clients

## LET'S MAKE A BRAIN STORM

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

1. 直接依赖 Quartz 默认支持的多个 Scheduler, 如对一个数据库默认分配多个 Scheduler:

```
Scheduler1, Scheduler2, Scheduler3, Scheduler4, Scheduler5, ...
```
2. 每个 Scheduler 由 2 个以上的实例来保证稳定性, 组成一个 Swarm Sharding Node, SSN 通过 SignalR 组成集群, 每个 SSN 启动后把信息添加到数据库, 并定时更新心跳. 每个 SSN 定时扫描注册表, 如果发现掉线的节点(分布式锁)则触发迁移
3. 每触发一个任务在数据库中给对应的 Scheduler 增加 1 次计数
4. SSN 还可以创建、删除、修改、触发, 同时也是对外的连接中心, 管理分布式客户端的触发, 创建任务的时候, 查询各 Scheduler 的触发计数, 取最小的一个 Scheduler 调用其Quartz数据库接口添加任务(负载算法可调整)
5. 当 SSN 触发一个任务时, 通过任务信息归属和分片信息等分发到对应的客户端
6. 当某个 SSN 只有一个实例时, 提示警告信息, 当 SSN 节点完全当掉时, 集群中的其它 SSN 应该把此节点的所有任务迁移到其它节点或者备用节点

       +----------------------+  +-----------------------+  +----------------------+ 
       |        JOBS          |  |        NODES          |  |      CLIENTS         | 
       +----------------------+  +-----------------------+  +----------------------+ 
       |  TriggerType         |  |  SchedInstanceId      |  |  Name                | 
       |  PermformerType      |  |  SchedName            |  |  Group               | 
       |  LastModificationTime|  |  Provider             |  |  ConnectionId        | 
       |  Name                |  |  TriggerTimes         |  |  Ip                  | 
       |  Group               |  |  LastModificationTime |  |  Os                  | 
       |  CreationTime        |  |  CreationTime         |  |  CoreCount           |  
       |  Description         |  |  ConnectString        |  |  Memory              | 
       |  Owner               |  +-----------------------+  |  CreationTime        | 
       |  Load                |                             |  LastModificationTime| 
       |  Sharding            |                             |  IsConnected         | 
       |  ShardingParameters  |                             |  SchedName           | 
       |  StartAt             |                             |  SchedInstanceId     | 
       |  EndAt               |                             |  IsConnected         |                           　
       |  AllowConcurrent     |                             +----------------------+                
       |  SchedInstanceId     |
       |  SchedName           |                             
       +----------------------+                             
       |  ExecutorType        |                                                                       
       |  Cron                |      
       |  CallbackHost        |  
       |  App                 |  
       |  AppArguments        |  
       +----------------------+  

       +----------------------+  +----------------------+  +----------------------+
       |   CLIENT_JOBS        |  |   CLIENT_PROCESSES   |  |         LOG          |
       +----------------------+  +----------------------+  +----------------------+
       |   Name               |  |  Name                |  |  ClientName          |
       |   Group              |  |  Group               |  |  ClientGroup         |
       |   ClassName          |  |  ProcessId           |  |  TraceId             |
       |   CreationTime       |  |  JobId               |  |  JobId               |
       +----------------------+  |  App                 |  |  Sharding            |
                                 |  AppArguments        |  |  Msg                 |
                                 |  LastModificationTime|  |  CreationTime        |
                                 |  CreationTime        |  +----------------------+
                                 |  Sharding            |   
                                 |  Msg                 |  
                                 |  TraceId             | 
                                 |  State               |
                                 +----------------------+  
                                  
                                       
       
实际上以上设计已经完全不关心SSN是同一个数据库还是不同的数据库了

Module | Feature | Interface | Description |Compelete | Unit Tests |
-------|-------|-------|-------|-------|  -------|
SSN | Heartbeat | ISwarmCluster| 从配置文件读取信息: SchedName, NodeId, 以此为条件更新心跳时间到数据库. 如果更新影响行数为 0, 则插入一条新的记录, 初始 TriggerTimes 为 0. 循环执行   |   ☑    |  ☐   |
SSN | Sharding | ISharding  | 从数据库中取出负载最小的节点　|    ☑    |  ☐   |
SSN | Health Check | ISwarmCluster | 每隔一定时间查询心跳超时节点, 发现警告信息或邮件给管理员, 严重的自动迁移数据, 需要分布式锁　|    ☐    |  ☐   |
SSN | Client Process Timeout Check | ISwarmCluster | 每隔一定时间查询客户端进程表, 发现超时的标识为超时　|    ☐    |  ☐   |
SSN | Create Job | IJobService | 参数验证 -> 判断任务是否存在 -> 通过分片接查询节点 -> 创建 Sched -> 添加任务到节点 ->　任务更新节点编号, 保存任务到 Swarm　数据库, 保存失败需要从节点中删除  　|    ☑    |  ☐   |
SSN | Delete Job | IJobService | 参数验证 -> 判断任务是否存在 -> 通过任务中的节点编号查询节点 -> 创建节点对应的 Sched ->　删除任务, 删除触发器 ->　从 Swarm 数据库中删除任务信息  　|    ☑    |  ☐   |
SSN | Update Job | IJobService | 参数验证 -> 判断任务是否存在 -> ...  　|    ☐    |  ☐   |
SSN | Trigger Job | IJobService | 参数验证 -> 判断任务是否存在 -> 通过任务中的节点编号查询节点 -> 创建节点对应的 Sched ->　触发任务 　|    ☑    |  ☐   |
SSN | Exit All | IJobService | 参数验证 -> 判断任务是否存在 -> 通知所有节点根据任务编号退出此任务所有进程(Http触发任务无法退出) 　|    ☑    |  ☐   |
SSN | Exit  | IJobService | 参数验证 -> 判断任务是否存在 -> 根据任务编号, 批次, 分片查询客户端连接信息, 通知对应节点退出对应任务 　|    ☐    |  ☐   |
Client | Loop Connect | IClient | 配置重试次数, 如果连接失败或者连接被断开则重试　|    ☑     |  ☐   |
Client | Register Jobs | IClient | 递归扫描　/{base}/jobs/下所有 DLLs, 扫描得到继承 ISwarmJob 的类型, 并注册到　SSN中　|    ☐     |  ☐   |
Client | ExecutorFactory | IExecutorFactory | 通过名字创建对应的任务执行器　|    ☑     |  ☐   |
Client | Process Executor | IExecutor | 启动一个新进程, 执行配置好的任务　|    ☑     |  ☐   |
Client | Reflection Executor | IExecutor | 反射任务类型, 执行配置好的任务　|    ☑     |  ☐   |
Client | Process Storage | IProcessStorage | 存储正在执行的任务, 一旦客户端崩溃重启依据本地存储信息检测还在跑的进程有哪些和SSN同步状态, 执行存储操作前先同步到 SSN　|    ☐    |  ☐   |
Client | Log Filter | ILogFilter | 筛选用户需要的日志上传到 SSN, 默认是全部上传　|    ☐     |  ☐   |



## CONSIDERATION

+ Client can't connect to Swarm server, but processes of jobs are still running, when the server restart, server should know those information and update job's state to database?
+ Client down, all processes it opened still alive?  may be we should store process<-> job info to help rescue client.


## CONTRIBUTION

1. Fork the project
2. Create Feat_xxx branch
3. Commit your code
4. Create Pull Request