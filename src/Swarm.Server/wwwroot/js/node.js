$(function () {
    $('#nodeMenu').addClass('active');
    const vue = new Vue({
        el: '#view',
        data: {
            els: []
        }, mounted: function () {
            loadView(this);
        }
    });

    function loadView(v) {
        const url = 'swarm/v1.0/node?';
        hub.get(url, function (result) {
            v.$data.els = result.data;
        });
    }

    setInterval(loadView, 2000, vue);

});

