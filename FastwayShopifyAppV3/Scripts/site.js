/*jslint browser: true*/
/*global document, $*/
document.getElementById("load").style.visibility = "hidden";
var addressString = document.getElementById("deliveryAddress").value;
if (addressString === "NoAddress") {
    document.getElementById("warning1").innerText = "NO DELIVERY ADDRESS FOUND, PLEASE CHECK WITH YOUR CUSTOMER AND INPUT ACCORDINGLY";
    document.getElementById("warning1").style.display = "block";
} else if (addressString === "MoreThanOne") {
    if (document.getElementById("shopUrl").value === "fastway-test-2.myshopify.com" ||
        document.getElementById("shopUrl").value === "mytreat-co-nz.myshopify.com" ||
        document.getElementById("shopUrl").value === "fastway-test-sa.myshopify.com") {
        enableBulkPrint();
        loadOrders();
    } else {
        disableAllFields();
        document.getElementById("warning3").innerText = "THIS IS CURRENTLY IN TEST, PLEASE TRY AGAIN LATER";
        document.getElementById("warning3").style.display = "block";
    }
} else if (addressString === "International") {
    disableAllFields();
    document.getElementById("warning3").innerText = "THIS IS AN INTERNATIONAL ADDRESS, PLEASE CONTACT YOUR LOCAL FASTWAY DEPOT FOR MORE INFORMATION";
    document.getElementById("warning3").style.display = "block";
} else {
    addPackage();
    var addressDetails = JSON.parse(addressString);
    if (addressDetails.Company !== "" && addressDetails.Company !== null) {
        document.getElementById("tbxCustomerName").value = addressDetails.Company;
        document.getElementById("tbContactName").value = addressDetails.Name;
    } else {
        document.getElementById("tbxCustomerName").value = addressDetails.Name;
    }
    document.getElementById("tbxDeliveryAddress").value = addressDetails.Address1;
    document.getElementById("tbxDeliveryAddress1").value = addressDetails.Address2;
    document.getElementById("tbxDeliveryCity").value = addressDetails.City;
    document.getElementById("tbxDeliveryPostcode").value = addressDetails.Zip;
    document.getElementById("tbxRegion").value = addressDetails.Province;
    document.getElementById("tbPhone").value = addressDetails.Phone;
    document.getElementById("tbEmail").value = document.getElementById("emailAddress").value;
    document.getElementById("tbInstructions").value = document.getElementById("specialInstruction").value;
}

function enableBulkPrint() {
    document.getElementById("leftMainPanel").style.display = "none";
    document.getElementById("rightMainPanel").style.display = "none";
    document.getElementById("packageTablePanel").style.display = "none";
    document.getElementById("mainControlPanel").style.display = "none";
    document.getElementById("pageTitle").style.display = "none";
    document.getElementById("pageTitleBulk").style.display = "block";
    document.getElementById("tblOrders").style.display = "block";
    document.getElementById("orderControlPanel").style.display = "block";
    document.getElementById("multiLabelControlPanel").style.display = "block";
}
function disableAllFields() {
    document.getElementById("leftMainPanel").style.display = "none";
    document.getElementById("rightMainPanel").style.display = "none";
    document.getElementById("packageTablePanel").style.display = "none";
    document.getElementById("mainControlPanel").style.display = "none";
    document.getElementById("pageTitle").style.display = "none";
    document.getElementById("pageTitleBulk").style.display = "none";
    document.getElementById("tblOrders").style.display = "none";
    document.getElementById("orderControlPanel").style.display = "none";
    document.getElementById("multiLabelControlPanel").style.display = "none";
}

function resetDetails(ele) {
    var item = $(ele).attr("id").replace("packaging", "");
    document.getElementById("items" + item).value = 1;
    document.getElementById("weight" + item).value = "";
    document.getElementById("totalCost" + item).value = "";
    document.getElementById("extra" + item).innerHTML = "";
}

function reCalculate(ele) {
    var item = $(ele).attr("id").replace("items", "");
    if (ele.value <= 0) {
        document.getElementById("totalCost" + item).value = "";
        document.getElementById("extra" + item).innerHTML = "";
    } else if (document.getElementById("weight" + item).value > 0 && document.getElementById("weight" + item).value !== "") {
        serviceQuery(document.getElementById("weight" + item));
        updateTotalCost();
    }
}

function serviceQuery(ele) {
    var item = $(ele).attr("id").replace("weight", "");
    if (ele.value <= 0 || ele.value === "") {
        document.getElementById("extra" + item).innerHTML = "";
        document.getElementById("totalCost" + item).value = "";
    } else {
        var weight = ele.value;
        if (document.getElementById("cubic" + item).value !== "") {
            var cubicWeight = document.getElementById("cubic" + item).value * 200;
            if (cubicWeight > weight) {
                weight = cubicWeight;
            }
        }
        var parcelDetails = {
            ShopUrl: document.getElementById("shopUrl").value,
            Address1: document.getElementById("tbxDeliveryAddress").value,
            Address2: document.getElementById("tbxDeliveryAddress1").value,
            Suburb: document.getElementById("tbxDeliveryCity").value,
            Postcode: document.getElementById("tbxDeliveryPostcode").value,
            Region: document.getElementById("tbxRegion").value,
            Weight: weight,
            Type: document.getElementById("packaging" + item).value
        };
        document.getElementById("load").style.visibility = "visible";
        $.ajax(
            {
                type: "POST",
                url: "/Home/LabelQuery",
                data: JSON.stringify(parcelDetails),
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    if (data.Error === "No Service Available") {
                        document.getElementById("extra" + item).innerHTML = data.Error;
                        document.getElementById("totalCost" + item).value = "";
                        updateTotalCost();
                    } else {
                        var result = (parseFloat(data.TotalCost) * parseFloat(document.getElementById("items" + item).value)).toFixed(2);
                        document.getElementById("totalCost" + item).value = result;
                        var extra = data.BaseLabelColour + "<br>";
                        if (data.Rural) {
                            extra = extra + "Rural <br>";
                        }
                        if (data.Excess > 0) {
                            extra = extra + data.Excess + " Excess";
                        }
                        document.getElementById("extra" + item).innerHTML = extra;
                        if (data.Saturday === "True") {
                            document.getElementById("satFooter").style.display = "block";
                        }
                        updateTotalCost();
                    }
                    document.getElementById("load").style.visibility = "hidden";
                },
                error: function () {
                    document.getElementById("extra" + item).innerHTML = "There was an error, please try again later";
                    document.getElementById("totalCost" + item).value = "";
                    updateTotalCost();
                    document.getElementById("load").style.visibility = "hidden";
                }
            }
        );
    }
}

function addPackage() {
    var itemTable = document.getElementById("tblItems"),
        i = itemTable.getElementsByTagName("tr").length;
    if (i >= 2) {
        var check = true;
        for (var k = 0; k < (i - 1); k++) {
            if (document.getElementById("totalCost" + k).value == "") check = false;
        }
        if (!check) {
            document.getElementById("warning3").innerText = "There are unused package(s)";
            document.getElementById("warning3").style.display = "block";
        }
        else {
            document.getElementById("warning3").style.display = "none";
            addRow(itemTable, i);
        }
    }
    else {
        document.getElementById("warning3").style.display = "none";
        addRow(itemTable, i);
    }
}

function addRow(table, rowNumber) {
    var row = table.insertRow(rowNumber);
    row.classList.add("consignmentItems");
    var cells = ["reference", "packaging", "items", "weight", "cubic", "length", "width", "height", "totalCost", "extra"];
    for (var j = 0; j < cells.length; j++) {
        var newCell = row.insertCell(-1);
        createCell(newCell, cells[j], rowNumber - 1);
    }
}

function createCell(cell, type, k) {
    if (type == "packaging") {
        cell.classList.add("col-xs-2");
        cell.style.padding = "1px";
        var select = document.createElement("select");
        select.setAttribute("id", "packaging" + k);
        select.classList.add("form-control");
        select.onchange = function () {
            document.getElementById("items" + k).value = 1;
            if (document.getElementById("packaging" + k).value != "Parcel") {
                var cCode = document.getElementById("countryCode").value;
                switch (cCode) {
                    case "1":
                        switch (document.getElementById("packaging" + k).value) {
                            case "SAT-NAT-A5":
                                document.getElementById("weight" + k).value = 0.5;
                                break;
                            case "SAT-NAT-A4":
                                document.getElementById("weight" + k).value = 1;
                                break;
                            case "SAT-NAT-A3":
                                document.getElementById("weight" + k).value = 3;
                                break;
                            case "SAT-NAT-A2":
                                document.getElementById("weight" + k).value = 5;
                                break;
                        }
                        break;
                    default:
                        document.getElementById("weight" + k).value = 5;
                        break;
                }
                document.getElementById("totalCost" + k).value = "";
                document.getElementById("extra" + k).innerHTML = "";
                document.getElementById("length" + k).value = 0;
                document.getElementById("height" + k).value = 0;
                document.getElementById("width" + k).value = 0;
                document.getElementById("cubic" + k).value = "";
                if (!document.getElementById("tbxCustomerName").disabled) {
                    disableAddress();
                }
                serviceQuery(document.getElementById("weight" + k));
            }
            else {
                document.getElementById("weight" + k).value = "";
                document.getElementById("totalCost" + k).value = "";
                document.getElementById("extra" + k).innerHTML = "";
                document.getElementById("length" + k).value = 0;
                document.getElementById("height" + k).value = 0;
                document.getElementById("width" + k).value = 0;
                document.getElementById("cubic" + k).value = "";
            }
            updateTotalCost();
        };
        var countryCode = document.getElementById("countryCode").value;
        switch (countryCode) {
            case "1":
                var options = ["Parcel", "Satchel A5", "Satchel A4", "Satchel A3", "Satchel A2"];
                break;
            case "24":
                var options = ["Parcel", "Satchel A5", "Satchel A4", "Satchel A3", "Satchel A2"];
                break;
            default:
                options = ["Parcel", "Satchel DL", "Satchel A5", "Satchel A4", "Satchel A3", "Satchel A2"];
                break;
        }
        for (var l = 0; l < options.length; l++) {
            var option = document.createElement("option");
            option.text = options[l];
            switch (options[l]) {
                case "Parcel":
                    option.value = "Parcel";
                    break;
                case "Satchel DL":
                    option.value = "SAT-NAT-E11";
                    break;
                case "Satchel A5":
                    option.value = "SAT-NAT-A5";
                    break;
                case "Satchel A4":
                    option.value = "SAT-NAT-A4";
                    break;
                case "Satchel A3":
                    option.value = "SAT-NAT-A3";
                    break;
                case "Satchel A2":
                    option.value = "SAT-NAT-A2";
                    break;
            }
            select.add(option);
        }
        cell.appendChild(select);
    }
    else if (type == "reference") {
        cell.classList.add("col-xs-2");
        cell.style.padding = "1px";
        var input = document.createElement("input");
        input.classList.add("form-control");
        input.setAttribute("id", "reference" + k);
        input.setAttribute("type", "text");
        cell.appendChild(input);
    }
    else if (type == "extra") {
        cell.classList.add("col-xs-1");
        cell.style.padding = "1px";
        var label = document.createElement("label");
        label.setAttribute("id", "extra" + k);
        label.setAttribute("type", "text");
        cell.appendChild(label);
    }
    else {
        cell.classList.add("col-xs-1");
        cell.style.padding = "1px";
        var input = document.createElement("input");
        input.classList.add("form-control");
        input.setAttribute("id", type + k);
        input.setAttribute("type", "text");
        if (type == "items") {
            input.value = "1";
            input.onchange = function () {
                if (document.getElementById("items" + k).value <= 0 || document.getElementById("items" + k).value == "") {
                    document.getElementById("totalCost" + k).value = "";
                    document.getElementById("extra" + k).innerHTML = "";
                    updateTotalCost();
                }
                else if (document.getElementById("weight" + k).value > 0 && document.getElementById("weight" + k).value != "") {
                    serviceQuery(document.getElementById("weight" + k));
                }
            };
        }
        else if (type == "weight") {
            input.setAttribute("type", "number");
            input.setAttribute("min", 0);
            input.onchange = function () {
                if (document.getElementById("weight" + k).value == 0 || document.getElementById("weight" + k).value == "") {
                    document.getElementById("extra" + k).innerHTML = "";
                    document.getElementById("totalCost" + k).value = "";
                    updateTotalCost();
                }
                else {
                    if (addressCheck()) {
                        document.getElementById("warning1").style.display = "none";
                        document.getElementById("warning2").style.display = "none";
                        if (!document.getElementById("tbxCustomerName").disabled) {
                            disableAddress();
                        }
                        serviceQuery(document.getElementById("weight" + k));
                    }
                    else {
                        document.getElementById("warning1").style.display = "none";
                        document.getElementById("warning2").style.display = "block";
                        document.getElementById("warning2").innerText = "Please check required address details";
                        document.getElementById("weight" + k).value = "";
                    }
                }
            };
        }
        else if (type == "cubic" || type == "totalCost") {
            input.disabled = true;
        }
        else {
            input.setAttribute("type", "number");
            input.setAttribute("min", 0);
            input.setAttribute("max", 200);
            input.value = 0;
            input.onchange = function () {
                if (document.getElementById("weight" + k).value != "" && document.getElementById("weight" + k).value != 0) {
                    if (document.getElementById("length" + k).value > 0 && document.getElementById("width" + k).value > 0 && document.getElementById("height" + k).value > 0) {
                        var le = document.getElementById("length" + k).value;
                        var we = document.getElementById("width" + k).value;
                        var he = document.getElementById("height" + k).value;
                        var cubic = le * we * he / 1000000;
                        if (document.getElementById("cubic" + k).value == "") {
                            document.getElementById("cubic" + k).value = cubic.toFixed(3);
                            if (cubic * 200 > document.getElementById("weight" + k).value) {
                                serviceQuery(document.getElementById("weight" + k));
                            }
                        }
                        else {
                            if (document.getElementById("cubic" + k).value * 200 > document.getElementById("weight" + k).value) {
                                if (cubic > document.getElementById("cubic" + k).value) {
                                    document.getElementById("cubic" + k).value = cubic.toFixed(3);
                                    serviceQuery(document.getElementById("weight" + k));
                                }
                                else if (cubic * 200 < document.getElementById("weight" + k).value) {
                                    document.getElementById("cubic" + k).value = cubic.toFixed(3);
                                    serviceQuery(document.getElementById("weight" + k));
                                }
                                else {
                                    document.getElementById("cubic" + k).value = cubic.toFixed(3);
                                }
                            }
                            else {
                                if (cubic * 200 > document.getElementById("weight" + k).value) {
                                    document.getElementById("cubic" + k).value = cubic.toFixed(3);
                                    serviceQuery(document.getElementById("weight" + k));
                                }
                                else {
                                    document.getElementById("cubic" + k).value = cubic.toFixed(3);
                                }
                            }
                        }
                    }
                    else {
                        if (document.getElementById("cubic" + k).value * 200 > document.getElementById("weight" + k).value) {
                            document.getElementById("cubic" + k).value = "";
                            serviceQuery(document.getElementById("weight" + k));
                        }
                        else {
                            document.getElementById("cubic" + k).value = "";
                        }
                    }
                }
                else {
                    if (document.getElementById("length" + k).value > 0 && document.getElementById("width" + k).value > 0 && document.getElementById("height" + k).value > 0) {
                        var le = document.getElementById("length" + k).value;
                        var we = document.getElementById("width" + k).value;
                        var he = document.getElementById("height" + k).value;
                        var cubic = le * we * he / 1000000;
                        document.getElementById("cubic" + k).value = cubic.toFixed(3);
                    }
                }
            };
        }
        cell.appendChild(input);
    }
}

function enableAddress() {
    document.getElementById("tbxCustomerName").disabled = false;
    document.getElementById("tbxDeliveryAddress").disabled = false;
    document.getElementById("tbxDeliveryAddress1").disabled = false;
    document.getElementById("tbxDeliveryCity").disabled = false;
    document.getElementById("tbxDeliveryPostcode").disabled = false;
    document.getElementById("tbxRegion").disabled = false;
}

function disableAddress() {
    document.getElementById("tbxCustomerName").disabled = true;
    document.getElementById("tbxDeliveryAddress").disabled = true;
    document.getElementById("tbxDeliveryAddress1").disabled = true;
    document.getElementById("tbxDeliveryCity").disabled = true;
    document.getElementById("tbxDeliveryPostcode").disabled = true;
    document.getElementById("tbxRegion").disabled = true;
}

function reset() {
    enableAddress();
    $('#tblItems tr').slice(1).remove();
    addPackage();
    document.getElementById('spanExGST').innerHTML = "0.00";
    document.getElementById('spanGST').innerHTML = "0.00";
    document.getElementById('spanTotal').innerHTML = "0.00";
    document.getElementById("satCbx").checked = false;
    document.getElementById("satFooter").style.display = "none";
}

function addressCheck() {
    var result = true;
    if (document.getElementById('tbxCustomerName').value == "" || document.getElementById('tbxDeliveryAddress').value == "" || document.getElementById('tbxDeliveryCity').value == "" || document.getElementById('tbxDeliveryPostcode').value == "") {
        result = false;
    }
    return result;
}

function printLabels() {
    if (document.getElementById('tbxDeliveryAddress').value.length > 30 || document.getElementById('tbxDeliveryAddress1').value.length > 30) {
        document.getElementById('warning3').innerText = "Max length for address details are 30 characters, please check and retry";
        document.getElementById('warning3').style.display = "block";
    }
    else {
        document.getElementById('warning3').style.display = "none";
        var itemTable = document.getElementById("tblItems");
        var n = itemTable.getElementsByTagName("tr").length;
        if (n >= 2) {
            var check = true;
            for (var k = 0; k < (n - 1); k++) {
                if (document.getElementById('totalCost' + k).value == "") check = false;
            }
            if (!check) {
                document.getElementById('warning3').innerText = "There are unused package(s)";
                document.getElementById('warning3').style.display = "block";
            }
            else {
                document.getElementById('warning3').style.display = "none";
                var phone = "";
                if (document.getElementById('tbMobilePhone').value != "") {
                    phone = document.getElementById('tbMobilePhone').value;
                }
                else {
                    phone = document.getElementById('tbPhone').value;
                }
                var deliveryDetails = {
                    Address1: document.getElementById('tbxDeliveryAddress').value,
                    Address2: document.getElementById('tbxDeliveryAddress1').value,
                    Suburb: document.getElementById('tbxDeliveryCity').value,
                    Postcode: document.getElementById('tbxDeliveryPostcode').value,
                    Company: document.getElementById('tbxCustomerName').value,
                    ContactName: document.getElementById('tbContactName').value,
                    ContactPhone: phone,
                    SpecialInstruction1: document.getElementById('tbInstructions').value,
                    ContactEmail: document.getElementById('tbEmail').value
                };
                var itemNumber = itemTable.getElementsByTagName("tr").length - 1;
                var saturday = document.getElementById("satCbx").checked;
                var packagingDetails = '[';
                for (var i = 0; i < itemNumber; i++) {
                    var extraString = document.getElementById("extra" + i).innerHTML;
                    if (extraString != "") {
                        packagingDetails += '{' +
                            '"Reference":"' + document.getElementById("reference" + i).value + '",' +
                            '"Items":"' + document.getElementById("items" + i).value + '",' +
                            '"Weight":"' + document.getElementById("weight" + i).value + '",' +
                            '"BaseLabel":"' + getService(extraString) + '"' +
                            '}';
                        if (i != itemNumber - 1) {
                            packagingDetails += ',';
                        }
                    }
                }
                var notifyFlag = "0";
                if (document.getElementById('notifyCbx').checked) {
                    notifyFlag = "1";
                }
                packagingDetails += ']';
                var printDetails = {
                    ShopUrl: document.getElementById('shopUrl').value,
                    DeliveryDetails: JSON.stringify(deliveryDetails),
                    PackagingDetails: packagingDetails,
                    Saturday: saturday
                };
                document.getElementById('load').style.visibility = "visible";
                $.ajax(
                    {
                        type: "POST",
                        url: "/Home/LabelPrintingV2",
                        data: JSON.stringify(printDetails),
                        dataType: "json",
                        contentType: "application/json; charset=utf-8",
                        success: function (data) {
                            window.open("data:application/pdf;base64, " + data.PdfBase64Stream, '', "height=600,width=800");
                            window.location = "OrdersFulfillment?shopUrl=" + document.getElementById("shopUrl").value + "&orderIds=" + document.getElementById("orderDetails").value + "&labelNumbers=" + data.Labels + "&notifyFlag=" + notifyFlag;
                            document.getElementById('load').style.visibility = "hidden";
                        },
                        error: function () {
                            document.getElementById('load').style.visibility = "hidden";
                            alert("There was an error!");
                        }
                    });
            }
        }
    }
}

function getService(str) {
    var spacePosition = str.indexOf('<');
    if (spacePosition === -1)
        return str;
    else
        return str.substr(0, spacePosition);
}

function updateTotalCost() {
    var itemTable = document.getElementById("tblItems");
    var i = itemTable.getElementsByTagName("tr").length;
    var total = 0;
    for (var j = 0; j < (i - 1); j++) {
        if (document.getElementById('totalCost' + j).value != "") {
            var float = parseFloat(document.getElementById('totalCost' + j).value);
            total += float;
        }
    }
    var GST = total * 15 / 100;
    var inGST = total + GST;
    document.getElementById('spanExGST').innerHTML = total.toFixed(2);
    document.getElementById('spanGST').innerHTML = GST.toFixed(2);
    document.getElementById('spanTotal').innerHTML = inGST.toFixed(2);
    document.getElementById('warning1').style.display = "none";
    document.getElementById('warning2').style.display = "none";
    document.getElementById('warning3').style.display = "none";
}

//Bulk Print
function loadOrders() {
    var orderTable = document.getElementById("tblOrders");
    var orderString = document.getElementById("ordersAddresses").value;
    var orderDetails = JSON.parse(orderString);
    for (var i = 0; i < orderDetails.length; i++) {
        loadOneOrder(orderTable, orderDetails[i],i+1);
    }
    if (document.getElementById("countryCode") != 6) {
        var x = document.getElementById("customType");
        x.remove(1);
    }
}

function loadOneOrder(table, order, row) {
    var row = table.insertRow(row);
    row.classList.add('consignmentItems');
    var cells = ["Name", "Address1", "Address2", "Suburb", "Postcode", "Label", "Rural", "Service"];
    for (var i = 0; i < cells.length; i++) {
        var newCell = row.insertCell(-1);
        createOrderCell(newCell, cells[i], row - 1, order);
    }
}

function createOrderCell(cell, type, i, order) {
    cell.style.padding = "1px";
    var input = document.createElement('input');
    input.classList.add('form-control');
    input.setAttribute('type', 'text');
    switch (type) {
        case "Name":
            cell.classList.add('col-xs-2');
            input.value = order.Name;
            break;
        case "Address1":
            cell.classList.add('col-xs-2');
            input.value = order.Address1;
            break;
        case "Address2":
            cell.classList.add('col-xs-1');
            input.value = order.Address2;
            break;
        case "Suburb":
            cell.classList.add('col-xs-1');
            input.value = order.City;
            break;
        case "Postcode":
            cell.classList.add('col-xs-1');
            input.value = order.Zip;
            break;
        case "Label":
            cell.classList.add('col-xs-2');
            input.disabled = true;
            break;
        case "Rural":
            cell.classList.add('col-xs-2');
            input.disabled = true;
            break;
        case "Service":
            cell.classList.add('col-xs-1');
            input.disabled = true;
            break;
    }
    if (input.value.length > 30) {
        input.style.backgroundColor = "#ff8080";
    }
    input.onchange = function () {
        if (input.value.length <= 30) {
            input.style.backgroundColor = "#ffffff";
        } else {
            input.style.backgroundColor = "#ff8080";
        }
    };
    cell.appendChild(input);
}

function selectCustomType() {
    var customParcel = document.getElementById("customType").value;
    if (customParcel == "Parcel") {
        document.getElementById("customWeight").value = "";
    } else {
        if (document.getElementById("countryCode") != 1) {
            document.getElementById("customWeight").value = 5;
        } else {
            switch (customParcel){
                case "SAT-NAT-A5":
                    document.getElementById("customWeight").value = 0.5;
                    break;
                case "SAT-NAT-A4":
                    document.getElementById("customWeight").value = 1;
                    break;
                case "SAT-NAT-A3":
                    document.getElementById("customWeight").value = 3;
                    break;
                case "SAT-NAT-A2":
                    document.getElementById("customWeight").value = 5;
                    break; 
            }

        }
        
    }
}

function queryLabels() {
    var orderTable = document.getElementById("tblOrders"),
        trs = orderTable.getElementsByTagName("tr"),
        rowCount = trs.length,
        weight = 0,
        weightInput = document.getElementById("customWeight").value,
        lengthInput = document.getElementById("customLength").value,
        widthInput = document.getElementById("customWidth").value,
        heightInput = document.getElementById("customHeight").value;

    if (lengthInput && widthInput && heightInput) {
        var cubicweight = lengthInput * lengthInput * heightInput / 5000;
        weight = Math.max(weightInput, cubicweight);
    } else {
        weight = weightInput;
    }

    if (weight) {
        document.getElementById("warning2").style.display = "none";
        for (var i = 1; i < rowCount; i++) {
            var tds = trs[i].getElementsByTagName("td");
            if (!tds[7].children[0].value) {
                //preparing data to query labels
                queryCallback(tds, i, weight);
            }
        }
    } else {
        document.getElementById("customWeight").backgroundColor = "#ff8080";
        document.getElementById("warning2").innerText = "Please select parcels type and weight";
        document.getElementById("warning2").style.display = "block";
    }

    
}

function queryCallback(tds, i, weight) {
    var parcelDetails = {
        ShopUrl: document.getElementById('shopUrl').value,
        Address1: tds[1].children[0].value,
        Address2: tds[2].children[0].value,
        Suburb: tds[3].children[0].value,
        Postcode: tds[4].children[0].value,
        Weight: weight,
        Instruction: document.getElementById('customInstruction').value,
        Type: document.getElementById('customType').value
    };
    document.getElementById('load').style.visibility = "visible";
    $.ajax(
        {
            type: "POST",
            url: "/Home/MultiLabelQuery",
            data: JSON.stringify(parcelDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                tds[5].children[0].value = data.BaseLabel;
                tds[6].children[0].value = data.RuralLabel;
                tds[7].children[0].value = data.Service;
            },
            error: function () {
                tds[5].children[0].value = "ERROR";
            }
        }
    );
    document.getElementById('load').style.visibility = "hidden";
}

function weightError() {
    var weightInput = document.getElementById("customWeight");
    if (weightInput.value === 0 || weightInput.value == null || weightInput.value == "") {
        weightInput.backgroundColor = "#ffffff";
    } else {
        weightInput.backgroundColor = "#ff8080";
    }
}

function bulkPrintLabels() {
    var orderTable = document.getElementById("tblOrders"),
        addresses = JSON.parse(document.getElementById("ordersAddresses").value),
        trs = orderTable.getElementsByTagName("tr"),
        rowCount = trs.length,
        weight = 0,
        weightInput = document.getElementById("customWeight").value,
        lengthInput = document.getElementById("customLength").value,
        widthInput = document.getElementById("customWidth").value,
        heightInput = document.getElementById("customHeight").value;

    if (lengthInput && widthInput && heightInput) {
        var cubicweight = lengthInput * lengthInput * heightInput / 5000;
        weight = Math.max(weightInput, cubicweight);
    } else {
        weight = weightInput;
    }

    var labelDetails = '[';
    for (var i = 1; i < rowCount; i++) {
        var tds = trs[i].getElementsByTagName("td");
        labelDetails += '{' +
            '"Company":"' + addresses[i - 1].Company + '",' +
            '"Name":"' + tds[0].children[0].value + '",' +
            '"Address1":"' + tds[1].children[0].value + '",' +
            '"Address2":"' + tds[2].children[0].value + '",' +
            '"Suburb":"' + tds[3].children[0].value + '",' +
            '"Postcode":"' + tds[4].children[0].value + '",' +
            '"BaseLabel":"' + tds[5].children[0].value + '",' +
            '"RuralLabel":"' + tds[6].children[0].value + '",' +
            '"Service":"' + tds[7].children[0].value + '"' +
            '}';
        if (i != rowCount - 1) {
            labelDetails += ',';
        }
    }
    labelDetails += "]";


    var multiLabelDetails = {
        ShopUrl : document.getElementById("shopUrl").value,
        Instruction : document.getElementById("customInstruction").value,
        Weight : weight,
        Labels : labelDetails,
        FullDetails: document.getElementById("ordersAddresses").value
    };

    document.getElementById('load').style.visibility = "visible";
    $.ajax(
        {
            type: "POST",
            url: "/Home/MultiLabelPrinting",
            data: JSON.stringify(multiLabelDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                document.getElementById('load').style.visibility = "hidden";
                window.open("data:application/pdf;base64, " + data.PdfBase64Stream, '', "height=600,width=800");
            },
            error: function () {
                document.getElementById('load').style.visibility = "hidden";
                alert("There was an error!");
            }
        }
    );
}
    
function fulfillOrders(){
    var orderTable = document.getElementById("tblOrders"),
        orderIds = document.getElementById("orderIds").value,
        trs = orderTable.getElementsByTagName("tr"),
        rowCount = trs.length,
        labelnumbers = "";

    orderIds = orderIds.replace("[", "");
    orderIds = orderIds.replace("]", "");

    for (var i = 1; i < rowCount; i++) {
        var tds = trs[i].getElementsByTagName("td");
        labelnumbers += tds[5].children[0].value;
        if (i !== rowCount - 1) {
            labelnumbers += ",";
        }
    }

    var notifyFlag = "0";
    if (document.getElementById('notifyBlukCbx').checked) {
        notifyFlag = "1";
    }

    window.location = "OrdersFulfillment?shopUrl=" + document.getElementById("shopUrl").value + "&orderIds=" + orderIds + "&labelNumbers=" + labelnumbers + "&notifyFlag=" + notifyFlag;

}
//End Bulk Print 