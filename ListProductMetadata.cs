using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ShopifySharp;
using System.Collections.Generic;

public static class ListProductMetadata
{
    [FunctionName("ListProductMetadata")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        // Get the site URL and access token from the request body
        string siteUrl = req.Form["siteUrl"];
        string accessToken = req.Form["accessToken"];

        // Check if the url and token are null or empty
        if (string.IsNullOrEmpty(siteUrl) || string.IsNullOrEmpty(accessToken))
        {
            return new BadRequestObjectResult("Please provide both siteUrl and accessToken in the request body");
        }

        // Initialize the Shopify service
        var shopify = new ShopifyShopService(siteUrl, accessToken);

        // Get a list of all products
        var products = await shopify.ListAsync();

        // Create a list to store all the product metadata
        var productMetadata = new List<Dictionary<string, string>>();

        // Iterate through each product
        foreach (var product in products)
        {
            // Get the product's metafields
            var metafields = await shopify.MetaFields.ListAsync(product.Id.Value);

            // Filter metafields to only include those with key containing "msrp"
            metafields = metafields.Where(m => m.Key.ToLower().Contains("msrp")).ToList();
            
            // Create a dictionary to store the product's metadata
            var productMetadataFields = new Dictionary<string, string>();

            // Add the product's metadata to the dictionary
            foreach (var metafield in metafields)
            {
                productMetadataFields.Add(metafield.Key, metafield.Value);
            }

            // Add the product's metadata to the list
            productMetadata.Add(productMetadataFields);
        }

        // Return the list of product metadata
        return new OkObjectResult(productMetadata);
    }
}