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
        /// NOTE: Will need to redirect customer to their shop admin Page
        /// </summary>
        /// <param name="shopUrl">shopUrl acquired from ShopifyController: Install /or passed from other page</param>
        /// <returns>shopUrl</returns>
        public ActionResult Index(string shopUrl)
        {
            Response.Write("<input id='shopUrl' type='hidden' value='" + shopUrl + "'>");
            return View();
        }
        /// <summary>
        /// Writing shopUrl in hidden field 'shopUrl' and return View()
        /// NOTE: Will need to redirect customer to their shop admin Page
        /// </summary>
        /// <param name="shopUrl">shopUrl acquired from ShopifyController: Install /or passed from other page</param>
        /// <returns></returns>
        public ActionResult Installed(string shopUrl)
        {
            Response.Write("<input id='shopUrl' type='hidden' value='" + shopUrl + "'>");
            return View();
        }
        /// <summary>
        /// Listen to calls from shopify admin page, receive shop url and order ids. Parse orders and pass details to View()
        /// </summary>
        /// <param name="shop">shopUrl received from Shopify</param>
        /// <param name="ids">orderIds received from Shopify</param>
        /// <returns></returns>
        public async Task<ActionResult> NewConsignment(string shop, string[] ids)
        {
            //Get order numbers
            string orders = Request.QueryString["ids[]"];
            //required objects
            List<string> orderIds = new List<string>();//list of orderIds
            List<Order> orderDetails = new List<Order>();//list of order details
            List<Address> deliveryAddress = new List<Address>();//list of delivery details
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
                    if (deliveryAddress.Count > 0)
                    {
                        for (var l = 0; l < deliveryAddress.Count; l++)
                        {
                            if (deliveryAddress[l].Name == k.ShippingAddress.Name)
                            {
                                check = false;
                            }
                        }
                    }
                    if (check == true)
                    {
                        deliveryAddress.Add(k.ShippingAddress);
                        emails.Add(k.Email);
                    }
                }
                orderDetails.Add(k);//add order details into list of order details
            }

            ////jsonserialiser object to form json from list
            JavaScriptSerializer jsonSerialiser = new JavaScriptSerializer();
            ////creating json about orders to pass back to View()
            //string orderJson = jsonSerialiser.Serialize(orderDetails);


            //creating json about delivery address to pass back to View()
            string address = "";
            string note = "";
            if (deliveryAddress.Count == 0)
            {//No delivery address found
                address = "NoAddress";
            } else if (deliveryAddress.Count > 1)
            {//More than one addresses found
                address = "MoreThanOne";
            } else
            {//one address
                address = jsonSerialiser.Serialize(deliveryAddress[0]);
                for (int i = 0; i< orderDetails.Count; i++)
                {
                    if (orderDetails[i].Note != "")
                    {
                        note += orderDetails[i].Note;
                    }
                }
            }
            

            Response.Write("<input id='shopUrl' type='hidden' value='" + shop + "'>");//passing shopUrl to View() for further queries
            Response.Write("<input id='countryCode' type='hidden' value='" + cCode + "'>");//passing countryCode to View() for further queries
            Response.Write("<input id='orderDetails' type='hidden' value='" + orders + "'>");//passing orderIds to View() for further queries
            Response.Write("<input id='deliveryAddress' type='hidden' value='" + address + "'>");//passing address to View() for further queries
            if(emails.Count>=1) Response.Write("<input id='emailAddress' type='hidden' value='" + emails[0] + "'>");//passing email address
            if (note != "") Response.Write("<input id='specialInstruction' type='hidden' value='" + note + "'>");
            return View();

        }

        //public async Task<ActionResult> RePrintLabels(string shop, string id)
        //{



        //    Response.Write("<input id='shopUrl' value='" + shop + "'>");
        //    Response.Write("<input id='orderid' value='" + id + "'>");

        //    return View();
        //}

        /// <summary>
        /// Listen to query from NewConsignment controler, query and response with available services
        /// </summary>
        /// <param name="ShopUrl">web Url of the store</param>
        /// <param name="Address1">delivery address1</param>
        /// <param name="Suburb">delivery suburb</param>
        /// <param name="Postcode">delivery postcode</param>
        /// <param name="Region">delivery region</param>
        /// <param name="Weight">parcel weight</param>
        /// <param name="Type">parcel type</param>
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
                    } else//service(s) available
                    {
                        if (Type == "Parcel")
                        {//type was "Parcel", assign parcel option to response json
                            service = services.First();
                        } else
                        {//type was NOT "Parcel", get service based on value of Type
                            if (Type == "SAT-NAT-A3")
                            {
                                if (services.First().BaseLabelColour == "BROWN")
                                {
                                    service = services[services.FindIndex(a => a.BaseLabelColour == "SAT-LOC-A3")];
                                } else
                                {
                                    service = services[services.FindIndex(a => a.BaseLabelColour == Type)];
                                }
                            } else
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
                } else
                {
                    return Json(new
                    {//Error code from Fastway NOTE: will need to handle different type of error HERE
                        Error = "No Service Available"
                    });
                }
            } catch (Exception e) {
                //general error code Note: will need to handle these
                throw e;
            }
            
        }
        /// <summary>
        /// Listen to query from NewConsignment controller, query and response with label numbers
        /// </summary>
        /// <param name="ShopUrl">web url of the store</param>
        /// <param name="DeliveryDetails">delivery details from front-end</param>
        /// <param name="PackagingDetails">packaging details from front-end</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult LabelPrinting(string ShopUrl, string DeliveryDetails, string PackagingDetails)
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
            //parse delivery details            
            JObject d = JObject.Parse(DeliveryDetails);
            //assign receiver details
            label.toAddress1 = d["Address1"].ToString();
            label.toPostcode = d["Postcode"].ToString();
            label.toCity = d["Suburb"].ToString();

            if (d["Company"].ToString() != "")
            {
                label.toCompany = d["Company"].ToString();
                label.toContactName = d["ContactName"].ToString();
            } else
            {
                label.toCompany = d["ContactName"].ToString();
            }

            label.toContactPhone = d["ContactPhone"].ToString();
            label.toEmail = d["ContactEmail"].ToString();

            //parse packaging details
            JArray p = JArray.Parse(PackagingDetails);
            //object to store labelNumbers
            List<string> labelNumbers = new List<string>();
            //TEST label with details
            //List<Labeldetails> labelDetails = new List<Labeldetails>();
            
            for (int i = 0; i < p.Count; i++)
            {//loop through packaging details to query Fastway API and get label numbers //TEST details
                for (int j = 0; j < (int)p[i]["Items"]; j++)
                {//repeat this steps for number of item on each parcel type
                    //package details
                    label.weight = (double)p[i]["Weight"];
                    label.labelColour = p[i]["BaseLabel"].ToString();
                    //new fastwayAPI object to query
                    FastwayAPI getLabel = new FastwayAPI();
                    //a string object to hold label numbers
                    string labelNumber = getLabel.LabelQuery(label);
                    //TEST details, a LabelDetails oblect to hold labelDetails
                    //Labeldetails details = getLabel.LabelsQueryWithDetails(label);


                    //NOTE: reference
                    //label.reference = p["Reference"].ToString();

                    if (labelNumber.Contains(','))
                    {//if rural label exist
                        List<string> labelNumbersList = labelNumber.Split(',').ToList();
                        foreach (string st in labelNumbersList)
                        {//add multiple labels to result
                            labelNumbers.Add(st);
                        }
                    }
                    else
                    {//only one label
                        labelNumbers.Add(labelNumber);
                    }
                    //TEST details
                    //labelDetails.Add(details);
                    //labelNumbers.Add(details.labelNumber);
                }
            }

            //new fastway api to printlabel
            FastwayAPI printLabel = new FastwayAPI();

            string pdfString = printLabel.PrintLabelNumbersPdf(labelNumbers, label.apiKey);
            //TEST details
            //string pdfString = printLabel.PrintLabelWithDetails(labelDetails, label.apiKey);

            
            try
            {
                return Json(new
                {//returning results to front-end
                    Labels = String.Join(",", labelNumbers),
                    PdfBase64Stream = pdfString
                    //Test print type image
                    //JpegString = jpegString
                });
            } catch (Exception e)
            {//NOTE: manage exception if required
                throw e;
            }
        }
        /// <summary>
        /// V2 of LabelPrinting using generate-label call instead of generate-label-for-labelnumber
        /// </summary>
        /// <param name="ShopUrl"></param>
        /// <param name="DeliveryDetails"></param>
        /// <param name="PackagingDetails"></param>
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
            //parse delivery details
            JObject d = JObject.Parse(DeliveryDetails);
            //assign receiver details
            label.toAddress1 = d["Address1"].ToString();
            label.toAddress2 = d["Address2"].ToString();
            label.toPostcode = d["Postcode"].ToString();
            label.toCity = d["Suburb"].ToString();
            
            label.specialInstruction1 = d["SpecialInstruction1"].ToString();

            if (d["Company"].ToString() != "")
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
        /// <param name="ShopUrl">web url of the store</param>
        /// <param name="OrderIds">orderIds to be fulfilled</param>
        /// <param name="LabelNumbers">Label numbers to be added</param>
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
                else if(!fulfillments.Contains(","))
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
                foreach (string id in orderIds)
                {//loop through orderIds list and fulfill/update fulfillment as per required
                    ShopifyAPI newApi = new ShopifyAPI();
                    string fulfillments = await newApi.GetFulfillment(ShopUrl, token, id);
                    if (fulfillments == "")
                    {//if not fulfilled yet, fulfill it
                        string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, id, LabelNumbers, NotifyFlag);
                        //string fulfillmentId = await newApi.NewFulfillment(ShopUrl, token, id, LabelNumbers);
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
            }

            Response.Write("<input id='shopUrl' type='hidden'  value='" + ShopUrl + "'>");//passing shopUrl to View() for further queries
            return View();
        }
        /// <summary>
        /// Controller to retrieve customer preferences
        /// </summary>
        /// <param name="shopUrl">web url of the store</param>
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
        /// <param name="ShopUrl">web url of the store</param>
        /// <param name="StoreName">Name of store to display on label</param>
        /// <param name="StoreAddress1">Store address to display on label</param>
        /// <param name="Suburb">Store suburb to display on label</param>
        /// <param name="Postcode">Store postcode to display on label</param>
        /// <param name="ApiKey">Fastway Apikey to make calls</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UpdatePreferences(string ShopUrl, string StoreName, string StoreAddress1, string Suburb, string Postcode, string ApiKey, int CountryCode)
        {
            //update values
            DbEngine conn = new DbEngine();
            conn.UpdateStringValues(ShopUrl, "FastwayApiKey", ApiKey);
            conn.UpdateStringValues(ShopUrl, "StoreName", StoreName);
            conn.UpdateStringValues(ShopUrl, "StoreAddress1", StoreAddress1);
            conn.UpdateStringValues(ShopUrl, "Suburb", Suburb);
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
            } catch (Exception e)
            {//error
                throw e;
            }
            


        }
               
    }
}