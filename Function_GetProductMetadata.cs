using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Shopify.Functions
{
    public static class GetProductMetadata
    {
        namespace Shopify.Functions
    {
        public static class GetProductMetadata
        {
            [FunctionName("GetProductMetadata")]
            public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                // Get store id from request 
                string storeID = req.Query["storeID"];

                // Create request for products in store 
                string query = @"query {
      shop {
        products(first: 10) {
          pageInfo {
            hasNextPage
            hasPreviousPage
            startCursor
            endCursor
          }
          edges {
            node {
              id
              title
              description
              metafields {
                key
                value
              }
            }
          }
        }
      }
    }";
                var request = WebRequest.CreateHttp("https://" + storeID + ".myshopify.com/admin/api/2020-01/graphql.json");
                var postData = Encoding.UTF8.GetBytes(query);

                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = postData.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }

                // Declare variables 
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                dynamic products = JsonConvert.DeserializeObject(responseString);
                var productMetadata = new List<dynamic>();

                // Loop through all products in store 
                while (products.data.shop.products.pageInfo.hasNextPage)
                {
                    productMetadata.AddRange(products.data.shop.products.edges.Select(edge => edge.node));

                    // Create request for next page of products 
                    query = @"query {
          shop {
            products(first: 10, after: """ + products.data.shop.products.pageInfo.endCursor + @""") {
              pageInfo {
                hasNextPage
                hasPreviousPage
                startCursor
                endCursor
              }
              edges {
                node {
                  id
                  title
                  description
                  metafields {
                    key
                    value
                  }
                }
              }
            }
          }
        }";
                    postData = Encoding.UTF8.GetBytes(query);

                    request.ContentLength =
            request.ContentLength = postData.Length;

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(postData, 0, postData.Length);
                    }

                    response = (HttpWebResponse)request.GetResponse();
                    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    products = JsonConvert.DeserializeObject(responseString);

                    // Return product metadata in response 
                    return new OkObjectResult(productMetadata);
                }
            }
        }
    }
}