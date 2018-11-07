$(function () {
    $('#dashboardMenu').addClass('active');
    const vue = new Vue({
        el: '#view',
        data: {
            jobCount: 0,
            nodeCount: 0,
            clientCount: 0,
            triggerTimes: 0,
            offlineCount: 0
        },
        mounted: function () {
            loadView(this);
        }
    });

    function loadView(vue) {
        const url = 'swarm/v1.0/dashboard';
        hub.get(url, function (result) {
            vue.$data.jobCount = result.data.jobCount;
            vue.$data.nodeCount = result.data.nodeCount;
            vue.$data.clientCount = result.data.clientCount;
            vue.$data.triggerTimes = result.data.triggerTimes;
            vue.$data.offlineCount = result.data.offlineCount;
        });
    }

    setInterval(loadView, 5000, vue);

});

