/*jslint browser: true*/
/*global document, $*/
document.getElementById("load").style.visibility = "hidden";
if (document.getElementById("shopUrl").value !== "fastway-test-2.myshopify.com") {
    document.getElementById("warning3").innerText = "INTERNATIONAL FEATURE IS IN TEST, PLEASE COME BACK LATER.";
    document.getElementById("warning3").style.display = "block";
    document.getElementById("leftMainPanel").style.display = "none";
    document.getElementById("rightMainPanel").style.display = "none";
} else {
    if (document.getElementById("addString").value === "MoreThanOne") {
        document.getElementById("warning3").innerText = "INTERNATIONAL ORDERS HAVE TO BE PROCESSED INDIVIDUALLY, PLEASE SELECT ONE ORDER ONLY";
        document.getElementById("warning3").style.display = "block";
        document.getElementById("leftMainPanel").style.display = "none";
        document.getElementById("rightMainPanel").style.display = "none";
    } else if (document.getElementById("addString").value === "") {
        document.getElementById("warning3").innerText = "THIS IS A DOMESTIC ORDER, PLEASE USE DOMESTIC LINK TO PROCESS";
        document.getElementById("warning3").style.display = "block";
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
                if (data.Suggestions == "Verified") {
                    document.getElementById("intLabelControlPanel").style.display = "block";
                    alert("Address is verified!");
                } else if (data.Suggestions == "[]") {
                    alert("We can't indentify the address, please check and try again!");
                } else {
                    alert(data.Suggestions);
                }
            },
            error: function () {
                document.getElementById("load").style.visibility = "hidden";
                alert("There was an error!");
            }
        }
    );
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
        Email: document.getElementById("tbEmail").value
    };

    $.ajax(
        {
            type: "POST",
            url: "/Home/CreateAramexShipment",
            data: JSON.stringify(addressDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.Shipments == "Error") {
                    alert("Error");
                } else if (data.Shipments == "[]") {
                    alert("No Shipment Created");
                } else {
                    alert(data.Shipments);
                    if (data.Labels.includes(",")) {
                        var labels = str.split(data.Labels);
                        for (var i = 0; i < labels.length; i++) {
                            window.open(labels[i], "_blank");
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