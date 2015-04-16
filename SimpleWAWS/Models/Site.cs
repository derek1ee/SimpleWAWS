﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Kudu.Client.Editor;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Newtonsoft.Json;
using SimpleWAWS.Kudu;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using SimpleWAWS.Code.CsmExtensions;

namespace SimpleWAWS.Models
{
    public class Site : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName, SiteName);
            }
        }

        [JsonProperty("name")]
        public string SiteName { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> AppSettings { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonIgnore]
        public string HostName { get; set; }

        public string ScmHostName { get; set; }

        private const string UserIdMetadataKey = "USERID";
        private const string AppServiceMetadataKey = "APP_SERVICE_METADATA";
        private const string SiteUniqueIdMetadataKey = "SITEUNIQUEID";

        public Site(string subscriptionId, string resourceGroupName, string name)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
            this.SiteName = name;
        }

        [JsonProperty("url")]
        public string Url
        {
            get 
            {
                return String.Format("https://{0}/", HostName);
            }
        }

        [JsonProperty("scmUrl")]
        public string ScmUrl
        {
            get
            {
                return String.Format("https://{0}/", ScmHostName);
            }
        }

        [JsonProperty("scmUrlWithCreds")]
        public string ScmUrlWithCreds
        {
            get
            {
                return String.Format("https://{0}:{1}@{2}/", PublishingUserName, PublishingPassword, ScmHostName);
            }
        }

        [JsonProperty("kuduConsoleWithCreds")]
        public string KuduConsoleWithCreds
        {
            get
            {
                return ScmUrlWithCreds + "DebugConsole";
            }
        }

        [JsonProperty("gitUrlWithCreds")]
        public string GitUrlWithCreds
        {
            get
            {
                return ScmUrlWithCreds + SiteName + ".git";
            }
        }

        [JsonProperty("monacoUrl")]
        public string MonacoUrl
        {
            get
            {
                return ScmUrl + "dev";
            }
        }

        [JsonProperty("contentDownloadUrl")]
        public string ContentDownloadUrl
        {
            get
            {
                return ScmUrl + "zip/site/wwwroot";
            }
        }

        [JsonProperty("publishingUserName")]
        public string PublishingUserName { get; set; }

        [JsonProperty("publishingPassword")]
        public string PublishingPassword { get; set; }

        [JsonProperty("isRbacEnabled")]
        public bool IsRbacEnabled { get; set; }

       public async Task MarkAsInUseAsync(string userId, TimeSpan lifeTime, AppService appService = AppService.Web)
        {
            await CsmManager.Update(this, new { properties = new { lastModifiedTimeUtc = DateTime.UtcNow.ToString() } });

            AppSettings["USER_ID"] = userId;
            AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString();
            AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = lifeTime.TotalMinutes.ToString();

            await CsmManager.UpdateAppSettings(this);

            Metadata[UserIdMetadataKey] = userId;
            Metadata[AppServiceMetadataKey] = appService.ToString();

            await CsmManager.UpdateMetadata(this);

            // Turn on HttpLogging for analytics later on.
            await CsmManager.UpdateConfig(this, new { properties = new { httpLoggingEnabled = true } });
        }

        public void FireAndForget()
        {
            try
            {
                var httpHeaders = "GET / HTTP/1.0\r\n" +
                "Host: " + this.HostName + "\r\n" +
                "\r\n";
                using (var tcpClient = new TcpClient(this.HostName, 80))
                {
                    tcpClient.Client.Send(Encoding.ASCII.GetBytes(httpHeaders));
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                //log and ignore any tcp exceptions
                Trace.TraceWarning(ex.ToString());
            }
        }
    }
}
