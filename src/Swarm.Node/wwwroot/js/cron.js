$(function () {
    $('#jobMenu').addClass('active');
    const vue = new Vue({
        el: '#view',
        data: {
            jobs: [],
            page: hub.queryString('page') || 1,
            size: hub.queryString('size') || 16,
            total: 0,
            keyword: decodeURIComponent(hub.queryString('keyword') || '')
        },
        mounted: function () {
            loadView(this);
        },
        methods: {
            search: function () {
                window.location.href = '/job/cron?keyword=' + vue.$data.keyword + '&page=' + vue.$data.page + '&size=' + vue.$data.size;
            },
            exit: function (id) {
                let that = this;
                swal({
                    title: "Sure to exit this job?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.post("/swarm/v1.0/job/" + id, null, function () {
                        loadView(that);
                        swal({
                            title: "Exit success",
                            type: "success",
                            showCancelButton: false
                        });
                    });
                });
            },
            trigger: function (id, state, concurrentExecutionDisallowed) {
                let that = this;
                if ((state !== 'Exit' && state !== 'Exiting') && concurrentExecutionDisallowed) {
                    swal({
                        title: "Job is running and disallower concurrent.",
                        type: "error",
                        showCancelButton: false
                    });
                } else {
                    hub.post("/swarm/v1.0/trigger/" + id, null, function () {
                        loadView(that);
                        swal({
                            title: "Trigger success",
                            type: "success",
                            showCancelButton: false
                        });
                    });
                }
            },
            remove: function (id) {
                let that = this;
                swal({
                    title: "Sure to remove this job?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.delete("/swarm/v1.0/job/" + id, function () {
                        loadView(that);
                    });
                });
            }
        }
    });

    function loadView(vue) {
        const url = '/swarm/v1.0/job?trigger=cron&keyword=' + vue.$data.keyword + '&page=' + vue.$data.page + '&size=' + vue.$data.size;
        hub.get(url, function (result) {
            vue.$data.jobs = result.data.result;
            vue.$data.total = result.data.total;
            vue.$data.page = result.data.page;
            vue.$data.size = result.data.size;

            hub.ui.initPagination('#pagination', result.data, function (page) {
                window.location.href = '/job/cron?keyword=' + vue.$data.keyword + '&page=' + page + '&size=' + vue.$data.size;
            });
        });
    }
});

