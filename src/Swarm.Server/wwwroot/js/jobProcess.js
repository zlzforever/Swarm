$(function () {
    new Vue({
        el: '#view',
        data: {
            states: [],
            page: hub.queryString('page') || 1,
            size: hub.queryString('size') || 10,
            total: 0,
            state: decodeURIComponent(hub.queryString('state') || ''),
            jobId: decodeURIComponent(hub.queryString('jobId') || '')
        },
        mounted: function () {
            loadView(this);
        }
    });

    function loadView(vue) {
        const url = '/swarm/v1.0/jobProcess?jobId=' + vue.$data.jobId + '&state=' + vue.$data.state + '&page=' + vue.$data.page + '&size=' + vue.$data.size;
        hub.get(url, function (result) {
            vue.$data.states = result.data.result;
            vue.$data.total = result.data.total;
            vue.$data.page = result.data.page;
            vue.$data.size = result.data.size;

            hub.ui.initPagination('#pagination', result.data, function (page) {
                window.location.href = '/job/jobProcess?jobId=' + vue.$data.jobId + '&state=' + vue.$data.state + '&page=' + page + '&size=' + vue.$data.size;
            });
        });
    }
});

