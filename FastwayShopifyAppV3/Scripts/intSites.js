/*jslint browser: true*/
/*global document, $*/
document.getElementById("load").style.visibility = "hidden";
if (document.getElementById("shopUrl").value !== "fastway-test-2.myshopify.com") {
    document.getElementById("warning1").innerText = "INTERNATIONAL FEATURE IS IN TEST, PLEASE COME BACK LATER.";
    document.getElementById("warning1").style.display = "block";
    document.getElementById("leftMainPanel").style.display = "none";
    document.getElementById("rightMainPanel").style.display = "none";
} else {
    if (document.getElementById("addString").value === "MoreThanOne") {
        document.getElementById("warning1").innerText = "INTERNATIONAL ORDERS HAVE TO BE PROCESSED INDIVIDUALLY, PLEASE SELECT ONE ORDER ONLY";
        document.getElementById("warning1").style.display = "block";
        document.getElementById("leftMainPanel").style.display = "none";
        document.getElementById("rightMainPanel").style.display = "none";
    } else if (document.getElementById("addString").value === "") {
        document.getElementById("warning1").innerText = "THIS IS A DOMESTIC ORDER, PLEASE USE DOMESTIC LINK TO PROCESS";
        document.getElementById("warning1").style.display = "block";
        document.getElementById("leftMainPanel").style.display = "none";
        document.getElementById("rightMainPanel").style.display = "none";
    } else {
        var addString = document.getElementById("addString").value;

        var addressDetails = JSON.parse(addString);
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
        document.getElementById("tbxCountry").value = addressDetails.Country;
        document.getElementById("tbxCountryCode").value = addressDetails.CountryCode;
        document.getElementById("tbPhone").value = addressDetails.Phone;

        if (document.getElementById("emailString").value) {
            document.getElementById("tbEmail").value = document.getElementById("emailString").value;
        }
        document.getElementById("load").style.visibility = "visible";
        addressValidating();
    }
}

function addressValidating() {
    var addressDetails = {
        Address1: document.getElementById("tbxDeliveryAddress").value,
        Address2: document.getElementById("tbxDeliveryAddress1").value,
        Suburb: document.getElementById("tbxDeliveryCity").value,
        Postcode: document.getElementById("tbxDeliveryPostcode").value,
        CountryCode: document.getElementById("tbxCountryCode").value
    };
    $.ajax(
        {
            type: "POST",
            url: "/Home/AddressValidation",
            data: JSON.stringify(addressDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                document.getElementById("load").style.visibility = "hidden";
                if (data.Suggestions === "Verified") {
                    document.getElementById("intLabelControlPanel").style.display = "block";
                    document.getElementById("addressSelectorFound").style.display = "none";
                    addPackage();
                    alert("Address is verified!");
                } else if (data.Suggestions === "[]") {
                    alert("We can't indentify the address, please check and try again!");
                } else {
                    populateSelection(data.Suggestions);
                }
            },
            error: function () {
                document.getElementById("load").style.visibility = "hidden";
                alert("There was an error!");
            }
        }
    );
}

function populateSelection(suggestions) {
    var select = document.getElementById("suburbSelect");
    select.options.length = 0;
    var o = document.createElement("option");
    o.text = "Please Select ... ";
    select.add(o);
    var sugs = JSON.parse(suggestions);
    for (var i = 0; i < sugs.length; i++) {
        var option = document.createElement("option");
        option.value = sugs[i]["City"] + "/" + sugs[i]["PostCode"];
        option.text = sugs[i]["City"] + "/" + sugs[i]["PostCode"];
        select.add(option);
    }
    document.getElementById("addressSelectorFound").style.display = "block";
}

function repopulateSelections() {
    var selected = document.getElementById("suburbSelect").value;

    if (selected) {
        var res = selected.split("/");
        document.getElementById("tbxDeliveryCity").value = res[0];
        document.getElementById("tbxDeliveryPostcode").value = res[1];
        addressValidating()
    }
}


function addPackage() {
    var itemTable = document.getElementById("tblItems"),
        i = itemTable.getElementsByTagName("tr").length;
        document.getElementById("warning3").style.display = "none";
        addRow(itemTable, i);
}

function addRow(table, rowNumber) {
    var row = table.insertRow(rowNumber);
    row.classList.add("consignmentItems");
    var cells = ["reference", "content", "items", "weight", "value", "extra"];
    for (var j = 0; j < cells.length; j++) {
        var newCell = row.insertCell(-1);
        createCell(newCell, cells[j], rowNumber - 1);
    }
}

function createCell(cell, type, k) {
    if (type === "content") {
        cell.classList.add("col-xs-2");
        cell.style.padding = "1px";
        var select = document.createElement("select");
        select.setAttribute("id", "content" + k);
        select.classList.add("form-control");
        var options = ["Document", "Parcel"];
        for (var l = 0; l < options.length; l++) {
            var option = document.createElement("option");
            option.text = options[l];
            if (options[l] === "Document") {
                option.value = "PDX";
            } else {
                option.value = "PPX";
            }
            select.add(option);
        }
        cell.appendChild(select);
    } else if (type === "reference") {
        cell.classList.add("col-xs-2");
        cell.style.padding = "1px";
        var input = document.createElement("input");
        input.classList.add("form-control");
        input.setAttribute("id", "reference" + k);
        input.setAttribute("type", "text");
        cell.appendChild(input);
    } else if (type === "extra") {
        cell.classList.add("col-xs-1");
        cell.style.padding = "1px";
        var label = document.createElement("label");
        label.setAttribute("id", "extra" + k);
        label.setAttribute("type", "text");
        cell.appendChild(label);
    } else {
        cell.classList.add("col-xs-1");
        cell.style.padding = "1px";
        var input = document.createElement("input");
        input.classList.add("form-control");
        input.setAttribute("id", type + k);
        input.setAttribute("type", "number");
        input.setAttribute("min", 0);
        if (type === "value") {
            input.value = "0";
        } else {
            input.value = "1";
            input.setAttribute("maxlength", 3);
        }
        cell.appendChild(input);
    }
}



function printAramexLabels() {
    var addressDetails = {
        CompanyName: document.getElementById("tbxCustomerName").value,
        Address1: document.getElementById("tbxDeliveryAddress").value,
        Address2: document.getElementById("tbxDeliveryAddress1").value,
        Suburb: document.getElementById("tbxDeliveryCity").value,
        Postcode: document.getElementById("tbxDeliveryPostcode").value,
        CountryCode: document.getElementById("tbxCountryCode").value,
        Phone: document.getElementById("tbPhone").value,
        Email: document.getElementById("tbEmail").value,
        Descriptions: document.getElementById("tbShipmentDescriptions").value
    };

    $.ajax(
        {
            type: "POST",
            url: "/Home/CreateAramexShipment",
            data: JSON.stringify(addressDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            async: false,
            success: function (data) {
                if (data.Errors) {
                    alert(data.Errors);
                } else if (data.Shipments == "[]") {
                    alert("No Shipment Created");
                } else {
                    if (data.Labels.includes(",")) {
                        var labels = data.Labels.split(",");
                        for (var i = 0; i < labels.length; i++) {
                            window.open(labels[i], "_blank", "height=600,width=800");
                        }
                    } else {
                        window.open(data.Labels, "_blank", "height=600,width=800");
                    }
                }
            },
            error: function () {
                alert("There was an error!");
            }
        }
    );
}
