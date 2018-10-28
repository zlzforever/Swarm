$(function () {
    var view = new Vue({
        el: '#view',
        data: {
            job: {
                name: '',
                group: 'DEFAULT',
                cron: '*/30 * * * * ?',
                sharding: 1,
                shardingParameters: '',
                load: 1,
                retryCount: 1,
                owner: '',
                description: '',
                application: '',
                arguments: '',
                concurrentExecutionDisallowed: 'True',
                logPattern: '\[ERR\]'
            }
        },
        mounted: function () {
            $('select').formSelect();
        },
        methods: {
            create: function () {
                var job = this.$data.job;
                hub.post("/swarm/v1.0/job?cron=" + job.cron + "&application=" + job.application + "&logpattern=" +
                    job.logPattern + "&arguments=" + job.arguments, {
                    name: job.name,
                    group: job.group,
                    retryCount: job.retryCount,
                    performer: 'SignalR',
                    executor: 'Process',
                    description: job.description,
                    load: job.load,
                    owner: job.owner,
                    sharding: job.sharding,
                    shardingParameters: job.shardingParameters
                }, function () {
                    window.location.href = '/job';
                });
            }
        }
    });
});