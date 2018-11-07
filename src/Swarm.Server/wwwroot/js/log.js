$(function () {
    $('#jobMenu').addClass('active');
    const vue = new Vue({
        el: '#view',
        data: {
            els: [],
            page: hub.queryString('page') || 1,
            size: hub.queryString('size') || 40,
            total: 0,
            jobId: decodeURIComponent(hub.queryString('jobId') || '')
        },
        mounted: function () {
            loadView(this);
        }
    });

    setInterval(loadView, 2000, vue);

    function loadView(vue) {
        const url = '/swarm/v1.0/log?jobId=' + vue.$data.jobId + '&page=' + vue.$data.page + '&size=' + vue.$data.size;
        hub.get(url, function (result) {
            vue.$data.els = result.data.result;
            vue.$data.total = result.data.total;
            vue.$data.page = result.data.page;
            vue.$data.size = result.data.size;

            hub.ui.initPagination('#pagination', result.data, function (page) {
                window.location.href = '/job/log?jobId=' + vue.$data.jobId + '&page=' + page + '&size=' + vue.$data.size;
            });
        });
    }
});

