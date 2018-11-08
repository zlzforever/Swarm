$(function () {
    $('#jobMenu').addClass('active');
    new Vue({
        el: '#view',
        data: {
            job: {
                name: '',
                group: 'DEFAULT',
                cron: '*/30 * * * * ?',
                sharding: 1,
                shardingParameters: '',
                load: 1,
                owner: '',
                description: '',
                application: '',
                arguments: '',
                logPattern: '\\w+'
            }
        },
        mounted: function () {
            const allowConcurrent = $('.select2');
            allowConcurrent.val('False');
            allowConcurrent.select2({
                minimumResultsForSearch: Infinity
            });
        },
        methods: {
            create: function () {
                const job = this.$data.job;
                const url = "/swarm/v1.0/job";
                const data = {
                    name: job.name,
                    group: job.group,
                    performer: 'SignalR',
                    executor: 'Process',
                    description: job.description,
                    load: job.load,
                    owner: job.owner,
                    sharding: job.sharding,
                    shardingParameters: job.shardingParameters,
                    allowConcurrent: $('.select2').val(),
                    properties: {
                        "cron": job.cron,
                        "application": job.application,
                        "logpattern": job.logPattern,
                        "arguments": job.arguments
                    },
                    schedName: 'auto',
                    schedInstanceId: 'auto'
                };
                hub.post(url, data, function () {
                    window.location.href = '/job/cron';
                });
            }
        }
    });
});