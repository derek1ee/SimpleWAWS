﻿using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<Site> LoadAppSettings(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GetSiteAppSettings.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.AppSettings = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.AppSettings.Select(e => new { name = e.Key, value = e.Value });
            response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteAppSettings.Bind(site), new { properties = properties });
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateAppSettings(this Site site)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteAppSettings.Bind(site), new { properties = site.AppSettings.Select(s => new { name = s.Key, value = s.Value }) });
            csmResponse.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> LoadMetadata(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GetSiteMetadata.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.Metadata = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.Metadata.Select(e => new { name = e.Key, value = e.Value });
            response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteMetadata.Bind(site), new { properties = properties });
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateMetadata(this Site site)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteMetadata.Bind(site), new { properties = site.Metadata});
            csmResponse.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> Load(this Site site, CsmWrapper<CsmSite> csmSite = null)
        {
            Validate.ValidateCsmSite(site);
            if (!site.IsSimpleWAWSOriginalSite) return site;

            if (csmSite == null)
            {
                var csmSiteResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Site.Bind(site));
                csmSiteResponse.EnsureSuccessStatusCode();
                csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();
            }

            site.HostName = csmSite.properties.hostNames.FirstOrDefault();
            site.ScmHostName = csmSite.properties.enabledHostNames.FirstOrDefault(h => h.IndexOf(".scm.", StringComparison.OrdinalIgnoreCase) != -1);

            await Task.WhenAll(LoadAppSettings(site), LoadMetadata(site), LoadPublishingCredentials(site), UpdateConfig(site, new { properties = new { scmType = "LocalGit" } }));

            site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
            site.AppSettings["MONACO_EXTENSION_VERSION"] = "beta";
            site.AppSettings["WEBSITE_TRY_MODE"] = "1";
            await site.UpdateAppSettings();
            return site;
        }

        public static async Task<Site> LoadPublishingCredentials(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.SitePublishingCredentials.Bind(site));
            var publishingCredentials = await response.Content.ReadAsAsync<CsmWrapper<CsmSitePublishingCredentials>>();

            site.PublishingUserName = publishingCredentials.properties.publishingUserName;
            site.PublishingPassword = publishingCredentials.properties.publishingPassword;

            return site;
        }

        public static async Task<Site> Update(this Site site, object update)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(update, "update");

            var response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.Site.Bind(site), update);
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateConfig(this Site site, object config)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(config, "config");

            var response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.SiteConfig.Bind(site), config);
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Stream> GetPublishingProfile(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.SitePublishingProfile.Bind(site));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public static async Task Delete(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Delete, CsmTemplates.Site.Bind(site));
            response.EnsureSuccessStatusCode();
        }

        public static async Task<DeployStatus> GetKuduDeploymentStatus(this Site site, bool block)
        {
            Validate.ValidateCsmSite(site);
            while (true)
            {
                DeployStatus? value;
                do
                {
                    var response = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.SiteDeployments.Bind(site));
                    response.EnsureSuccessStatusCode();

                    var deployment = await response.Content.ReadAsAsync<CsmArrayWrapper<CsmSiteDeployment>>();
                    value = deployment.value.Select(s => s.properties.status).FirstOrDefault();

                } while (block && value != DeployStatus.Failed && value != DeployStatus.Success);

                return value.Value;
            }
        }

        public static async Task EnableZRay(this Site site, string location)
        {
            var response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.ZRayForSite.Bind(site), new
            {
                location = location,
                plan = new
                {
                    name = "free",
                    publisher = "zend-technologies",
                    product = "z-ray"
                },
                properties = new { }
            });
            response.EnsureSuccessStatusCode();
        }
    }
}