$(function () {
    $('#clientMenu').addClass('active');
    let vue = new Vue({
        el: '#view',
        data: {
            els: [],
            page: hub.queryString('page') || 1,
            size: hub.queryString('size') || 60,
            total: 0
        },
        mounted: function () {
            loadView(this);
        },
        methods: {
            exit: function (connectionId) {
                let that = this;
                swal({
                    title: "Sure to exit this client?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.delete("swarm/v1.0/client/" + connectionId, function () {
                        loadView(that);
                    });
                });
            },
            remove: function (id) {
                let that = this;
                swal({
                    title: "Sure to remove this client?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.delete("swarm/v1.0/client/?clientId=" + id, function () {
                        loadView(that);
                    });
                });
            }
        }
    });

    setInterval(loadView, 2000, vue);

    function loadView(vue) {
        const url = 'swarm/v1.0/client?page=' + vue.$data.page + '&size=' + vue.$data.size;
        hub.get(url, function (result) {
            vue.$data.els = result.data.result;
            vue.$data.total = result.data.total;
            vue.$data.page = result.data.page;
            vue.$data.size = result.data.size;

            hub.ui.initPagination('#pagination', result.data, function () {
                window.location.href = 'client?page=' + vue.$data.page + '&size=' + vue.$data.size;
            });
        });
    }
});

