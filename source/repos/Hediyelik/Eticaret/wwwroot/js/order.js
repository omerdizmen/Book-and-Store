var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    console.log(url);

    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
        console.log("inprocess");
    //} else {
    //    if (url.includes("completed")) {
    //        loadDataTable("completed");
    //        console.log("completed");
    //    } else {
    //        if (url.includes("pending")) {
    //            loadDataTable("pending");
    //        } else {
    //            loadDataTable("all");
    //        }
    //    }
    }
    else if (url.includes("completed")) {
        loadDataTable("completed");
           console.log("completed");
        
    }

});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url":"/Admin/Order/GetAll?status="+status
        },
        "columns": [
            { "data": "id", "width": "15%" },
            { "data": "name", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "15%" },   
            {
                "data": "id", "render": function (data) {
                    return `
                    <div class="w-75 btn-group" role="group">
                        <a href="/Admin/Order/Details?orderId=${data}" class="btn "><i class="bi bi-pencil-square"></i> Details</a>                        
                    </div>                                    
                    `
                },
                "width": "15%"
            }
            ]
        });
}