### Quartz

#### 定时系统的基本原理与缺陷

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
2. 查询需要触发的 Trigger: SELECT TOP {maxCount} FROM TRIGGERS WHERE NextFireTime <=noLaterThan AND NextFireTime >= @noEarlierThan AND State = StateWaiting , 其中 noLaterThan 是当前时间 和 noEarlierThan 是当前时间加扫描频率, 在 Quartz中是可以设置的.
3. 计算每一个 Trigger 下一次的触发时间, 修改 State 和下一次触发时间到数据库
4. 释放原子锁

```
那么其实这一类任务触发系统的问题也就很明显了, 在单位扫描时间内如果不能处理完所有的任务, 就会造成 miss fire, 严重的 Trigger 积压可以导致整个定时系统失去准确度。

#### 集群是否能解决问题

Quartz 的集群模式是通过 Scheduler name 来标识同一组集群, 即在上面的第二步查找时添加 SchedulerName= @Name 这样的限制条件, 同时分布式锁的实现也是对Scheduler级别