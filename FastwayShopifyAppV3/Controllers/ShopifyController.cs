using FastwayShopifyAppV3.Engine;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FastwayShopifyAppV3.Controllers
{
    public class ShopifyController : Controller
    {
        /// <summary>
        /// Default values for apps, acquired from Web.config
        /// </summary>
        string appUrl = Engine.ShopifyAppEngine.ApplicationUrl;
        string apiKey = Engine.ShopifyAppEngine.ShopifyApiKey;
        string secretKey = Engine.ShopifyAppEngine.ShopifySecretKey;
        /// <summary>
        /// Install method
        ///     If (appInstalled){
        ///         redirect to Home/Index with parameter shopUrl
        ///         } else {
        ///         redirect to Authorise service
        ///         }
        /// </summary>
        /// <param name="shop">shop Url</param>
        /// <returns></returns>
        public ActionResult Install(string shop)
        {
            
            DbEngine DBConnection = new DbEngine();
            //check if this shop has already installed app
            if (DBConnection.ExistingShop(shop) && DBConnection.GetIntergerValues(shop, "AppInstalled")==1)
            {
                //return to Home
                return RedirectToAction("Index", "Home", new { shopUrl = shop });
            }
            else
            {
                //set app permissions
                var permissions = new List<string>()
            {
                "read_orders",
                "write_orders",
                "read_fulfillments",
                "write_fulfillments",
            };
                //build authorise Url
                string redirectUrl = appUrl + "Shopify/Authorise";
                var authUrl = AuthorizationService.BuildAuthorizationUrl(permissions, shop, apiKey, redirectUrl);
                //redirect to Authorise method
                return Redirect(authUrl.ToString());
            }

        }
        /// <summary>
        /// Authorise method
        ///     Process returned authorisation sync from Shopify, get the updated accessToken
        ///     Check and add or update accessToken/install status if required
        /// </summary>
        /// <param name="shop">shop Url</param>
        /// <param name="code">authorisation code</param>
        /// <param name="state">authorisation state</param>
        /// <returns></returns>
        public async Task<ActionResult> Authorise(string shop, string code, string state)
        {
            //Get updating accessToken to Shopify Store
            string accessToken = await AuthorizationService.Authorize(code, shop, apiKey, secretKey);

            DbEngine DBConnection = new DbEngine();
            if (DBConnection.ExistingShop(shop))//Check if shop has installed the app
            {
               
                if (DBConnection.GetIntergerValues(shop, "AppInstalled")==0) //App previously uninstalled
                {
                    DBConnection.UpdateIntergerValues(shop, "AppInstalled", 1);//reset indicator
                    var service = new WebhookService(shop, accessToken);
                    var hook = new Webhook()
                    {
                        Address = appUrl + "Shopify/Uninstalled?shopUrl=" + shop,
                        CreatedAt = DateTime.Now,
                        Format = "json",
                        Topic = "app/uninstalled"
                    };

                    try
                    {
                        hook = await service.CreateAsync(hook);
                    }
                    catch (ShopifyException e)
                    {
                        throw e;
                    }
                }

                string currentToken = DBConnection.GetStringValues(shop, "ShopifyToken");
                if (currentToken != accessToken) //check and update Shopify Token
                {
                    DBConnection.UpdateStringValues(shop, "ShopifyToken", accessToken);
                }
            } else
            {//initiat a webhook to manage uninstalls
                DBConnection.InsertNewShop(shop, accessToken);
                var service = new WebhookService(shop, accessToken);
                var hook = new Webhook()
                {
                    Address = appUrl + "Shopify/Uninstalled?shopUrl=" + shop,
                    CreatedAt = DateTime.Now,
                    Format = "json",
                    Topic = "app/uninstalled"
                };

                try
                {
                    hook = await service.CreateAsync(hook);
                } catch (ShopifyException e)
                {
                    throw e;
                }

            }
            
            //Redirect to Home/Index with parameter
            return RedirectToAction("Installed", "Home", new { shopUrl = shop});
        }
        /// <summary>
        /// Listen to Uninstall event, query DB and set value accordingly. NOTE: Might need to re-process this
        /// </summary>
        /// <param name="shop">shopurl to update DB</param>
        public async Task<string> Uninstalled(string shopUrl)
        {
            if(await AuthorizationService.IsAuthenticWebhook(Request.Headers.ToKvps(), Request.InputStream, secretKey))//if request is authentic
            {
                //DB conn to query
                DbEngine DBConnection = new DbEngine();
                try
                {
                    if (DBConnection.ExistingShop(shopUrl))//Check if shop has installed the app
                    {
                        if (DBConnection.GetIntergerValues(shopUrl, "AppInstalled") == 1)
                        {
                            DBConnection.UpdateIntergerValues(shopUrl, "AppInstalled", 0);//reset indicator
                        }
                    }
                } catch (Exception e)
                {
                    throw e;
                }
                 
            } else
            {
                return "FAILED";//NOTE: Log
            }
            return "SUCCESS!";//NOTE: Log
        }

    }
}