﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ApiApp : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.AppService/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName, ApiAppName);
            }
        }

        public string MicroserviceId { get; set; }

        public string Location { get; set; }

        public string ApiAppName { get; set; }

        public JObject GeneratePayload()
        {
            return JObject.FromObject(new
            {
                microserviceId = MicroserviceId,
                hostingPlan = new
                {
                    subscriptionId = SubscriptionId,
                    resourceGroup = ResourceGroupName,
                    hostingPlanName = "Default1",
                    isNewHostingPlan = false,
                    computeMode = "Shared",
                    siteMode = "Limited",
                    sku = "Free",
                    workerSize = 0,
                    location = Location
                },
                settings = new { },
                dependsOn = new object[] { }
            });
        }

        public JObject GenerateTemplateParameters()
        {
            return JObject.FromObject(new Dictionary<string, object> 
            {
                { "location", new { value = Location } },
                { MicroserviceId,  new { value = new Dictionary<string, string> { { "$apiAppName", ApiAppName } } } }
            });
        }
    }
}