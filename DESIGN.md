### Quartz 一类触发系统的缺点

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

在不考虑miss的情况下, 循环查询规划表以SQL SERVER举例:

1. 取得原子锁
2. 查询需要触电的 SELECT TOP {maxCount} FROM TRIGGERS WHERE NextFireTime <=noLaterThan AND NextFireTime >= @noEarlierThan AND State = StateWaiting , 其中 noLaterThan 是当前时间 和 noEarlierThan 是当前时间加扫描频率, 在 Quartz中是可以设置的.
3. 计算各 Trigger 下一次的触发时间, 修改 State 和下一次触发时间到数据库
4. 释放原子锁

```
那么其实这一类任务触发系统的问题也就很明确了