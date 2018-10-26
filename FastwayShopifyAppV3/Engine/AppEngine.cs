using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using ShopifySharp;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Primitives;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.IO;
using FastwayShopifyAppV3.AramexLocation;
using FastwayShopifyAppV3.AramexShipping;

namespace FastwayShopifyAppV3.Engine
{
    /// <summary>
    /// Static application values including
    /// 1. Shopify app credentials
    /// 2. DB connection string
    /// </summary>
    public class ShopifyAppEngine
    {
        public static string ShopifySecretKey { get; } =
            ConfigurationManager.AppSettings.Get("Shopify_Secret_Key");

        public static string ShopifyApiKey { get; } =
            ConfigurationManager.AppSettings.Get("Shopify_API_Key");

        public static string ConnectionString { get; } =
            ConfigurationManager.AppSettings.Get("DB_Connection_String");

        public static string ApplicationUrl { get; } =
            ConfigurationManager.AppSettings.Get("ApplicationUrl");
    }
    /// <summary>
    /// A class to handle database query
    /// Query improvement might be needed at some later stage
    /// </summary>
    public class DbEngine
    {
        /// <summary>
        /// Insert new shop into database once Installation confirmed
        /// </summary>
        public void InsertNewShop(string shop, string token)
        {
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("INSERT INTO tuochuynh.StoreDB (StoreId, StoreUrl, ShopifyToken, AppInstalled) VALUES (NEWID(),'"  + shop + "', '" + token + "', '" + 1 + "')", newCon))
            {
                newCon.Open();
                cmd.ExecuteNonQuery();
                newCon.Close();
            }
        }
        /// <summary>
        /// Update a string value on specified column
        /// of a specified store
        /// </summary>
        public void UpdateStringValues(string shop, string column, string value)
        {
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("UPDATE tuochuynh.StoreDB SET " + column + " = '" + value + "' WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                cmd.ExecuteNonQuery();
                newCon.Close();
            }
        }
        /// <summary>
        /// Update an interger value on specified column
        /// of a specified store
        /// </summary>
        public void UpdateIntergerValues(string shop, string column, int value)
        {
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("UPDATE tuochuynh.StoreDB SET " + column + " = '" + value + "' WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                cmd.ExecuteNonQuery();
                newCon.Close();
            }
        }
        /// <summary>
        /// Retrieve all saved data of the specified store
        /// </summary>
        public StoreRecord GetShopRecord(string shop)
        {
            List<StoreRecord> thisShop = new List<StoreRecord>();

            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM tuochuynh.StoreDB WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                using (SqlDataReader result = cmd.ExecuteReader())
                {
                    if (result.HasRows)
                    {
                        while (result.Read())
                        {
                            StoreRecord i = new StoreRecord();
                            i.StoreId = result.GetGuid(result.GetOrdinal("StoreId"));
                            i.StoreUrl = result[result.GetOrdinal("StoreUrl")] as string;
                            i.StoreName = result[result.GetOrdinal("StoreName")] as string;
                            i.StoreAddress1 = result[result.GetOrdinal("StoreAddress1")] as string;
                            i.Suburb = result[result.GetOrdinal("Suburb")] as string;
                            i.Postcode = result[result.GetOrdinal("Postcode")] as string;
                            i.Phone = result[result.GetOrdinal("Phone")] as string;
                            i.FastwayApiKey = result[result.GetOrdinal("FastwayApiKey")] as string;
                            i.ShopifyToken = result[result.GetOrdinal("ShopifyToken")] as string;
                            i.CountryCode = result[result.GetOrdinal("CountryCode")] as int? ?? -1;
                            thisShop.Add(i);
                        }
                        result.Close();
                    }
                }
                newCon.Close();
            }

            if (thisShop.Count == 0)
            {
                return null;
            }
            else
            {
                return thisShop.First();
            }

        }
        /// <summary>
        /// Query a string value on specified column
        /// from a specified store
        /// </summary>
        public string GetStringValues(string shop, string column)
        {
            string result="";
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM tuochuynh.StoreDB WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                using (SqlDataReader shopRecord = cmd.ExecuteReader())
                {
                    if (shopRecord.HasRows)
                    {
                        while (shopRecord.Read())
                        {
                            if (!shopRecord.IsDBNull(shopRecord.GetOrdinal(column)))
                            {
                                result = shopRecord.GetString(shopRecord.GetOrdinal(column));
                            }
                            else result = "";
                        }
                    }

                }
            }
            return result;
        }
        /// <summary>
        /// Query an integer value on specified column
        /// from a specified store
        /// </summary>
        public int GetIntergerValues(string shop, string column)
        {
            int result = -1;
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM tuochuynh.StoreDB WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                using (SqlDataReader shopRecord = cmd.ExecuteReader())
                {
                    if (shopRecord.HasRows)
                    {
                        while (shopRecord.Read())
                        {
                            result = shopRecord.GetInt32(shopRecord.GetOrdinal(column));
                        }
                    }

                }
            }
            return result;
        }
        /// <summary>
        /// Return a boolean value whether a specified store
        /// is found in the DB
        /// </summary>
        public bool ExistingShop(string shop)
        {
            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM tuochuynh.StoreDB WHERE StoreUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                using (SqlDataReader result = cmd.ExecuteReader())
                {
                    if (result.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        /// <summary>
        /// Query for customer parcels from specified store
        /// </summary>
        public List<CustomParcel> GetCustomParcel(string shop)
        {
            List<CustomParcel> customParcels = new List<CustomParcel>();

            using (SqlConnection newCon = new SqlConnection(ShopifyAppEngine.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM tuochuynh.CustomParcels WHERE ShopUrl ='" + shop + "'", newCon))
            {
                newCon.Open();
                using (SqlDataReader result = cmd.ExecuteReader())
                {
                    if (result.HasRows)
                    {
                        while (result.Read())
                        {
                            CustomParcel i = new CustomParcel();
                            i.CPId = result[result.GetOrdinal("CustomParcelID")] as int? ?? 0;
                            i.CPName = result[result.GetOrdinal("CustomParcelName")] as string;
                            i.CPType = result[result.GetOrdinal("CustomParcelType")] as string;
                            i.CPWeight = result.GetDouble(result.GetOrdinal("CustomParcelWeight"));
                            i.CPCubic = result.GetDouble(result.GetOrdinal("CustomParcelCubic"));
                            i.CPLength = result.GetDouble(result.GetOrdinal("CustomParcelLength"));
                            i.CPWidth = result.GetDouble(result.GetOrdinal("CustomParcelWidth"));
                            i.CPHeight = result.GetDouble(result.GetOrdinal("CustomParcelHeight"));
                            i.CPShop = result[result.GetOrdinal("ShopUrl")] as string;
                            customParcels.Add(i);
                        }
                        result.Close();
                    }
                }
                newCon.Close();
            }

            return customParcels;
        }
    }

    /// <summary>
    /// Class to present custom parcels
    /// </summary>
    public class CustomParcel
    {
        public int CPId { get; set; }
        public string CPName { get; set; }
        public string CPType { get; set; }
        public double CPWeight { get; set; }
        public double CPCubic { get; set; }
        public double CPLength { get; set; }
        public double CPWidth { get; set; }
        public double CPHeight { get; set; }
        public string CPShop { get; set; }

    }

    /// <summary>
    /// Class for Store records, include selected items
    /// from the Shopify API response
    /// </summary>
    public class StoreRecord
    {
        public Guid StoreId { get; set; }
        public string StoreUrl { get; set; }
        public string StoreName { get; set; }
        public string StoreAddress1 { get; set; }
        //public string StoreAddress2 { get; set; }
        public string Phone { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }
        public string ShopifyToken { get; set; }
        public string FastwayApiKey { get; set; }
        public int AppInstalled { get; set; }
        public int AppRemoved { get; set; }
        public int CountryCode { get; set; }
        //public string CustomParcels { get; set; }
    }

    /// <summary>
    /// Class to make Shopify API calls
    /// </summary>
    public class ShopifyAPI
    {
        /// <summary>
        /// Method to retrieve a single order
        /// from specified store
        /// with specified Shopify system order ID
        /// </summary>
        public async Task<Order> GetOrder(string shop, string token, string orderId)
        {
            Order order = new Order();

            var service = new OrderService(shop, token);
            long i = Convert.ToInt64(orderId);
            order = await service.GetAsync(i);

            return order;
        }
        /// <summary>
        /// Method to retrieve a single fulfillment
        /// from specified store
        /// with specified Shopify system order ID
        /// </summary>
        public async Task<string> GetFulfillment(string shop, string token, string orderId)
        {
            var service = new FulfillmentService(shop, token);
            IEnumerable<Fulfillment> fulfillments = await service.ListAsync(Convert.ToInt64(orderId));//Get a list of fulfillment assigned to this orderId
            //Creating the string which contains fulfillmentIds separated by ","
            string fulfillmentIds = "";

            if (fulfillments.Count() > 0)
            {
                foreach (Fulfillment f in fulfillments)
                {
                    if (fulfillmentIds == "")
                    {
                        fulfillmentIds += f.Id;
                    } else
                    {
                        fulfillmentIds += ","+f.Id;
                    }
                }
            }
            return fulfillmentIds;
        }
        /// <summary>
        /// Method to fulfill an order
        /// from specified store
        /// with specified Shopify system order ID
        /// when no fulfillment has been created for this orderId
        /// with tracking number and option to notify receiver
        /// </summary>
        public async Task<string> NewFulfillment(string shop, string token, string orderId, string labelNumbers, string notifyCustomer)
        {
            var trackingCompany = "";
            var trackingUrl = "";

            DbEngine conn = new DbEngine();
            int cCode = conn.GetIntergerValues(shop, "CountryCode");

            switch (cCode) {
                case 1:
                    trackingCompany = "Fastway Couriers (Australia)";
                    trackingUrl = "https://www.fastway.com.au/tools/track?l=";
                    break;
                case 6:
                    trackingCompany = "Fastway Couriers (NZ) Ltd.";
                    trackingUrl = "https://www.fastway.co.nz/track/track-your-parcel?l=";
                    //trackingUrl = "https://www.fastway.co.nz/tools/track?l=";
                    break;
                case 24:
                    trackingCompany = "Fastway Couriers (South Africa) Ltd.";
                    trackingUrl = "http://www.fastway.co.za/our-services/track-your-parcel?l=";
                    break;
            }
            
            //creating template for fulfillment details
            var fulfillment = new Fulfillment()
            {
                TrackingCompany = trackingCompany,
            };

            if (labelNumbers.Contains(","))
            {// if there are more than one labels
                //list of tracking numbers
                List<string> trackingNumbers = labelNumbers.Split(',').ToList();
                fulfillment.TrackingNumbers = trackingNumbers;
                //list of tracking urls
                List<string> trackingUrls = new List<string>();
                foreach (string number in trackingNumbers)
                {
                    trackingUrls.Add(trackingUrl + number);
                }
                fulfillment.TrackingUrls = trackingUrls;
            } else
            {// only one tracking number
                fulfillment.TrackingNumber = labelNumbers;
                fulfillment.TrackingUrl = trackingUrl + labelNumbers;
            }
            //fulfillmentservice object to create fulfillment
            var service = new FulfillmentService(shop, token);
            //get location id
            var locService = new LocationService(shop, token);
            var locations = await locService.ListAsync();

            //check locations to match
            if (locations.Count() == 1)
            {
                string locationId = locations.First().Id.ToString();
                fulfillment.LocationId = locationId;
            } else if (locations.Count() > 1 && shop == "don-deal.myshopify.com")
            {
                var ls = locations.ToList();
                var db = new DbEngine();
                string city = db.GetStringValues(shop, "Suburb");
                for (var i = 0; i < locations.Count(); i++)
                {
                    if (ls[i].City == city)
                    {
                        fulfillment.LocationId = ls[i].Id.ToString();
                        break;
                    }                
                }
            }
            

            //creating fulfillment to fulfill order
            if (notifyCustomer == "1") { fulfillment = await service.CreateAsync(Convert.ToInt64(orderId), fulfillment, true); }
            else { fulfillment = await service.CreateAsync(Convert.ToInt64(orderId), fulfillment, false); }

            //fulfillment = await service.CreateAsync(Convert.ToInt64(orderId), fulfillment,true);
            //returning fulfillment id
            return fulfillment.Id.ToString();
        }
        /// <summary>
        /// Method to fulfill an order
        /// from specified store
        /// with specified Shopify system order ID
        /// when no fulfillment has been created for this orderId
        /// with updated tracking number and option to notify receiver
        /// </summary>
        public async Task<string> UpdateFulfillment(string shop, string token, string orderId, string fulfillmentId, string labelNumbers, string notifyCustomer)
        {
            //FulfillmentService object to query
            var service = new FulfillmentService(shop, token);

            var trackingCompany = "";
            var trackingUrl = "";

            DbEngine conn = new DbEngine();
            int cCode = conn.GetIntergerValues(shop, "CountryCode");

            switch (cCode)
            {
                case 1:
                    trackingCompany = "Fastway Couriers (Australia)";
                    trackingUrl = "https://www.fastway.com.au/tools/track?l=";
                    break;
                case 6:
                    trackingCompany = "Fastway Couriers (NZ) Ltd.";
                    trackingUrl = "https://www.fastway.co.nz/track/track-your-parcel?l=";
                    //trackingUrl = "https://www.fastway.co.nz/tools/track?l=";
                    break;
                case 24:
                    trackingCompany = "Fastway Couriers (South Africa) Ltd.";
                    trackingUrl = "http://www.fastway.co.za/our-services/track-your-parcel?l=";
                    break;
            }

            //var f1 = await service.GetAsync(Convert.ToInt64(orderId), Convert.ToInt64(fulfillmentId));

            //f1.TrackingNumbers = f1.TrackingNumbers.Concat(new[] { labelNumbers });
            //f1.TrackingUrls = f1.TrackingUrls.Concat(new[] { trackingUrl });


            //Fulfillment template
            var fulfillment = new Fulfillment()
            {
                TrackingCompany = trackingCompany,
            };

            if (labelNumbers.Contains(","))
            {//more than one label numbers
                List<string> trackingNumbers = labelNumbers.Split(',').ToList();
                fulfillment.TrackingNumbers = trackingNumbers;
                List<string> trackingUrls = new List<string>();
                foreach (string number in trackingNumbers)
                {
                    trackingUrls.Add(trackingUrl + number);
                }
                fulfillment.TrackingUrls = trackingUrls;
            }
            else
            {//one label number
                fulfillment.TrackingNumber = labelNumbers;
                fulfillment.TrackingUrl = trackingUrl + labelNumbers;
            }
            //get location id
            var locService = new LocationService(shop, token);
            var locations = await locService.ListAsync();
            string locationId = locations.First().Id.ToString();
            fulfillment.LocationId = locationId;
            //update fulfillment with provided data
            if (notifyCustomer == "1") { fulfillment = await service.UpdateAsync(Convert.ToInt64(orderId), Convert.ToInt64(fulfillmentId), fulfillment, true); }
            else {fulfillment = await service.UpdateAsync(Convert.ToInt64(orderId), Convert.ToInt64(fulfillmentId), fulfillment, false); }

            //returning fulfillment id
            return fulfillment.Id.ToString();
        }
        
    }
    /// <summary>
    /// Class for Usable label details, inlcude selected data
    /// from the Fastway API response
    /// </summary>
    public class UsableLabel
    {
        public string BaseLabelColour { get; set; }
        public int ExcessLabelCount { get; set; }
        public string CostexgstAdditionalAdminFee { get; set; }
        public string CostexgstProfitValue { get; set; }
        public string CostexgstLabel { get; set; }
        public double CostexgstTotalChargeToEndUser { get; set; }
        public string BaseLabelCostExgst { get; set; }
        public double RuralLabelCostExgst { get; set; }
        public double PscPriceExgst { get; set; }
        public string ExcessLabelCostExgst { get; set; }
        public string Type { get; set; }
        public int SortOrder { get; set; }
        public int BaseWeight { get; set; }
        public int MaxWeight { get; set; }
        public string Saturday { get; set; }
    }
    /// <summary>
    /// Class to make Fastway API calls
    /// </summary>
    public class FastwayAPI
    {
        /// <summary>
        /// Structure represents parameters necessary for Fastway API calls.
        /// </summary>
        public struct Labeldetails
        {
            public string apiKey;

            public string labelColour;
            public string labelNumber;
            public double weight;
            public int excess;
            public string ruralNumber;
            public string saturdayNumber;
            public bool saturday;

            public string toCompany;
            public string toAddress1;
            public string toAddress2;
            public string toCity;
            public string toPostcode;
            public string toContactName;
            public string toContactPhone;
            public string toRfName;
            public string toEmail;

            public string fromCompany;
            public string fromAddress1;
            public string fromCity;
            public string fromPostcode;
            public string fromPhone;

            public string reference;
            public string specialInstruction1;

            public string labelDate;

            public int countryCode;

            public string printType;
        }
        /// <summary>
        /// Make consignor-consignee call to query available services
        /// Return a list of UsableLabel
        /// For specified Labeldetails
        /// </summary>
        public List<UsableLabel> ServiceQuery(Labeldetails details)
        {
            //RestClient objet to ustilise RESTAPI call
            var client = new RestClient();
            //Fastway API url (NOTE: NZ only, need to be changed if using for other countries)
            client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");
            //Rest request object to populate required parameter for API call
            var request = new RestRequest();
            //populate all data required
            request.Resource = "dynamiclabels/allocate-with-consignor-consignee-details";

            request.AddParameter("api_key", details.apiKey);

            request.AddParameter("PickupName", details.fromCompany);
            request.AddParameter("PickupAddr1", details.fromAddress1);
            request.AddParameter("PickupPostcode", details.fromPostcode);
            request.AddParameter("PickupTown", details.fromCity);

            request.AddParameter("DeliveryAddr1", details.toAddress1);
            request.AddParameter("DeliveryPostcode", details.toPostcode);
            request.AddParameter("DeliveryTown", details.toCity);

            request.AddParameter("WeightInKg", details.weight);
            request.AddParameter("CountryCode", details.countryCode); // NEED TO CHECK this if using for other countries

            request.AddParameter("RequiresPickup", "False"); //will turn to true in live

            request.AddParameter("TestMode", "false");

            //Call API await for response
            IRestResponse response = client.Execute(request);
            //parse response content
            JObject o = JObject.Parse(response.Content);
            //List of Usablelabel objects to hold labels data
            List<UsableLabel> labels = new List<UsableLabel>();
            //usable label object to hold cheapest parcel option
            UsableLabel parcel = new UsableLabel();

            if (o["error"] != null)
            {//if API return an error
                //returning an empty usablelabel object with BaseLabelColour = Error message
                //NOTE: will turn this to error message if required
                UsableLabel s = new UsableLabel();
                s.BaseLabelColour = o["error"].ToString();
                labels.Add(s);
            }

            if (o["result"] != null)
            {//if API returns success
                //parsing the cheapest parcel then add this first to the list
                JObject cheapParcel = JObject.Parse(o["result"]["cheapest_parcel"].ToString());
                UsableLabel l = new UsableLabel();
                l.BaseLabelColour = cheapParcel["base_label_colour"].ToString();
                l.ExcessLabelCount = (int)cheapParcel["excess_label_count"];
                l.CostexgstAdditionalAdminFee = cheapParcel["costexgst_additional_admin_fee"].ToString();
                l.CostexgstProfitValue = cheapParcel["costexgst_profit_value"].ToString();
                l.CostexgstLabel = cheapParcel["costexgst_label"].ToString();
                l.CostexgstTotalChargeToEndUser = (double)cheapParcel["costexgst_total_charge_to_end_user"];
                l.BaseLabelCostExgst = cheapParcel["base_label_cost_exgst"].ToString();
                l.RuralLabelCostExgst = (double)cheapParcel["rural_label_cost_exgst"];
                l.PscPriceExgst = (double)cheapParcel["psc_price_exgst"];
                l.ExcessLabelCostExgst = cheapParcel["excess_label_cost_exgst"].ToString();
                l.Type = cheapParcel["type"].ToString();
                l.SortOrder = (int)cheapParcel["sort_order"];
                l.BaseWeight = (int)cheapParcel["base_weight"];
                l.MaxWeight = (int)cheapParcel["max_weight"];
                l.Saturday = o["result"]["isSaturdayDeliveryAvailable"].ToString();
                labels.Add(l);

                //parsing usable labels
                JArray test = JArray.Parse(o["result"]["usable_labels"].ToString());
                //add all Satchel options to the list
                for (var i = 0; i < test.Count; i++)
                {
                    if (test[i]["base_label_colour"].ToString().Contains("SAT-NAT-") || test[i]["base_label_colour"].ToString().Contains("SAT-LOC-"))
                    {
                        UsableLabel s = new UsableLabel();
                        s.BaseLabelColour = test[i]["base_label_colour"].ToString();
                        s.ExcessLabelCount = (int)test[i]["excess_label_count"];
                        s.CostexgstAdditionalAdminFee = test[i]["costexgst_additional_admin_fee"].ToString();
                        s.CostexgstProfitValue = test[i]["costexgst_profit_value"].ToString();
                        s.CostexgstLabel = test[i]["costexgst_label"].ToString();
                        s.CostexgstTotalChargeToEndUser = (double)test[i]["costexgst_total_charge_to_end_user"];
                        s.BaseLabelCostExgst = test[i]["base_label_cost_exgst"].ToString();
                        s.RuralLabelCostExgst = (double)test[i]["rural_label_cost_exgst"];
                        s.PscPriceExgst = (double)test[i]["psc_price_exgst"];
                        s.ExcessLabelCostExgst = test[i]["excess_label_cost_exgst"].ToString();
                        s.Type = test[i]["type"].ToString();
                        s.SortOrder = (int)test[i]["sort_order"];
                        s.BaseWeight = (int)test[i]["base_weight"];
                        s.MaxWeight = (int)test[i]["max_weight"];
                        s.Saturday = o["result"]["isSaturdayDeliveryAvailable"].ToString();
                        labels.Add(s);
                    }
                }
            }
            //returning list of usable label for process
            return labels;
        }

        /// <summary>
        /// Method to query for pdfstreams on label numbers
        /// </summary>
        /// <param name="labelNumbers">Fastway label numbers</param>
        /// <param name="apiKey">Fastway apiKey</param>
        /// <returns>byte content from API call response</returns>
        //public string PrintLabelNumbers(List<string> labelNumbers,string apiKey)
        //{
        //    //RestClient to make API calls
        //    var client = new RestClient();
        //    client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");
        //    //New restclient request
        //    var request = new RestRequest();
        //    //populate data required for API calls
        //    request.Resource = "dynamiclabels/generate-label-for-labelnumber";

        //    for (int i = 0; i<labelNumbers.Count; i++)
        //    {
        //        request.AddParameter(string.Concat("LabelNumbers[", i, "]"), labelNumbers[i]);
        //    }

        //    request.AddParameter("api_key", apiKey);

        //    // test print type image
        //    //request.AddParameter("Type", "Image");

        //    //Execute API request, await for response
        //    IRestResponse response = client.Execute(request);
        //    //Convert response to rawBytes format and return
        //    byte[] content = response.RawBytes;
        //    var pdfBase64Code = Convert.ToBase64String(content);

        //    return pdfBase64Code;
        //}

        /// <summary>
        /// Method to query for pdfstreams on label numbers //NOTE: Currently not active
        /// </summary>
        /// <param name="labelNumbers">Fastway label numbers</param>
        /// <param name="apiKey">Fastway apiKey</param>
        /// <returns>string content from API call response</returns>
        //public string PrintLabelNumbersPdf(List<string> labelNumbers, string apiKey)
        //{
        //    //RestClient to make API calls
        //    var client = new RestClient();
        //    client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");
        //    //New restclient request
        //    var request = new RestRequest();
        //    //populate data required for API calls
        //    request.Resource = "dynamiclabels/generate-label-for-labelnumber";

        //    for (int i = 0; i < labelNumbers.Count; i++)
        //    {//add labels numbers
        //        request.AddParameter(string.Concat("LabelNumbers[", i, "]"), labelNumbers[i]);
        //    }
        //    //Fastway api key
        //    request.AddParameter("api_key", apiKey);

        //    // Get label using image type
        //    request.AddParameter("Type", "Image");

        //    //Execute API request, await for response
        //    IRestResponse response = client.Execute(request);
            
        //    //Parsing reresponse
        //    JObject o = JObject.Parse(response.Content);
        //    //Parsing result portion of response to get jpeg image strings
        //    try
        //    {
        //        JArray a = JArray.Parse(o["result"]["jpegs"].ToString());

        //        List<string> labels = new List<string>();

        //        PdfDocument doc = new PdfDocument();

        //        for (int j = 0; j < a.Count; j++)
        //        {
        //            byte[] jpgByteArray = Convert.FromBase64String(a[j]["base64Utf8Bytes"].ToString());

        //            PdfPage page = doc.AddPage();

        //            page.Width = XUnit.FromInch(4);
        //            page.Height = XUnit.FromInch(6);

        //            MemoryStream stream = new MemoryStream(jpgByteArray);

        //            XImage image = XImage.FromStream(stream);
        //            XGraphics gfx = XGraphics.FromPdfPage(page);

        //            gfx.DrawImage(image, 0, 0, 285, 435);
        //        }

        //        MemoryStream pdfStream = new MemoryStream();
        //        doc.Save(pdfStream, false);
        //        byte[] pdfBytes = pdfStream.ToArray();

        //        var pdfBase64Code = Convert.ToBase64String(pdfBytes);

        //        return pdfBase64Code;
        //    } catch (Exception e)
        //    {
        //        throw e;
        //    }
            
        //}

        /// <summary>
        /// Query pdf labels with specified details including 2 phases
        /// 1. Query for label numbers
        /// 2. Query for pdf labels with given label numbers
        /// </summary>
        public PdfDocument PrintLabels(List<Labeldetails> labels, PdfDocument doc)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");

            var request = new RestRequest();

            //For printng labels
            request.Resource = "dynamiclabels/generatelabel";

            this.RequestPopulating(request, labels[0]);
            if (labels[0].toAddress2 != "")
            {
                request.AddParameter("toAddress2", labels[0].toAddress2);
            }
            
            
            for (int i = 0; i < labels.Count; i++)
            {
                request.AddParameter(string.Concat("items[", i, "].colour"), labels[i].labelColour);
                request.AddParameter(string.Concat("items[", i, "].labelNumber"), labels[i].labelNumber);
                request.AddParameter(string.Concat("items[", i, "].weight"), labels[i].weight + " kg");
                request.AddParameter(string.Concat("items[", i, "].numberOfExcess"), labels[i].excess);
                if (labels[i].reference != "")
                {
                    request.AddParameter("customerReference", labels[i].reference);
                }
            }

            //Execute API request, await for response
            IRestResponse response = client.Execute(request);

            //Parsing reresponse
            JObject o = JObject.Parse(response.Content);

            //Parsing result portion of response to get jpeg image strings
            try
            {
                JArray a = JArray.Parse(o["result"]["jpegs"].ToString());

                for (int j = 0; j < a.Count; j++)
                {
                    byte[] jpgByteArray = Convert.FromBase64String(a[j]["base64Utf8Bytes"].ToString());

                    PdfPage page = doc.AddPage();

                    page.Width = XUnit.FromInch(4);
                    page.Height = XUnit.FromInch(6);

                    MemoryStream stream = new MemoryStream(jpgByteArray);

                    XImage image = XImage.FromStream(stream);
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    gfx.DrawImage(image, 0, 0, 285, 435);
                }

                if (labels[0].ruralNumber != null && labels[0].ruralNumber != "")
                {
                    var clientRural = new RestClient();
                    clientRural.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");

                    var requestRural = new RestRequest();

                    requestRural.Resource = "dynamiclabels/generatelabel";

                    this.RequestPopulating(requestRural, labels[0]);
                    if (labels[0].toAddress2 != "")
                    {
                        requestRural.AddParameter("toAddress2", labels[0].toAddress2);
                    }
                    
                    

                    for (int l = 0; l < labels.Count; l++)
                    {
                        requestRural.AddParameter(string.Concat("items[", l, "].colour"), "RURAL");
                        requestRural.AddParameter(string.Concat("items[", l, "].labelNumber"), labels[l].ruralNumber);
                        requestRural.AddParameter(string.Concat("items[", l, "].weight"), labels[l].weight + " kg");
                        if (labels[l].reference != "")
                        {
                            requestRural.AddParameter("customerReference", labels[l].reference);
                        }
                    }

                    IRestResponse responseRural = clientRural.Execute(requestRural);

                    //Parsing response
                    JObject oRural = JObject.Parse(responseRural.Content);
                    JArray aRural = JArray.Parse(oRural["result"]["jpegs"].ToString());

                    for (int k = 0; k < aRural.Count; k++)
                    {
                        byte[] jpgRuralByteArray = Convert.FromBase64String(aRural[k]["base64Utf8Bytes"].ToString());

                        PdfPage page = doc.AddPage();

                        page.Width = XUnit.FromInch(4);
                        page.Height = XUnit.FromInch(6);

                        MemoryStream stream = new MemoryStream(jpgRuralByteArray);

                        XImage image = XImage.FromStream(stream);
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        gfx.DrawImage(image, 0, 0, 285, 435);
                    }
                }
                

                if (labels[0].saturdayNumber != null && labels[0].saturdayNumber != "")
                {
                    var clientSaturday = new RestClient();
                    clientSaturday.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");

                    var requestSaturday = new RestRequest();

                    requestSaturday.Resource = "dynamiclabels/generatelabel";

                    this.RequestPopulating(requestSaturday, labels[0]);
                    
                    if (labels[0].toAddress2 != "")
                    {
                        requestSaturday.AddParameter("toAddress2", labels[0].toAddress2);
                    }
                    
                    

                    for (int l = 0; l < labels.Count; l++)
                    {
                        requestSaturday.AddParameter(string.Concat("items[", l, "].colour"), "SATURDAY");
                        requestSaturday.AddParameter(string.Concat("items[", l, "].labelNumber"), labels[l].saturdayNumber);
                        requestSaturday.AddParameter(string.Concat("items[", l, "].weight"), labels[l].weight + " kg");
                        if (labels[l].reference != "")
                        {
                            requestSaturday.AddParameter("customerReference", labels[l].reference);
                        }
                    }

                    IRestResponse responseSaturday = clientSaturday.Execute(requestSaturday);

                    //Parsing response
                    JObject oSaturday = JObject.Parse(responseSaturday.Content);
                    JArray aSaturday = JArray.Parse(oSaturday["result"]["jpegs"].ToString());

                    for (int k = 0; k < aSaturday.Count; k++)
                    {
                        byte[] jpgRuralByteArray = Convert.FromBase64String(aSaturday[k]["base64Utf8Bytes"].ToString());

                        PdfPage page = doc.AddPage();

                        page.Width = XUnit.FromInch(4);
                        page.Height = XUnit.FromInch(6);

                        MemoryStream stream = new MemoryStream(jpgRuralByteArray);

                        XImage image = XImage.FromStream(stream);
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        gfx.DrawImage(image, 0, 0, 285, 435);
                    }
                }

                return doc;
            }
            catch (Exception e)
            {
                return doc;
                throw e;
            }
        }
        /// <summary>
        /// Query pdf labels for bulk print feature
        /// with specified label numbers and details
        /// </summary>
        public PdfDocument PrintMultipleLabels(List<Labeldetails> labels, PdfDocument doc)
        {
            try
            {
                for (var i = 0; i < labels.Count; i++)
                {

                    var client = new RestClient();
                    client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");

                    var request = new RestRequest();

                    //For printng labels
                    request.Resource = "dynamiclabels/generatelabel";

                    this.RequestPopulating(request, labels[i]);
                    if (labels[i].toAddress2 != "")
                    {
                        request.AddParameter("toAddress2", labels[i].toAddress2);
                    }

                    request.AddParameter(string.Concat("items[", 0, "].colour"), labels[i].labelColour);
                    request.AddParameter(string.Concat("items[", 0, "].labelNumber"), labels[i].labelNumber);
                    request.AddParameter(string.Concat("items[", 0, "].weight"), labels[i].weight + " kg");
                    request.AddParameter(string.Concat("items[", 0, "].numberOfExcess"), labels[i].excess);
                    if (labels[i].reference != "")
                    {
                        request.AddParameter("customerReference", labels[i].reference);
                    }

                    //Execute API request, await for response
                    IRestResponse response = client.Execute(request);

                    //Parsing reresponse
                    JObject o = JObject.Parse(response.Content);


                    JArray a = JArray.Parse(o["result"]["jpegs"].ToString());

                    for (int j = 0; j < a.Count; j++)
                    {
                        byte[] jpgByteArray = Convert.FromBase64String(a[j]["base64Utf8Bytes"].ToString());

                        PdfPage page = doc.AddPage();

                        page.Width = XUnit.FromInch(4);
                        page.Height = XUnit.FromInch(6);

                        MemoryStream stream = new MemoryStream(jpgByteArray);

                        XImage image = XImage.FromStream(stream);
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        gfx.DrawImage(image, 0, 0, 285, 435);

                        if (labels[i].ruralNumber != "")
                        {
                            var ruralRequest = new RestRequest();

                            //For printng labels
                            ruralRequest.Resource = "dynamiclabels/generatelabel";

                            this.RequestPopulating(ruralRequest, labels[i]);
                            if (labels[i].toAddress2 != "")
                            {
                                ruralRequest.AddParameter("toAddress2", labels[i].toAddress2);
                            }

                            ruralRequest.AddParameter(string.Concat("items[", 0, "].colour"), "RURAL");
                            ruralRequest.AddParameter(string.Concat("items[", 0, "].labelNumber"), labels[i].ruralNumber);
                            ruralRequest.AddParameter(string.Concat("items[", 0, "].weight"), labels[i].weight + " kg");
                            ruralRequest.AddParameter(string.Concat("items[", 0, "].numberOfExcess"), labels[i].excess);
                            if (labels[i].reference != "")
                            {
                                ruralRequest.AddParameter("customerReference", labels[i].reference);
                            }

                            //Execute API request, await for response
                            IRestResponse ruralResponse = client.Execute(ruralRequest);

                            //Parsing reresponse
                            JObject oRural = JObject.Parse(ruralResponse.Content);


                            JArray aRural = JArray.Parse(oRural["result"]["jpegs"].ToString());

                            for (int k = 0; k < a.Count; k++)
                            {
                                byte[] jpgRuralByteArray = Convert.FromBase64String(aRural[k]["base64Utf8Bytes"].ToString());

                                PdfPage ruralPage = doc.AddPage();

                                ruralPage.Width = XUnit.FromInch(4);
                                ruralPage.Height = XUnit.FromInch(6);

                                MemoryStream ruralStream = new MemoryStream(jpgRuralByteArray);

                                XImage ruralImage = XImage.FromStream(ruralStream);
                                XGraphics ruralGfx = XGraphics.FromPdfPage(ruralPage);

                                ruralGfx.DrawImage(ruralImage, 0, 0, 285, 435);
                            }
                        }
                    }


                }
                return doc;
            }
            catch (Exception)
            {
                return doc;
                throw;
            }



        }

        /// <summary>
        /// Method to query for label numbers on provided details (addresses/serivce to be used)
        /// </summary>
        /// <param name="details"></param>
        /// <returns>label numbers (including rural label numbers)</returns>
        //public string LabelQuery(Labeldetails details)
        //{
        //    //string object to hold label numbers to be returned
        //    string label = "";
        //    //RestClient to make API calls
        //    var client = new RestClient();
        //    client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");
        //    //Request object to hold data for querying
        //    var request = new RestRequest();
        //    //API type to call
        //    request.Resource = "dynamiclabels/allocate-with-consignor-consignee-details";
            
        //    //populate parameters required
        //    request.AddParameter("api_key", details.apiKey);

        //    request.AddParameter("PickupName", details.fromCompany);
        //    request.AddParameter("PickupAddr1", details.fromAddress1);
        //    request.AddParameter("PickupPostcode", details.fromPostcode);
        //    request.AddParameter("PickupTown", details.fromCity);

        //    request.AddParameter("DeliveryContactName", details.toCompany);
        //    request.AddParameter("DeliveryAddr1", details.toAddress1);
        //    request.AddParameter("DeliveryPostcode", details.toPostcode);
        //    request.AddParameter("DeliveryTown", details.toCity);

        //    request.AddParameter("WeightInKg", details.weight);
        //    request.AddParameter("CountryCode", details.countryCode);

        //    if (details.toContactName != "")
        //    {
        //        request.AddParameter("DeliveryContactName", details.toContactName);
        //    }
        //    if (details.toContactPhone != "")
        //    {
        //        request.AddParameter("DeliveryContactPhone", details.toContactPhone);
        //    }
        //    if (details.toEmail != "")
        //    {
        //        request.AddParameter("DeliveryEmailAddress", details.toEmail);
        //    }

        //    //NOTE: will turn to true in live
        //    request.AddParameter("RequiresPickup", "False");
        //    request.AddParameter("TestMode", "false");
        //    //Service to be used, this is base on servicequery method
        //    request.AddParameter("LabelColour", details.labelColour);
        //    //execute API calls await for response
        //    IRestResponse response = client.Execute(request);
        //    //parsing response content to her labels numbers
        //    JObject o = JObject.Parse(response.Content);

        //    JArray test = JArray.Parse(o["result"]["usable_labels"].ToString());
        //    //forming return strings containings all labels number required on this call (NOTE: excluding excess label number at this stage)
        //    label = test[0]["base_label_number"].ToString();
        //    if (test[0]["rural_label_number"] != null)
        //    {
        //        label += ',' + test[0]["rural_label_number"].ToString();
        //    }
            
        //    return label;
        //}

        /// <summary>
        /// Method to query for labels details V2 to use generate-label call
        /// instead of generate-label-for-labelnumbers
        /// with given Labeldetails
        /// </summary>
        public Labeldetails LabelQueryV2(Labeldetails details)
        {
            //RestClient to make API calls
            var client = new RestClient();
            client.BaseUrl = new Uri("http://nz.api.fastway.org/v2/");
            //Request object to hold data for querying
            var request = new RestRequest();
            //API type to call
            request.Resource = "dynamiclabels/allocate-with-consignor-consignee-details";

            //populate parameters required
            request.AddParameter("api_key", details.apiKey);

            request.AddParameter("PickupName", details.fromCompany);
            request.AddParameter("PickupAddr1", details.fromAddress1);
            request.AddParameter("PickupPostcode", details.fromPostcode);
            request.AddParameter("PickupTown", details.fromCity);

            request.AddParameter("DeliveryContactName", details.toCompany);
            request.AddParameter("DeliveryAddr1", details.toAddress1);
            //request.AddParameter("DeliveryAddr2", details.toAddress2);
            request.AddParameter("DeliveryPostcode", details.toPostcode);
            request.AddParameter("DeliveryTown", details.toCity);

            request.AddParameter("WeightInKg", details.weight);
            request.AddParameter("CountryCode", details.countryCode);
            //This is currently not supported by Fastway API.
            request.AddParameter("CustomerReference", details.reference);

            if (details.toContactName != "")
            {
                request.AddParameter("DeliveryContactName", details.toContactName);
            }
            if (details.toContactPhone != "")
            {
                request.AddParameter("DeliveryContactPhone", details.toContactPhone);
            }
            if (details.toEmail != "")
            {
                request.AddParameter("DeliveryEmailAddress", details.toEmail);
            }

            //NOTE: will turn to true in live
            request.AddParameter("RequiresPickup", "False");
            request.AddParameter("TestMode", "false");
            //Service to be used, this is base on servicequery method
            request.AddParameter("LabelColour", details.labelColour);

            if (details.saturday)
            {
                request.AddParameter("SaturdayDelivery", "true");
            }
            //execute API calls await for response
            IRestResponse response = client.Execute(request);
            //parsing response content to her labels numbers
            JObject o = JObject.Parse(response.Content);

            JArray test = JArray.Parse(o["result"]["usable_labels"].ToString());
            //forming return strings containings all labels number required on this call (NOTE: excluding excess label number at this stage)
            details.labelNumber = test[0]["base_label_number"].ToString();
            details.excess = (int)test[0]["excess_label_count"];
            if (test[0]["rural_label_number"] != null)
            {
                details.ruralNumber = test[0]["rural_label_number"].ToString();
            }
            if (test[0]["saturday_label_number"]!= null)
            {
                details.saturdayNumber = test[0]["saturday_label_number"].ToString();
            }

            details.toRfName = o["result"]["delivery_rf"].ToString();


            return details;
        }

        /// <summary>
        /// Method to populate the required parameters
        /// NOTE: Just to tidy up repeating code
        /// </summary>
        public void RequestPopulating(RestRequest req, Labeldetails label)
        {
            req.AddParameter("api_key", label.apiKey);

            req.AddParameter("toCompany", label.toCompany);
            req.AddParameter("toAddress1", label.toAddress1);
            req.AddParameter("toCity", label.toCity);
            req.AddParameter("toPostCode", label.toPostcode);

            req.AddParameter("specialInstruction1", label.specialInstruction1);
            req.AddParameter("contactName", label.toContactName);
            req.AddParameter("contactPhone", label.toContactPhone);

            req.AddParameter("fromCompanyName", label.fromCompany);
            req.AddParameter("fromAddress1", label.fromAddress1);
            req.AddParameter("fromCity", label.fromCity);
            req.AddParameter("fromPhone", label.fromPhone);


            req.AddParameter("labelDate", DateTime.Now.AddMinutes(720).ToString("dd/MM/yyyy"));
            req.AddParameter("destRF", label.toRfName);

            req.AddParameter("Type", "Image");
        }

    }

    /// <summary>
    /// Class to make Aramex API calls
    /// </summary>
    public class AramexAPI
    {
        /// <summary>
        /// Method used to validate the address from Shopify
        /// Return
        /// 1. Null if address is velified
        /// 2. Empty list if Aramex API couldn't revifed the address
        /// 3. A list of suggestions if Aramex API found some similarities
        /// </summary>
        public async Task<List<AramexLocation.Address>> AddressValidation(AramexLocation.Address add, AramexLocation.ClientInfo client)
        {
            List<AramexLocation.Address> result = new List<AramexLocation.Address>();

            var cInfo = new AramexLocation.ClientInfo();
            cInfo.UserName = client.UserName;
            cInfo.Password = client.Password;
            cInfo.AccountEntity = client.AccountEntity;
            cInfo.AccountNumber = client.AccountNumber;
            cInfo.AccountPin = client.AccountPin;
            cInfo.AccountCountryCode = client.AccountCountryCode;
            cInfo.Version = client.Version;

            var trans = new AramexLocation.Transaction();

            var req = new AddressValidationRequest(cInfo, trans, add);

            var lService = new AramexLocation.Service_1_0Client();
            lService.Open();
            AddressValidationResponse res = await lService.ValidateAddressAsync(req);
            lService.Close();

            if (res.HasErrors)
            {
                result = res.SuggestedAddresses.ToList();
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Method used to create a shipment with specified details
        /// Return a ShipmentCreationResponse which inlude a PDF labels link if success
        /// </summary>
        public async Task<AramexShipping.ShipmentCreationResponse> CreateShipments(AramexShipping.ShipmentCreationRequest req)
        {
            var sService = new AramexShipping.Service_1_0Client();
            sService.Open();
            ShipmentCreationResponse res = await sService.CreateShipmentsAsync(req);
            sService.Close();
            return res;
        }
    }

    /// <summary>
    /// Extension class to manage list value from request.Headers
    /// </summary>
    public static class Extensions
    {
        public static List<KeyValuePair<string, StringValues>> ToKvps(this System.Collections.Specialized.NameValueCollection qs)
        {
            Dictionary<string, string> parameters = qs.Keys.Cast<string>().ToDictionary(key => key, value => qs[value]);
            var kvps = new List<KeyValuePair<string, StringValues>>();

            parameters.ToList().ForEach(x =>
            {
                kvps.Add(new KeyValuePair<string, StringValues>(x.Key, new StringValues(x.Value)));
            });

            return kvps;
        }
    }

}