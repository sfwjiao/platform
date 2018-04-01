'use strict';

app.controller('DataTableCtrl', function($scope) {
    $scope.simpleTableOptions = {
        sAjaxSource: 'lib/jquery/datatable/data.json',
        aoColumns: [
            { data: 'users.first_name' },
            { data: 'users.last_name' },
            { data: 'users.phone' },
            { data: 'sites.name' }
        ],
        "sDom": "Tflt<'row DTTTFooter'<'col-sm-6'i><'col-sm-6'p>>",
        "iDisplayLength": 5,
        "oTableTools": {
            "aButtons": [
                "copy", "csv", "xls", "pdf", "print"
            ],
            "sSwfPath": "assets/swf/copy_csv_xls_pdf.swf"
        },
        "language": {
            "search": "",
            "sLengthMenu": "_MENU_",
            "oPaginate": {
                "sPrevious": "Prev",
                "sNext": "Next"
            }
        },
        "aaSorting": []
    };
});