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

+ 2 or more Swarm Server as a quartz cluster node to make sure triggers will be performed. SSN use a independent DB.

 ##### SHARDING MODULE

+ Sharding request server load, database load, job count from Swarm servers every 5 second
+ Sharding choose a Scheduler to create/update/delete job

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

## CONSIDERATION

+ Client can't connect to Swarm server, but processes of jobs are still running, when the server restart, server should know those information and update job's state to database?
+ Client down, all processes it opened still alive?  may be we should store process<-> job info to help rescue client.


## CONTRIBUTION

1. Fork the project
2. Create Feat_xxx branch
3. Commit your code
4. Create Pull Request