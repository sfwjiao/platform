(function () {
    angular.module("app").factory("queryFilterFactory", function () {
        return {
            filterOptions: {},
            pagination: {
                pageSize: 20,
                pageIndex: 1,
                numPages: 0,
                totalCount: 0,
                pageNumberIndex: 1,
                pageNumberCount: 5,
                start: function () {
                    return this.pageSize * (this.pageIndex - 1);
                },
                setPage: function (pageNo) {
                    if (pageNo > this.numPages) pageNo = this.numPages;
                    if (pageNo <= 0) pageNo = 1;
                    this.pageIndex = pageNo;
                }
            }

        }
    });
})();