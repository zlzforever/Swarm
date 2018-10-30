let hub = {};
// TODO: AFTER PERMISSION SYSTEM FINISHED
hub.accessToken="%wTAd6IgcnQZauJKDTGdkmxyJgFxffXe";
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
            'SwarmAccessToken': hub.accessToken
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
            'SwarmAccessToken': hub.accessToken
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
            'SwarmAccessToken': hub.accessToken
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
            'SwarmAccessToken': hub.accessToken
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
    const total = option.total || 1;
    const size = option.size || 10;
    const page = option.page || 1;
    const totalPages = parseInt((total / size), 10) + ((total % size) > 0 ? 1 : 0) || 1;
    const currOption = {
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
    const filter = hub.queryString('filter');
    if (!filter) {
        return '';
    }
    const kvs = filter.split('|');
    let filters = {};
    for (i = 0; i < kvs.length; ++i) {
        const kv = kvs[i].split('::');
        filters[kv[0]] = kv[1];
    }
    return filters[key];
};
hub.formatDate = function (time, format = 'YY-MM-DD hh:mm:ss') {
    const date = new Date(time);

    const year = date.getFullYear(),
        month = date.getMonth() + 1,//月份是从0开始的
        day = date.getDate(),
        hour = date.getHours(),
        min = date.getMinutes(),
        sec = date.getSeconds();
    const preArr = Array.apply(null, Array(10)).map(function (elem, index) {
        return '0' + index;
    });

    return format.replace(/YY/g, year)
        .replace(/MM/g, preArr[month] || month)
        .replace(/DD/g, preArr[day] || day)
        .replace(/hh/g, preArr[hour] || hour)
        .replace(/mm/g, preArr[min] || min)
        .replace(/ss/g, preArr[sec] || sec);
};

$(function () {
    $(".dropdown-trigger").dropdown();
});
 
