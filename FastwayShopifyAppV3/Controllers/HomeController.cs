using FastwayShopifyAppV3.Engine;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using static FastwayShopifyAppV3.Engine.FastwayAPI;
using Newtonsoft.Json.Linq;
using PdfSharp.Pdf;
using System.IO;
using FastwayShopifyAppV3.AramexShipping;

namespace FastwayShopifyAppV3.Controllers
{
    public class HomeController : Controller
    {
        public object ShopifyApi { get; private set; }

        string appUrl = Engine.ShopifyAppEngine.ApplicationUrl;
        string apiKey = Engine.ShopifyAppEngine.ShopifyApiKey;
        string secretKey = Engine.ShopifyAppEngine.ShopifySecretKey;

        /// <summary>
        /// Writing shopUrl in hidden field 'shopUrl' and return View()
        /// </summary>
        /// <returns>shopUrl</returns>
        public ActionResult Index(string shopUrl)
        {
            Response.Write("<input id='shopUrl' type='hidden' value='" + shopUrl + "'>");
            return View();
        }
        /// <summary>
        /// Writing shopUrl in hidden field 'shopUrl' and return View()
        /// <returns></returns>
        public ActionResult Installed(string shopUrl)
        {
            Response.Write("<input id='shopUrl' type='hidden' value='" + shopUrl + "'>");
            return View();
        }

        /// <summary>
        /// Listen to calls from shopify admin page, receive shop url and order ids. Parse orders and pass details to View()
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> NewConsignment(string shop, string[] ids)
        {
            //Get order numbers
            string orders = Request.QueryString["ids[]"];
            //required objects
            List<string> orderIds = new List<string>();//list of orderIds
            List<string> processingOrderIds = new List<string>();//list of orderIds
            List<string> processingOrderPreferences = new List<string>();//list of orderIds
            List<Order> orderDetails = new List<Order>();//list of order details
            List<ShopifySharp.Address> deliveryAddress = new List<ShopifySharp.Address>();//list of delivery details
            List<string> emails = new List<string>();//list of emails addresses

            if (orders == null)//No order select (in case customer reach this page from outside of their admin page
            {
                return View();//NOTE: might need to redirect them to their admin page
            }

            if (orders.Contains(','))//if there are more than one order
            {
                //get a list of order numbers received
                orderIds = orders.Split(',').ToList();
            }
            else
            {
                //get a list of ONE order number
                orderIds.Add(orders);
            }
            //DB connection required to query store details
            DbEngine conn = new DbEngine();
            //Get Shopify Token to access Shopify API
            string token = conn.GetStringValues(shop, "ShopifyToken");
            int cCode = conn.GetIntergerValues(shop, "CountryCode");
            List<CustomParcel> lCustomParcels = conn.GetCustomParcel(shop);
            //ShopifyAPI object
            ShopifyAPI api = new ShopifyAPI();
            //foreach order number from list, get a list of delivery details
            for (int i = 0; i < orderIds.Count; i++)
            {
                //get the order with order number
                Order k = await api.GetOrder(shop, token, orderIds[i]);
                if (k.ShippingAddress != null)
                {//if shipping address exist, add to list of delivery details
                    bool check = true;
                    //check if order is international
                    switch (cCode)
                    {
                        case 6:
                            if (k.ShippingAddress.Country != "New Zealand")
                            {
                                check = false;
                            }
                            break;
                        case 1:
                            if (k.ShippingAddress.Country != "Australia")
                            {
                                check = false;
                            }
                            break;
                    }
                    //check if duplicate addresses
                    if (deliveryAddress.Count > 0)
                    {
                        for (var l = 0; l < deliveryAddress.Count; l++)
                        {
                            if (deliveryAddress[l].Name == k.ShippingAddress.Name)
                            {
                                check = false;
                                break;
                            }
                        }
                    }
                    if (check == true)
                    {
                        deliveryAddress.Add(k.ShippingAddress);
                        emails.Add(k.Email);
                        processingOrderIds.Add(orderIds[i]);
                        processingOrderPreferences.Add(k.OrderNumber.ToString());
                    }
                }
                orderDetails.Add(k);//add order details into list of order details
            }

            ////jsonserialiser object to form json from list
            JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
            ////creating json about orders to pass back to View()
            //string orderJson = jsonSerialiser.Serialize(orderDetails);
            string stringCP = jsonSerialiser.Serialize(lCustomParcels);

            string stringOrderReference = string.Join(",",processingOrderPreferences);


            //creating json about delivery address to pass back to View()
            string address = "";
            string note = "";
            string addresses = "";
            string strOrderIds = "";
            if (deliveryAddress.Count == 0)
            {//No delivery address found
                address = "NoAddress";
            }
            else if (deliveryAddress.Count > 1)
            {//More than one addresses found
                address = "MoreThanOne";
                addresses = jsonSerialiser.Serialize(deliveryAddress);
                strOrderIds = string.Join(",", processingOrderIds);
            }
            else
            {//one address
                address = jsonSerialiser.Serialize(deliveryAddress[0]);
                switch (cCode)
                {
                    case 6:
                        if (deliveryAddress[0].Country !="New Zealand")
                        {
                            address = "International";
                        }
                        break;
                    case 1:
                        if (deliveryAddress[0].Country != "Australia")
                        {
                            address = "International";
                        }
                        break;
                }
                for (int i = 0; i < orderDetails.Count; i++)
                {
                    if (orderDetails[i].Note != "")
                    {
                        note += orderDetails[i].Note;
                    }
                }
            }


            Response.Write("<input id='shopUrl' type='hidden' value='" + shop + "'>");//passing shopUrl to View() for further queries
            Response.Write("<input id='cpStrings' type='hidden' value='" + stringCP + "'>");//passing Custom Parcels if any to View() for further queries
            Response.Write("<input id='orderReference' type='hidden' value='" + stringOrderReference + "'>");//passing order reference number if any to View() for further queries
            Response.Write("<input id='countryCode' type='hidden' value='" + cCode + "'>");//passing countryCode to View() for further queries
            Response.Write("<input id='orderDetails' type='hidden' value='" + orders + "'>");//passing orderIds to View() for further queries
            Response.Write("<input id='deliveryAddress' type='hidden' value='" + address + "'>");//passing address to View() for further queries
            Response.Write("<input id='ordersAddresses' type='hidden' value='" + addresses + "'>");//passing ordersdetails to View() for further queries
            Response.Write("<input id='orderIds' type='hidden' value='" + strOrderIds + "'>");//passing ordersdetails to View() for further queries
            if (emails.Count >= 1) Response.Write("<input id='emailAddress' type='hidden' value='" + string.Join(",", emails) + "'>");//passing email address
            if (note != "") Response.Write("<input id='specialInstruction' type='hidden' value='" + note + "'>");
            return View();

        }

        /// <summary>
        /// Listen to calls from shopify admin page, receive shop url and order ids. Parse orders and pass details to View()
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> NewInternationalConsignment(string shop, string[] ids)
        {
            //Get order numbers
            string orders = Request.QueryString["ids[]"];

            if (orders.Contains(','))
            {
                Response.Write("<input id='shopUrl' type='hidden' value='" + shop + "'>");//passing shopUrl to View() for further queries
                Response.Write("<input id='addString' type='hidden' value='" + "MoreThanOne" + "'>");//passing shopUrl to View() for further queries
                return View();
            } else
            {

                //DB connection required to query store details
                DbEngine conn = new DbEngine();
                //Get Shopify Token to access Shopify API
                string token = conn.GetStringValues(shop, "ShopifyToken");
                int cCode = conn.GetIntergerValues(shop, "CountryCode");
                //ShopifyAPI object
                ShopifyAPI api = new ShopifyAPI();

                Order o = await api.GetOrder(shop, token, orders);

                string address = "";
                string email = "";
                JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();


                if (o.ShippingAddress.Country != "New Zealand")
                {
                    address = jsonSerialiser.Serialize(o.ShippingAddress);
                    email = o.Email;
                }

                Response.Write("<input id='shopUrl' type='hidden' value='" + shop + "'>");//passing shopUrl to View() for further queries
                Response.Write("<input id='emailString' type='hidden' value='" + email + "'>");//passing address string to View() for further queries
                Response.Write("<input id='addString' type='hidden' value='" + address + "'>");//passing address string to View() for further queries
                return View();
            }

        }


        /// <summary>
        /// Listen to query from NewConsignment controler, query and response with available services
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult LabelQuery(string ShopUrl, string Address1, string Address2, string Suburb, string Postcode, string Region, float Weight, string Type)
        {
            //DB connection to query store details
            DbEngine newDB = new DbEngine();
            //get store details with provided url
            StoreRecord storeDetails = newDB.GetShopRecord(ShopUrl);
            //Labeldetails entity to query
            Labeldetails label = new Labeldetails();
            //populate store details for query
            label.apiKey = storeDetails.FastwayApiKey;
            label.fromAddress1 = storeDetails.StoreAddress1;
            label.fromCity = storeDetails.Suburb;
            label.fromPostcode = storeDetails.Postcode;
            //populate delivery details for query
            label.toAddress1 = Address1;
            label.toAddress2 = Address2;
            label.toCity = Suburb;
            label.toPostcode = Postcode;
            //populate parcel details for query
            label.weight = (double)Weight;
            label.countryCode = storeDetails.CountryCode;
            //FastwayAPI object for service query
            FastwayAPI newApiCall = new FastwayAPI();
            //Call fastway API and receive back a list of available service
            List<UsableLabel> services = newApiCall.ServiceQuery(label);
            //UsableLabel entity to respond
            UsableLabel service = new UsableLabel();

            try
            {
                if (services.First().CostexgstTotalChargeToEndUser != 0)
                {//if no service found
                    if (services.Count == 1 && Type != "Parcel")
                    {//no service and type was "Parcel"
                        return Json(new
                        {//return an Error code
                            Error = "No Service Available"
                        });
                    }
                    else//service(s) available
                    {
                        if (Type == "Parcel")
                        {//type was "Parcel", assign parcel option to response json
                            service = services.First();
                        }
                        else
                        {//type was NOT "Parcel", get service based on value of Type
                            if (Type == "SAT-NAT-A3")
                            {
                                if (services.First().BaseLabelColour == "BROWN")
                                {
                                    service = services[services.FindIndex(a => a.BaseLabelColour == "SAT-LOC-A3")];
                                }
                                else
                                {
                                    service = services[services.FindIndex(a => a.BaseLabelColour == Type)];
                                }
                            }
                            else
                            {
                                service = services[services.FindIndex(a => a.BaseLabelColour == Type)];
                            }

                        }
                        return Json(new
                        {//return details about availabel service
                            BaseLabelColour = service.BaseLabelColour,
                            TotalCost = service.CostexgstTotalChargeToEndUser,
                            Rural = service.RuralLabelCostExgst > 0 ? true : false,
                            Excess = service.ExcessLabelCount,
                            Saturday = services.First().Saturday
                        });
                    }
                }
                else
                {
                    return Json(new
                    {//Error code from Fastway NOTE: will need to handle different type of error HERE
                        Error = "No Service Available"
                    });
                }
            }
            catch (Exception e)
            {
                //general error code Note: will need to handle these
                throw e;
            }

        }

        /// <summary>
        /// V2 of LabelPrinting using generate-label call instead of generate-label-for-labelnumber
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult LabelPrintingV2(string ShopUrl, string DeliveryDetails, string PackagingDetails, bool Saturday)
        {
            //labeldetails object to call Fastway API
            Labeldetails label = new Labeldetails();
            //DB connection to query sender details
            DbEngine conn = new DbEngine();
            label.apiKey = conn.GetStringValues(ShopUrl, "FastwayApiKey");
            //assign sender details
            label.fromAddress1 = conn.GetStringValues(ShopUrl, "StoreAddress1");
            label.fromPostcode = conn.GetStringValues(ShopUrl, "Postcode");
            label.fromCity = conn.GetStringValues(ShopUrl, "Suburb");
            label.fromCompany = conn.GetStringValues(ShopUrl, "StoreName");
            label.countryCode = conn.GetIntergerValues(ShopUrl, "CountryCode");
            label.fromPhone = conn.GetStringValues(ShopUrl, "Phone");

            

            //parse delivery details
            JObject d = JObject.Parse(DeliveryDetails);
            //assign receiver details
            label.toAddress1 = d["Address1"].ToString();
            label.toAddress2 = d["Address2"].ToString();
            label.toPostcode = d["Postcode"].ToString();
            label.toCity = d["Suburb"].ToString();

            label.specialInstruction1 = d["SpecialInstruction1"].ToString();

            if (d["Company"].ToString() != "" && d["Company"].ToString() != "null" && d["Company"].ToString() != null)
            {
                label.toCompany = d["Company"].ToString();
                label.toContactName = d["ContactName"].ToString();
            }
            else
            {
                label.toCompany = d["ContactName"].ToString();
            }

            label.toContactPhone = d["ContactPhone"].ToString();
            //pull through email address for expect messaging
            label.toEmail = d["ContactEmail"].ToString();

            //parse packaging details
            JArray p = JArray.Parse(PackagingDetails);
            //list of labelDetails that hold the labels being used
            List<Labeldetails> labelDetails = new List<Labeldetails>();
            List<string> labelNumbers = new List<string>();



            for (int i = 0; i < p.Count; i++)
            {
                for (int j = 0; j < (int)p[i]["Items"]; j++)
                {
                    //package details
                    label.weight = (double)p[i]["Weight"];
                    label.labelColour = p[i]["BaseLabel"].ToString();
                    label.reference = p[i]["Reference"].ToString();
                    label.saturday = Saturday;

                    //new fastwayAPI object to query
                    FastwayAPI getLabel = new FastwayAPI();
                    //get label with V2 method
                    Labeldetails l = new Labeldetails();
                    l = getLabel.LabelQueryV2(label);
                    labelDetails.Add(l);
                    labelNumbers.Add(l.labelNumber);
                }
            }

            PdfDocument doc = new PdfDocument();

            if (labelDetails.Count > 0)
            {
                FastwayAPI getBase = new FastwayAPI();
                doc = getBase.PrintLabels(labelDetails, doc);
            }

            MemoryStream pdfStream = new MemoryStream();
            doc.Save(pdfStream, false);
            byte[] pdfBytes = pdfStream.ToArray();

            var pdfString = Convert.ToBase64String(pdfBytes);

            try
            {
                return Json(new
                {//return status success
                    Labels = String.Join(",", labelNumbers),
                    PdfBase64Stream = pdfString
                });
            }
            catch (Exception e)
            {//error
                throw e;
            }
        }

        /// <summary>
        /// Controller to fulfill orders as required.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> OrdersFulfillment(string ShopUrl, string OrderIds, string LabelNumbers, string NotifyFlag)
        {
            //Db connection to query store details
            DbEngine conn = new DbEngine();
            //get store Shopify's token to access API
            string token = conn.GetStringValues(ShopUrl, "ShopifyToken");

            if (!OrderIds.Contains(","))
            {//If there in only one order
                //new ShopifyAPI objects to query
                ShopifyAPI newApi = new ShopifyAPI();
                //get fulfillment ids (if existed on this order)
                string fulfillments = await newApi.GetFulfillment(ShopUrl, token, OrderIds);

                if (fulfillments == "")
                {//if not fulfilled yet, fulfill it
                    string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, OrderIds, LabelNumbers, NotifyFlag);
                    //string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, OrderIds, LabelNumbers);
                }
                else if (!fulfillments.Contains(","))
                {//if fulfilled, update tracking information
                    string fulfillmentId = await newApi.UpdateFulfillment(ShopUrl, token, OrderIds, fulfillments, LabelNumbers, NotifyFlag);
                }
                else
                {
                    List<string> fulfillmentIds = fulfillments.Split(',').ToList();
                    foreach (string fId in fulfillmentIds)
                    {
                        string fulfillmentId = await newApi.UpdateFulfillment(ShopUrl, token, OrderIds, fId, LabelNumbers, NotifyFlag);
                    }
                }
            }
            else
            {//more than one order
                //get the list of orderIds
                List<string> orderIds = OrderIds.Split(',').ToList();
                //bulk print
                List<string> labelNumbers = LabelNumbers.Split(',').ToList();

                //foreach (string id in orderIds)
                for (var i=0; i<orderIds.Count;i++)
                {//loop through orderIds list and fulfill/update fulfillment as per required
                    ShopifyAPI newApi = new ShopifyAPI();
                    string fulfillments = await newApi.GetFulfillment(ShopUrl, token, orderIds[i]);
                    if (fulfillments == "")
                    {//if not fulfilled yet, fulfill it
                        string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, orderIds[i], labelNumbers[i], NotifyFlag);
                        //string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, id, LabelNumbers);
                    }
                    else if (!fulfillments.Contains(","))
                    {//if fulfilled, update tracking information
                        string fulfillmentId = await newApi.UpdateFulfillment(ShopUrl, token, orderIds[i], fulfillments, labelNumbers[i], NotifyFlag);
                    }
                    else
                    {
                        List<string> fulfillmentIds = fulfillments.Split(',').ToList();
                        foreach (string fId in fulfillmentIds)
                        {
                            string fulfillmentId = await newApi.UpdateFulfillment(ShopUrl, token, orderIds[i], fId, labelNumbers[i], NotifyFlag);
                        }
                    }
                }
            }

            Response.Write("<input id='shopUrl' type='hidden'  value='" + ShopUrl + "'>");//passing shopUrl to View() for further queries
            return View();
        }
        /// <summary>
        /// Controller to retrieve customer preferences
        /// </summary>
        /// <returns></returns>
        public ActionResult Preferences(string shopUrl)
        {
            //object to get store data from DB
            StoreRecord details = new StoreRecord();
            //db object to query
            DbEngine conn = new DbEngine();
            //get store data
            details = conn.GetShopRecord(shopUrl);
            //from store data forming json for front-end
            JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
            string storeDetails = jsonSerialiser.Serialize(details);

            Response.Write("<input id='shopUrl' type='hidden' value='" + shopUrl + "'>");
            Response.Write("<input id='shopDetails' type='hidden' value='" + storeDetails + "'>");
            return View();
        }
        /// <summary>
        /// Listen to query from front-end to update preferences and response accordingly
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UpdatePreferences(string ShopUrl, string StoreName, string StoreAddress1, string Suburb, string Postcode,string Phone, string ApiKey, int CountryCode)
        {
            //update values
            DbEngine conn = new DbEngine();
            conn.UpdateStringValues(ShopUrl, "FastwayApiKey", ApiKey);
            conn.UpdateStringValues(ShopUrl, "StoreName", StoreName);
            conn.UpdateStringValues(ShopUrl, "StoreAddress1", StoreAddress1);
            conn.UpdateStringValues(ShopUrl, "Suburb", Suburb);
            conn.UpdateStringValues(ShopUrl, "Phone", Phone);
            conn.UpdateStringValues(ShopUrl, "Postcode", Postcode);
            conn.UpdateIntergerValues(ShopUrl, "CountryCode", CountryCode);
            //from store data forming json for front-end
            StoreRecord details = conn.GetShopRecord(ShopUrl);
            JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
            string storeDetails = jsonSerialiser.Serialize(details);

            try
            {
                return Json(new
                {//return status success
                    Updated = storeDetails
                });
            }
            catch (Exception e)
            {//error
                throw e;
            }



        }
        /// <summary>
        /// Query Labels for multi print
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult MultiLabelQuery(string ShopUrl, string Address1, string Address2, string Suburb, string Postcode, float Weight, string Instruction, string Type)
        {
            //DB connection to query store details
            DbEngine newDB = new DbEngine();
            //get store details with provided url
            StoreRecord storeDetails = newDB.GetShopRecord(ShopUrl);
            //Labeldetails entity to query
            Labeldetails label = new Labeldetails();
            //populate store details for query
            label.apiKey = storeDetails.FastwayApiKey;
            label.fromAddress1 = storeDetails.StoreAddress1;
            label.fromCity = storeDetails.Suburb;
            label.fromPostcode = storeDetails.Postcode;
            //populate delivery details for query
            label.toAddress1 = Address1;
            label.toAddress2 = Address2;
            label.toCity = Suburb;
            label.toPostcode = Postcode;
            //populate parcel details for query
            label.weight = (double)Weight;
            label.countryCode = storeDetails.CountryCode;

            List<string> labelNumbers = new List<string>();
            string destRF = "";
            
            try
            {
                if (Type != "Parcel")
                {
                    if (Weight > 5)
                    {
                        labelNumbers.Add("No Service Found");
                    }
                    else
                    {
                        label.specialInstruction1 = Instruction;

                        //new fastwayAPI object to query
                        FastwayAPI getLabel = new FastwayAPI();
                        //get label with V2 method
                        Labeldetails l = new Labeldetails();

                        if (Type == "SAT-NAT-A3")
                        {
                            List<UsableLabel> services = getLabel.ServiceQuery(label);
                            if (services.First().BaseLabelColour == "BROWN")
                            {
                                label.labelColour = services[services.FindIndex(a => a.BaseLabelColour == "SAT-LOC-A3")].BaseLabelColour;
                            }
                            else
                            {
                                label.labelColour = Type;
                            }
                        }
                        else
                        {
                            label.labelColour = Type;
                        }
                        l = getLabel.LabelQueryV2(label);
                        labelNumbers.Add(l.labelNumber);
                        labelNumbers.Add(l.ruralNumber);
                        destRF = l.toRfName;
                    }
                }
                else
                {
                    //new fastwayAPI object to query
                    FastwayAPI getLabel = new FastwayAPI();
                    //get label with V2 method
                    Labeldetails l = new Labeldetails();
                    List<UsableLabel> services = getLabel.ServiceQuery(label);
                    label.labelColour = services.First().BaseLabelColour;
                    l = getLabel.LabelQueryV2(label);
                    labelNumbers.Add(l.labelNumber);
                    labelNumbers.Add(l.ruralNumber);
                    destRF = l.toRfName;
                }
            }
            catch (Exception)
            {
                labelNumbers.Add("No Service Found");
            }
            return Json(new
            {//return details about availabel service
                BaseLabel = labelNumbers[0],
                RuralLabel = labelNumbers[1],
                Service = label.labelColour,
                DestRF = destRF
            });
        }

        /// <summary>
        /// Listen to query from NewConsignment Controller and response with labels acquired from Fastway API
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult MultiLabelPrinting(string ShopUrl, string Instruction, float Weight, string Labels, string Reference, string FullDetails)
        {
            //DB connection to query store details
            DbEngine newDB = new DbEngine();
            //get store details with provided url
            StoreRecord storeDetails = newDB.GetShopRecord(ShopUrl);
            //Labeldetails entity to query
            Labeldetails label = new Labeldetails();
            ///populate store details for query
            label.apiKey = storeDetails.FastwayApiKey;
            label.fromCompany = storeDetails.StoreName;

            if (storeDetails.Phone!="" && storeDetails.Phone != null && storeDetails.Phone != "null")
            {
                label.fromPhone = storeDetails.Phone;
            }
            label.fromAddress1 = storeDetails.StoreAddress1;
            label.fromCity = storeDetails.Suburb;
            label.fromPostcode = storeDetails.Postcode;
            label.weight = Math.Ceiling(Weight * 100)/100;
            label.specialInstruction1 = Instruction;
            label.countryCode = storeDetails.CountryCode;

            //parsing the labels addresses details
            JArray ls = JArray.Parse(Labels);
            JArray fds = JArray.Parse(FullDetails);

            List<string> references = Reference.Split(',').ToList();

            List<Labeldetails> labelDetails = new List<Labeldetails>();

            for (var i = 0; i < ls.Count; i++)
            {
                if (ls[i]["BaseLabel"].ToString() != "")
                {
                    Labeldetails l = label;
                    if (ls[i]["Company"].ToString() != "" && ls[i]["Company"].ToString() != null && ls[i]["Company"].ToString() != "null")
                    {
                        l.toCompany = ls[i]["Company"].ToString();
                        l.toContactName = ls[i]["Name"].ToString();
                    }
                    else
                    {
                        l.toCompany = ls[i]["Name"].ToString();
                    }

                    if (references.Count > 0)
                    {
                        l.reference = references[i];
                    }

                    l.toAddress1 = ls[i]["Address1"].ToString();
                    l.toAddress2 = ls[i]["Address2"].ToString();
                    l.toPostcode = ls[i]["Postcode"].ToString();
                    l.toCity = ls[i]["Suburb"].ToString();

                    l.labelColour = ls[i]["Service"].ToString();
                    l.labelNumber = ls[i]["BaseLabel"].ToString();
                    l.ruralNumber = ls[i]["RuralLabel"].ToString();

                    l.toRfName = ls[i]["Destination"].ToString();

                    labelDetails.Add(l);
                }
                
            }

            PdfDocument doc = new PdfDocument();
            if (labelDetails.Count > 0)
            {
                FastwayAPI getBase = new FastwayAPI();
                doc = getBase.PrintMultipleLabels(labelDetails, doc);
            }

            MemoryStream pdfStream = new MemoryStream();
            doc.Save(pdfStream, false);
            byte[] pdfBytes = pdfStream.ToArray();

            var pdfString = Convert.ToBase64String(pdfBytes);

            try
            {
                return Json(new
                {//return status success
                    PdfBase64Stream = pdfString
                });
            }
            catch (Exception e)
            {//error
                return Json(new
                {//return status success
                    Error = e.Message
                });
            }
        }

        //START ARAMEX
        /// <summary>
        /// Validate address details and response with suggestions if any
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> AddressValidation(string Address1, string Address2, string Suburb, string Postcode, string CountryCode)
        {
            var add = new AramexLocation.Address();
            add.Line1 = Address1;
            add.Line2 = Address2;
            add.PostCode = Postcode;
            add.City = Suburb;
            add.CountryCode = CountryCode;

            var cInfo = new AramexLocation.ClientInfo();
            //NEED TO USE DATA FROM ARAMEX DB
            cInfo.UserName = "ollie@fastway.co.nz";
            cInfo.Password = "Fastway123!";
            cInfo.AccountEntity = "AKL";
            cInfo.AccountNumber = "152451";
            cInfo.AccountPin = "226321";
            cInfo.AccountCountryCode = "NZ";
            cInfo.Version = "v1.0";

            try
            {
                AramexAPI api = new AramexAPI();
                var res = await api.AddressValidation(add, cInfo);

                if (res != null)
                {
                    JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
                    string adds = jsonSerialiser.Serialize(res);
                    return Json(new
                    {
                        Suggestions = adds
                    });
                }
                else
                {
                    return Json(new
                    {
                        Suggestions = "Verified"
                    });
                }

            }
            catch (Exception e)
            {
                return Json(new
                {
                    Suggestions = e.Message
                });
            }
        }

        /// <summary>
        /// Create a shipment using Aramex API and response with coresponding labels
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> CreateAramexShipment(string CompanyName, string Address1, string Address2, string Suburb, string Postcode, string CountryCode, string Phone, string Email, string Descriptions)
        {
            try
            {
                //Get from DB
                var cInfo = new AramexShipping.ClientInfo();
                cInfo.UserName = "ollie@fastway.co.nz";
                cInfo.Password = "Fastway123!";
                cInfo.AccountEntity = "AKL";
                cInfo.AccountNumber = "152451";
                cInfo.AccountPin = "226321";
                cInfo.AccountCountryCode = "NZ";
                cInfo.Version = "v1.0";

                //can be left empty
                var transaction = new AramexShipping.Transaction();
                
                //default
                var lInfo = new AramexShipping.LabelInfo();
                lInfo.ReportID = 9201;
                lInfo.ReportType = "URL";

                //shipment
                var shipment = new Shipment();

                //shipper
                var shipper = new Party();
                shipper.AccountNumber = "152451";
                var sAdd = new AramexShipping.Address();
                sAdd.Line1 = "1 Lever Street";
                sAdd.Line2 = "Shed 5, Level 1";
                sAdd.PostCode = "1010";
                sAdd.City = "Auckland";
                sAdd.CountryCode = "NZ";
                shipper.PartyAddress = sAdd;
                //shipper contact
                var contact = new AramexShipping.Contact();
                contact.PersonName = "Ollie Huynh";
                contact.CompanyName = "Fastway Test";
                contact.CellPhone = "06 833 6333";
                contact.EmailAddress = "ollie@fastway.co.nz";
                contact.PhoneNumber1 = "06 833 6333";

                shipper.Contact = contact;
                //end shipper

                shipment.Shipper = shipper;
                
                //consignee
                var consignee = new Party();
                //delivery add
                var add = new AramexShipping.Address();
                add.Line1 = Address1;
                add.Line2 = Address2;
                add.PostCode = Postcode;
                add.City = Suburb;
                add.CountryCode = CountryCode;

                consignee.PartyAddress = add;
                //end delivery add
                //consignee contact
                var consingeeContact = new AramexShipping.Contact();

                consingeeContact.PersonName = CompanyName;
                consingeeContact.CompanyName = CompanyName;
                consingeeContact.CellPhone = Phone;
                consingeeContact.EmailAddress = Email;
                consingeeContact.PhoneNumber1 = Phone;

                consignee.Contact = consingeeContact;
                //end consignee contact
                shipment.Consignee = consignee;
                //end consignee

                //default - NOT SURE
                shipment.ShippingDateTime = DateTime.Now.AddDays(1);
                shipment.DueDate = DateTime.Now.AddMonths(1);
                

                //shipment details USER INPUT
                var details = new AramexShipping.ShipmentDetails();
                var weight = new AramexShipping.Weight();
                weight.Unit = "KG";
                weight.Value = 1;

                //NEED TO HAVE A LOGIC TO DETERMINE THESE PARAMETERS
                details.ActualWeight = weight;
                details.ChargeableWeight = weight;
                details.NumberOfPieces = 1;
                details.ProductGroup = "EXP";
                details.ProductType = "PPX";
                details.PaymentType = "P";
                details.PaymentOptions = "ACCT";
                details.DescriptionOfGoods = "Test API";
                details.GoodsOriginCountry = "NZ";
                
                //USER INPUT
                //MONEY
                var money = new AramexShipping.Money();
                money.CurrencyCode = "NZD";
                money.Value = 10;

                details.CustomsValueAmount = money;

                details.CashAdditionalAmount = money;
                details.CollectAmount = money;

                details.DescriptionOfGoods = Descriptions;

                //Items USER INPUT
                var item = new AramexShipping.ShipmentItem();
                item.Quantity = 1;
                item.Weight = weight;

                List<ShipmentItem> lItems = new List<ShipmentItem>();
                lItems.Add(item);

                details.Items = lItems.ToArray();
                //end Items

                shipment.Details = details;
                //end shipment details
                
                //form shipment array
                List<Shipment> sms = new List<Shipment>();
                sms.Add(shipment);
                var shipments = sms.ToArray();
                //form shipment request
                var req = new ShipmentCreationRequest(cInfo, transaction, shipments, lInfo);

                try
                {
                    var sService = new AramexShipping.Service_1_0Client();
                    sService.Open();
                    ShipmentCreationResponse res = await sService.CreateShipmentsAsync(req);
                    sService.Close();


                    //AramexAPI api = new AramexAPI();

                    //AramexShipping.ShipmentCreationResponse res = await api.CreateShipments(req);

                    if (res != null)
                    {
                        JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
                        string shipmentsString = jsonSerialiser.Serialize(res.Shipments.ToList());
                        string labelString = "";
                        for (var i = 0; i < res.Shipments.Length; i++)
                        {
                            if (res.Shipments[i].ShipmentLabel.LabelURL != null && res.Shipments[i].ShipmentLabel.LabelURL != "")
                            {
                                labelString += res.Shipments[i].ShipmentLabel.LabelURL;
                            }
                            if (i != res.Shipments.Length - 1)
                            {
                                labelString += ",";
                            }
                        }
                        return Json(new
                        {
                            Shipments = shipmentsString,
                            Labels = labelString
                        });
                    }
                    else
                    {
                        string shipmentsString = "";
                        for (var i = 0; i < res.Notifications.Length; i++)
                        {
                            shipmentsString += res.Notifications[i].Message.ToString();
                            if (i != res.Notifications.Length - 1)
                            {
                                shipmentsString += ", ";
                            }
                        }
                        return Json(new
                        {
                            Shipments = shipmentsString
                        });
                    }
                    
                }
                catch (Exception e)
                {
                    return Json(new
                    {
                        Errors = e.Message
                    });
                }
            } catch (Exception e)
            {
                return Json(new
                {
                    Shipments = e.Message
                });
            }


            
        }

        //END ARAMEX
    }
}