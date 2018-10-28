var hub = {};

hub.queryString = function (name) {
    var result = location.search.match(new RegExp("[\?\&]" + name + "=([^\&]+)", "i"));
    if (result === null || result.length < 1) {
        return "";
    }
    return result[1];
};

hub.post = function (url, data, success, error) {
    $.ajax({
        url: url,
        headers: {
            'SwarmAccessToken': 'BBBBBBBB'
        },
        data: JSON.stringify(data),
        method: 'POST',
        dataType: 'json',
        contentType: 'application/json',
        success: function (result) {
            if (result && result.code === 200) {
                if (success) {
                    success(result);
                }
            } else {
                if (error) {
                    error(result);
                } else {
                    if (swal) {
                        if (result.msg) {
                            swal(result.msg, '', "error");
                        }
                    }
                }
            }
        },
        error: function (result) {
            if (error) {
                error(result);
            } else {
                if (swal) {
                    swal(result.msg, '', "error");
                }
            }
        }
    });
};

hub.get = function (url, success, error) {
    $.ajax({
        url: url,
        headers: {
            'SwarmAccessToken': 'BBBBBBBB'
        },
        method: 'GET',
        success: function (result) {
            if (result && result.code === 200) {
                if (success) {
                    success(result);
                }
            } else {
                if (error) {
                    error(result);
                } else {
                    if (swal) {
                        if (result.msg) {
                            swal(result.msg, '', "error");
                        }
                    }
                }
            }
        },
        error: function (result) {
            if (error) {
                error(result);
            } else {
                if (swal) {
                    swal(result.msg, '', "error");
                }
            }
        }
    });
};

hub.delete = function (url, success, error) {
    $.ajax({
        url: url,
        headers: {
            'SwarmAccessToken': 'BBBBBBBB'
        },
        method: 'DELETE',
        success: function (result) {
            if (result && result.code === 200) {
                if (success) {
                    success(result);
                }
            } else {
                if (error) {
                    error(result);
                } else {
                    if (swal) {
                        if (result.msg) {
                            swal(result.msg, '', "error");
                        }
                    }
                }
            }
        },
        error: function (result) {
            if (error) {
                error(result);
            } else {
                if (swal) {
                    swal(result.msg, '', "error");
                }
            }
        }
    });
};


hub.put = function (url, data, success, error) {
    $.ajax({
        url: url,
        headers: {
            'SwarmAccessToken': 'BBBBBBBB'
        },
        data: JSON.stringify(data),
        method: 'PUT',
        dataType: 'json',
        contentType: 'application/json',
        success: function (result) {
            if (result && result.code === 200) {
                if (success) {
                    success(result);
                }
            } else {
                if (error) {
                    error(result);
                } else {
                    if (swal) {
                        if (result.msg) {
                            swal(result.msg, '', "error");
                        }
                    }
                }
            }
        },
        error: function (result) {
            if (error) {
                error(result);
            } else {
                if (swal) {
                    swal(result.msg, '', "error");
                }
            }
        }
    });
};

hub.ui = {};

hub.ui.setBusy = function () {
    $("#loading").css("display", "");
};
hub.ui.clearBusy = function () {
    $("#loading").css("display", "none");
};

hub.pagers = {};
hub.ui.initPagination = function (query, option, click) {
    var total = option.total || 1;
    var size = option.size || 10;
    var page = option.page || 1;
    var totalPages = parseInt((total / size), 10) + ((total % size) > 0 ? 1 : 0) || 1;

    var currOption = {
        startPage: page,
        totalPages: totalPages,
        visiblePages: 10,
        first: "First",
        prev: "Previous",
        next: "Next",
        last: "Last",
        onPageClick: function (event, page) {
            if (!hub.pagers[query]) {
                hub.pagers[query] = true;
                return;
            }
            click(page);
        }
    };

    if (hub.pagers.hasOwnProperty(query)) {
        $(query).twbsPagination("destroy");
    }
    hub.pagers[query] = false;
    $(query).twbsPagination(currOption);
};

hub.getFilter = function (key) {
    var filter = hub.queryString('filter');
    if (!filter) {
        return '';
    }
    var kvs = filter.split('|');
    var filters = {};
    for (i = 0; i < kvs.length; ++i) {
        var kv = kvs[i].split('::');
        filters[kv[0]] = kv[1];
    }
    return filters[key];
};
hub.formatDate = function (time, format = 'YY-MM-DD hh:mm:ss') {
    var date = new Date(time);

    var year = date.getFullYear(),
        month = date.getMonth() + 1,//月份是从0开始的
        day = date.getDate(),
        hour = date.getHours(),
        min = date.getMinutes(),
        sec = date.getSeconds();
    var preArr = Array.apply(null, Array(10)).map(function (elem, index) {
        return '0' + index;
    });////开个长度为10的数组 格式为 00 01 02 03

    var newTime = format.replace(/YY/g, year)
        .replace(/MM/g, preArr[month] || month)
        .replace(/DD/g, preArr[day] || day)
        .replace(/hh/g, preArr[hour] || hour)
        .replace(/mm/g, preArr[min] || min)
        .replace(/ss/g, preArr[sec] || sec);

    return newTime;
};

$(function () {
    $(".dropdown-trigger").dropdown();
});
 
