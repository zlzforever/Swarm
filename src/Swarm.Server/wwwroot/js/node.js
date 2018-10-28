$(function () {
    new Vue({
        el: '#view',
        data: {
            nodes: [],
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
                    title: "Sure to exit this node?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.get("swarm/v1.0/node/" + connectionId + '?action=exit', function () {
                        loadView(that);
                    });
                });
            },
            remove: function (name, group) {
                let that = this;
                swal({
                    title: "Sure to remove this node?",
                    type: "warning",
                    showCancelButton: true
                }, function () {
                    hub.delete("swarm/v1.0/node/?name=" + name + "&group=" + group, function () {
                        loadView(that);
                    });
                });
            }
        }
    });

    function loadView(vue) {
        const url = 'swarm/v1.0/node?page=' + vue.$data.page + '&size=' + vue.$data.size;
        hub.get(url, function (result) {
            vue.$data.nodes = result.data.result;
            vue.$data.total = result.data.total;
            vue.$data.page = result.data.page;
            vue.$data.size = result.data.size;

            hub.ui.initPagination('#pagination', result.data, function () {
                window.location.href = 'node?page=' + vue.$data.page + '&size=' + vue.$data.size;
            });
        });
    }
});

