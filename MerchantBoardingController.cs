using System.Web.Mvc;
using Nop.Core;
using Nop.Web.Framework.Controllers;
using System;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Common;
using System.Linq;
using System.Web;
using System.IO;
using Misc.Plugin.MerchantBoarding.Services;
using Nop.Web.Framework.Kendoui;
using Nop.Services.Security;
using System.Collections.Generic;
using Nop.Services.Catalog;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using System.Json;
using System.Security.Cryptography;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Framework;
using Nop.Core.Domain.Messages;
using Nop.Services.Messages;
using Nop.Services.Logging;
using Nop.Web.Framework.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Samples.HelperCode;
using Nop.Core.Domain.Localization;
using Nop.Services.Authentication;
using Nop.Services.Events;
using Misc.Plugin.MerchantBoarding.Models;
using Nop.Services.Topics;
using Nop.Services.Media;
using Nop.Services.Affiliates;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Common;
using Nop.Data;
using Nop.Web.Models.Common;
using Nop.Web.Infrastructure.Cache;
using Nop.Core.Caching;
using Nop.Web.Factories;
using Nop.Web.Framework.Themes;
using Misc.Plugin.MerchantBoarding.Mapper;
using Nop.Services.Stores;
using Nop.Services.Configuration;
using Nop.Services;
using iTextSharp.text.pdf;

namespace Misc.Plugin.MerchantBoarding.Controllers
{
    public class MerchantBoardingController : BaseController
    {
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly IMerchantBoardingService _merchantBoardingService;
        private readonly IPermissionService _permissionService;
        private readonly IPriceFormatter _priceFormatter;


        private readonly CaptchaSettings _captchaSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IWorkContext _workContext;

        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IWebHelper _webHelper;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ITopicService _topicService;
        private readonly IPictureService _pictureService;
        private readonly IDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly ITINCheckService _tINCheckService;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly MerchantBoardingtSettings _merchantBoardingtSettings;

        #region Static Veriables
        private static string PartnerType = ""; // PartnerType can not be use static. Please change this approach
        #endregion


        #region Microsoft Dynamics CRM Variables
        //Provides a persistent client-to-CRM server communication channel.
        private HttpClient httpClient;
        //Start with base version and update with actual version later.
        private Version webAPIVersion = new Version(9, 0);
        //Centralized collection of entity URIs used to manage lifetimes.
        List<string> entityUris = new List<string>();

        //A set of variables to hold the state of and URIs for primary entity instances.
        private JObject registerForm = new JObject(), merchantForm = new JObject(),
            retrievedData;
        private JObject account1 = new JObject(), account2 = new JObject();

        #endregion

        public MerchantBoardingController(ICustomerService customerService,
            CustomerSettings customerSettings,
            IGenericAttributeService genericAttributeService,
            ICustomerRegistrationService customerRegistrationService,
            IMerchantBoardingService merchantBoardingService,
            IPermissionService permissionService,
            IPriceFormatter priceFormatter,
            IStoreContext storeContext,
            IQueuedEmailService queuedEmailService,
            IEmailAccountService emailAccountService,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            CaptchaSettings captchaSettings,
            EmailAccountSettings emailAccountSettings,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            IWebHelper webHelper,
            IAuthenticationService authenticationService,
            IEventPublisher eventPublisher,
            ITopicService topicService,
            IPictureService pictureService,
            IDbContext dbContext,
            ILogger logger,
            ITINCheckService tINCheckService,
            IStoreService storeService,
            ISettingService settingService,
            MerchantBoardingtSettings merchantBoardingtSettings
            )
        {
            this._customerService = customerService;
            this._customerSettings = customerSettings;
            this._genericAttributeService = genericAttributeService;
            this._customerRegistrationService = customerRegistrationService;
            this._merchantBoardingService = merchantBoardingService;
            this._permissionService = permissionService;
            this._priceFormatter = priceFormatter;

            this._storeContext = storeContext;
            this._queuedEmailService = queuedEmailService;
            this._emailAccountService = emailAccountService;
            this._localizationService = localizationService;
            this._customerActivityService = customerActivityService;
            this._captchaSettings = captchaSettings;
            this._emailAccountSettings = emailAccountSettings;
            this._workContext = workContext;

            this._workflowMessageService = workflowMessageService;
            this._localizationSettings = localizationSettings;
            this._webHelper = webHelper;
            this._authenticationService = authenticationService;
            this._eventPublisher = eventPublisher;
            this._topicService = topicService;
            this._pictureService = pictureService;
            this._dbContext = dbContext;
            this._logger = logger;
            this._tINCheckService = tINCheckService;

            this._storeService = storeService;
            this._settingService = settingService;
            _merchantBoardingtSettings = merchantBoardingtSettings;
        }

        #region Private Methods - Common
        private bool ConvertToBoolean(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return Convert.ToBoolean(value);
            }
            else
            {
                return false;
            }
        }
        private List<bool> SetCheckBoxValues(string values, List<bool> boolValues)
        {
            try
            {
                if (!string.IsNullOrEmpty(values))
                {
                    string[] arrValues = values.Split(',');
                    if (arrValues.Length > 0)
                    {
                        for (int i = 0; i < arrValues.Length; i++)
                        {
                            int index;
                            if (int.TryParse(arrValues[i], out index))
                            {
                                boolValues[index - 1] = true;
                            }
                        }
                    }
                }
                return boolValues;
            }
            catch (Exception ex)
            {
                return boolValues;
            }
        }
        private string GetCheckboxValues(List<bool> boolValues)
        {
            string strValues = string.Empty;
            try
            {
                if (boolValues != null && boolValues.Any())
                {
                    for (int i = 0; i < boolValues.Count(); i++)
                    {
                        if (boolValues[i])
                        {
                            strValues += i + 1 + ",";
                        }
                    }
                }
                return strValues;
            }
            catch (Exception ex)
            {
                return strValues;
            }
        }
        private string SetPrice(string priceValue)
        {
            string actualPrice = "$ 0";
            if (!string.IsNullOrEmpty(priceValue))
            {
                actualPrice = "$ " + priceValue;
            }
            return actualPrice;
        }
        #endregion

        #region Configuration Methods
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var merchantBoardingtSettings = _settingService.LoadSetting<MerchantBoardingtSettings>(storeScope);

            var model = new ConfigurationModel();
            model.EnableTINCheck = merchantBoardingtSettings.EnableTINCheck;
            model.TINCheckModeId = Convert.ToInt32(merchantBoardingtSettings.TINCheckMode);
            model.TINCheckTestUsername = merchantBoardingtSettings.TINCheckTestUsername;
            model.TINCheckTestPassword = merchantBoardingtSettings.TINCheckTestPassword;
            model.TINCheckLiveUsername = merchantBoardingtSettings.TINCheckLiveUsername;
            model.TINCheckLivePassword = merchantBoardingtSettings.TINCheckLivePassword;
            model.TINCheckModeValues = merchantBoardingtSettings.TINCheckMode.ToSelectList();
            model.EnableBlockScore = merchantBoardingtSettings.EnableBlockScore;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.TINCheckModeId_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.TINCheckMode, storeScope);
                model.EnableTINCheck_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.EnableTINCheck, storeScope);
                model.TINCheckTestUsername_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.TINCheckTestUsername, storeScope);
                model.TINCheckTestPassword_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.TINCheckTestPassword, storeScope);
                model.TINCheckLiveUsername_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.TINCheckLiveUsername, storeScope);
                model.TINCheckLivePassword_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.TINCheckLivePassword, storeScope);
                model.EnableBlockScore_OverrideForStore = _settingService.SettingExists(merchantBoardingtSettings, x => x.EnableBlockScore, storeScope);

            }

            return View("~/Plugins/Misc.Plugin.MerchantBoarding/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var merchantBoardingtSettings = _settingService.LoadSetting<MerchantBoardingtSettings>(storeScope);

            //save settings
            merchantBoardingtSettings.TINCheckMode = (TINCheckMode)model.TINCheckModeId;
            merchantBoardingtSettings.EnableTINCheck = model.EnableTINCheck;
            merchantBoardingtSettings.TINCheckTestUsername = model.TINCheckTestUsername;
            merchantBoardingtSettings.TINCheckTestPassword = model.TINCheckTestPassword;
            merchantBoardingtSettings.TINCheckLiveUsername = model.TINCheckLiveUsername;
            merchantBoardingtSettings.TINCheckLivePassword = model.TINCheckLivePassword;
            merchantBoardingtSettings.EnableBlockScore = model.EnableBlockScore;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.TINCheckMode, model.TINCheckModeId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.EnableTINCheck, model.EnableTINCheck_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.TINCheckTestUsername, model.TINCheckTestUsername_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.TINCheckTestPassword, model.TINCheckTestPassword_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.TINCheckLiveUsername, model.TINCheckLiveUsername_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.TINCheckLivePassword, model.TINCheckLivePassword_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(merchantBoardingtSettings, x => x.EnableBlockScore, model.EnableBlockScore_OverrideForStore, storeScope, false);


            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
        #endregion

        #region Microsoft Dynamics CRM Methods
        private string getVersionedWebAPIPath()
        {
            return string.Format("v{0}/", webAPIVersion.ToString(2));
        }

        public async Task getWebAPIVersion()
        {

            HttpRequestMessage RetrieveVersionRequest =
              new HttpRequestMessage(HttpMethod.Get, getVersionedWebAPIPath() + "RetrieveVersion");

            HttpResponseMessage RetrieveVersionResponse =
                await httpClient.SendAsync(RetrieveVersionRequest);
            if (RetrieveVersionResponse.StatusCode == HttpStatusCode.OK)  //200
            {
                string str1 = await RetrieveVersionResponse.Content.ReadAsStringAsync();
                JObject RetrievedVersion = JsonConvert.DeserializeObject<JObject>(
                    await RetrieveVersionResponse.Content.ReadAsStringAsync());
                //Capture the actual version available in this organization
                webAPIVersion = Version.Parse((string)RetrievedVersion.GetValue("Version"));
            }
            else
            {
                Console.WriteLine("Failed to retrieve the version for reason: {0}",
                    RetrieveVersionResponse.ReasonPhrase);
                throw new CrmHttpResponseException(RetrieveVersionResponse.Content);
            }

        }

        /// <summary>
        /// Obtains the connection information from the application's configuration file, then 
        /// uses this info to connect to the specified CRM service.
        /// </summary>
        /// <param name="args"> Command line arguments. The first specifies the name of the 
        ///  connection string setting. </param>
        private void ConnectToCRM()
        {
            //Create a helper object to read app.config for service URL and application 
            // registration settings.
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //httpClient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential("akhasia@olb.com", "eVance1234!", "olb.com") });
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");
            // httpClient.Timeout = new TimeSpan(0, 2, 0);
            //httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "8.2");
            //httpClient.DefaultRequestHeaders.Add("OData-Version", "8.2");
            //httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>  
        /// Demonstrates basic create, update, and retrieval operations for entity instances and 
        ///  single properties.  
        /// </summary>
        public async Task BasicCreateAndUpdatesAsync(FormCollection form)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //httpClient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential("akhasia@olb.com", "eVance1234!", "olb.com") });
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");
            // httpClient.Timeout = new TimeSpan(0, 2, 0);


            string queryOptions;  //select, expand and filter clauses
            //First create a new contact instance,  then add additional property values and update 
            // several properties.
            //Local representation of CRM Contact instance
            registerForm.Add("new_companyname", form["CorporateName"]);
            registerForm.Add("new_name", form["ContactName"]);
            registerForm.Add("new_businessemailaddress", form["EmailAddress"]);
            registerForm.Add("new_businessphonenumber", form["TelephoneNumber"]);
            registerForm.Add("new_transactiontypecsv", form["transactionType"]);

            // Need to ask this to Anupam for Bank Type mapping
            //registerForm.Add("new_BankTemplate@odata.bind", "/new_banktemplates(0c21a47e-5619-e911-a818-000d3a1617d5)");
            if (!string.IsNullOrEmpty(form["PartnerId"]))
            {
                registerForm.Add("new_MerchantPartner@odata.bind", "/new_merchantpartners(" + form["PartnerId"] + ")");
            }

            HttpRequestMessage createRequest1 =
            new HttpRequestMessage(HttpMethod.Post, getVersionedWebAPIPath() + "new_merchantboardings");
            createRequest1.Content = new StringContent(registerForm.ToString(),
                Encoding.UTF8, "application/json");
            HttpResponseMessage createResponse1 =
                await httpClient.SendAsync(createRequest1);
            if (createResponse1.StatusCode == HttpStatusCode.NoContent)  //204
            {
                // Merchant Registered
            }
            else
            {
                throw new CrmHttpResponseException(createResponse1.Content);
            }

            #region Other methods
            ////Add additional property values to the existing contact.  As a general 
            //// rule, only transmit a minimum working set of properties.
            //JObject contact1Add = new JObject();
            //contact1Add.Add("annualincome", 10000);
            //contact1Add.Add("jobtitle", "SSE");

            //HttpRequestMessage updateRequest1 = new HttpRequestMessage(
            //    new HttpMethod("PATCH"), contact1Uri);
            //updateRequest1.Content = new StringContent(contact1Add.ToString(),
            //    Encoding.UTF8, "application/json");
            //HttpResponseMessage updateResponse1 =
            //    await httpClient.SendAsync(updateRequest1);
            //if (updateResponse1.StatusCode == HttpStatusCode.NoContent) //204
            //{
            //    Console.WriteLine("Contact '{0} {1}' updated with job title" +
            //        " and annual income.", contact1.GetValue("firstname"),
            //        contact1.GetValue("lastname"));
            //}
            //else
            //{
            //    Console.WriteLine("Failed to update contact for reason: {0}",
            //        updateResponse1.ReasonPhrase);
            //    throw new CrmHttpResponseException(updateResponse1.Content);
            //}

            ////Retrieve the contact with its explicitly initialized properties.
            ////fullname is a read-only calculated value.
            //queryOptions = "?$select=fullname,annualincome,jobtitle,description";
            //HttpResponseMessage retrieveResponse1 = await httpClient.GetAsync(
            //    contact1Uri + queryOptions);
            //if (retrieveResponse1.StatusCode == HttpStatusCode.OK) //200
            //{
            //    retrievedContact1 = JsonConvert.DeserializeObject<JObject>(
            //        await retrieveResponse1.Content.ReadAsStringAsync());
            //    Console.WriteLine("Contact '{0}' retrieved: \n\tAnnual income: {1}" +
            //        "\n\tJob title: {2} \n\tDescription: {3}.",
            //        // Can use either indexer or GetValue method (or a mix of two)
            //        retrievedContact1.GetValue("fullname"),
            //        retrievedContact1["annualincome"],
            //        retrievedContact1["jobtitle"],
            //        retrievedContact1["description"]);   //description is initialized empty.
            //}
            //else
            //{
            //    Console.WriteLine("Failed to retrieve contact for reason: {0}",
            //        retrieveResponse1.ReasonPhrase);
            //    throw new CrmHttpResponseException(retrieveResponse1.Content);
            //}

            ////Modify specific properties and then update entity instance.
            //JObject contact1Update = new JObject();
            //contact1Update.Add("jobtitle", "Senior Developer");
            //contact1Update.Add("annualincome", 95000);
            //contact1Update.Add("description", "Assignment to-be-determined");
            //HttpRequestMessage updateRequest2 = new HttpRequestMessage(
            //    new HttpMethod("PATCH"), contact1Uri);
            //updateRequest2.Content = new StringContent(contact1Update.ToString(),
            //    Encoding.UTF8, "application/json");
            //HttpResponseMessage updateResponse2 =
            //    await httpClient.SendAsync(updateRequest2);
            //if (updateResponse2.StatusCode == HttpStatusCode.NoContent)
            //{
            //    Console.WriteLine("Contact '{0}' updated:", retrievedContact1["fullname"]);
            //    Console.WriteLine("\tJob title: {0}", contact1Update["jobtitle"]);
            //    Console.WriteLine("\tAnnual income: {0}", contact1Update["annualincome"]);
            //    Console.WriteLine("\tDescription: {0}", contact1Update["description"]);
            //}
            //else
            //{
            //    Console.WriteLine("Failed to update contact for reason: {0}",
            //        updateResponse2.ReasonPhrase);
            //    throw new CrmHttpResponseException(updateResponse2.Content);
            //}

            //// Change just one property 
            //string phone1 = "555-0105";
            //// Create unique identifier by appending property name 
            //string contactPhoneUri =
            //        string.Format("{0}/{1}", contact1Uri, "telephone1");
            //JObject phoneValue = new JObject();
            //phoneValue.Add("value", phone1);   //Updates must use keyword "value". 

            //HttpRequestMessage updateRequest3 =
            //    new HttpRequestMessage(HttpMethod.Put, contactPhoneUri);
            //updateRequest3.Content = new StringContent(phoneValue.ToString(),
            //    Encoding.UTF8, "application/json");
            //HttpResponseMessage updateResponse3 =
            //    await httpClient.SendAsync(updateRequest3);
            //if (updateResponse3.StatusCode == HttpStatusCode.NoContent)
            //{
            //    Console.WriteLine("Contact '{0}' phone number updated.",
            //        retrievedContact1["fullname"]);
            //}
            //else
            //{
            //    Console.WriteLine("Failed to update the contact phone number for reason: {0}.",
            //        updateResponse3.ReasonPhrase);
            //    throw new CrmHttpResponseException(updateResponse3.Content);
            //}

            ////Now retrieve just the single property.
            //JObject retrievedProperty1;
            //HttpResponseMessage retrieveResponse2 =
            //    await httpClient.GetAsync(contactPhoneUri);
            //if (retrieveResponse2.StatusCode == HttpStatusCode.OK)
            //{
            //    retrievedProperty1 = JsonConvert.DeserializeObject<JObject>(
            //        await retrieveResponse2.Content.ReadAsStringAsync());
            //    Console.WriteLine("Contact's telephone# is: {0}.",
            //        retrievedProperty1["value"]);
            //}
            //else
            //{
            //    Console.WriteLine("Failed to retrieve the contact phone number for reason: {0}.",
            //        retrieveResponse2.ReasonPhrase);
            //    throw new CrmHttpResponseException(retrieveResponse2.Content);
            //}

            ////The following capabilities require version 8.2 or higher
            //if (webAPIVersion >= Version.Parse("8.2"))
            //{

            //    //Alternately, starting with December 2016 update (v9.0), a contact instance can be 
            //    //created and its properties returned in one operation by using a 
            //    //'Prefer: return=representation' header. Note that a 201 (Created) success status 
            //    // is returned, and not a 204 (No content).
            //    string contactAltUri;
            //    JObject contactAlt = new JObject();
            //    contactAlt.Add("firstname", "Peter_Alt");
            //    contactAlt.Add("lastname", "Cambel");
            //    contactAlt.Add("jobtitle", "Junior Developer");
            //    contactAlt.Add("annualincome", 80000);
            //    contactAlt.Add("telephone1", "555-0110");

            //    queryOptions = "?$select=fullname,annualincome,jobtitle,contactid";
            //    HttpRequestMessage createRequestAlt =
            //        new HttpRequestMessage(HttpMethod.Post, getVersionedWebAPIPath() + "contacts" + queryOptions);
            //    createRequestAlt.Content = new StringContent(contactAlt.ToString(),
            //        Encoding.UTF8, "application/json");
            //    createRequestAlt.Headers.Add("Prefer", "return=representation");

            //    HttpResponseMessage createResponseAlt = await httpClient.SendAsync(createRequestAlt);
            //    if (createResponseAlt.StatusCode == HttpStatusCode.Created)  //201
            //    {
            //        //Body should contain the requested new-contact information.
            //        JObject createdContactAlt = JsonConvert.DeserializeObject<JObject>(
            //            await createResponseAlt.Content.ReadAsStringAsync());
            //        //Because 'OData-EntityId' header not returned in a 201 response, you must instead 
            //        // construct the URI.
            //        contactAltUri = httpClient.BaseAddress + getVersionedWebAPIPath() + "contacts(" + createdContactAlt["contactid"] + ")";
            //        entityUris.Add(contactAltUri);
            //        Console.WriteLine(
            //            "Contact '{0}' created: \n\tAnnual income: {1}" + "\n\tJob title: {2}",
            //            createdContactAlt["fullname"],
            //            createdContactAlt["annualincome"],
            //            createdContactAlt["jobtitle"]);
            //        Console.WriteLine("Contact URI: {0}", contactAltUri);
            //    }
            //    else
            //    {
            //        Console.WriteLine("Failed to create contact for reason: {0}",
            //            createResponseAlt.ReasonPhrase);
            //        throw new CrmHttpResponseException(createResponseAlt.Content);
            //    }

            //    //Similarly, the December 2016 update (v9.0) also enables returning selected properties   
            //    //after an update operation (PATCH), with the 'Prefer: return=representation' header.
            //    //Here a success is indicated by 200 (OK) instead of 204 (No content).

            //    //Since we're reusing a local JObject, reinitialize it to remove extraneous properties.
            //    contactAlt.RemoveAll();
            //    contactAlt["annualincome"] = 95000;
            //    contactAlt["jobtitle"] = "Senior Developer";
            //    contactAlt["description"] = "MS Azure and Dynamics 365 Specialist";

            //    queryOptions = "?$select=fullname,annualincome,jobtitle";
            //    HttpRequestMessage updateRequestAlt = new HttpRequestMessage(
            //        new HttpMethod("PATCH"), contactAltUri + queryOptions);
            //    updateRequestAlt.Content = new StringContent(contactAlt.ToString(),
            //        Encoding.UTF8, "application/json");
            //    updateRequestAlt.Headers.Add("Prefer", "return=representation");

            //    HttpResponseMessage updateResponseAlt = await httpClient.SendAsync(updateRequestAlt);
            //    if (updateResponseAlt.StatusCode == HttpStatusCode.OK) //200
            //    {
            //        //Body should contain the requested updated-contact information.
            //        JObject UpdatedContactAlt = JsonConvert.DeserializeObject<JObject>(
            //            await updateResponseAlt.Content.ReadAsStringAsync());
            //        Console.WriteLine(
            //            "Contact '{0}' updated: \n\tAnnual income: {1}" + "\n\tJob title: {2}",
            //            UpdatedContactAlt["fullname"],
            //            UpdatedContactAlt["annualincome"],
            //            UpdatedContactAlt["jobtitle"]);
            //    }
            //    else
            //    {
            //        Console.WriteLine("Failed to update contact for reason: {0}",
            //            updateResponse1.ReasonPhrase);
            //        throw new CrmHttpResponseException(updateResponse1.Content);
            //    }
            //}
            #endregion
        }

        public async Task RunAsync(FormCollection form)
        {
            try
            {
                //await getWebAPIVersion();
                await BasicCreateAndUpdatesAsync(form);
                // await CreateWithAssociationAsync();
                //  await CreateRelatedAsync();
                // await AssociateExistingAsync();
            }
            catch (Exception ex)
            { DisplayException(ex); }
        }

        /// <summary> Helper method to display caught exceptions </summary>
        private static void DisplayException(Exception ex)
        {
            Console.WriteLine("The application terminated with an error.");
            Console.WriteLine(ex.Message);
            while (ex.InnerException != null)
            {
                Console.WriteLine("\t* {0}", ex.InnerException.Message);
                ex = ex.InnerException;
            }
        }

        #endregion

        #region CRM Methods Common
        private async Task<NoteEntityModel> GetFIleFromNote(string subject, string entityId)
        {
            string fileName = string.Empty;
            string filebase64 = string.Empty;
            NoteEntityModel noteModel = new NoteEntityModel();
            if (!string.IsNullOrEmpty(subject))
            {
                // check firt any file is exist or not based on subject. if exist then just update documentbody
                // else add record in note
                var queryOptions = "?$filter=subject eq '" + subject + "' and _objectid_value eq " + entityId;
                var url = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "annotations" + queryOptions;
                HttpResponseMessage noteResponse = await httpClient.GetAsync(
              url);
                JObject retrievedData = new JObject();
                JObject note = new JObject(); // add in annotations
                if (noteResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await noteResponse.Content.ReadAsStringAsync());
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        // update note
                        noteModel.filename = Convert.ToString(jvalue[0].SelectToken("filename"));
                        noteModel.documentbody = "data:application/octet-stream;base64," + Convert.ToString(jvalue[0].SelectToken("documentbody"));
                        noteModel.documentbodybase64 = Convert.ToString(jvalue[0].SelectToken("documentbody"));
                    }
                }
            }
            if (!string.IsNullOrEmpty(noteModel.filename) && !string.IsNullOrEmpty(noteModel.documentbody))
            {
                return noteModel;
            }
            return null;
        }
        private async Task<bool> UploadFIleInNote(NoteEntityModel model, int index = 0, string fileName = "", string filebase64 = "")
        {
            if (string.IsNullOrEmpty(filebase64) && string.IsNullOrEmpty(fileName))
            {
                if (index >= 0) // For Upload file
                {
                    HttpPostedFileBase filePost = Request.Files[index];
                    if (filePost != null && filePost.ContentLength > 0)
                    {
                        StreamReader reader = new StreamReader(filePost.InputStream);
                        byte[] bytedata = System.Text.Encoding.Default.GetBytes(reader.ReadToEnd());
                        //var bytedata = new byte[filePost.InputStream.Length];
                        filebase64 = Convert.ToBase64String(bytedata);
                        fileName = filePost.FileName;

                        //if (IsAffiliateLogo && AffiliateId > 0)
                        //{
                        //    Stream stream = filePost.InputStream;
                        //    var fileBinary = new byte[filePost.InputStream.Length];
                        //    stream.Read(fileBinary, 0, fileBinary.Length);
                        //    UploadAffiliateLogo(filePost.ContentType, fileName, fileBinary, AffiliateId);
                        //}

                    }
                }
                else // for Signature
                {
                    fileName = model.subject + ".png";
                    filebase64 = model.documentbody;
                }
            }
            if (!string.IsNullOrEmpty(filebase64) && !string.IsNullOrEmpty(fileName))
            {
                // check firt any file is exist or not based on subject. if exist then just update documentbody
                // else add record in note
                var queryOptions = "?$select=annotationid&$filter=subject eq '" + model.subject + "' and _objectid_value eq " + model.entityId;
                HttpResponseMessage noteResponse = await httpClient.GetAsync(
              "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "annotations" + queryOptions);
                JObject retrievedData = new JObject();
                JObject note = new JObject(); // add in annotations
                if (noteResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await noteResponse.Content.ReadAsStringAsync());
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        // update note
                        string annotationId = jvalue[0].SelectToken("annotationid").ToString();
                        string annotationUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "annotations(" + annotationId + ")";

                        note.Add("filename", fileName);
                        //note.Add("mimetype", "application/pdf");
                        note.Add("documentbody", filebase64);

                        HttpRequestMessage updateRequestNote = new HttpRequestMessage(
             new HttpMethod("PATCH"), annotationUri);
                        updateRequestNote.Content = new StringContent(note.ToString(),
                            Encoding.UTF8, "application/json");
                        HttpResponseMessage updateNoteResponse =
                            await httpClient.SendAsync(updateRequestNote);
                        if (updateNoteResponse.StatusCode == HttpStatusCode.NoContent) //204
                        {
                            return true;
                        }
                        else
                        {
                            throw new CrmHttpResponseException(updateNoteResponse.Content);
                        }
                    }
                    else
                    {
                        // make a new entry in note
                        note.Add("notetext", model.notetext);
                        note.Add("subject", model.subject);
                        note.Add("filename", fileName);
                        //note.Add("mimetype", "application/pdf");
                        note.Add("objectid_" + model.LookupEntity + "@odata.bind", "/" + model.LookupEntity + "s(" + model.entityId + ")");
                        note.Add("documentbody", filebase64);
                        HttpRequestMessage createRequestNote =
                           new HttpRequestMessage(HttpMethod.Post, getVersionedWebAPIPath() + "annotations");
                        createRequestNote.Content = new StringContent(note.ToString(),
                            Encoding.UTF8, "application/json");
                        HttpResponseMessage createNoteResponse =
                            await httpClient.SendAsync(createRequestNote);
                        if (createNoteResponse.StatusCode == HttpStatusCode.NoContent)  //204
                        {
                            return true;
                        }
                        else
                        {
                            throw new CrmHttpResponseException(createRequestNote.Content);
                        }
                    }
                }
            }
            return false;
        }
        private void UploadAffiliateLogo(string contentType, string fileName, byte[] fileBinary, int AffiliateId)
        {
            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();
            if (String.IsNullOrEmpty(contentType))
            {
                switch (fileExtension)
                {
                    case ".bmp":
                        contentType = MimeTypes.ImageBmp;
                        break;
                    case ".gif":
                        contentType = MimeTypes.ImageGif;
                        break;
                    case ".jpeg":
                    case ".jpg":
                    case ".jpe":
                    case ".jfif":
                    case ".pjpeg":
                    case ".pjp":
                        contentType = MimeTypes.ImageJpeg;
                        break;
                    case ".png":
                        contentType = MimeTypes.ImagePng;
                        break;
                    case ".tiff":
                    case ".tif":
                        contentType = MimeTypes.ImageTiff;
                        break;
                    default:
                        break;
                }
            }
            var picture = _pictureService.InsertPicture(fileBinary, contentType, null);

            if (picture != null)
            {
                // update picture.Id to affiliate 
                _dbContext.ExecuteSqlCommand("update Affiliate SET LogoId=@p0 WHERE id=@p1", false, null,
              picture.Id, AffiliateId);
            }
        }
        #endregion

        #region All Forms CRM Methods - Merchant Boarding
        public async Task<FormCollection> MerchantRegisteredCRMGet(string merchantid)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            FormCollection form = new FormCollection();            

            // Get merchant details from merchant id            
            HttpResponseMessage merchantResponse = await httpClient.GetAsync(
           getVersionedWebAPIPath() + "new_merchantboardings(" + merchantid + ")");
            if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
            {
                var jvalue = JsonConvert.DeserializeObject<JObject>(
                    await merchantResponse.Content.ReadAsStringAsync());
                if (jvalue != null)
                {
                    form.Add("Password", "temporary");
                    form.Add("ContactName", Convert.ToString(jvalue.SelectToken("new_name")));
                    form.Add("EmailAddress", Convert.ToString(jvalue.SelectToken("new_businessemailaddress")));
                    form.Add("TelephoneNumber", Convert.ToString(jvalue.SelectToken("new_businessphonenumber")));
                    form.Add("CorporateName", Convert.ToString(jvalue.SelectToken("new_companyname")));
                }
            }
            else
            {
                throw new CrmHttpResponseException(merchantResponse.Content);
            }       
            return form;
        }

        public async Task<MerchantInformationModel> MerchantInformationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            MerchantInformationModel model = new MerchantInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = Convert.ToString(jvalue[0].SelectToken("new_merchantboardingid"));
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";
                        model.FaxNumber = Convert.ToString(jvalue[0].SelectToken("new_faxnumber"));
                        model.CellPhone = Convert.ToString(jvalue[0].SelectToken("new_cell"));
                        model.MerchantName = Convert.ToString(jvalue[0].SelectToken("new_merchantname"));
                        model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_locationaddress1"));
                        model.City = Convert.ToString(jvalue[0].SelectToken("new_city1"));
                        model.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode1"));
                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_businessemailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_businessphonenumber"));
                        int SelectedState1 = model.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state1"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state1")), out SelectedState1);
                            model.SelectedState1 = SelectedState1;
                        }

                        model.LocationAddress2 = jvalue[0].SelectToken("new_locationaddress2").ToString();
                        model.City2 = jvalue[0].SelectToken("new_city2").ToString();
                        model.Zip2 = jvalue[0].SelectToken("new_zipcode2").ToString();
                        int SelectedState2 = model.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state2"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state2")), out SelectedState2);
                            model.SelectedState2 = SelectedState2;
                        }
                        if (!string.IsNullOrEmpty(model.LocationAddress2) || !string.IsNullOrEmpty(model.City2)
                            || !string.IsNullOrEmpty(model.Zip2) || SelectedState2 >= 0)
                        {
                            model.IsMoreLocation = true;
                        }
                    }
                }

                // if jason != null
                //{
                // set merchant Uri and store it in model
                // Bind MerchantInformation details to MerchantInformationModel
                //}
            }
            return model;
        }
        public async Task<bool> MerchantInformationCRMPost(FormCollection fc, MerchantInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_faxnumber", model.FaxNumber);
            merchantForm.Add("new_cell", model.CellPhone);
            merchantForm.Add("new_merchantname", model.MerchantName);
            merchantForm.Add("new_locationaddress1", model.LocationAddress);
            merchantForm.Add("new_city1", model.City);
            merchantForm.Add("new_state1", model.SelectedState1);
            merchantForm.Add("new_zipcode1", model.Zip);
            if (model.IsMoreLocation)
            {
                merchantForm.Add("new_locationaddress2", model.LocationAddress2);
                merchantForm.Add("new_city2", model.City2);
                merchantForm.Add("new_state2", model.SelectedState2);
                merchantForm.Add("new_zipcode2", model.Zip2);
            }
            else
            {
                merchantForm.Add("new_locationaddress2", null);
                merchantForm.Add("new_city2", null);
                merchantForm.Add("new_state2", null);
                merchantForm.Add("new_zipcode2", null);
            }
            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public void MerchantInformationDBPost(FormCollection fc, MerchantInformationModel model)
        {
            try
            {
                _merchantBoardingService.AddUpdateEntity(model);
            }
            catch (Exception ex)
            {
                _logger.Error("MerchantBoardingController >> MerchantInformationDBPost >> " + model.CustomerEmail + " >> Error : " + ex);
            }

        }

        public async Task<LegalInformationModel> LegalInformationModelCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            LegalInformationModel model = new LegalInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.LegalName = jvalue[0].SelectToken("new_corporationlegalname").ToString();

                        model.LocationAddress = jvalue[0].SelectToken("new_corporateaddress").ToString();
                        model.City = jvalue[0].SelectToken("new_city3").ToString();
                        model.Zip = jvalue[0].SelectToken("new_zipcode3").ToString();

                        model.IsTaxId = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_federaltax")));
                        model.TaxOrSsn = jvalue[0].SelectToken("new_federaltaxid").ToString();

                        model.IsMailingAddressSame = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_mailingaddress")));
                        if (!model.IsMailingAddressSame)
                        {
                            model.MailLocationAddress = jvalue[0].SelectToken("new_mailingaddress1").ToString();
                            model.MailCity = jvalue[0].SelectToken("new_city4").ToString();
                            model.MailZip = jvalue[0].SelectToken("new_zipcode4").ToString();
                        }

                        model.CustomerEmail = jvalue[0].SelectToken("new_businessemailaddress").ToString();
                        model.ContactName = jvalue[0].SelectToken("new_name").ToString();

                        int SelectedState1 = model.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state3"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state3")), out SelectedState1);
                            model.SelectedState1 = SelectedState1;
                        }

                        int SelectedState2 = model.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state4"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state4")), out SelectedState2);
                            model.SelectedState2 = SelectedState2;
                        }

                        int MCCCode = model.MCCCode = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_mcccode"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_mcccode")), out MCCCode);
                            model.MCCCode = MCCCode;
                        }
                    }
                }

                // if jason != null
                //{
                // set merchant Uri and store it in model
                // Bind MerchantInformation details to MerchantInformationModel
                //}
            }
            return model;
        }
        public async Task<bool> LegalInformationCRMPost(FormCollection fc, LegalInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_corporationlegalname", model.LegalName);

            merchantForm.Add("new_corporateaddress", model.LocationAddress);
            merchantForm.Add("new_city3", model.City);
            merchantForm.Add("new_state3", model.SelectedState1);
            merchantForm.Add("new_zipcode3", model.Zip);

            merchantForm.Add("new_federaltax", model.IsTaxId);
            merchantForm.Add("new_federaltaxid", model.TaxOrSsn);

            merchantForm.Add("new_mcccode", model.MCCCode);

            merchantForm.Add("new_mailingaddress", model.IsMailingAddressSame);
            if (!model.IsMailingAddressSame)
            {
                merchantForm.Add("new_mailingaddress1", model.MailLocationAddress);
                merchantForm.Add("new_city4", model.MailCity);
                merchantForm.Add("new_state4", model.SelectedState2);
                merchantForm.Add("new_zipcode4", model.MailZip);
            }

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<BusinessInformationModel> BusinessInformationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            BusinessInformationModel model = new BusinessInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.IsPaymentCard = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_doyoucurrentlyacceptpaymentcards")));
                        if (model.IsPaymentCard)
                            model.PaymentCard = Convert.ToString(jvalue[0].SelectToken("new_whoisitwith"));
                        model.IsTerminated = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_hasthemerchantbeenterminatedfromaccepting")));
                        if (model.IsTerminated)
                            model.AcceptCardExplanation = Convert.ToString(jvalue[0].SelectToken("new_ifsoexplain"));
                        model.IsSecurityBranch = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_haveyouhadasecuritybreach")));
                        if (model.IsSecurityBranch)
                            model.SecurityBranchExplanation = Convert.ToString(jvalue[0].SelectToken("new_ifsoexplain1"));

                        int TypeOfBusiness;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_typeofbusiness")), out TypeOfBusiness);
                        model.TypeOfBusiness = TypeOfBusiness;
                        if (model.TypeOfBusiness == 4)
                        {
                            model.LLCCity = Convert.ToString(jvalue[0].SelectToken("new_city5"));
                        }

                        int BusinessYears;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_lengthoftimeinbusinessyears")), out BusinessYears);
                        model.BusinessYears = BusinessYears;
                        int BusinessMonths;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_lengthoftimeinbusinessmonths")), out BusinessMonths);
                        model.BusinessMonths = BusinessMonths;

                        int new_numberoflocations;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_numberoflocations")), out new_numberoflocations);
                        model.new_numberoflocations = new_numberoflocations;
                        model.new_customerserviceno = Convert.ToString(jvalue[0].SelectToken("new_customerserviceno"));

                        #region Get File from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //BusinessInfo_LatestProofOfPCIDDSCompliance
                        noteModel = await GetFIleFromNote("BusinessInfo_LatestProofOfPCIDDSCompliance", merchantId);
                        if (noteModel != null)
                        {
                            model.FileName = noteModel.filename;
                            model.FileBase64 = noteModel.documentbody;
                        }
                        //NonProfit_ProvideEvidence
                        if (model.TypeOfBusiness == 7)
                        {
                            noteModel = new NoteEntityModel();
                            noteModel = await GetFIleFromNote("NonProfit_ProvideEvidence", merchantId);
                            if (noteModel != null)
                            {
                                model.FileName2 = noteModel.filename;
                                model.FileBase642 = noteModel.documentbody;
                            }
                        }
                        #endregion

                    }
                }
            }
            return model;
        }
        public async Task<bool> BusinessInformationCRMPost(FormCollection fc, BusinessInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_doyoucurrentlyacceptpaymentcards", model.IsPaymentCard);
            if (model.IsPaymentCard)
                merchantForm.Add("new_whoisitwith", model.PaymentCard);
            merchantForm.Add("new_hasthemerchantbeenterminatedfromaccepting", model.IsTerminated);
            if (model.IsTerminated)
                merchantForm.Add("new_ifsoexplain", model.AcceptCardExplanation);
            merchantForm.Add("new_haveyouhadasecuritybreach", model.IsSecurityBranch);
            if (model.IsSecurityBranch)
                merchantForm.Add("new_ifsoexplain1", model.SecurityBranchExplanation);

            merchantForm.Add("new_typeofbusiness", model.TypeOfBusiness);
            if (model.TypeOfBusiness == 4)
                merchantForm.Add("new_city5", model.LLCCity);

            merchantForm.Add("new_lengthoftimeinbusinessyears", Convert.ToString(model.BusinessYears));
            merchantForm.Add("new_lengthoftimeinbusinessmonths", Convert.ToString(model.BusinessMonths));

            merchantForm.Add("new_numberoflocations", model.new_numberoflocations);
            merchantForm.Add("new_customerserviceno", model.new_customerserviceno);

            #region File Upload in Note entity
            //BusinessInfo_LatestProofOfPCIDDSCompliance
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "LATEST PROOF OF PCI DDS COMPLIANCE";
            noteModel.subject = "BusinessInfo_LatestProofOfPCIDDSCompliance";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            bool noteResult = await UploadFIleInNote(noteModel, 0);
            if (noteResult)
                merchantForm.Add("new_businessinfo_latestproofpciddscompliance", true);

            if (model.TypeOfBusiness == 7) // Non Profit
            {

                //NonProfit_ProvideEvidence
                noteModel = new NoteEntityModel();
                noteModel.notetext = "Non Profit Provide Evidence";
                noteModel.subject = "NonProfit_ProvideEvidence";
                noteModel.LookupEntity = "new_merchantboarding";
                noteModel.entityId = model.MerchantId;
                noteResult = await UploadFIleInNote(noteModel, 1);
                if (noteResult)
                    merchantForm.Add("new_businessinfo_nonprofitevidence", true);
            }
            #endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<BusinessInformation2Model> BusinessInformation2CRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            BusinessInformation2Model model = new BusinessInformation2Model();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        int NatureOfBusiness;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_natureofbusiness")), out NatureOfBusiness);
                        model.NatureOfBusiness = NatureOfBusiness;
                        //if (model.NatureOfBusiness == 3)
                        //{
                        //    model.InternetUrl = Convert.ToString(jvalue[0].SelectToken("new_youurl"));
                        //}
                        if (model.NatureOfBusiness == 14)
                        {
                            model.Other = Convert.ToString(jvalue[0].SelectToken("new_ifsoexplain2"));
                        }

                        model.IsSeasonalSales = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_seasonalsales")));
                        if (model.IsSeasonalSales)
                        {
                            model.VolumeMonths = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_pleaseselecthighvolumemonthscsv")), model.VolumeMonths);
                        }
                        model.ProductsOrServices = Convert.ToString(jvalue[0].SelectToken("new_productsorservicesbeingoffered"));

                        string MerchantUse = Convert.ToString(jvalue[0].SelectToken("new_equipmentinformationdoesthemerchantusecsv"));
                        model.MerchantUse = SetCheckBoxValues(MerchantUse, model.MerchantUse);
                        if (!string.IsNullOrEmpty(MerchantUse) && MerchantUse.Contains("2"))
                        {
                            model.PaymentApplicationName = Convert.ToString(jvalue[0].SelectToken("new_whatisthepaymentapplicationname"));
                            model.PaymentApplicationVersion = Convert.ToString(jvalue[0].SelectToken("new_whatistheversionofthepaymentapplicationin"));
                        }
                        if (!string.IsNullOrEmpty(MerchantUse) && MerchantUse.Contains("1"))
                        {
                            model.TerminalType = Convert.ToString(jvalue[0].SelectToken("new_ifterminalwhattype"));
                        }

                        model.MerchantNameAppear = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_merchantnametoappearonconsumerstatement")));

                        string MethodOfAcceptance = Convert.ToString(jvalue[0].SelectToken("new_methodofacceptancetotalstoequal100csv"));
                        model.MethodOfAcceptance = SetCheckBoxValues(MethodOfAcceptance, model.MethodOfAcceptance);
                        if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("1"))
                        {
                            model.CardSwipevalue = Convert.ToString(jvalue[0].SelectToken("new_cardswipevaluein"));
                        }
                        if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("2"))
                        {
                            model.Motovalue = Convert.ToString(jvalue[0].SelectToken("new_motovaluein"));
                        }
                        if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("3"))
                        {
                            model.KeyEntervalue = Convert.ToString(jvalue[0].SelectToken("new_keyenteredvaluein"));
                        }
                        if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("4"))
                        {
                            model.Internetvalue = Convert.ToString(jvalue[0].SelectToken("new_internetvaluein1"));
                        }
                        model.new_cardsnotaccept = Convert.ToString(jvalue[0].SelectToken("new_cardsnotaccept"));
                        model.new_websiteaddress = Convert.ToString(jvalue[0].SelectToken("new_websiteaddress"));
                    }
                }
            }
            return model;
        }
        public async Task<bool> BusinessInformation2CRMPost(FormCollection fc, BusinessInformation2Model model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model           
            merchantForm.Add("new_natureofbusiness", model.NatureOfBusiness);
            //if (model.NatureOfBusiness == 3)
            //    merchantForm.Add("new_youurl", model.InternetUrl);
            if (model.NatureOfBusiness == 14)
                merchantForm.Add("new_ifsoexplain2", model.Other);

            merchantForm.Add("new_seasonalsales", model.IsSeasonalSales);
            if (model.IsSeasonalSales)
            {
                merchantForm.Add("new_pleaseselecthighvolumemonthscsv", GetCheckboxValues(model.VolumeMonths));
            }
            merchantForm.Add("new_productsorservicesbeingoffered", model.ProductsOrServices);

            string MerchantUse = GetCheckboxValues(model.MerchantUse);
            merchantForm.Add("new_equipmentinformationdoesthemerchantusecsv", MerchantUse);
            if (!string.IsNullOrEmpty(MerchantUse) && MerchantUse.Contains("2"))
            {
                merchantForm.Add("new_whatisthepaymentapplicationname", model.PaymentApplicationName);
                merchantForm.Add("new_whatistheversionofthepaymentapplicationin", model.PaymentApplicationVersion);
            }
            if (!string.IsNullOrEmpty(MerchantUse) && MerchantUse.Contains("1"))
            {
                merchantForm.Add("new_ifterminalwhattype", model.TerminalType);
            }

            merchantForm.Add("new_merchantnametoappearonconsumerstatement", model.MerchantNameAppear);

            string MethodOfAcceptance = GetCheckboxValues(model.MethodOfAcceptance);
            merchantForm.Add("new_methodofacceptancetotalstoequal100csv", MethodOfAcceptance);
            if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("1"))
            {
                model.CardSwipevalue = (model.CardSwipevalue ?? string.Empty).Replace("%", "");
                decimal CardSwipevalue;
                decimal.TryParse(model.CardSwipevalue, out CardSwipevalue);
                merchantForm.Add("new_cardswipevaluein", CardSwipevalue);
            }
            if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("2"))
            {
                model.Motovalue = (model.Motovalue ?? string.Empty).Replace("%", "");
                decimal Motovalue;
                decimal.TryParse(model.Motovalue, out Motovalue);
                merchantForm.Add("new_motovaluein", Motovalue);
            }
            if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("3"))
            {
                model.KeyEntervalue = (model.KeyEntervalue ?? string.Empty).Replace("%", "");
                decimal KeyEntervalue;
                decimal.TryParse(model.KeyEntervalue, out KeyEntervalue);
                merchantForm.Add("new_keyenteredvaluein", KeyEntervalue);
            }
            if (!string.IsNullOrEmpty(MethodOfAcceptance) && MethodOfAcceptance.Contains("4"))
            {
                model.Internetvalue = (model.Internetvalue ?? string.Empty).Replace("%", "");
                decimal Internetvalue;
                decimal.TryParse(model.Internetvalue, out Internetvalue);
                merchantForm.Add("new_internetvaluein1", Internetvalue);
            }
            merchantForm.Add("new_cardsnotaccept", model.new_cardsnotaccept);
            merchantForm.Add("new_websiteaddress", model.new_websiteaddress);


            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<QuestionnaireModel> QuestionnaireCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            QuestionnaireModel model = new QuestionnaireModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";


                        // If Card Swipe % is < 805 then only MoTo form is allow
                        var CardSwipevalueString = Convert.ToString(jvalue[0].SelectToken("new_cardswipevaluein"));
                        decimal CardSwipevalue;
                        decimal.TryParse(CardSwipevalueString, out CardSwipevalue);
                        if (CardSwipevalue < 80)
                        {
                            model.IsAllowForm = true;
                        }

                        // We not using this condition for MoTo Form
                        // f Nature of Business is Internet-3 or Mail/PhoneOrder-12 then skip this form
                        //int NatureOfBusiness;
                        //int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_natureofbusiness")), out NatureOfBusiness);
                        //if (NatureOfBusiness == 3 || NatureOfBusiness == 12)
                        //{
                        //    model.IsAllowForm = true;
                        //}

                        model.BusinessPercentage = Convert.ToString(jvalue[0].SelectToken("new_whatpercentagedoyouselltobusiness"));
                        model.PublicPercentage = Convert.ToString(jvalue[0].SelectToken("new_whatpercentagedoyouselltopublic"));

                        model.IsRetailLocation = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_doyouhavearetaillocation")));
                        if (model.IsRetailLocation)
                        {
                            model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_locationaddress6"));
                            model.City = Convert.ToString(jvalue[0].SelectToken("new_city6"));
                            model.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode6"));
                        }

                        model.DoYouSell1 = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_doyousellaserviceorproductscsv")), model.DoYouSell1);

                        model.DecribeProduct = Convert.ToString(jvalue[0].SelectToken("new_describetheproductservices"));

                        string PercentageOfSale = Convert.ToString(jvalue[0].SelectToken("new_whatpercentageofsaleswillbefromcsv"));
                        model.PercentageOfSale = SetCheckBoxValues(PercentageOfSale, model.PercentageOfSale);
                        if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("1"))
                        {
                            model.MailValue = Convert.ToString(jvalue[0].SelectToken("new_mailvaluein"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("2"))
                        {
                            model.TelephoneValue = Convert.ToString(jvalue[0].SelectToken("new_telephonevaluein"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("3"))
                        {
                            model.InternetValue = Convert.ToString(jvalue[0].SelectToken("new_internetvaluein"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("4"))
                        {
                            model.CardPresentValue = Convert.ToString(jvalue[0].SelectToken("new_cardpresentvaluein"));
                        }

                        model.PhysicalAddress = Convert.ToString(jvalue[0].SelectToken("new_whatisthephysicaladdressofyourbusiness"));
                        model.PhysicalCity = Convert.ToString(jvalue[0].SelectToken("new_city7"));
                        model.PhysicalZip = Convert.ToString(jvalue[0].SelectToken("new_zipcode7"));

                        model.IsProductAddress = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_istheproductstoredattheaboveaddress")));
                        if (!model.IsProductAddress)
                        {
                            model.ProductAddress = Convert.ToString(jvalue[0].SelectToken("new_address8"));
                            model.ProductCity = Convert.ToString(jvalue[0].SelectToken("new_city8"));
                            model.ProductZip = Convert.ToString(jvalue[0].SelectToken("new_zipcode8"));
                        }

                        model.IsOwnProduct = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_doyouowntheproductinventory")));

                        model.DoYouSell2 = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_doyousellcheckallthatapplycsv")), model.DoYouSell2);

                        model.CardBrandProcessor = Convert.ToString(jvalue[0].SelectToken("new_whoisyourcurrentcardbrandprocessor"));
                        int ChargeBacks;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_howmanychargebacksdidyouhaveforthisprevio")), out ChargeBacks);
                        model.ChargeBacks = ChargeBacks;
                        model.ChargeBackAmount = Convert.ToString(jvalue[0].SelectToken("new_whatwasthetotaldollaramountofthosechargeb"));

                        int WhenCharged;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_whenisthecustomerscharged")), out WhenCharged);
                        model.WhenCharged = WhenCharged;
                        int DeliverDays;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_delivermerchandisetothecustomer")), out DeliverDays);
                        model.DeliverDays = DeliverDays;

                        model.IsOtherCompany = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_othershippingproduct")));
                        if (model.IsOtherCompany)
                        {
                            model.CompanyName = Convert.ToString(jvalue[0].SelectToken("new_name4"));
                            model.CompanyTelephone = Convert.ToString(jvalue[0].SelectToken("new_telephone9"));
                            model.CompanyAddress = Convert.ToString(jvalue[0].SelectToken("new_address9"));
                        }

                        model.Advertise = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_howdoyouadvertisecsv")), model.Advertise);

                        model.RefundPolicy = Convert.ToString(jvalue[0].SelectToken("new_pleasedescribeyourrefundpolicy"));

                        model.new_thirdpartycardholderdata = Convert.ToString(jvalue[0].SelectToken("new_thirdpartycardholderdata"));

                    }
                }
            }
            return model;
        }
        public async Task<bool> QuestionnaireCRMPost(FormCollection fc, QuestionnaireModel model)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            model.BusinessPercentage = (model.BusinessPercentage ?? string.Empty).Replace("%", "");
            decimal BusinessPercentage;
            decimal.TryParse(model.BusinessPercentage, out BusinessPercentage);
            merchantForm.Add("new_whatpercentagedoyouselltobusiness", BusinessPercentage);
            model.PublicPercentage = (model.PublicPercentage ?? string.Empty).Replace("%", "");
            decimal PublicPercentage;
            decimal.TryParse(model.PublicPercentage, out PublicPercentage);
            merchantForm.Add("new_whatpercentagedoyouselltopublic", Convert.ToDecimal(model.PublicPercentage));

            merchantForm.Add("new_doyouhavearetaillocation", model.IsRetailLocation);
            if (model.IsRetailLocation)
            {
                merchantForm.Add("new_locationaddress6", model.LocationAddress);
                merchantForm.Add("new_city6", model.City);
                merchantForm.Add("new_zipcode6", model.Zip);
            }

            merchantForm.Add("new_doyousellaserviceorproductscsv", GetCheckboxValues(model.DoYouSell1));

            merchantForm.Add("new_describetheproductservices", model.DecribeProduct);

            string PercentageOfSale = GetCheckboxValues(model.PercentageOfSale);
            merchantForm.Add("new_whatpercentageofsaleswillbefromcsv", PercentageOfSale);
            if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("1"))
            {
                model.MailValue = (model.MailValue ?? string.Empty).Replace("%", "");
                decimal MailValue;
                decimal.TryParse(model.MailValue, out MailValue);
                merchantForm.Add("new_mailvaluein", MailValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("2"))
            {
                model.TelephoneValue = (model.TelephoneValue ?? string.Empty).Replace("%", "");
                decimal TelephoneValue;
                decimal.TryParse(model.TelephoneValue, out TelephoneValue);
                merchantForm.Add("new_telephonevaluein", TelephoneValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("3"))
            {
                model.InternetValue = (model.InternetValue ?? string.Empty).Replace("%", "");
                decimal InternetValue;
                decimal.TryParse(model.InternetValue, out InternetValue);
                merchantForm.Add("new_internetvaluein", InternetValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfSale) && PercentageOfSale.Contains("4"))
            {
                model.CardPresentValue = (model.CardPresentValue ?? string.Empty).Replace("%", "");
                decimal CardPresentValue;
                decimal.TryParse(model.CardPresentValue, out CardPresentValue);
                merchantForm.Add("new_cardpresentvaluein", CardPresentValue);
            }

            merchantForm.Add("new_whatisthephysicaladdressofyourbusiness", model.PhysicalAddress);
            merchantForm.Add("new_city7", model.PhysicalCity);
            merchantForm.Add("new_zipcode7", model.PhysicalZip);

            merchantForm.Add("new_istheproductstoredattheaboveaddress", model.IsProductAddress);
            if (!model.IsProductAddress)
            {
                merchantForm.Add("new_address8", model.ProductAddress);
                merchantForm.Add("new_city8", model.ProductCity);
                merchantForm.Add("new_zipcode8", model.ProductZip);
            }

            merchantForm.Add("new_doyouowntheproductinventory", model.IsOwnProduct);

            merchantForm.Add("new_doyousellcheckallthatapplycsv", GetCheckboxValues(model.DoYouSell2));

            merchantForm.Add("new_whoisyourcurrentcardbrandprocessor", model.CardBrandProcessor);
            merchantForm.Add("new_howmanychargebacksdidyouhaveforthisprevio", model.ChargeBacks);
            model.ChargeBackAmount = (model.ChargeBackAmount ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal ChargeBackAmount;
            decimal.TryParse(model.ChargeBackAmount, out ChargeBackAmount);
            merchantForm.Add("new_whatwasthetotaldollaramountofthosechargeb", ChargeBackAmount);

            merchantForm.Add("new_whenisthecustomerscharged", model.WhenCharged);
            merchantForm.Add("new_delivermerchandisetothecustomer", model.DeliverDays);

            merchantForm.Add("new_othershippingproduct", model.IsOtherCompany);
            if (model.IsOtherCompany)
            {
                merchantForm.Add("new_name4", model.CompanyName);
                merchantForm.Add("new_telephone9", model.CompanyTelephone);
                merchantForm.Add("new_address9", model.CompanyAddress);
            }

            merchantForm.Add("new_howdoyouadvertisecsv", GetCheckboxValues(model.Advertise));

            merchantForm.Add("new_pleasedescribeyourrefundpolicy", model.RefundPolicy);

            merchantForm.Add("new_thirdpartycardholderdata", model.new_thirdpartycardholderdata);

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<Questionnaire2Model> Questionnaire2CRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            Questionnaire2Model model = new Questionnaire2Model();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        // f Nature of Business is Internet-3 or Mail/PhoneOrder-12 then skip this form
                        int NatureOfBusiness;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_natureofbusiness")), out NatureOfBusiness);
                        if (NatureOfBusiness == 3 || NatureOfBusiness == 12)
                        {
                            model.IsAllowForm = true;
                        }

                        // we do not these fields
                        //model.BusinessPercentage = Convert.ToString(jvalue[0].SelectToken("new_whatpercentagedoyouselltobusinessconsumer"));
                        //model.IndividualPercentage = Convert.ToString(jvalue[0].SelectToken("new_whatpercentagedoyouselltoindividualconsum"));

                        model.MethodOfMarketing = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_methodofmarketingcsv")), model.MethodOfMarketing);
                        if (Convert.ToString(jvalue[0].SelectToken("new_methodofmarketingcsv")).Contains("6"))
                        {
                            //new_checkbox
                            model.MethodOfMarketingOther = Convert.ToString(jvalue[0].SelectToken("new_ifsoexplain3"));
                        }

                        string PercentageOfProducts = Convert.ToString(jvalue[0].SelectToken("new_percentageofproductssoldviacsv"));
                        model.PercentageOfProducts = SetCheckBoxValues(PercentageOfProducts, model.PercentageOfProducts);
                        if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("1"))
                        {
                            model.MailFaxValue = Convert.ToString(jvalue[0].SelectToken("new_mailfaxordersin"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("2"))
                        {
                            model.TelephoneOrderValue = Convert.ToString(jvalue[0].SelectToken("new_telephoneordersvaluein"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("3"))
                        {
                            model.InternetOrderValue = Convert.ToString(jvalue[0].SelectToken("new_internetordersvaluein"));
                        }
                        if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("4"))
                        {
                            model.OtherValue = Convert.ToString(jvalue[0].SelectToken("new_othervaluein"));
                        }

                        model.WhoProcessOrder = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_whoprocessestheordercsv")), model.WhoProcessOrder);
                        if (Convert.ToString(jvalue[0].SelectToken("new_whoprocessestheordercsv")).Contains("3"))
                        {
                            //new_checkbox
                            model.WhoProcessOrderOther = Convert.ToString(jvalue[0].SelectToken("new_otherexpain"));
                        }
                        model.WhoEnterCreditCard = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_whoenterscreditcardinformationintothecsv")), model.WhoEnterCreditCard);
                        if (Convert.ToString(jvalue[0].SelectToken("new_whoenterscreditcardinformationintothecsv")).Contains("4"))
                        {
                            //new_checkbox
                            model.WhoEnterCreditCardOther = Convert.ToString(jvalue[0].SelectToken("new_otherexpalin1"));
                        }

                        model.IsCreditCardPayment = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_creditcardpaymentinformation")));
                        if (model.IsCreditCardPayment)
                        {
                            model.MerchantCertiNumber = Convert.ToString(jvalue[0].SelectToken("new_merchantcertificatenumber"));
                            model.Issuer = Convert.ToString(jvalue[0].SelectToken("new_certificateissuer"));
                            model.ExpDate = Convert.ToString(jvalue[0].SelectToken("new_expdate"));
                        }

                        model.IsProduct = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_doyouowntheproductinventory1")));

                        model.IsProductLocationSame = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_istheproductstoredatyourbusinesslocation")));
                        if (!model.IsProductLocationSame)
                        {
                            model.ProductLocation = Convert.ToString(jvalue[0].SelectToken("new_whereisitstored"));
                        }

                        int ProductShipDays;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_authorization")), out ProductShipDays);
                        model.ProductShipDays = ProductShipDays;

                        int WhoShipProduct;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_whoshipstheproduct")), out WhoShipProduct);
                        model.WhoShipProduct = WhoShipProduct;
                        int ProductShippedBy;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_productshippedby")), out ProductShippedBy);
                        model.ProductShippedBy = ProductShippedBy;

                        if (model.ProductShippedBy == 1)
                        {
                            model.OtherShippedBy = Convert.ToString(jvalue[0].SelectToken("new_other"));
                        }

                        model.IsDeliveryReceipt = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_deliveryreceiptrequested")));
                    }
                }
            }
            return model;
        }
        public async Task<bool> Questionnaire2CRMPost(FormCollection fc, Questionnaire2Model model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            // we do not these fields
            //model.BusinessPercentage = (model.BusinessPercentage ?? string.Empty).Replace("%", "");
            //decimal BusinessPercentage;
            //decimal.TryParse(model.BusinessPercentage, out BusinessPercentage);
            //merchantForm.Add("new_whatpercentagedoyouselltobusinessconsumer", BusinessPercentage);
            //model.IndividualPercentage = (model.IndividualPercentage ?? string.Empty).Replace("%", "");
            //decimal IndividualPercentage;
            //decimal.TryParse(model.IndividualPercentage, out IndividualPercentage);
            //merchantForm.Add("new_whatpercentagedoyouselltoindividualconsum", IndividualPercentage);
            ///--------------------------


            string MethodOfMarketing = GetCheckboxValues(model.MethodOfMarketing);
            merchantForm.Add("new_methodofmarketingcsv", MethodOfMarketing);
            if (MethodOfMarketing.Contains("6"))
            {
                //new_checkbox
                merchantForm.Add("new_ifsoexplain3", model.MethodOfMarketingOther);
            }

            string PercentageOfProducts = GetCheckboxValues(model.PercentageOfProducts);
            merchantForm.Add("new_percentageofproductssoldviacsv", PercentageOfProducts);
            if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("1"))
            {
                model.MailFaxValue = (model.MailFaxValue ?? string.Empty).Replace("%", "");
                decimal MailFaxValue;
                decimal.TryParse(model.MailFaxValue, out MailFaxValue);
                merchantForm.Add("new_mailfaxordersin", MailFaxValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("2"))
            {
                model.TelephoneOrderValue = (model.TelephoneOrderValue ?? string.Empty).Replace("%", "");
                decimal TelephoneOrderValue;
                decimal.TryParse(model.TelephoneOrderValue, out TelephoneOrderValue);
                merchantForm.Add("new_telephoneordersvaluein", TelephoneOrderValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("3"))
            {
                model.InternetOrderValue = (model.InternetOrderValue ?? string.Empty).Replace("%", "");
                decimal InternetOrderValue;
                decimal.TryParse(model.InternetOrderValue, out InternetOrderValue);
                merchantForm.Add("new_internetordersvaluein", InternetOrderValue);
            }
            if (!string.IsNullOrEmpty(PercentageOfProducts) && PercentageOfProducts.Contains("4"))
            {
                model.OtherValue = (model.OtherValue ?? string.Empty).Replace("%", "");
                decimal OtherValue;
                decimal.TryParse(model.OtherValue, out OtherValue);
                merchantForm.Add("new_othervaluein", OtherValue);
            }

            string WhoProcessOrder = GetCheckboxValues(model.WhoProcessOrder);
            merchantForm.Add("new_whoprocessestheordercsv", WhoProcessOrder);
            if (WhoProcessOrder.Contains("3"))
            {
                //new_checkbox
                merchantForm.Add("new_otherexpain", model.WhoProcessOrderOther);
            }
            string WhoEnterCreditCard = GetCheckboxValues(model.WhoEnterCreditCard);
            merchantForm.Add("new_whoenterscreditcardinformationintothecsv", WhoEnterCreditCard);
            if (WhoEnterCreditCard.Contains("4"))
            {
                //new_checkbox
                merchantForm.Add("new_otherexpalin1", model.WhoEnterCreditCardOther);
            }



            merchantForm.Add("new_creditcardpaymentinformation", model.IsCreditCardPayment);
            if (model.IsCreditCardPayment)
            {
                merchantForm.Add("new_merchantcertificatenumber", model.MerchantCertiNumber);
                merchantForm.Add("new_certificateissuer", model.Issuer);
                merchantForm.Add("new_expdate", model.ExpDate);
            }

            merchantForm.Add("new_doyouowntheproductinventory1", model.IsProduct);

            merchantForm.Add("new_istheproductstoredatyourbusinesslocation", model.IsProductLocationSame);
            if (!model.IsProductLocationSame)
            {
                merchantForm.Add("new_whereisitstored", model.ProductLocation);
            }

            merchantForm.Add("new_authorization", Convert.ToString(model.ProductShipDays));

            merchantForm.Add("new_whoshipstheproduct", model.WhoShipProduct);
            merchantForm.Add("new_productshippedby", model.ProductShippedBy);
            if (model.ProductShippedBy == 1) // Fulfillment Center
            {
                merchantForm.Add("new_other", model.OtherShippedBy);
            }

            merchantForm.Add("new_deliveryreceiptrequested", model.IsDeliveryReceipt);

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<ProcessingDetailsModel> ProcessingDetailsCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            ProcessingDetailsModel model = new ProcessingDetailsModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        // For Visa/MasterCard/DIscover
                        model.PaymentCardMonthly = Convert.ToString(jvalue[0].SelectToken("new_monthlypaymentcardvolume"));
                        model.AvgTicket = Convert.ToString(jvalue[0].SelectToken("new_averageticket"));
                        model.HighestTicket = Convert.ToString(jvalue[0].SelectToken("new_highestticket"));

                        // For Amex
                        model.AmericanExpressMonthly = Convert.ToString(jvalue[0].SelectToken("new_americanexpressmonthlyvolume"));
                        model.new_avgticketae = Convert.ToString(jvalue[0].SelectToken("new_avgticketae"));
                        model.new_maxticketae = Convert.ToString(jvalue[0].SelectToken("new_maxticketae"));

                        model.IsServicer = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_transmitscardholderinformation")));
                        if (model.IsServicer)
                        {
                            model.ServicerName = Convert.ToString(jvalue[0].SelectToken("new_name2"));
                            model.ServicerContactNumber = Convert.ToString(jvalue[0].SelectToken("new_contactnumber2"));
                        }

                        model.IsFulfillmentHouse = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_merchantuse")));
                        if (model.IsFulfillmentHouse)
                        {
                            model.HouseName = Convert.ToString(jvalue[0].SelectToken("new_name3")); ;
                            model.HouseContactNumber = Convert.ToString(jvalue[0].SelectToken("new_contactnumber3"));
                        }

                        // Not in use
                        //model.IsBankruptcy = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_havemerchantorownersprincipals")));
                        //if (model.IsBankruptcy)
                        //{
                        //    model.BankruptcyExplain = Convert.ToString(jvalue[0].SelectToken("new_pleaseexplain"));
                        //}

                        model.new_havemerchantbankruptcycsv = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_havemerchantbankruptcycsv")), model.new_havemerchantbankruptcycsv);
                        if (Convert.ToString(jvalue[0].SelectToken("new_havemerchantbankruptcycsv")).Contains("1") ||
                            Convert.ToString(jvalue[0].SelectToken("new_havemerchantbankruptcycsv")).Contains("2"))
                        {
                            //new_checkbox
                            model.BankruptcyExplain = Convert.ToString(jvalue[0].SelectToken("new_pleaseexplain"));
                        }

                        // we do not need these fields
                        //model.IsSeasonalMerchant = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_seasonalmerchant")));
                        //if (model.IsSeasonalMerchant)
                        //{
                        //    int MonthsSeason;
                        //    int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_monthsoutofseason")), out MonthsSeason);
                        //    model.MonthsSeason = MonthsSeason;
                        //}
                        //------------
                    }
                }
            }
            return model;
        }
        public async Task<bool> ProcessingDetailsCRMPost(FormCollection fc, ProcessingDetailsModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            model.PaymentCardMonthly = (model.PaymentCardMonthly ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal PaymentCardMonthly;
            decimal.TryParse(model.PaymentCardMonthly, out PaymentCardMonthly);
            merchantForm.Add("new_monthlypaymentcardvolume", PaymentCardMonthly);

            model.AvgTicket = (model.AvgTicket ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal AvgTicket;
            decimal.TryParse(model.AvgTicket, out AvgTicket);
            merchantForm.Add("new_averageticket", AvgTicket);

            model.HighestTicket = (model.HighestTicket ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal HighestTicket;
            decimal.TryParse(model.HighestTicket, out HighestTicket);
            merchantForm.Add("new_highestticket", HighestTicket);

            model.AmericanExpressMonthly = (model.AmericanExpressMonthly ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal AmericanExpressMonthly;
            decimal.TryParse(model.AmericanExpressMonthly, out AmericanExpressMonthly);
            merchantForm.Add("new_americanexpressmonthlyvolume", AmericanExpressMonthly);

            model.new_avgticketae = (model.new_avgticketae ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal new_avgticketae;
            decimal.TryParse(model.new_avgticketae, out new_avgticketae);
            merchantForm.Add("new_avgticketae", new_avgticketae);

            model.new_maxticketae = (model.new_maxticketae ?? string.Empty).Replace(" ", "").Replace("$", "");
            decimal new_maxticketae;
            decimal.TryParse(model.new_maxticketae, out new_maxticketae);
            merchantForm.Add("new_maxticketae", new_maxticketae);

            merchantForm.Add("new_transmitscardholderinformation", model.IsServicer);
            if (model.IsServicer)
            {
                merchantForm.Add("new_name2", model.ServicerName);
                merchantForm.Add("new_contactnumber2", model.ServicerContactNumber);
            }

            merchantForm.Add("new_merchantuse", model.IsFulfillmentHouse);
            if (model.IsFulfillmentHouse)
            {
                merchantForm.Add("new_name3", model.HouseName);
                merchantForm.Add("new_contactnumber3", model.HouseContactNumber);
            }

            // Not in use
            //merchantForm.Add("new_havemerchantorownersprincipals", model.IsBankruptcy);
            //if (model.IsBankruptcy)
            //{
            //    merchantForm.Add("new_pleaseexplain", model.BankruptcyExplain);
            //}

            string new_havemerchantbankruptcycsv = GetCheckboxValues(model.new_havemerchantbankruptcycsv);
            merchantForm.Add("new_havemerchantbankruptcycsv", new_havemerchantbankruptcycsv);
            if (new_havemerchantbankruptcycsv.Contains("1") || new_havemerchantbankruptcycsv.Contains("2"))
            {
                //new_checkbox
                merchantForm.Add("new_pleaseexplain", model.BankruptcyExplain);
            }

            // we do not need these fields
            //merchantForm.Add("new_seasonalmerchant", model.IsSeasonalMerchant);
            //if (model.IsSeasonalMerchant)
            //{
            //    merchantForm.Add("new_monthsoutofseason", Convert.ToString(model.MonthsSeason));
            //}

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<OwnershipInformation> OwnershipInformationCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            OwnershipInformation model = new OwnershipInformation();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        #region Owner 1
                        model.Owner1.FirstName = Convert.ToString(jvalue[0].SelectToken("new_firstname1"));
                        model.Owner1.MiddleName = Convert.ToString(jvalue[0].SelectToken("new_middleint"));
                        model.Owner1.LastName = Convert.ToString(jvalue[0].SelectToken("new_lastname1"));
                        model.Owner1.Title = Convert.ToString(jvalue[0].SelectToken("new_title"));
                        model.Owner1.SSN = Convert.ToString(jvalue[0].SelectToken("new_ssn"));
                        model.Owner1.OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownership"));
                        DateTime DateOfBirth;
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth")), out DateOfBirth);
                        model.Owner1.DateOfBirth = DateOfBirth.Date;
                        model.Owner1.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress"));
                        model.Owner1.City = Convert.ToString(jvalue[0].SelectToken("new_city9"));
                        model.Owner1.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode9"));
                        model.Owner1.HomePhone = Convert.ToString(jvalue[0].SelectToken("new_homephone9"));
                        model.Owner1.EmailAddress = Convert.ToString(jvalue[0].SelectToken("new_emailaddress9"));
                        model.Owner1.DrivingLicenseNumber = Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber1"));
                        DateTime DrivingLicenseExpDate;
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate1")), out DrivingLicenseExpDate);
                        model.Owner1.DrivingLicenseExpDate = DrivingLicenseExpDate.Date;
                        //new_state9
                        //new_dlstate9
                        int SelectedState1 = model.Owner1.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state9"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state9")), out SelectedState1);
                            model.Owner1.SelectedState1 = SelectedState1;
                        }
                        int SelectedState2 = model.Owner1.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_dlstate9"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dlstate9")), out SelectedState2);
                            model.Owner1.SelectedState2 = SelectedState2;
                        }
                        #endregion

                        #region owner 2
                        model.Owner2.FirstName = Convert.ToString(jvalue[0].SelectToken("new_firstname2"));
                        model.Owner2.MiddleName = Convert.ToString(jvalue[0].SelectToken("new_middleint1"));
                        model.Owner2.LastName = Convert.ToString(jvalue[0].SelectToken("new_lastname2"));
                        model.Owner2.Title = Convert.ToString(jvalue[0].SelectToken("new_title1"));
                        model.Owner2.SSN = Convert.ToString(jvalue[0].SelectToken("new_ssn1"));
                        model.Owner2.OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownership1"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth1")), out DateOfBirth);
                        model.Owner2.DateOfBirth = DateOfBirth.Date;
                        model.Owner2.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress1"));
                        model.Owner2.City = Convert.ToString(jvalue[0].SelectToken("new_city10"));
                        model.Owner2.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode10"));
                        model.Owner2.HomePhone = Convert.ToString(jvalue[0].SelectToken("new_homephone10"));
                        model.Owner2.EmailAddress = Convert.ToString(jvalue[0].SelectToken("new_emailaddress10"));
                        model.Owner2.DrivingLicenseNumber = Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber2"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate2")), out DrivingLicenseExpDate);
                        model.Owner2.DrivingLicenseExpDate = DrivingLicenseExpDate.Date;
                        //new_state10
                        //new_dlstate10
                        SelectedState1 = model.Owner2.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state10"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state10")), out SelectedState1);
                            model.Owner2.SelectedState1 = SelectedState1;
                        }
                        SelectedState2 = model.Owner2.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_dlstate10"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dlstate10")), out SelectedState2);
                            model.Owner2.SelectedState2 = SelectedState2;
                        }
                        if (!string.IsNullOrEmpty(model.Owner2.FirstName) || !string.IsNullOrEmpty(model.Owner2.MiddleName)
                           || !string.IsNullOrEmpty(model.Owner2.LastName) || !string.IsNullOrEmpty(model.Owner2.EmailAddress))
                        {
                            model.IsOwner2 = true;
                        }
                        #endregion

                        #region Owner 3
                        model.Owner3.FirstName = Convert.ToString(jvalue[0].SelectToken("new_firstname4"));
                        model.Owner3.MiddleName = Convert.ToString(jvalue[0].SelectToken("new_middleint3"));
                        model.Owner3.LastName = Convert.ToString(jvalue[0].SelectToken("new_lastname4"));
                        model.Owner3.Title = Convert.ToString(jvalue[0].SelectToken("new_title3"));
                        model.Owner3.SSN = Convert.ToString(jvalue[0].SelectToken("new_ssn3"));
                        model.Owner3.OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownership3"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth3")), out DateOfBirth);
                        model.Owner3.DateOfBirth = DateOfBirth.Date;
                        model.Owner3.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress4"));
                        model.Owner3.City = Convert.ToString(jvalue[0].SelectToken("new_city12"));
                        model.Owner3.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode12"));
                        model.Owner3.HomePhone = Convert.ToString(jvalue[0].SelectToken("new_homephone12"));
                        model.Owner3.EmailAddress = Convert.ToString(jvalue[0].SelectToken("new_emailaddress12"));
                        model.Owner3.DrivingLicenseNumber = Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber4"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate4")), out DrivingLicenseExpDate);
                        model.Owner3.DrivingLicenseExpDate = DrivingLicenseExpDate.Date;
                        //new_state10
                        //new_dlstate10
                        SelectedState1 = model.Owner3.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state12"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state12")), out SelectedState1);
                            model.Owner3.SelectedState1 = SelectedState1;
                        }
                        SelectedState2 = model.Owner3.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_dlstate12"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dlstate12")), out SelectedState2);
                            model.Owner3.SelectedState2 = SelectedState2;
                        }
                        if (!string.IsNullOrEmpty(model.Owner3.FirstName) || !string.IsNullOrEmpty(model.Owner3.MiddleName)
                           || !string.IsNullOrEmpty(model.Owner3.LastName) || !string.IsNullOrEmpty(model.Owner3.EmailAddress))
                        {
                            model.IsOwner3 = true;
                        }
                        #endregion

                        #region Owner 4
                        model.Owner4.FirstName = Convert.ToString(jvalue[0].SelectToken("new_firstname5"));
                        model.Owner4.MiddleName = Convert.ToString(jvalue[0].SelectToken("new_middleint4"));
                        model.Owner4.LastName = Convert.ToString(jvalue[0].SelectToken("new_lastname5"));
                        model.Owner4.Title = Convert.ToString(jvalue[0].SelectToken("new_title4"));
                        model.Owner4.SSN = Convert.ToString(jvalue[0].SelectToken("new_ssn4"));
                        model.Owner4.OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownership4"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth4")), out DateOfBirth);
                        model.Owner4.DateOfBirth = DateOfBirth.Date;
                        model.Owner4.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress5"));
                        model.Owner4.City = Convert.ToString(jvalue[0].SelectToken("new_city13"));
                        model.Owner4.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode13"));
                        model.Owner4.HomePhone = Convert.ToString(jvalue[0].SelectToken("new_homephone13"));
                        model.Owner4.EmailAddress = Convert.ToString(jvalue[0].SelectToken("new_emailaddress13"));
                        model.Owner4.DrivingLicenseNumber = Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber5"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate5")), out DrivingLicenseExpDate);
                        model.Owner4.DrivingLicenseExpDate = DrivingLicenseExpDate.Date;
                        //new_state10
                        //new_dlstate10
                        SelectedState1 = model.Owner4.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state13"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state13")), out SelectedState1);
                            model.Owner4.SelectedState1 = SelectedState1;
                        }
                        SelectedState2 = model.Owner4.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_dlstate13"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dlstate13")), out SelectedState2);
                            model.Owner4.SelectedState2 = SelectedState2;
                        }
                        if (!string.IsNullOrEmpty(model.Owner4.FirstName) || !string.IsNullOrEmpty(model.Owner4.MiddleName)
                           || !string.IsNullOrEmpty(model.Owner4.LastName) || !string.IsNullOrEmpty(model.Owner4.EmailAddress))
                        {
                            model.IsOwner4 = true;
                        }
                        #endregion

                        #region ControllingOwner / Management
                        model.ControllingOwner.FirstName = Convert.ToString(jvalue[0].SelectToken("new_firstname3"));
                        model.ControllingOwner.MiddleName = Convert.ToString(jvalue[0].SelectToken("new_middleint2"));
                        model.ControllingOwner.LastName = Convert.ToString(jvalue[0].SelectToken("new_lastname3"));
                        model.ControllingOwner.Title = Convert.ToString(jvalue[0].SelectToken("new_title2"));
                        model.ControllingOwner.SSN = Convert.ToString(jvalue[0].SelectToken("new_ssn2"));
                        model.ControllingOwner.OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownseship2"));
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth2")), out DateOfBirth);
                        model.ControllingOwner.DateOfBirth = DateOfBirth.Date;
                        model.ControllingOwner.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress3"));
                        model.ControllingOwner.City = Convert.ToString(jvalue[0].SelectToken("new_city11"));
                        model.ControllingOwner.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode11"));
                        model.ControllingOwner.HomePhone = Convert.ToString(jvalue[0].SelectToken("new_homephone11"));
                        model.ControllingOwner.EmailAddress = Convert.ToString(jvalue[0].SelectToken("new_emailaddress11"));
                        model.ControllingOwner.ControllingInterest = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_controllinginterest")));
                        //model.ControllingOwner.DrivingLicenseNumber = Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber3"));
                        //DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate3")), out DrivingLicenseExpDate);
                        //model.ControllingOwner.DrivingLicenseExpDate = DrivingLicenseExpDate.Date;

                        SelectedState1 = model.ControllingOwner.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state11"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state11")), out SelectedState1);
                            model.ControllingOwner.SelectedState1 = SelectedState1;
                        }
                        SelectedState2 = model.ControllingOwner.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_dlstate11"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dlstate11")), out SelectedState2);
                            model.ControllingOwner.SelectedState2 = SelectedState2;
                        }
                        #endregion

                        model.BankName = bankName;

                        #region Get File from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //PBO1_DrivingLicense
                        noteModel = await GetFIleFromNote("PBO1_DrivingLicense", merchantId);
                        if (noteModel != null)
                        {
                            model.Owner1.FileName = noteModel.filename;
                            model.Owner1.FileBase64 = noteModel.documentbody;
                        }

                        //PBO2_DrivingLicense
                        noteModel = new NoteEntityModel();
                        noteModel = await GetFIleFromNote("PBO2_DrivingLicense", merchantId);
                        if (noteModel != null)
                        {
                            model.Owner2.FileName = noteModel.filename;
                            model.Owner2.FileBase64 = noteModel.documentbody;
                        }

                        //PBO3_DrivingLicense
                        noteModel = new NoteEntityModel();
                        noteModel = await GetFIleFromNote("PBO3_DrivingLicense", merchantId);
                        if (noteModel != null)
                        {
                            model.Owner3.FileName = noteModel.filename;
                            model.Owner3.FileBase64 = noteModel.documentbody;
                        }

                        //PBO4_DrivingLicense
                        noteModel = new NoteEntityModel();
                        noteModel = await GetFIleFromNote("PBO4_DrivingLicense", merchantId);
                        if (noteModel != null)
                        {
                            model.Owner4.FileName = noteModel.filename;
                            model.Owner4.FileBase64 = noteModel.documentbody;
                        }

                        //CPBO_DrivingLicense
                        //noteModel = new NoteEntityModel();
                        //noteModel = await GetFIleFromNote("CPBO_DrivingLicense", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.ControllingOwner.FileName = noteModel.filename;
                        //    model.ControllingOwner.FileBase64 = noteModel.documentbody;
                        //}
                        #endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> OwnershipInformationCRMPost(FormCollection fc, OwnershipInformation model)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_firstname1", model.Owner1.FirstName);
            merchantForm.Add("new_middleint", model.Owner1.MiddleName);
            merchantForm.Add("new_lastname1", model.Owner1.LastName);
            merchantForm.Add("new_title", model.Owner1.Title);
            merchantForm.Add("new_ssn", model.Owner1.SSN);
            model.Owner1.OwnershipPercent = (model.Owner1.OwnershipPercent ?? string.Empty).Replace("%", "");
            decimal OwnershipPercent;
            decimal.TryParse(model.Owner1.OwnershipPercent, out OwnershipPercent);
            merchantForm.Add("new_ownership", OwnershipPercent);
            if (!string.IsNullOrEmpty(model.Owner1.DateOfBirthString))
                merchantForm.Add("new_dateofbirth", model.Owner1.DateOfBirthString);
            merchantForm.Add("new_homeaddress", model.Owner1.LocationAddress);
            merchantForm.Add("new_city9", model.Owner1.City);
            merchantForm.Add("new_zipcode9", model.Owner1.Zip);
            merchantForm.Add("new_homephone9", model.Owner1.HomePhone);
            merchantForm.Add("new_emailaddress9", model.Owner1.EmailAddress);
            merchantForm.Add("new_state9", model.Owner1.SelectedState1);
            merchantForm.Add("new_dlstate9", model.Owner1.SelectedState2);
            merchantForm.Add("new_drivinglicensenumber1", model.Owner1.DrivingLicenseNumber);
            if (!string.IsNullOrEmpty(model.Owner1.DrivingLicenseExpDateString))
                merchantForm.Add("new_drivinglicenseexpdate1", model.Owner1.DrivingLicenseExpDateString);

            if (model.IsOwner2)
            {
                merchantForm.Add("new_firstname2", model.Owner2.FirstName);
                merchantForm.Add("new_middleint1", model.Owner2.MiddleName);
                merchantForm.Add("new_lastname2", model.Owner2.LastName);
                merchantForm.Add("new_title1", model.Owner2.Title);
                merchantForm.Add("new_ssn1", model.Owner2.SSN);
                model.Owner2.OwnershipPercent = (model.Owner2.OwnershipPercent ?? string.Empty).Replace("%", "");
                decimal.TryParse(model.Owner2.OwnershipPercent, out OwnershipPercent);
                merchantForm.Add("new_ownership1", OwnershipPercent);
                if (!string.IsNullOrEmpty(model.Owner2.DateOfBirthString))
                    merchantForm.Add("new_dateofbirth1", model.Owner2.DateOfBirthString);
                merchantForm.Add("new_homeaddress1", model.Owner2.LocationAddress);
                merchantForm.Add("new_city10", model.Owner2.City);
                merchantForm.Add("new_zipcode10", model.Owner2.Zip);
                merchantForm.Add("new_homephone10", model.Owner2.HomePhone);
                merchantForm.Add("new_emailaddress10", model.Owner2.EmailAddress);
                merchantForm.Add("new_state10", model.Owner2.SelectedState1);
                merchantForm.Add("new_dlstate10", model.Owner2.SelectedState2);
                merchantForm.Add("new_drivinglicensenumber2", model.Owner2.DrivingLicenseNumber);
                if (!string.IsNullOrEmpty(model.Owner2.DrivingLicenseExpDateString))
                    merchantForm.Add("new_drivinglicenseexpdate2", model.Owner2.DrivingLicenseExpDateString);
            }
            else
            {
                merchantForm.Add("new_firstname2", null);
                merchantForm.Add("new_middleint1", null);
                merchantForm.Add("new_lastname2", null);
                merchantForm.Add("new_emailaddress10", null);
            }

            if (model.IsOwner3)
            {
                merchantForm.Add("new_firstname4", model.Owner3.FirstName);
                merchantForm.Add("new_middleint3", model.Owner3.MiddleName);
                merchantForm.Add("new_lastname4", model.Owner3.LastName);
                merchantForm.Add("new_title3", model.Owner3.Title);
                merchantForm.Add("new_ssn3", model.Owner3.SSN);
                model.Owner3.OwnershipPercent = (model.Owner3.OwnershipPercent ?? string.Empty).Replace("%", "");
                decimal.TryParse(model.Owner3.OwnershipPercent, out OwnershipPercent);
                merchantForm.Add("new_ownership3", OwnershipPercent);
                if (!string.IsNullOrEmpty(model.Owner3.DateOfBirthString))
                    merchantForm.Add("new_dateofbirth3", model.Owner3.DateOfBirthString);
                merchantForm.Add("new_homeaddress4", model.Owner3.LocationAddress);
                merchantForm.Add("new_city12", model.Owner3.City);
                merchantForm.Add("new_zipcode12", model.Owner3.Zip);
                merchantForm.Add("new_homephone12", model.Owner3.HomePhone);
                merchantForm.Add("new_emailaddress12", model.Owner3.EmailAddress);
                merchantForm.Add("new_state12", model.Owner3.SelectedState1);
                merchantForm.Add("new_dlstate12", model.Owner3.SelectedState2);
                merchantForm.Add("new_drivinglicensenumber4", model.Owner3.DrivingLicenseNumber);
                if (!string.IsNullOrEmpty(model.Owner3.DrivingLicenseExpDateString))
                    merchantForm.Add("new_drivinglicenseexpdate4", model.Owner3.DrivingLicenseExpDateString);
            }
            else
            {
                merchantForm.Add("new_firstname4", null);
                merchantForm.Add("new_middleint3", null);
                merchantForm.Add("new_lastname4", null);
                merchantForm.Add("new_emailaddress12", null);
            }

            if (model.IsOwner4)
            {
                merchantForm.Add("new_firstname5", model.Owner4.FirstName);
                merchantForm.Add("new_middleint4", model.Owner4.MiddleName);
                merchantForm.Add("new_lastname5", model.Owner4.LastName);
                merchantForm.Add("new_title4", model.Owner4.Title);
                merchantForm.Add("new_ssn4", model.Owner4.SSN);
                model.Owner4.OwnershipPercent = (model.Owner4.OwnershipPercent ?? string.Empty).Replace("%", "");
                decimal.TryParse(model.Owner4.OwnershipPercent, out OwnershipPercent);
                merchantForm.Add("new_ownership4", OwnershipPercent);
                if (!string.IsNullOrEmpty(model.Owner4.DateOfBirthString))
                    merchantForm.Add("new_dateofbirth4", model.Owner4.DateOfBirthString);
                merchantForm.Add("new_homeaddress5", model.Owner4.LocationAddress);
                merchantForm.Add("new_city13", model.Owner4.City);
                merchantForm.Add("new_zipcode13", model.Owner4.Zip);
                merchantForm.Add("new_homephone13", model.Owner4.HomePhone);
                merchantForm.Add("new_emailaddress13", model.Owner4.EmailAddress);
                merchantForm.Add("new_state13", model.Owner4.SelectedState1);
                merchantForm.Add("new_dlstate13", model.Owner4.SelectedState2);
                merchantForm.Add("new_drivinglicensenumber5", model.Owner4.DrivingLicenseNumber);
                if (!string.IsNullOrEmpty(model.Owner4.DrivingLicenseExpDateString))
                    merchantForm.Add("new_drivinglicenseexpdate5", model.Owner4.DrivingLicenseExpDateString);
            }
            else
            {
                merchantForm.Add("new_firstname5", null);
                merchantForm.Add("new_middleint4", null);
                merchantForm.Add("new_lastname5", null);
                merchantForm.Add("new_emailaddress13", null);
            }

            merchantForm.Add("new_firstname3", model.ControllingOwner.FirstName);
            merchantForm.Add("new_middleint2", model.ControllingOwner.MiddleName);
            merchantForm.Add("new_lastname3", model.ControllingOwner.LastName);
            merchantForm.Add("new_title2", model.ControllingOwner.Title);
            merchantForm.Add("new_ssn2", model.ControllingOwner.SSN);
            model.ControllingOwner.OwnershipPercent = (model.ControllingOwner.OwnershipPercent ?? string.Empty).Replace("%", "");
            decimal.TryParse(model.ControllingOwner.OwnershipPercent, out OwnershipPercent);
            merchantForm.Add("new_ownseship2", OwnershipPercent);
            if (!string.IsNullOrEmpty(model.ControllingOwner.DateOfBirthString))
                merchantForm.Add("new_dateofbirth2", model.ControllingOwner.DateOfBirthString);
            merchantForm.Add("new_homeaddress3", model.ControllingOwner.LocationAddress);
            merchantForm.Add("new_city11", model.ControllingOwner.City);
            merchantForm.Add("new_zipcode11", model.ControllingOwner.Zip);
            merchantForm.Add("new_homephone11", model.ControllingOwner.HomePhone);
            merchantForm.Add("new_emailaddress11", model.ControllingOwner.EmailAddress);
            merchantForm.Add("new_controllinginterest", model.ControllingOwner.ControllingInterest);
            merchantForm.Add("new_state11", model.ControllingOwner.SelectedState1);
            merchantForm.Add("new_dlstate11", model.ControllingOwner.SelectedState2);
            //merchantForm.Add("new_drivinglicensenumber3", model.ControllingOwner.DrivingLicenseNumber);
            //if (!string.IsNullOrEmpty(model.ControllingOwner.DrivingLicenseExpDateString))
            //    merchantForm.Add("new_drivinglicenseexpdate3", model.ControllingOwner.DrivingLicenseExpDateString);

            #region File Upload in Note entity
            //PBO1_DrivingLicense
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "Please Upload your Driving License - Owner 1";
            noteModel.subject = "PBO1_DrivingLicense";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            bool noteResult = await UploadFIleInNote(noteModel, 0);
            if (noteResult)
                merchantForm.Add("new_pbo1_drivinglicense", true);

            //PBO2_DrivingLicense
            noteModel = new NoteEntityModel();
            noteModel.notetext = "Please Upload your Driving License - Owner 2";
            noteModel.subject = "PBO2_DrivingLicense";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 1);
            if (noteResult)
                merchantForm.Add("new_pbo2_drivinglicense", true);

            //PBO3_DrivingLicense
            noteModel = new NoteEntityModel();
            noteModel.notetext = "Please Upload your Driving License - Owner 3";
            noteModel.subject = "PBO3_DrivingLicense";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 2);
            if (noteResult)
                merchantForm.Add("new_pbo3_drivinglicense", true);

            //PBO4_DrivingLicense
            noteModel = new NoteEntityModel();
            noteModel.notetext = "Please Upload your Driving License - Owner 4";
            noteModel.subject = "PBO4_DrivingLicense";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 3);
            if (noteResult)
                merchantForm.Add("new_pbo4_drivinglicense", true);

            //CPBO_DrivingLicense
            //noteModel = new NoteEntityModel();
            //noteModel.notetext = "Please Upload your Driving License - ControllingOwner";
            //noteModel.subject = "CPBO_DrivingLicense";
            //noteModel.LookupEntity = "new_merchantboarding";
            //noteModel.entityId = model.MerchantId;
            //noteResult = await UploadFIleInNote(noteModel, 2);
            //if (noteResult)
            //    merchantForm.Add("new_cpbo_drivinglicense", true);
            #endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<SiteInspectionModel> SiteInspectionCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            SiteInspectionModel model = new SiteInspectionModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.Comment = Convert.ToString(jvalue[0].SelectToken("new_comment")); ;
                        model.InspectorName = Convert.ToString(jvalue[0].SelectToken("new_inspectorname"));
                        DateTime InspectorDate;
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_inspectiondate")), out InspectorDate);
                        model.InspectorDate = InspectorDate.Date;
                        model.OperateBusiness = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_baseduponisosreviewdoesmerchanthavetheap")));

                        int SelectedState1 = model.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_merchanttype"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_merchanttype")), out SelectedState1);
                            model.SelectedState1 = SelectedState1;
                        }
                        int SelectedState2 = model.SelectedState2 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_buildingtype"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_buildingtype")), out SelectedState2);
                            model.SelectedState2 = SelectedState2;
                        }
                        int SelectedState3 = model.SelectedState3 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_areazone"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_areazone")), out SelectedState3);
                            model.SelectedState3 = SelectedState3;
                        }
                        int SelectedState4 = model.SelectedState4 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_squarefootage"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_squarefootage")), out SelectedState4);
                            model.SelectedState4 = SelectedState4;
                        }

                        model.BankName = bankName;

                        #region Get Signature from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //SiteInspection_Signature
                        noteModel = await GetFIleFromNote("SiteInspection_Signature", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase64 = noteModel.documentbody;
                            model.FileName = noteModel.filename;
                        }
                        #endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> SiteInspectionCRMPost(FormCollection fc, SiteInspectionModel model)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model         
            merchantForm.Add("new_baseduponisosreviewdoesmerchanthavetheap", model.OperateBusiness);
            merchantForm.Add("new_comment", model.Comment);
            merchantForm.Add("new_inspectorname", model.InspectorName);

            merchantForm.Add("new_merchanttype", model.SelectedState1);
            merchantForm.Add("new_buildingtype", model.SelectedState2);
            merchantForm.Add("new_areazone", model.SelectedState3);
            merchantForm.Add("new_squarefootage", model.SelectedState4);
            if (!string.IsNullOrEmpty(model.InspectorDateString))
                merchantForm.Add("new_inspectiondate", model.InspectorDateString);

            #region Signature Upload in Note entity
            //SiteInspection_Signature
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "";
            noteModel.subject = "SiteInspection_Signature";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
            bool noteResult = await UploadFIleInNote(noteModel, -1);
            if (noteResult)
                merchantForm.Add("", true);
            #endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<EquipmentInformationModel> EquipmentInformationCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            EquipmentInformationModel model = new EquipmentInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            model.BankName = bankName;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.new_primarycommunicationtypecsv = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_primarycommunicationtypecsv")), model.new_primarycommunicationtypecsv);

                        model.new_gateway = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_gateway")));
                        if (model.new_gateway)
                        {
                            model.new_gatewayname1 = Convert.ToString(jvalue[0].SelectToken("new_gatewayname1"));
                            model.new_gatewayversion = Convert.ToString(jvalue[0].SelectToken("new_gatewayversion"));
                        }

                        model.new_merchantprogramtypecsv = SetCheckBoxValues(Convert.ToString(jvalue[0].SelectToken("new_merchantprogramtypecsv")), model.new_merchantprogramtypecsv);
                        if (Convert.ToString(jvalue[0].SelectToken("new_merchantprogramtypecsv")).Contains("8"))
                        {
                            model.new_otherebtetc = Convert.ToString(jvalue[0].SelectToken("new_otherebtetc"));
                        }

                        model.new_pcsoftwarename = Convert.ToString(jvalue[0].SelectToken("new_pcsoftwarename"));
                        model.new_softwareversion = Convert.ToString(jvalue[0].SelectToken("new_softwareversion"));
                        model.new_qircertifierfirstlastname = Convert.ToString(jvalue[0].SelectToken("new_qircertifierfirstlastname"));

                        model.new_batchoptions = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_batchoptions")));
                        if (!model.new_batchoptions)
                        {
                            model.new_autobatchtime = Convert.ToString(jvalue[0].SelectToken("new_autobatchtime"));
                        }
                        else
                        {
                            model.new_manualbatchtime = Convert.ToString(jvalue[0].SelectToken("new_manualbatchtime"));
                        }
                    }
                }
            }
            return model;
        }
        public async Task<bool> EquipmentInformationCRMPost(FormCollection fc, EquipmentInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_primarycommunicationtypecsv", GetCheckboxValues(model.new_primarycommunicationtypecsv));

            merchantForm.Add("new_gateway", model.new_gateway);
            if (model.new_gateway)
            {
                merchantForm.Add("new_gatewayname1", model.new_gatewayname1);
                merchantForm.Add("new_gatewayversion", model.new_gatewayversion);
            }

            string checkboxes = GetCheckboxValues(model.new_merchantprogramtypecsv);
            merchantForm.Add("new_merchantprogramtypecsv", checkboxes);
            if (!string.IsNullOrEmpty(checkboxes) && checkboxes.Contains("8"))
            {
                merchantForm.Add("new_otherebtetc", model.new_otherebtetc);
            }

            merchantForm.Add("new_pcsoftwarename", model.new_pcsoftwarename);
            merchantForm.Add("new_softwareversion", model.new_softwareversion);
            merchantForm.Add("new_qircertifierfirstlastname", model.new_qircertifierfirstlastname);

            merchantForm.Add("new_batchoptions", model.new_batchoptions);
            if (!model.new_batchoptions)
            {
                merchantForm.Add("new_autobatchtime", model.new_autobatchtime);
            }
            else
            {
                merchantForm.Add("new_manualbatchtime", model.new_manualbatchtime);
            }


            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }
        #endregion

        #region All Forms CRM Methods - Bank Template
        public async Task<BankDisclosureModel> BankDisclosureCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            BankDisclosureModel model = new BankDisclosureModel();

            #region Bank Template
            if (!string.IsNullOrEmpty(bankName))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_name eq '" + bankName + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_banktemplates" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string bankId = Convert.ToString(jvalue[0].SelectToken("new_banktemplateid"));
                        model.BankUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")";
                        var topic = _topicService.GetTopicBySystemName("BankDisclosure");
                        if (topic != null)
                        {
                            model.Description = topic.Body;
                        }
                        //model.Description = Convert.ToString(jvalue[0].SelectToken("new_bankformattemplate"));
                        model.BankName = bankName;
                        model.CustomerEmail = _workContext.CurrentCustomer.Email;
                    }
                }
            }
            #endregion

            #region Merchant Boarding
            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));

                        #region Get Signature from Note entity -
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //BankDisclosure_MerchantSignature
                        noteModel = await GetFIleFromNote("BankDisclosure_MerchantSignature", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase64 = noteModel.documentbody;
                            model.FileName = noteModel.filename;
                        }
                        #endregion

                    }
                }
            }
            #endregion
            return model;
        }
        public async Task<bool> BankDisclosureCRMPost(FormCollection fc, BankDisclosureModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model     

                #region Signature Upload in Note entity
                //BankDisclosure_MerchantSignature
                NoteEntityModel noteModel = new NoteEntityModel();
                noteModel.notetext = "Merchant Signature/Title";
                noteModel.subject = "BankDisclosure_MerchantSignature";
                noteModel.LookupEntity = "new_merchantboarding";
                noteModel.entityId = model.MerchantId;
                noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                bool noteResult = await UploadFIleInNote(noteModel, -1);
                if (noteResult)
                    merchantForm.Add("new_bankdisclosure_merchantsignature", true);
                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        //throw new CrmHttpResponseException(updateResponse.Content);
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<BankingInformationModel> BankingInformationCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            BankingInformationModel model = new BankingInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            #region Merchant Boarding
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = Convert.ToString(jvalue[0].SelectToken("new_merchantboardingid"));
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.AccountBankName = Convert.ToString(jvalue[0].SelectToken("new_bankname"));
                        model.Transit = Convert.ToString(jvalue[0].SelectToken("new_transitabarouting"));
                        model.Account = Convert.ToString(jvalue[0].SelectToken("new_accountdda"));
                        model.AccountContact = Convert.ToString(jvalue[0].SelectToken("new_contact"));
                        //model.AccountPhone = Convert.ToString(jvalue[0].SelectToken("new_phone"));
                        model.Owner1Name = string.Join(" ",
                            Convert.ToString(jvalue[0].SelectToken("new_firstname1")),
                            Convert.ToString(jvalue[0].SelectToken("new_middleint")),
                            Convert.ToString(jvalue[0].SelectToken("new_lastname1")),
                            Convert.ToString(jvalue[0].SelectToken("new_title")));
                        model.Owner2Name = string.Join(" ",Convert.ToString(jvalue[0].SelectToken("new_firstname2")),
                            Convert.ToString(jvalue[0].SelectToken("new_middleint1")),
                            Convert.ToString(jvalue[0].SelectToken("new_lastname2")),
                            Convert.ToString(jvalue[0].SelectToken("new_title1")));

                        #region Get File from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //BankInfo_BankStatement
                        noteModel = await GetFIleFromNote("BankInfo_BankStatement", merchantId);
                        if (noteModel != null)
                        {
                            model.FileName = noteModel.filename;
                            model.FileBase64 = noteModel.documentbody;
                        }
                        #endregion

                        // not needed now, we just showing checkbox to accept instead signature
                        #region Get Signature from Note entity
                        //noteModel = new NoteEntityModel();
                        ////BankInfo_SignaturePrincipal1
                        //noteModel = await GetFIleFromNote("BankInfo_SignaturePrincipal1", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase642 = noteModel.documentbody;
                        //    model.FileName2 = noteModel.filename;
                        //}

                        //noteModel = new NoteEntityModel();
                        ////BankInfo_SignaturePrincipal2
                        //noteModel = await GetFIleFromNote("BankInfo_SignaturePrincipal2", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase643 = noteModel.documentbody;
                        //    model.FileName3 = noteModel.filename;
                        //}
                        #endregion
                    }
                }
            }
            #endregion

            #region Bank Template - No need of GuarantorDescription now from CRM, it is moved to last form and render from topic CMS
            //// Bank template fetching
            //if (!string.IsNullOrEmpty(bankName))
            //{
            //    // Get merchant details from customer email
            //    //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
            //    string queryOptions = "?$filter=new_name eq '" + bankName + "'";
            //    HttpResponseMessage merchantResponse = await httpClient.GetAsync(
            //   getVersionedWebAPIPath() + "new_banktemplates" + queryOptions);
            //    if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
            //    {
            //        retrievedData = JsonConvert.DeserializeObject<JObject>(
            //            await merchantResponse.Content.ReadAsStringAsync());
            //    }
            //    else
            //    {
            //        throw new CrmHttpResponseException(merchantResponse.Content);
            //    }
            //    if (retrievedData != null)
            //    {
            //        var jvalue = retrievedData.GetValue("value");
            //        if (jvalue != null && jvalue.Count() > 0)
            //        {
            //            string bankId = Convert.ToString(jvalue[0].SelectToken("new_banktemplateid"));
            //            model.BankUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")";
            //            model.GuarantorDescription = Convert.ToString(jvalue[0].SelectToken("new_continuingpersonalguarantyprovision"));                        
            //        }
            //    }
            //}
            #endregion

            model.CustomerEmail = _workContext.CurrentCustomer.Email;
            model.BankName = bankName;
            return model;
        }
        public async Task<bool> BankingInformationCRMPost(FormCollection fc, BankingInformationModel model)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model         
            merchantForm.Add("new_bankname", model.AccountBankName);
            merchantForm.Add("new_transitabarouting", model.Transit);
            merchantForm.Add("new_accountdda", model.Account);
            merchantForm.Add("new_contact", model.AccountContact);
            //merchantForm.Add("new_phone", model.AccountPhone);

            #region File Upload in Note entity
            //BankInfo_BankStatement
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "Please Upload Your Bank Statement Of Last 3 Months";
            noteModel.subject = "BankInfo_BankStatement";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            bool noteResult = await UploadFIleInNote(noteModel, 0);
            if (noteResult)
                merchantForm.Add("new_bankinfo_bankstatement", true);
            #endregion

            // not needed now, we just showing checkbox to accept instead signature
            #region Signature Upload in Note entity
            ////BankInfo_SignaturePrincipal1
            //noteModel = new NoteEntityModel();
            //noteModel.notetext = "Signature Principal #1";
            //noteModel.subject = "BankInfo_SignaturePrincipal1";
            //noteModel.LookupEntity = "new_merchantboarding";
            //noteModel.entityId = model.MerchantId;
            //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase642) ? model.FileBase642.Replace("data:image/png;base64,", "") : model.FileBase642);
            //noteResult = await UploadFIleInNote(noteModel, -1);
            //if (noteResult)
            //    merchantForm.Add("new_bankinfo_signatureprincipal1", true);

            ////BankInfo_SignaturePrincipal2
            //noteModel = new NoteEntityModel();
            //noteModel.notetext = "Signature Principal #2";
            //noteModel.subject = "BankInfo_SignaturePrincipal2";
            //noteModel.LookupEntity = "new_merchantboarding";
            //noteModel.entityId = model.MerchantId;
            //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase643) ? model.FileBase643.Replace("data:image/png;base64,", "") : model.FileBase643);
            //noteResult = await UploadFIleInNote(noteModel, -1);
            //if (noteResult)
            //    merchantForm.Add("new_bankinfo_signatureprincipal2", true);
            #endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<ImportantInformationModel> ImportantInformationCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            ImportantInformationModel model = new ImportantInformationModel();
            var topic = _topicService.GetTopicBySystemName("FeeScheduleDescription");
            if (topic != null)
            {
                model.NewAccountDescription = topic.Body;
            }
            model.BankName = bankName;
            model.CustomerEmail = _workContext.CurrentCustomer.Email;

            #region Bank Template - Not in use currently. Instead NewAccountDescription is used by topic
            //if (!string.IsNullOrEmpty(bankName))
            //{
            //    // Get merchant details from customer email
            //    //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
            //    string queryOptions = "?$filter=new_name eq '" + bankName + "'";
            //    HttpResponseMessage merchantResponse = await httpClient.GetAsync(
            //   getVersionedWebAPIPath() + "new_banktemplates" + queryOptions);
            //    if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
            //    {
            //        retrievedData = JsonConvert.DeserializeObject<JObject>(
            //            await merchantResponse.Content.ReadAsStringAsync());
            //    }
            //    else
            //    {
            //        throw new CrmHttpResponseException(merchantResponse.Content);
            //    }
            //    if (retrievedData != null)
            //    {
            //        var jvalue = retrievedData.GetValue("value");
            //        if (jvalue != null && jvalue.Count() > 0)
            //        {
            //            string bankId = Convert.ToString(jvalue[0].SelectToken("new_banktemplateid"));
            //            model.BankUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")";                      
            //            //model.NewAccountDescription = Convert.ToString(jvalue[0].SelectToken("new_merchantapplicationandagreementacceptance"));
            //            //model.CertificationDescription = Convert.ToString(jvalue[0].SelectToken("new_certificationofbeneficialowners"));
            //            model.BankName = bankName;
            //            model.CustomerEmail = _workContext.CurrentCustomer.Email;
            //        }
            //    }
            //}
            #endregion

            #region Merchant Boarding
            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        model.new_importantinformationcheck = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_importantinformationcheck")));

                        model.Owner1Name = Convert.ToString(jvalue[0].SelectToken("new_firstname1")) + " " +
                            Convert.ToString(jvalue[0].SelectToken("new_lastname1"));

                        model.Owner2Name = Convert.ToString(jvalue[0].SelectToken("new_firstname2")) + " " +
                      Convert.ToString(jvalue[0].SelectToken("new_lastname2"));

                        #region Get Signature from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //ImpInfo_Signature1
                        noteModel = await GetFIleFromNote("ImpInfo_Signature1", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase64 = noteModel.documentbody;
                            model.FileName = noteModel.filename;
                        }

                        noteModel = new NoteEntityModel();
                        //ImpInfo_Signature2
                        noteModel = await GetFIleFromNote("ImpInfo_Signature2", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase642 = noteModel.documentbody;
                            model.FileName2 = noteModel.filename;
                        }

                        // It will move to PersonalGuarantee
                        //noteModel = new NoteEntityModel();
                        ////CertificationOfBO_Signature
                        //noteModel = await GetFIleFromNote("CertificationOfBO_Signature", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase643 = noteModel.documentbody;
                        //    model.FileName3 = noteModel.filename;
                        //}
                        #endregion

                        #region Merchant Fees fields
                        model.new_visamcdistiered1 = Convert.ToString(jvalue[0].SelectToken("new_visamcdistiered1"));
                        model.new_visamcdispassthru1 = Convert.ToString(jvalue[0].SelectToken("new_visamcdispassthru1"));
                        model.new_visamcdistiered2 = Convert.ToString(jvalue[0].SelectToken("new_visamcdistiered2"));
                        model.new_visamcdispassthru2 = Convert.ToString(jvalue[0].SelectToken("new_visamcdispassthru2"));
                        model.new_authfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_authfee")));
                        model.new_monthlymanagfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlymanagfee")));   
                        model.new_onlineservicefee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_onlineservicefee")));
                        model.MonthlyMinimum = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlyminimum")));
                        model.AnualFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_annualfee")));
                        model.PinDebitFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_pindebittransactionfee")));
                        model.BatchFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_batchsettlementfee")));
                        model.ChargebackPer = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackperchargeback")));
                        model.PerAchRejectFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_perachrejectfee")));
                        model.ChargebackRetrieval = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackretrievalperretrievaltype")));
                        model.VoicePerAuthFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_voiceperauthfee")));
                        model.new_passthrufee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_passthrufee")));
                        model.new_opvoiceperauthfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_opvoiceperauthfee")));
                        model.new_transfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_transfee")));
                        model.AvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_avsfeeaddressverificationservice")));
                        model.EarlyTerminationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_earlyterminationfee")));
                        model.new_ebttransfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ebttransfee")));
                        model.new_ebtstatementfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ebtstatementfee")));
                        model.new_otherfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_otherfee")));
                        model.new_visamcdisaxpmidqual = Convert.ToString(jvalue[0].SelectToken("new_visamcdisaxpmidqual"));
                        model.new_visamcdisaxpnonqual = Convert.ToString(jvalue[0].SelectToken("new_visamcdisaxpnonqual"));
                        model.NonEmvCompliance = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_government_compliance_fee")));
                        model.new_tinmismatchfee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_tinmismatchfee")));
                        model.MonthlyPciFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlypcifee")));
                        model.NonPciComplianceFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_nonpcicompliancefee")));
                        #endregion
                    }
                }
            }
            #endregion
            return model;
        }

        public async Task<bool> ImportantInformationCRMPost(FormCollection fc, ImportantInformationModel model)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            merchantForm.Add("new_importantinformationcheck", model.new_importantinformationcheck);

            // Generate json object from model
            #region Signature Upload in Note entity
            //ImpInfo_Signature1
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "Signature #1";
            noteModel.subject = "ImpInfo_Signature1";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
            bool noteResult = await UploadFIleInNote(noteModel, -1);
            if (noteResult)
                merchantForm.Add("new_impinfo_signature1", true);

            //ImpInfo_Signature2
            noteModel = new NoteEntityModel();
            noteModel.notetext = "Signature #2	Signature #2";
            noteModel.subject = "ImpInfo_Signature2";
            noteModel.LookupEntity = "new_merchantboarding";
            noteModel.entityId = model.MerchantId;
            noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase642) ? model.FileBase642.Replace("data:image/png;base64,", "") : model.FileBase642);
            noteResult = await UploadFIleInNote(noteModel, -1);
            if (noteResult)
                merchantForm.Add("new_impinfo_signature2", true);

            //CertificationOfBO_Signature - Will move to PersonalGuarantee
            //noteModel = new NoteEntityModel();
            //noteModel.notetext = "CERTIFICATION OF BENEFICIAL OWNER(S)";
            //noteModel.subject = "CertificationOfBO_Signature";
            //noteModel.LookupEntity = "new_merchantboarding";
            //noteModel.entityId = model.MerchantId;
            //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase643) ? model.FileBase643.Replace("data:image/png;base64,", "") : model.FileBase643);
            //noteResult = await UploadFIleInNote(noteModel, -1);
            //if (noteResult)
            //    merchantForm.Add("new_certificationofbo_signature", true);
            #endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    //throw new CrmHttpResponseException(updateResponse.Content);
                    return false;
                }
            }
            return false;
        }

        public async Task<RatesFeesModel> RatesFeesCRMGet(string bankName)
        {
            // All Fees from Rates & Fees are moved to my sccount, so do not need those fees at here
            // We need only input fields of "Rates & Fees"

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            RatesFeesModel model = new RatesFeesModel();
            model.BankName = bankName;
            model.CustomerEmail = _workContext.CurrentCustomer.Email;
            //if (!string.IsNullOrEmpty(bankName))
            //{
            //    // Get merchant details from customer email
            //    //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
            //    string queryOptions = "?$filter=new_name eq '" + bankName + "'";
            //    HttpResponseMessage merchantResponse = await httpClient.GetAsync(
            //   getVersionedWebAPIPath() + "new_banktemplates" + queryOptions);
            //    if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
            //    {
            //        retrievedData = JsonConvert.DeserializeObject<JObject>(
            //            await merchantResponse.Content.ReadAsStringAsync());
            //    }
            //    else
            //    {
            //        throw new CrmHttpResponseException(merchantResponse.Content);
            //    }
            //    if (retrievedData != null)
            //    {
            //        var jvalue = retrievedData.GetValue("value");
            //        if (jvalue != null && jvalue.Count() > 0)
            //        {
            //            string bankId = Convert.ToString(jvalue[0].SelectToken("new_banktemplateid"));
            //            model.BankUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")";
            //            model.BankName = bankName;
            //            model.CustomerEmail = _workContext.CurrentCustomer.Email;

            //            model.MonthlyMinimum = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlyminimum")));
            //            model.MonthlyPciFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlypcifee")));
            //            model.TransactionFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_visamcdiscovertransactionfee")));
            //            model.MonthlyCustomerSvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlycustomersvsfee")));
            //            model.NonPciComplianceFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_nonpcicompliancefee")));
            //            model.AmexFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_amextransactionfee")));
            //            model.RegulatoryFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_irsregulatoryfee")));
            //            model.PinDebitFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_pindebittransactionfee")));
            //            model.AnualFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_annualfee")));
            //            model.NonEmvCompliance = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_government_compliance_fee")));
            //            model.VoicePerAuthFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_voiceperauthfee")));
            //            model.MerchantReporting = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merchantonlineportalreporting")));
            //            model.EarlyTerminationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_earlyterminationfee")));
            //            model.EmvTransaction = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvtransactionperdevice")));
            //            model.G2Monitoring = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_g2monitoring")));
            //            model.ApplicationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_applicationfee")));
            //            model.BatchFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_batchsettlementfee")));
            //            model.ExcessiveHelpDeskCall = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_excessivehelpdeskcalls")));
            //            model.AvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_avsfeeaddressverificationservice")));
            //            model.EmvRecidencyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvresidencyfee")));
            //            model.MerlinkChargeBack = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargeback")));
            //            model.IsMerlinkChargeBack = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargebackbool")));
            //        }
            //    }
            //}
            return model;
        }

        public async Task<PersonalGuaranteeModel> PersonalGuaranteeCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            PersonalGuaranteeModel model = new PersonalGuaranteeModel();
            string bankId = string.Empty;            

            #region Merchant Boarding
            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        #region Get Signature from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        noteModel = new NoteEntityModel();
                        //CertificationOfBO_Signature
                        noteModel = await GetFIleFromNote("CertificationOfBO_Signature", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase643 = noteModel.documentbody;
                            model.FileName3 = noteModel.filename;
                        }

                        //Equipment_MerchantSignature - not in use
                        //noteModel = await GetFIleFromNote("Equipment_MerchantSignature", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase64 = noteModel.documentbody;
                        //    model.FileName = noteModel.filename;
                        //}
                        #endregion

                    }
                }
            }
            #endregion            

            return model;
        }
        public async Task<bool> PersonalGuaranteeCRMPost(FormCollection fc, PersonalGuaranteeModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model
                #region Signature Upload in Note entity
                //Equipment_MerchantSignature - Not in use
                //NoteEntityModel noteModel = new NoteEntityModel();
                //noteModel.notetext = "Authorized Merchant Signature";
                //noteModel.subject = "Equipment_MerchantSignature";
                //noteModel.LookupEntity = "new_merchantboarding";
                //noteModel.entityId = model.MerchantId;
                //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                //bool noteResult = await UploadFIleInNote(noteModel, -1);
                //if (noteResult)
                //    merchantForm.Add("new_equipment_merchantsignature", true);

                //CertificationOfBO_Signature
                NoteEntityModel noteModel = new NoteEntityModel();
                noteModel.notetext = "CERTIFICATION OF BENEFICIAL OWNER(S)";
                noteModel.subject = "CertificationOfBO_Signature";
                noteModel.LookupEntity = "new_merchantboarding";
                noteModel.entityId = model.MerchantId;
                noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase643) ? model.FileBase643.Replace("data:image/png;base64,", "") : model.FileBase643);
                bool noteResult = await UploadFIleInNote(noteModel, -1);
                if (noteResult)
                    merchantForm.Add("new_certificationofbo_signature", true);

                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        //throw new CrmHttpResponseException(updateResponse.Content);
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<RecurringFeesModel> MerchantFeesCRMGet(string bankName)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            RecurringFeesModel model = new RecurringFeesModel();
            string bankId = string.Empty;
            #region Bank Template - Get bank Id
            if (!string.IsNullOrEmpty(bankName))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_name eq '" + bankName + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_banktemplates" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        bankId = Convert.ToString(jvalue[0].SelectToken("new_banktemplateid"));
                        //model.BankUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")";
                        //model.BankName = bankName;
                        //model.CustomerEmail = _workContext.CurrentCustomer.Email;

                        //model.MonthlyMinimum = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlyminimum")));
                        //model.MonthlyPciFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlypcifee")));
                        //model.TransactionFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_visamcdiscovertransactionfee")));
                        //model.MonthlyCustomerSvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlycustomersvsfee")));
                        //model.NonPciComplianceFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_nonpcicompliancefee")));
                        //model.AmexFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_amextransactionfee")));
                        //model.RegulatoryFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_irsregulatoryfee")));
                        //model.PinDebitFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_pindebittransactionfee")));
                        //model.AnualFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_annualfee")));
                        //model.NonEmvCompliance = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_nonemvcompliance")));
                        //model.VoicePerAuthFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_voiceperauthfee")));
                        //model.MerchantReporting = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merchantonlineportalreporting")));
                        //model.EarlyTerminationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_earlyterminationfee")));
                        //model.EmvTransaction = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvtransactionperdevice")));
                        //model.G2Monitoring = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_g2monitoring")));
                        //model.ApplicationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_applicationfee")));
                        //model.BatchFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_batchsettlementfee")));
                        //model.ExcessiveHelpDeskCall = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_excessivehelpdeskcalls")));
                        //model.AvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_avsfeeaddressverificationservice")));
                        //model.EmvRecidencyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvresidencyfee")));
                        //model.MerlinkChargeBack = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargeback")));
                        //model.IsMerlinkChargeBack = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargebackbool")));

                        //model.MultipassSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipasssetupfee")));
                        //model.MultipassGatewayFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipassgatewaymonthlyfee")));
                        //model.MultipassPerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipasspertransfee")));
                        //model.EnsureBillSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillsetupfee")));
                        //model.EnsureBillMonthlyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillmonthlyfee")));
                        //model.EnsureBillItemFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillupdateperitemfee")));
                        //model.MobileSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_wirelessmobilesetupfee")));
                        //model.MonthlyMobileFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlywirelessmobilefee")));
                        //model.MobilePerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_wirelessmobilepertransfee")));
                        //model.GatewaySetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaysetupfee")));
                        //model.GatewayMonthlyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaymonthlyfee")));
                        //model.GatewayPerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaypertransfee")));

                        //model.ChargebackPer = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackperchargeback")));
                        //model.ChargebackReversals = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackreversalsperreversal")));
                        //model.ChargebackRetrieval = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackretrievalperretrievaltype")));
                        //model.ChargebackPreArbitration = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackprearbitrationprearbitration")));
                        //model.PerAchRejectFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_perachrejectfee")));
                        //model.FANF = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_visafixedacquirernetworkfeefanf")));
                    }
                }
            }
            #endregion

            #region Merchant Boarding
            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email                
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);

                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantboardingid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantboardings(" + merchantId + ")";

                        #region Merchant Fees fields
                        model.MonthlyMinimum = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlyminimum")));
                        model.MonthlyPciFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlypcifee")));
                        model.TransactionFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_visamcdiscovertransactionfee")));
                        model.MonthlyCustomerSvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlycustomersvsfee")));
                        model.NonPciComplianceFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_nonpcicompliancefee")));
                        model.AmexFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_amextransactionfee")));
                        model.RegulatoryFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_irsregulatoryfee")));
                        model.PinDebitFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_pindebittransactionfee")));
                        model.AnualFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_annualfee")));
                        model.NonEmvCompliance = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_government_compliance_fee")));
                        model.VoicePerAuthFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_voiceperauthfee")));
                        model.MerchantReporting = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merchantonlineportalreporting")));
                        model.EarlyTerminationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_earlyterminationfee")));
                        model.EmvTransaction = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvtransactionperdevice")));
                        model.G2Monitoring = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_g2monitoring")));
                        model.ApplicationFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_applicationfee")));
                        model.BatchFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_batchsettlementfee")));
                        model.ExcessiveHelpDeskCall = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_excessivehelpdeskcalls")));
                        model.AvsFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_avsfeeaddressverificationservice")));
                        model.EmvRecidencyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_emvresidencyfee")));
                        model.MerlinkChargeBack = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargeback")));
                        model.IsMerlinkChargeBack = ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_merlinkchargebackbool")));

                        model.MultipassSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipasssetupfee")));
                        model.MultipassGatewayFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipassgatewaymonthlyfee")));
                        model.MultipassPerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_multipasspertransfee")));
                        model.EnsureBillSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillsetupfee")));
                        model.EnsureBillMonthlyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillmonthlyfee")));
                        model.EnsureBillItemFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_ensurebillupdateperitemfee")));
                        model.MobileSetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_wirelessmobilesetupfee")));
                        model.MonthlyMobileFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_monthlywirelessmobilefee")));
                        model.MobilePerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_wirelessmobilepertransfee")));
                        model.GatewaySetupFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaysetupfee")));
                        model.GatewayMonthlyFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaymonthlyfee")));
                        model.GatewayPerTransFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_gatewaypertransfee")));

                        model.ChargebackPer = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackperchargeback")));
                        model.ChargebackReversals = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackreversalsperreversal")));
                        model.ChargebackRetrieval = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackretrievalperretrievaltype")));
                        model.ChargebackPreArbitration = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_chargebackprearbitrationprearbitration")));
                        model.PerAchRejectFee = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_perachrejectfee")));
                        model.FANF = SetPrice(Convert.ToString(jvalue[0].SelectToken("new_visafixedacquirernetworkfeefanf")));
                        #endregion

                        #region Get Signature from Note entity
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //Equipment_MerchantSignature
                        noteModel = await GetFIleFromNote("Equipment_MerchantSignature", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase64 = noteModel.documentbody;
                            model.FileName = noteModel.filename;
                        }
                        #endregion

                    }
                }
            }
            #endregion

            #region Optional Products or Services
            if (!string.IsNullOrEmpty(bankId))
            {
                //https://crm365.securepay.com:444/api/data/v9.0/new_banktemplates(32a3e3da-e3b9-e811-a951-000d3a037737)/new_new_banktemplate_new_optionalproductorser                
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_banktemplates(" + bankId + ")/new_new_banktemplate_new_optionalproductorser");
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        model.ListOptionalProductsServices = JsonConvert.DeserializeObject<List<OptionalProductsServices>>(jvalue.ToString());
                    }
                }
            }
            #endregion

            return model;
        }
        public async Task<bool> MerchantFeesCRMPost(FormCollection fc, RecurringFeesModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model
                #region Signature Upload in Note entity
                //
                NoteEntityModel noteModel = new NoteEntityModel();
                noteModel.notetext = "Authorized Merchant Signature";
                noteModel.subject = "Equipment_MerchantSignature";
                noteModel.LookupEntity = "new_merchantboarding";
                noteModel.entityId = model.MerchantId;
                noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                bool noteResult = await UploadFIleInNote(noteModel, -1);
                if (noteResult)
                    merchantForm.Add("new_equipment_merchantsignature", true);

                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        //throw new CrmHttpResponseException(updateResponse.Content);
                        return false;
                    }
                }
                return false;
            }
            return true;
        }
        #endregion

        #region All Forms CRM Methods - Partner - ISO/Bank Information
        public async Task<CompanyInformationModel> CompanyInformationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            CompanyInformationModel model = new CompanyInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        PartnerType = Convert.ToString(jvalue[0].SelectToken("new_merchantpartnertype"));
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        model.DBAName = Convert.ToString(jvalue[0].SelectToken("new_dbaname"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_contactname"));
                        model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_address"));
                        model.City = Convert.ToString(jvalue[0].SelectToken("new_city"));
                        model.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode"));
                        int SelectedState1 = model.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state")), out SelectedState1);
                            model.SelectedState1 = SelectedState1;
                        }

                        model.TaxId = Convert.ToString(jvalue[0].SelectToken("new_federaltaxid"));
                        int CorporationType;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_corporationtype")), out CorporationType);
                        model.CorporationType = CorporationType;
                        if (model.CorporationType == 4) // LLC: State
                        {
                            int SelectedState2 = model.SelectedState2 = -1;
                            if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state1"))))
                            {
                                int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state1")), out SelectedState2);
                                model.SelectedState2 = SelectedState2;
                            }
                        }
                        if (model.CorporationType == 7) // Non Profit
                        {
                            #region Get File from Note entity
                            NoteEntityModel noteModel = new NoteEntityModel();
                            //CompanyInfo_NonProfitEvidence
                            noteModel = await GetFIleFromNote("CompanyInfo_NonProfitEvidence", merchantId);
                            if (noteModel != null)
                            {
                                model.FileName = noteModel.filename;
                                model.FileBase64 = noteModel.documentbody;
                            }
                            #endregion
                        }

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));
                    }
                }
            }
            return model;
        }
        public async Task<bool> CompanyInformationCRMPost(FormCollection fc, CompanyInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model                        
            merchantForm.Add("new_dbaname", model.DBAName);
            merchantForm.Add("new_contactname", model.ContactName);
            merchantForm.Add("new_address", model.LocationAddress);
            merchantForm.Add("new_city", model.City);
            merchantForm.Add("new_zipcode", model.Zip);
            merchantForm.Add("new_state", model.SelectedState1);

            merchantForm.Add("new_federaltaxid", model.TaxId);
            merchantForm.Add("new_corporationtype", model.CorporationType);
            if (model.CorporationType == 4) // LLC: State
            {
                merchantForm.Add("new_state1", model.SelectedState2);
            }
            if (model.CorporationType == 7) // Non Profit
            {
                #region File Upload in Note entity
                //CompanyInfo_NonProfitEvidence
                NoteEntityModel noteModel = new NoteEntityModel();
                noteModel.notetext = "Non Profit Document";
                noteModel.subject = "CompanyInfo_NonProfitEvidence";
                noteModel.LookupEntity = "new_merchantpartner";
                noteModel.entityId = model.MerchantId;
                bool noteResult = await UploadFIleInNote(noteModel, 0);
                if (noteResult)
                    merchantForm.Add("new_nonprofitevidence", true);
                #endregion
            }

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<PrincipleInformationModel> PrincipleInformationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            PrincipleInformationModel model = new PrincipleInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        model.FullName = Convert.ToString(jvalue[0].SelectToken("new_fullname"));
                        model.Title = Convert.ToString(jvalue[0].SelectToken("new_title"));
                        model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_homeaddress"));
                        model.City = Convert.ToString(jvalue[0].SelectToken("new_city1"));
                        model.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode1"));
                        int SelectedState1 = model.SelectedState1 = -1;
                        if (!string.IsNullOrEmpty(Convert.ToString(jvalue[0].SelectToken("new_state2"))))
                        {
                            int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state2")), out SelectedState1);
                            model.SelectedState1 = SelectedState1;
                        }
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_homephone"));
                        model.MobilePhone = Convert.ToString(jvalue[0].SelectToken("new_mobilephone"));
                        model.SSN = Convert.ToString(jvalue[0].SelectToken("new_socialsecurity"));
                        DateTime BirthDate = DateTime.Now;
                        DateTime.TryParse(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth")), out BirthDate);
                        model.BirthDate = BirthDate.Date;

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));
                    }
                }
            }
            return model;
        }
        public async Task<bool> PrincipleInformationCRMPost(FormCollection fc, PrincipleInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model            
            merchantForm.Add("new_fullname", model.FullName);
            merchantForm.Add("new_title", model.Title);
            merchantForm.Add("new_homeaddress", model.LocationAddress);
            merchantForm.Add("new_city1", model.City);
            merchantForm.Add("new_zipcode1", model.Zip);
            merchantForm.Add("new_state2", model.SelectedState1);
            merchantForm.Add("new_homephone", model.TelePhoneNumber);
            merchantForm.Add("new_mobilephone", model.MobilePhone);
            merchantForm.Add("new_socialsecurity", model.SSN);
            if (!string.IsNullOrEmpty(model.BirthDateString))
                merchantForm.Add("new_dateofbirth", model.BirthDateString);

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<AuthorizationModel> AuthorizationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            AuthorizationModel model = new AuthorizationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        //model.AuthorizationDescription = Convert.ToString(jvalue[0].SelectToken("new_authorizationtemplate"));
                        var topic = _topicService.GetTopicBySystemName("AuthorizationDescription");
                        if (topic != null)
                        {
                            model.AuthorizationDescription = topic.Body;
                        }
                        model.SSN = Convert.ToString(jvalue[0].SelectToken("new_socialsecurity"));
                        model.BirthDate = Convert.ToString(jvalue[0].SelectToken("new_dateofbirth"));
                        model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_address"));
                        model.City = Convert.ToString(jvalue[0].SelectToken("new_city"));
                        model.Zip = Convert.ToString(jvalue[0].SelectToken("new_zipcode"));
                        int SelectedState1;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_state")), out SelectedState1);
                        model.SelectedState1 = SelectedState1;
                        // new_authorization_signature will be here                        

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));

                        #region Get Signature from Note entity
                        //NoteEntityModel noteModel = new NoteEntityModel();
                        ////Authorization_Signature
                        //noteModel = await GetFIleFromNote("Authorization_Signature", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase64 = noteModel.documentbody;
                        //    model.FileName = noteModel.filename;
                        //}
                        #endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> AuthorizationCRMPost(FormCollection fc, AuthorizationModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model                

                #region Signature Upload in Note entity
                ////
                //NoteEntityModel noteModel = new NoteEntityModel();
                //noteModel.notetext = "Authorization Signature/Title";
                //noteModel.subject = "Authorization_Signature";
                //noteModel.LookupEntity = "new_merchantpartner";
                //noteModel.entityId = model.MerchantId;
                //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                //bool noteResult = await UploadFIleInNote(noteModel, -1);
                //if (noteResult)
                //    merchantForm.Add("new_authorization_signature", true);
                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<AgreementModel> AgreementCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            AgreementModel model = new AgreementModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        //model.AgreementDescription = Convert.ToString(jvalue[0].SelectToken("new_agreementtemplate"));
                        var topic = _topicService.GetTopicBySystemName("AgreementDescription");
                        if (topic != null)
                        {
                            model.AgreementDescription = topic.Body;
                        }
                        // new_signatureprincipal will be here

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));

                        #region Get Signature from Note entity
                        //NoteEntityModel noteModel = new NoteEntityModel();
                        ////Agreement_SignaturePrincipal
                        //noteModel = await GetFIleFromNote("Agreement_SignaturePrincipal", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase64 = noteModel.documentbody;
                        //    model.FileName = noteModel.filename;
                        //}
                        #endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> AgreementCRMPost(FormCollection fc, AgreementModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model                

                #region Signature Upload in Note entity
                //
                //NoteEntityModel noteModel = new NoteEntityModel();
                //noteModel.notetext = "Agreement SignaturePrincipal";
                //noteModel.subject = "Agreement_SignaturePrincipal";
                //noteModel.LookupEntity = "new_merchantpartner";
                //noteModel.entityId = model.MerchantId;
                //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                //bool noteResult = await UploadFIleInNote(noteModel, -1);
                //if (noteResult)
                //    merchantForm.Add("new_signatureprincipal", true);
                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<AppendixAModel> AppendixACRMGet(string merchantId)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            AppendixAModel model = new AppendixAModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            model.MerchantId = merchantId;
            model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";
            #region Get Partner by Email id
            //if (!string.IsNullOrEmpty(CustomerEmail))
            //{
            //    // Get merchant details from customer email
            //    //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
            //    string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
            //    HttpResponseMessage merchantResponse = await httpClient.GetAsync(
            //   getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
            //    if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
            //    {
            //        retrievedData = JsonConvert.DeserializeObject<JObject>(
            //            await merchantResponse.Content.ReadAsStringAsync());
            //    }
            //    else
            //    {
            //        throw new CrmHttpResponseException(merchantResponse.Content);
            //    }
            //    if (retrievedData != null)
            //    {
            //        var jvalue = retrievedData.GetValue("value");
            //        if (jvalue != null && jvalue.Count() > 0)
            //        {
            //            string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
            //            model.MerchantId = merchantId;
            //            model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

            //            //Revenue Share----------------
            //            model.new_revenuepercentagepaidonaboveallcosts = Convert.ToString(jvalue[0].SelectToken("new_revenuepercentagepaidonaboveallcosts"));
            //            //Transaction Fees------------------
            //            model.new_visamcdiscovertransactionfee = Convert.ToString(jvalue[0].SelectToken("new_visamcdiscovertransactionfee"));
            //            model.new_ebttransactions = Convert.ToString(jvalue[0].SelectToken("new_ebttransactions"));
            //            model.new_addressverificationsystemavselectronic = Convert.ToString(jvalue[0].SelectToken("new_addressverificationsystemavselectronic"));
            //            model.new_microspertransactionz = Convert.ToString(jvalue[0].SelectToken("new_microspertransactionz"));
            //            model.new_nonbankcardtransactionfee = Convert.ToString(jvalue[0].SelectToken("new_nonbankcardtransactionfee"));
            //            model.new_debittransactionfeeplusnetworkfees = Convert.ToString(jvalue[0].SelectToken("new_debittransactionfeeplusnetworkfees"));

            //            //Setup, Maintenance and Membership Fees------------------------
            //            model.new_batchheaderachfee = Convert.ToString(jvalue[0].SelectToken("new_batchheaderachfee"));
            //            model.new_chargebackinitialcase = Convert.ToString(jvalue[0].SelectToken("new_chargebackinitialcase"));
            //            model.new_chargebackreversals = Convert.ToString(jvalue[0].SelectToken("new_chargebackreversals"));
            //            model.new_merchantstatementserviceperlocation = Convert.ToString(jvalue[0].SelectToken("new_merchantstatementserviceperlocation"));
            //            model.new_merchantaccountonfilefee = Convert.ToString(jvalue[0].SelectToken("new_merchantaccountonfilefee"));
            //            model.new_annualfee = Convert.ToString(jvalue[0].SelectToken("new_annualfee"));
            //            model.new_cancellationfee = Convert.ToString(jvalue[0].SelectToken("new_cancellationfee"));
            //            model.new_pcicompliancemonthlyfee = Convert.ToString(jvalue[0].SelectToken("new_pcicompliancemonthlyfee"));
            //            model.new_pcinoncompliancefee = Convert.ToString(jvalue[0].SelectToken("new_pcinoncompliancefee"));
            //            model.new_tinmismatchfee = Convert.ToString(jvalue[0].SelectToken("new_tinmismatchfee"));
            //            model.new_govtcompliancefee = Convert.ToString(jvalue[0].SelectToken("new_govtcompliancefee"));
            //            model.new_interactivevoiceresponseautharuorivr = Convert.ToString(jvalue[0].SelectToken("new_interactivevoiceresponseautharuorivr"));
            //            model.new_achrejectfees = Convert.ToString(jvalue[0].SelectToken("new_achrejectfees"));
            //            model.new_retrievals = Convert.ToString(jvalue[0].SelectToken("new_retrievals"));
            //            model.new_welcomekits = Convert.ToString(jvalue[0].SelectToken("new_welcomekits"));
            //            model.new_merchantonlinereporting = Convert.ToString(jvalue[0].SelectToken("new_merchantonlinereporting"));
            //            //Association Rates...................
            //            model.new_visamcdiscinterchangeduesandassessments = Convert.ToString(jvalue[0].SelectToken("new_visamcdiscinterchangeduesandassessments"));
            //            model.new_debitnetworkinterchange = Convert.ToString(jvalue[0].SelectToken("new_debitnetworkinterchange"));
            //            model.new_binbanksponsorshipexpense = Convert.ToString(jvalue[0].SelectToken("new_binbanksponsorshipexpense"));
            //            model.new_americanexpressbluenetworkinterchange = Convert.ToString(jvalue[0].SelectToken("new_americanexpressbluenetworkinterchange"));
            //            model.new_americanexpresssponsorshipfee = Convert.ToString(jvalue[0].SelectToken("new_americanexpresssponsorshipfee"));
            //            model.new_associationbrandingfees = Convert.ToString(jvalue[0].SelectToken("new_associationbrandingfees"));

            //            //Additional Service Fees-----------------------
            //            model.new_helpdeskpercallafterhourstsys = Convert.ToString(jvalue[0].SelectToken("new_helpdeskpercallafterhourstsys"));
            //            model.new_customerservicepermidmonthly = Convert.ToString(jvalue[0].SelectToken("new_aaaaaaa"));

            //            //Wireless Service-------------------
            //            model.new_wirelesstransactionfeegprscdma = Convert.ToString(jvalue[0].SelectToken("new_wirelesstransactionfeegprscdma"));
            //            model.new_wirelessinitialsetupperunit = Convert.ToString(jvalue[0].SelectToken("new_wirelessinitialsetupperunit"));
            //            model.new_wirelessmonthlyipaddressfeelabel = Convert.ToString(jvalue[0].SelectToken("new_wirelessmonthlyipaddressfeelabel"));
            //            model.new_wirelessgsmsimcardtsysposperdevice = Convert.ToString(jvalue[0].SelectToken("new_wirelessgsmsimcardtsysposperdevice"));
            //            model.new_wirelessgsmsimcardnontsysposperdevice = Convert.ToString(jvalue[0].SelectToken("new_wirelessgsmsimcardnontsysposperdevice"));


            //            //Virtual Terminal Fees-------------------        
            //            model.new_securepaymonthly = Convert.ToString(jvalue[0].SelectToken("new_securepaymonthly"));
            //            model.new_securepaytransaction = Convert.ToString(jvalue[0].SelectToken("new_securepaytransaction"));
            //            model.new_nmimonthlyfullaccessunlimitedmobiledevice = Convert.ToString(jvalue[0].SelectToken("new_nmimonthlyfullaccessunlimitedmobiledevice"));
            //            model.new_mobileapponlymonthlyperdevicedoesnotinclu = Convert.ToString(jvalue[0].SelectToken("new_mobileapponlymonthlyperdevicedoesnotinclu"));
            //            model.new_pertransaction = Convert.ToString(jvalue[0].SelectToken("new_pertransaction"));
            //            model.new_paytracetransaction = Convert.ToString(jvalue[0].SelectToken("new_paytracetransaction"));
            //            model.new_paytracemonthly = Convert.ToString(jvalue[0].SelectToken("new_paytracemonthly"));
            //            model.new_paytracesetupfee = Convert.ToString(jvalue[0].SelectToken("new_paytracesetupfee"));

            //            //Equipment Setup Fees----------------------------
            //            model.new_equipmentprogramsencryption = Convert.ToString(jvalue[0].SelectToken("new_equipmentprogramsencryption"));

            //            model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
            //            model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
            //            model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));

            //            #region Get Signature from Note entity
            //            NoteEntityModel noteModel = new NoteEntityModel();
            //            //AppendixA_Signature
            //            noteModel = await GetFIleFromNote("AppendixA_Signature", merchantId);
            //            if (noteModel != null)
            //            {
            //                model.FileBase64 = noteModel.documentbody;
            //                model.FileName = noteModel.filename;
            //            }
            //            #endregion
            //        }
            //    }
            //}
            #endregion
            if (!string.IsNullOrEmpty(merchantId))
            {
                model.MerchantId = merchantId;
                model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";
                //https://crm365.securepay.com:444/api/data/v9.0/new_merchantpartners(01dc2f94-22b0-e811-a950-000d3a037737)/new_new_merchantpartner_new_merchantfee
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")/new_new_merchantpartner_new_merchantfee");
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        model.ListMerchantFees = JsonConvert.DeserializeObject<List<MerchantFees>>(jvalue.ToString());
                        if (model.ListMerchantFees != null && model.ListMerchantFees.Any())
                        {
                            model.ListMerchantFees = model.ListMerchantFees.Where(f => f.new_feevaluetype != null && f.new_feevaluetype > 0).ToList();
                        }
                    }
                }
            }

            #region Get Signature from Note entity
            //NoteEntityModel noteModel = new NoteEntityModel();
            ////AppendixA_Signature
            //noteModel = await GetFIleFromNote("AppendixA_Signature", merchantId);
            //if (noteModel != null)
            //{
            //    model.FileBase64 = noteModel.documentbody;
            //    model.FileName = noteModel.filename;
            //}
            #endregion

            return model;
        }
        public async Task<bool> AppendixACRMPost(FormCollection fc, AppendixAModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model
                #region Signature Upload in Note entity
                ////AppendixA_Signature
                //NoteEntityModel noteModel = new NoteEntityModel();
                //noteModel.notetext = "AppendixA Signature";
                //noteModel.subject = "AppendixA_Signature";
                //noteModel.LookupEntity = "new_merchantpartner";
                //noteModel.entityId = model.MerchantId;
                //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                //bool noteResult = await UploadFIleInNote(noteModel, -1);
                //if (noteResult)
                //    merchantForm.Add("new_appendixa_signature", true);
                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<AppendixBModel> AppendixBCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            AppendixBModel model = new AppendixBModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        model.EthicsStatementDesciption = Convert.ToString(jvalue[0].SelectToken("new_appendixbtemplate"));
                        var topic = _topicService.GetTopicBySystemName("EthicsStatementDesciption");
                        if (topic != null)
                        {
                            model.EthicsStatementDesciption = topic.Body;
                        }

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));

                        #region Get Signature from Note entity
                        //NoteEntityModel noteModel = new NoteEntityModel();
                        ////AppendixB_Signature
                        //noteModel = await GetFIleFromNote("AppendixB_Signature", merchantId);
                        //if (noteModel != null)
                        //{
                        //    model.FileBase64 = noteModel.documentbody;
                        //    model.FileName = noteModel.filename;
                        //}
                        #endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> AppendixBCRMPost(FormCollection fc, AppendixBModel model)
        {
            if (!string.IsNullOrEmpty(model.FileBase64)) // If form is not signed then only need to post in CRM
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model
                #region Signature Upload in Note entity
                ////AppendixB_Signature
                //NoteEntityModel noteModel = new NoteEntityModel();
                //noteModel.notetext = "AppendixB Signature";
                //noteModel.subject = "AppendixB_Signature";
                //noteModel.LookupEntity = "new_merchantpartner";
                //noteModel.entityId = model.MerchantId;
                //noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase64) ? model.FileBase64.Replace("data:image/png;base64,", "") : model.FileBase64);
                //bool noteResult = await UploadFIleInNote(noteModel, -1);
                //if (noteResult)
                //    merchantForm.Add("new_appendixb_signature", true);
                #endregion

                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        public async Task<PaymentInformationModel> PaymentInformationCRMGet()
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            PaymentInformationModel model = new PaymentInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = jvalue[0].SelectToken("new_merchantpartnerid").ToString();
                        PartnerType = Convert.ToString(jvalue[0].SelectToken("new_merchantpartnertype"));
                        model.MerchantId = merchantId;
                        model.MerchantUri = "https://crm365.securepay.com:444/api/data/" + getVersionedWebAPIPath() + "new_merchantpartners(" + merchantId + ")";

                        model.ApplicantName = Convert.ToString(jvalue[0].SelectToken("new_applicantsname"));
                        model.DBACompanyName = Convert.ToString(jvalue[0].SelectToken("new_dbacompanyname"));
                        model.LocationAddress = Convert.ToString(jvalue[0].SelectToken("new_address1"));
                        model.BankName = Convert.ToString(jvalue[0].SelectToken("new_bankname"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_bankphone"));
                        model.RoutingNumber = Convert.ToString(jvalue[0].SelectToken("new_routingnumber"));
                        model.AccountNumber = Convert.ToString(jvalue[0].SelectToken("new_accountnumber"));

                        model.CustomerEmail = Convert.ToString(jvalue[0].SelectToken("new_emailaddress"));
                        model.ContactName = Convert.ToString(jvalue[0].SelectToken("new_name"));
                        model.TelePhoneNumber = Convert.ToString(jvalue[0].SelectToken("new_telephonenumber"));

                        #region Get File from Note entity
                        // get file from new_copyofdriverslicense
                        // get file from new_copyofpreprintedvoidedcheckfordeposits
                        // get file from new_copyofw9form
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //PaymentInfo_DriversLicense
                        noteModel = await GetFIleFromNote("PaymentInfo_DriversLicense", merchantId);
                        if (noteModel != null)
                        {
                            model.FileName = noteModel.filename;
                            model.FileBase64 = noteModel.documentbody;
                        }
                        noteModel = new NoteEntityModel();
                        //PaymentInfo_CheckForDeposit
                        noteModel = await GetFIleFromNote("PaymentInfo_CheckForDeposit", merchantId);
                        if (noteModel != null)
                        {
                            model.FileName2 = noteModel.filename;
                            model.FileBase642 = noteModel.documentbody;
                        }
                        noteModel = new NoteEntityModel();
                        //PaymentInfo_CopyOfw9Form
                        noteModel = await GetFIleFromNote("PaymentInfo_CopyOfw9Form", merchantId);
                        if (noteModel != null)
                        {
                            model.FileName3 = noteModel.filename;
                            model.FileBase643 = noteModel.documentbody;
                        }
                        #endregion

                        #region Get Signature from Note entity
                        noteModel = new NoteEntityModel();
                        //PaymentInfo_Signature
                        noteModel = await GetFIleFromNote("PaymentInfo_Signature", merchantId);
                        if (noteModel != null)
                        {
                            model.FileBase644 = noteModel.documentbody;
                            model.FileName4 = noteModel.filename;
                        }
                        #endregion

                        //#region Upload Controls for Corporation Type
                        //int CorporationType;
                        //int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_corporationtype")), out CorporationType);
                        //model.CorporationType = CorporationType;
                        //if (model.CorporationType == 5 || model.CorporationType == 6 || model.CorporationType == 7)
                        //{
                        //    if (model.CorporationType == 5)
                        //        model.CorporationTypeName = "Goverment";
                        //    else if (model.CorporationType == 6)
                        //        model.CorporationTypeName = "Publicly Traded";
                        //    else if (model.CorporationType == 7)
                        //        model.CorporationTypeName = "Non Profit";

                        //    noteModel = await GetFIleFromNote("CorporationType_Document1", merchantId);
                        //    if (noteModel != null)
                        //    {
                        //        model.FileName5 = noteModel.filename;
                        //        model.FileBase645 = noteModel.documentbody;
                        //    }
                        //    noteModel = await GetFIleFromNote("CorporationType_Document2", merchantId);
                        //    if (noteModel != null)
                        //    {
                        //        model.FileName6 = noteModel.filename;
                        //        model.FileBase646 = noteModel.documentbody;
                        //    }

                        //    noteModel = await GetFIleFromNote("CorporationType_Document3", merchantId);
                        //    if (noteModel != null)
                        //    {
                        //        model.FileName7 = noteModel.filename;
                        //        model.FileBase647 = noteModel.documentbody;
                        //    }
                        //    noteModel = await GetFIleFromNote("CorporationType_Document4", merchantId);
                        //    if (noteModel != null)
                        //    {
                        //        model.FileName8 = noteModel.filename;
                        //        model.FileBase648 = noteModel.documentbody;
                        //    }
                        //    noteModel = await GetFIleFromNote("CorporationType_Document5", merchantId);
                        //    if (noteModel != null)
                        //    {
                        //        model.FileName9 = noteModel.filename;
                        //        model.FileBase649 = noteModel.documentbody;
                        //    }
                        //}
                        //#endregion
                    }
                }
            }
            return model;
        }
        public async Task<bool> PaymentInformationCRMPost(FormCollection fc, PaymentInformationModel model)
        {

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            // Generate json object from model
            merchantForm.Add("new_applicantsname", model.ApplicantName);
            merchantForm.Add("new_dbacompanyname", model.DBACompanyName);
            merchantForm.Add("new_address1", model.LocationAddress);
            merchantForm.Add("new_bankname", model.BankName);
            merchantForm.Add("new_routingnumber", model.RoutingNumber);
            merchantForm.Add("new_bankphone", model.TelePhoneNumber);

            #region Signature Upload in Note entity
            //PaymentInfo_Signature
            NoteEntityModel noteModel = new NoteEntityModel();
            noteModel.notetext = "PaymentInf _Signature";
            noteModel.subject = "PaymentInfo_Signature";
            noteModel.LookupEntity = "new_merchantpartner";
            noteModel.entityId = model.MerchantId;
            noteModel.documentbody = (!string.IsNullOrEmpty(model.FileBase644) ? model.FileBase644.Replace("data:image/png;base64,", "") : model.FileBase644);
            bool noteResult = await UploadFIleInNote(noteModel, -1);
            if (noteResult)
                merchantForm.Add("new_paymentinfo_signature", true);
            #endregion

            #region File Upload in Note entity
            // post file from new_copyofdriverslicense
            // post file from new_copyofpreprintedvoidedcheckfordeposits
            // post file from new_copyofw9form
            //PaymentInfo_DriversLicense
            noteModel = new NoteEntityModel();
            noteModel.notetext = "COPY OF DRIVER’S LICENSE";
            noteModel.subject = "PaymentInfo_DriversLicense";
            noteModel.LookupEntity = "new_merchantpartner";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 0);
            if (noteResult)
                merchantForm.Add("new_copyofdriverslicense", true);

            //PaymentInfo_CheckForDeposit
            noteModel = new NoteEntityModel();
            noteModel.notetext = "COPY OF PRE PRINTED VOIDED CHECK FOR DEPOSITS";
            noteModel.subject = "PaymentInfo_CheckForDeposit";
            noteModel.LookupEntity = "new_merchantpartner";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 1);
            if (noteResult)
                merchantForm.Add("new_copyofpreprintedvoidedcheckfordeposits", true);

            //PaymentInfo_CopyOfw9Form
            noteModel = new NoteEntityModel();
            noteModel.notetext = "COPY OF W9 FORM";
            noteModel.subject = "PaymentInfo_CopyOfw9Form";
            noteModel.LookupEntity = "new_merchantpartner";
            noteModel.entityId = model.MerchantId;
            noteResult = await UploadFIleInNote(noteModel, 2);
            if (noteResult)
                merchantForm.Add("new_copyofw9form", true);
            #endregion

            //#region Upload Controls for Corporation Type
            //if (model.CorporationType == 5 || model.CorporationType == 6 || model.CorporationType == 7)
            //{
            //    noteModel = new NoteEntityModel();
            //    noteModel.notetext = "Corporation Type - Documetn 1";
            //    noteModel.subject = "CorporationType_Document1";
            //    noteModel.LookupEntity = "new_merchantpartner";
            //    noteModel.entityId = model.MerchantId;
            //    noteResult = await UploadFIleInNote(noteModel, 0);
            //    if (noteResult)
            //        merchantForm.Add("new_corporationtypedocument1", true);

            //    noteModel = new NoteEntityModel();
            //    noteModel.notetext = "Corporation Type - Documetn 2";
            //    noteModel.subject = "CorporationType_Document2";
            //    noteModel.LookupEntity = "new_merchantpartner";
            //    noteModel.entityId = model.MerchantId;
            //    noteResult = await UploadFIleInNote(noteModel, 0);
            //    if (noteResult)
            //        merchantForm.Add("new_corporationtypedocument2", true);

            //    noteModel = new NoteEntityModel();
            //    noteModel.notetext = "Corporation Type - Documetn 3";
            //    noteModel.subject = "CorporationType_Document3";
            //    noteModel.LookupEntity = "new_merchantpartner";
            //    noteModel.entityId = model.MerchantId;
            //    noteResult = await UploadFIleInNote(noteModel, 0);
            //    if (noteResult)
            //        merchantForm.Add("new_corporationtypedocument3", true);

            //    noteModel = new NoteEntityModel();
            //    noteModel.notetext = "Corporation Type - Documetn 4";
            //    noteModel.subject = "CorporationType_Document4";
            //    noteModel.LookupEntity = "new_merchantpartner";
            //    noteModel.entityId = model.MerchantId;
            //    noteResult = await UploadFIleInNote(noteModel, 0);
            //    if (noteResult)
            //        merchantForm.Add("new_corporationtypedocument4", true);

            //    noteModel = new NoteEntityModel();
            //    noteModel.notetext = "Corporation Type - Documetn 5";
            //    noteModel.subject = "CorporationType_Document5";
            //    noteModel.LookupEntity = "new_merchantpartner";
            //    noteModel.entityId = model.MerchantId;
            //    noteResult = await UploadFIleInNote(noteModel, 0);
            //    if (noteResult)
            //        merchantForm.Add("new_corporationtypedocument5", true);
            //}
            //#endregion

            //Update Merchat
            if (!string.IsNullOrEmpty(model.MerchantUri))
            {
                HttpRequestMessage updateRequest = new HttpRequestMessage(
               new HttpMethod("PATCH"), model.MerchantUri);
                updateRequest.Content = new StringContent(merchantForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse =
                    await httpClient.SendAsync(updateRequest);
                if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public PartnerLogo PartnerLogoDBGet()
        {
            PartnerLogo model = new PartnerLogo();
            model.CustomerEmail = _workContext.CurrentCustomer.Email;

            // Get Affiliate based on email id
            var affiliateService = EngineContext.Current.Resolve<IAffiliateService>();
            var affiliates = affiliateService.GetAllAffiliates();
            var affiliate = new Affiliate();
            if (affiliates != null && affiliates.Any())
            {
                affiliate = affiliates.Where(a => a.Address != null && a.Address.Email == model.CustomerEmail).FirstOrDefault();
            }
            if (affiliate != null)
            {
                model.AffiliateId = affiliate.Id;
                var logoId =
               _dbContext.SqlQuery<int?>(
                   "select top 1 LogoId from [Affiliate] where id=@p0", affiliate.Id)
                   .FirstOrDefault();
                if (logoId != null && logoId > 0)
                {
                    // then get picture based on Logoid from affiliate - GetPictureById
                    var picture = _pictureService.GetPictureById(Convert.ToInt32(logoId));
                    if (picture != null)
                    {
                        model.PictureUrl = _pictureService.GetPictureUrl(picture, 100, true);
                    }
                }
            }

            // get image url from _pictureService.GetPictureUrl(picture,100,true);

            return model;
        }
        public async Task<bool> PartnerAffiliateDB_CRMPost(FormCollection fc, PartnerLogo model)
        {
            HttpPostedFileBase filePost = Request.Files[0];
            if (filePost != null && filePost.ContentLength > 0)
            {
                Stream stream = filePost.InputStream;
                var fileBinary = new byte[filePost.InputStream.Length];
                stream.Read(fileBinary, 0, fileBinary.Length);
                string filebase64 = Convert.ToBase64String(fileBinary);
                string fileName = filePost.FileName;

                UploadAffiliateLogo(filePost.ContentType, filePost.FileName, fileBinary, model.AffiliateId);

                #region Upload Logo to CRM
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

                // Generate json object from model         
                var _sRepCode = GeneratePartnerRepCode();
                if (!string.IsNullOrEmpty(_sRepCode))
                {
                    merchantForm.Add("new_repcode", _sRepCode);
                }

                #region File Upload in Note entity
                //file_subject
                NoteEntityModel noteModel = new NoteEntityModel();
                noteModel.notetext = "Pertner Logo";
                noteModel.subject = "partnerlogo";
                noteModel.LookupEntity = "new_merchantpartner";
                noteModel.entityId = model.MerchantId;
                bool noteResult = await UploadFIleInNote(noteModel, 0, fileName, filebase64);
                if (noteResult)
                    merchantForm.Add("new_partnerlogo", true);
                #endregion


                //Update Merchat
                if (!string.IsNullOrEmpty(model.MerchantUri))
                {
                    HttpRequestMessage updateRequest = new HttpRequestMessage(
                   new HttpMethod("PATCH"), model.MerchantUri);
                    updateRequest.Content = new StringContent(merchantForm.ToString(),
                        Encoding.UTF8, "application/json");
                    HttpResponseMessage updateResponse =
                        await httpClient.SendAsync(updateRequest);
                    if (updateResponse.StatusCode == HttpStatusCode.NoContent) //204
                    {
                        if (!string.IsNullOrEmpty(_sRepCode))
                        {
                            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_Rep_Code", _sRepCode);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                #endregion
            }

            return true;
        }


        public async Task<int> AddUpdateAffiliate()
        {
            int affiliateId = 0;

            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_emailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_emailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantpartners" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");
                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        bool IsExist = false;
                        var affiliateService = EngineContext.Current.Resolve<IAffiliateService>();
                        var affiliates = affiliateService.GetAllAffiliates();
                        var affiliate = new Affiliate();
                        if (affiliates != null && affiliates.Any())
                        {
                            affiliate = affiliates.Where(a => a.Address != null && a.Address.Email == CustomerEmail).FirstOrDefault();
                        }
                        if (affiliate != null)
                        {
                            IsExist = true; // update
                        }
                        else
                        {
                            affiliate = new Affiliate(); // add
                            affiliate.Address = new Address();
                        }
                        affiliate.Active = true;
                        //var address = new Address();
                        string BankName = Convert.ToString(jvalue[0].SelectToken("new_bankname"));

                        affiliate.Address.FirstName = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName);
                        affiliate.Address.LastName = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.LastName);
                        affiliate.Address.Email = CustomerEmail;
                        affiliate.Address.Company = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.Company);
                        // Set Countru id -> get id from name and then save it
                        // Set State id -> get id from name and then save it
                        affiliate.Address.City = Convert.ToString(jvalue[0].SelectToken("new_city"));
                        affiliate.Address.Address1 = Convert.ToString(jvalue[0].SelectToken("new_address"));
                        affiliate.Address.Address2 = Convert.ToString(jvalue[0].SelectToken("new_homeaddress"));
                        affiliate.Address.ZipPostalCode = Convert.ToString(jvalue[0].SelectToken("new_zipcode1"));
                        affiliate.Address.PhoneNumber = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.Phone);
                        //affiliate.Address = address;                        
                        if (!string.IsNullOrEmpty(BankName))
                            affiliate.FriendlyUrlName = affiliate.ValidateFriendlyUrlName(BankName);

                        if (affiliate.Address.CountryId == 0)
                            affiliate.Address.CountryId = null;
                        if (affiliate.Address.StateProvinceId == 0)
                            affiliate.Address.StateProvinceId = null;
                        if (IsExist)
                        {
                            affiliateService.UpdateAffiliate(affiliate);
                            _customerActivityService.InsertActivity("EditAffiliate", _localizationService.GetResource("ActivityLog.EditAffiliate"), affiliate.Id);
                        }
                        else
                        {
                            affiliate.Address.CreatedOnUtc = DateTime.UtcNow;
                            affiliateService.InsertAffiliate(affiliate);
                            //activity log
                            _customerActivityService.InsertActivity("AddNewAffiliate", _localizationService.GetResource("ActivityLog.AddNewAffiliate"), affiliate.Id);
                        }
                        affiliateId = affiliate.Id;
                        if (affiliateId > 0)
                        {
                            _dbContext.ExecuteSqlCommand("update Affiliate SET PartnerId=@p0 WHERE id=@p1", false, null,
              Convert.ToString(jvalue[0].SelectToken("new_merchantpartnerid")), affiliateId);
                        }
                    }
                }
            }
            return affiliateId;
        }
        #endregion

        #region Merchant Methods
        private string RegisterCustomer(FormCollection form, out List<string> errors)
        {
            errors = new List<string>();

            if (_workContext.CurrentCustomer.IsRegistered())
            {
                //Already registered customer. 
                _authenticationService.SignOut();

                //raise logged out event       
                _eventPublisher.Publish(new CustomerLoggedOutEvent(_workContext.CurrentCustomer));

                //Save a new record
                _workContext.CurrentCustomer = _customerService.InsertGuestCustomer();
            }
            var customer = _workContext.CurrentCustomer;
            //string Password = "temporary";
            string Password = form["Password"];

            bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
            var registrationRequest = new CustomerRegistrationRequest(customer,
                     form["EmailAddress"],
                     form["EmailAddress"],
                     Password,
                     _customerSettings.DefaultPasswordFormat,
                     _storeContext.CurrentStore.Id,
                     isApproved);
            // Add "Merchant" role manually
            var registeredRole = _customerService.GetCustomerRoleBySystemName("Merchant");
            if (registeredRole != null)
                registrationRequest.Customer.CustomerRoles.Add(registeredRole);
            //----------------------------

            var registrationResult = _customerRegistrationService.RegisterCustomer(registrationRequest);
            if (registrationResult.Success)
            {
                //form fields
                string fullName = form["ContactName"];
                string firstName = string.Empty;
                string lastName = string.Empty;
                if (!string.IsNullOrEmpty(fullName) && fullName.Split(' ').Length > 1)
                {
                    string[] name = fullName.Split(' ');
                    if (name.Length > 1)
                    {
                        firstName = name[0];
                        lastName = name[1];
                    }
                    else
                    {
                        firstName = name[0];
                    }
                }
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.FirstName, firstName);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.LastName, lastName);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Company, form["CorporateName"]);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Phone, form["TelephoneNumber"]);

                //notifications
                if (_customerSettings.NotifyNewCustomerRegistration)
                    _workflowMessageService.SendCustomerRegisteredNotificationMessage(customer, _localizationSettings.DefaultAdminLanguageId);

                //login customer now
                if (isApproved)
                    _authenticationService.SignIn(customer, true);

                switch (_customerSettings.UserRegistrationType)
                {
                    case UserRegistrationType.EmailValidation:
                        {
                            //email validation message
                            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.AccountActivationToken, Guid.NewGuid().ToString());
                            _workflowMessageService.SendCustomerEmailValidationMessage(customer, _workContext.WorkingLanguage.Id);

                            //result                            
                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });

                        }
                    case UserRegistrationType.AdminApproval:
                        {
                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                        }
                    case UserRegistrationType.Standard:
                        {
                            //send customer welcome message
                            _workflowMessageService.SendCustomerWelcomeMessage(customer, _workContext.WorkingLanguage.Id);

                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
                        }
                    default:
                        {
                            return Url.RouteUrl("HomePage");
                        }
                }
            }
            else
            {
                foreach (var err in registrationResult.Errors)
                {
                    errors.Add(err);
                }
                return "";
            }
        }


        public ActionResult RegisterMerchant()
        {
            ViewBag.Error = TempData["Error"];
            ViewBag.PartnerId = TempData["PartnerId"];
            return View("/Themes/Main/Views/MerchantBoarding/Register.cshtml");
        }

        public ActionResult RegisterMerchantForID(string merchantid = "")
        {
            string redirectUrl = "/";
            List<string> errors = new List<string>();
            if (string.IsNullOrEmpty(merchantid))
            {
                errors.Add(_localizationService.GetResource("Plugin.MerchantBoarding.Warning.InvalidMerchantId"));
                TempData["Error"] = string.Join("<br>", errors);
                return Redirect(redirectUrl);
            }
            FormCollection form = new FormCollection();
            Task.WaitAll(Task.Run(async () => { form = await MerchantRegisteredCRMGet(merchantid); }));

            if (!string.IsNullOrEmpty(form["ContactName"]) && !string.IsNullOrEmpty(form["EmailAddress"]) &&
                          !string.IsNullOrEmpty(form["TelephoneNumber"]) && !string.IsNullOrEmpty(form["CorporateName"]))
            {
                // Register as a customer                
                try
                {
                    redirectUrl = RegisterCustomer(form, out errors);
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        TempData["Error"] = string.Join("<br>", errors);
                        return Redirect("/");
                    }
                }
                catch (System.Exception ex) { DisplayException(ex); }
                finally
                {
                    if (httpClient != null)
                    { httpClient.Dispose(); }
                }
            }
            else
            {
                errors.Add(_localizationService.GetResource("Plugin.MerchantBoarding.Warning.InvalidMerchantInfo"));
                TempData["Error"] = string.Join("<br>", errors);
            }
            return Redirect(redirectUrl);
        }

        [HttpPost]
        public ActionResult RegisterMerchant(FormCollection form)
        {
            string redirectUrl = "/";
            try
            {
                //Read configuration file and connect to specified CRM server.
                //ConnectToCRM();
                List<string> errors = new List<string>();
                redirectUrl = RegisterCustomer(form, out errors);
                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    Task.WaitAll(Task.Run(async () => await RunAsync(form)));
                }
                else
                {
                    TempData["Error"] = string.Join("<br>", errors);
                    return Redirect("/");
                }
            }
            catch (System.Exception ex) { DisplayException(ex); }
            finally
            {
                if (httpClient != null)
                { httpClient.Dispose(); }
            }
            return Redirect(redirectUrl);
        }

        public ActionResult merchanthtml()
        {
            return View("/Themes/Main/Views/MerchantBoarding/HomeTest.cshtml");
        }

        [Authorize]
        public ActionResult MerchantInformation()
        {
            MerchantInformationModel model = new MerchantInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await MerchantInformationCRMGet(); }));

            return View("/Themes/Main/Views/MerchantBoarding/_MerchantInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult MerchantInformation(FormCollection fc, MerchantInformationModel model)
        {
            bool result = false;
            #region TINCheck
            TINCheckModel _oTINCheckModel = new TINCheckModel();
            _oTINCheckModel.FName = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName);
            _oTINCheckModel.LName = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.LastName);
            _oTINCheckModel.Address2 = model.LocationAddress;
            _oTINCheckModel.City = model.City;
            _oTINCheckModel.State = fc["state1"];
            _oTINCheckModel.Zip5 = model.Zip;

            TempData["_oTINCheckModel"] = _oTINCheckModel;
            #endregion
            Task.WaitAll(Task.Run(async () => { result = await MerchantInformationCRMPost(fc, model); }));
            Task.WaitAll(Task.Run(async () => { MerchantInformationDBPost(fc, model); }));


            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("BusinessInformation");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult BusinessInformation()
        {
            BusinessInformationModel model = new BusinessInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await BusinessInformationCRMGet(); }));
            //TempData.Keep("_oTINCheckModel");
            return View("/Themes/Main/Views/MerchantBoarding/_BusinessInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BusinessInformation(FormCollection fc, BusinessInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await BusinessInformationCRMPost(fc, model); }));
            //TempData.Keep("_oTINCheckModel");
            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("LegalInformation");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult LegalInformation()
        {
            LegalInformationModel model = new LegalInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await LegalInformationModelCRMGet(); }));
            //TempData.Keep("_oTINCheckModel");
            return View("/Themes/Main/Views/MerchantBoarding/_LegalInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult LegalInformation(FormCollection fc, LegalInformationModel model)
        {
            bool result = false;
            #region TINCheck
            var _oTINCheckResponse = new TINCheckResponse();
            if (TempData["_oTINCheckModel"] != null)
            {
                TempData.Keep("_oTINCheckModel");
                var _oTINCheckModel = TempData["_oTINCheckModel"] as TINCheckModel;
                _oTINCheckModel.TIN = model.TaxOrSsn;
                _oTINCheckResponse = _tINCheckService.TINCheckVerification(_oTINCheckModel);
                if (_oTINCheckResponse != null)
                {
                    TempData["_oTINCheckResponse"] = _oTINCheckResponse;
                    if (!_oTINCheckResponse.IsTINCheckVerify)
                    {
                        return Json(new { tinCheckResponse = TempData["_oTINCheckResponse"] as TINCheckResponse });
                    }
                }

                // 1) Need to pass this _oTINCheckJsonResult to CRM. For that Need one CRM field to pass Jason response                
                // 2) if this nagetive or positive bad, not to go next form and will show Alert Popup with
                // reason (Must come from language resource. Parse TIN check - TINNAME_DETAILS by {0} string.format)
            }
            #endregion
            Task.WaitAll(Task.Run(async () => { result = await LegalInformationCRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("BusinessInformation2");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult BusinessInformation2()
        {
            BusinessInformation2Model model = new BusinessInformation2Model();
            Task.WaitAll(Task.Run(async () => { model = await BusinessInformation2CRMGet(); }));
            #region TINCheck
            if (TempData["_oTINCheckResponse"] != null)
            {
                ViewBag.TINCheckResponse = TempData["_oTINCheckResponse"] as TINCheckResponse;
            }
            #endregion
            return View("/Themes/Main/Views/MerchantBoarding/_BusinessInformation2.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BusinessInformation2(FormCollection fc, BusinessInformation2Model model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await BusinessInformation2CRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                //return RedirectToAction("Questionnaire");
                return RedirectToAction("ProcessingDetails");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult Questionnaire()
        {
            QuestionnaireModel model = new QuestionnaireModel();
            Task.WaitAll(Task.Run(async () => { model = await QuestionnaireCRMGet(); }));
            if (!model.IsAllowForm)
            {
                return RedirectToAction("BankDisclosure", new { bankName = "Esquire Bank" });
            }
            return View("/Themes/Main/Views/MerchantBoarding/_Questionnaire.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Questionnaire(FormCollection fc, QuestionnaireModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await QuestionnaireCRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("BankDisclosure", new { bankName = "Esquire Bank" });
                //return RedirectToAction("Questionnaire2");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult Questionnaire2()
        {
            Questionnaire2Model model = new Questionnaire2Model();
            Task.WaitAll(Task.Run(async () => { model = await Questionnaire2CRMGet(); }));
            if (!model.IsAllowForm)
            {
                return RedirectToAction("BankDisclosure", new { bankName = "Esquire Bank" });
            }

            return View("/Themes/Main/Views/MerchantBoarding/_Questionnaire2.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Questionnaire2(FormCollection fc, Questionnaire2Model model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await Questionnaire2CRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                //return RedirectToAction("ProcessingDetails");
                return RedirectToAction("BankDisclosure", new { bankName = "Esquire Bank" });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult ProcessingDetails()
        {
            ProcessingDetailsModel model = new ProcessingDetailsModel();
            Task.WaitAll(Task.Run(async () => { model = await ProcessingDetailsCRMGet(); }));

            return View("/Themes/Main/Views/MerchantBoarding/_ProcessingDetails.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult ProcessingDetails(FormCollection fc, ProcessingDetailsModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await ProcessingDetailsCRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                //return RedirectToAction("BankDisclosure", new { bankName = "Esquire Bank" });
                return RedirectToAction("Questionnaire");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult BankDisclosure(string bankName)
        {
            BankDisclosureModel model = new BankDisclosureModel();
            Task.WaitAll(Task.Run(async () => { model = await BankDisclosureCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_BankDisclosure.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BankDisclosure(FormCollection fc, BankDisclosureModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await BankDisclosureCRMPost(fc, model); }));
            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("OwnershipInformation", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult OwnershipInformation(string bankName)
        {
            OwnershipInformation model = new OwnershipInformation();
            Task.WaitAll(Task.Run(async () => { model = await OwnershipInformationCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_OwnershipInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult OwnershipInformation(FormCollection fc, OwnershipInformation model)
        {
            TempData.Keep("bankName");
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await OwnershipInformationCRMPost(fc, model); }));

            if (result == true)
            {
                //return RedirectToAction("SiteInspection", new { bankName = model.BankName });
                SetFormCompletedStatus();
                return RedirectToAction("BankingInformation", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize] // sign
        public ActionResult SiteInspection(string bankName)
        {
            TempData.Keep("bankName");
            SiteInspectionModel model = new SiteInspectionModel();
            Task.WaitAll(Task.Run(async () => { model = await SiteInspectionCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_SiteInspection.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult SiteInspection(FormCollection fc, SiteInspectionModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await SiteInspectionCRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("BankingInformation", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize] // sign
        public ActionResult BankingInformation(string bankName)
        {
            BankingInformationModel model = new BankingInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await BankingInformationCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_BankingInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult BankingInformation(FormCollection fc, BankingInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await BankingInformationCRMPost(fc, model); }));

            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("ImportantInformation", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize] // sign
        public ActionResult ImportantInformation(string bankName)
        {
            ImportantInformationModel model = new ImportantInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await ImportantInformationCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_ImportantInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult ImportantInformation(FormCollection fc, ImportantInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await ImportantInformationCRMPost(fc, model); }));
            if (result == true)
            {
                SetFormCompletedStatus();
                //return RedirectToAction("EquipmentInformation", new { bankName = model.BankName });
                return RedirectToAction("PersonalGuarantee", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult EquipmentInformation(string bankName)
        {
            EquipmentInformationModel model = new EquipmentInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await EquipmentInformationCRMGet(bankName); }));
            model.BankName = bankName;

            return View("/Themes/Main/Views/MerchantBoarding/_EquipmentInformation.cshtml", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult EquipmentInformation(FormCollection fc, EquipmentInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await EquipmentInformationCRMPost(fc, model); }));
            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("RatesFees", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult RatesFees(string bankName)
        {
            RatesFeesModel model = new RatesFeesModel();
            Task.WaitAll(Task.Run(async () => { model = await RatesFeesCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_RatesFees.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult RatesFees(FormCollection fc, RatesFeesModel model)
        {
            bool result = false;
            result = true;
            //Task.WaitAll(Task.Run(async () => { result = await RatesFeesCRMPost(fc, model); }));
            if (result == true)
            {
                SetFormCompletedStatus();
                return RedirectToAction("RecurringFees", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize] // sign
        public ActionResult PersonalGuarantee(string bankName)
        {
            // The whole Fees are moved to my account -> Merchant Fees
            // Here only Fee Disclosures cards information shown
            PersonalGuaranteeModel model = new PersonalGuaranteeModel();
            model.BankName = bankName;
            model.CustomerEmail = _workContext.CurrentCustomer.Email;
            var topic = _topicService.GetTopicBySystemName("PersonalGuaranteeDescription");
            if (topic != null)
            {
                model.PersonalGuaranteeDesc = topic.Body;
            }
            Task.WaitAll(Task.Run(async () => { model = await PersonalGuaranteeCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_PersonalGuarantee.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult PersonalGuarantee(FormCollection fc, PersonalGuaranteeModel model)
        {
            // The whole Fees are moved to my account -> Merchant Fees
            // Here only Fee Disclosures cards information shown
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await PersonalGuaranteeCRMPost(fc, model); }));
            if (result == true)
            {
                SetFormCompletedStatus();

                // Docusign
                //--------

                return RedirectToAction("MerchantThankYou", new { bankName = model.BankName });
            }
            else
            {
                return Content("");
            }            
        }

        [Authorize] // sign
        public ActionResult MerchantFees(string bankName = "Esquire Bank")
        {
            RecurringFeesModel model = new RecurringFeesModel();
            Task.WaitAll(Task.Run(async () => { model = await MerchantFeesCRMGet(bankName); }));

            return View("/Themes/Main/Views/MerchantBoarding/_MerchantFees.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult MerchantFees(FormCollection fc, RecurringFeesModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await MerchantFeesCRMPost(fc, model); }));
            if (result == true)
            {
                return Content("/");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult MerchantThankYou(string bankName)
        {
            MerchantThankYouModel model = new MerchantThankYouModel();
            // Send email to merchant
            model.BankName = bankName;
            return View("/Themes/Main/Views/MerchantBoarding/_MerchantThankYou.cshtml", model);
        }

        [Authorize]
        public ActionResult MerchantSteps(string _pCurrentForm)
        {
            FormSteps model = new FormSteps();
            model.FormsCompleted = GetFormCompletedStatus();
            model.CurrentForm = _pCurrentForm;
            return View("/Themes/Main/Views/MerchantBoarding/_MerchantSteps.cshtml", model);
        }


        #endregion

        #region Partner ISO/Bank Methods        
        public async Task RegisterPartnerCRM(FormCollection form)
        {
            try
            {
                Configuration config = null;
                config = new FileConfiguration(null);
                //Create a helper object to authenticate the user with this connection info.
                Authentication auth = new Authentication(config);
                //Next use a HttpClient object to connect to specified CRM Web service.
                httpClient = new HttpClient(auth.ClientHandler, true);
                //httpClient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential("akhasia@olb.com", "eVance1234!", "olb.com") });
                //Define the Web API base address, the max period of execute time, the 
                // default OData version, and the default response payload format.
                httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");
                // httpClient.Timeout = new TimeSpan(0, 2, 0);              

                // new_merchantpartnertype                
                registerForm.Add("new_merchantpartnertype", int.Parse(form["partnerType"]));
                // new_parentpartner - Need to ask Ronny
                registerForm.Add("new_corporatename", form["CorporateName"]);
                registerForm.Add("new_name", form["ContactName"]);
                registerForm.Add("new_emailaddress", form["EmailAddress"]);
                registerForm.Add("new_telephonenumber", form["TelephoneNumber"]);
                //registerForm.Add("new_transactiontype", int.Parse(form["transactionType"]));
                registerForm.Add("new_transactiontypecsv", form["transactionType"]);

                //new_bycheckingthisboxiamconsentingtoproviding - Need to Ask Ronny , do we need to store in CRM or not

                HttpRequestMessage createRequest1 =
                    new HttpRequestMessage(HttpMethod.Post, getVersionedWebAPIPath() + "new_merchantpartners");
                createRequest1.Content = new StringContent(registerForm.ToString(),
                    Encoding.UTF8, "application/json");
                HttpResponseMessage createResponse1 =
                    await httpClient.SendAsync(createRequest1);
                if (createResponse1.StatusCode == HttpStatusCode.NoContent)  //204
                {
                    // Partner registered
                }
                else
                {
                    throw new CrmHttpResponseException(createResponse1.Content);
                }
            }
            catch (Exception ex)
            { DisplayException(ex); }
        }
        /// <summary>
        /// Regaister partner (ISO/Bank) as a customer with role of "Partner"
        /// </summary>
        /// <param name="form"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private string RegisterPartner(FormCollection form, out List<string> errors)
        {
            errors = new List<string>();

            if (_workContext.CurrentCustomer.IsRegistered())
            {
                //Already registered customer. 
                _authenticationService.SignOut();

                //raise logged out event       
                _eventPublisher.Publish(new CustomerLoggedOutEvent(_workContext.CurrentCustomer));

                //Save a new record
                _workContext.CurrentCustomer = _customerService.InsertGuestCustomer();
            }
            var customer = _workContext.CurrentCustomer;
            string Password = form["Password"];

            bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
            var registrationRequest = new CustomerRegistrationRequest(customer,
                     form["EmailAddress"],
                     form["EmailAddress"],
                     Password,
                     _customerSettings.DefaultPasswordFormat,
                     _storeContext.CurrentStore.Id,
                     isApproved);

            // Add "Partner" role manually
            var registeredRole = _customerService.GetCustomerRoleBySystemName("Partner");
            if (registeredRole != null)
                registrationRequest.Customer.CustomerRoles.Add(registeredRole);
            //----------------------------

            var registrationResult = _customerRegistrationService.RegisterCustomer(registrationRequest);
            if (registrationResult.Success)
            {

                //form fields
                string fullName = form["ContactName"];
                string firstName = string.Empty;
                string lastName = string.Empty;
                if (!string.IsNullOrEmpty(fullName) && fullName.Split(' ').Length > 1)
                {
                    string[] name = fullName.Split(' ');
                    if (name.Length > 1)
                    {
                        firstName = name[0];
                        lastName = name[1];
                    }
                    else
                    {
                        firstName = name[0];
                    }
                }
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.FirstName, firstName);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.LastName, lastName);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Company, form["CorporateName"]);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.Phone, form["TelephoneNumber"]);

                //notifications
                if (_customerSettings.NotifyNewCustomerRegistration)
                    _workflowMessageService.SendCustomerRegisteredNotificationMessage(customer, _localizationSettings.DefaultAdminLanguageId);

                //login customer now
                if (isApproved)
                    _authenticationService.SignIn(customer, true);

                switch (_customerSettings.UserRegistrationType)
                {
                    case UserRegistrationType.EmailValidation:
                        {
                            //email validation message
                            _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.AccountActivationToken, Guid.NewGuid().ToString());
                            _workflowMessageService.SendCustomerEmailValidationMessage(customer, _workContext.WorkingLanguage.Id);

                            //result                            
                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });

                        }
                    case UserRegistrationType.AdminApproval:
                        {
                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                        }
                    case UserRegistrationType.Standard:
                        {
                            //send customer welcome message
                            _workflowMessageService.SendCustomerWelcomeMessage(customer, _workContext.WorkingLanguage.Id);

                            return Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
                        }
                    default:
                        {
                            return Url.RouteUrl("HomePage");
                        }
                }
            }
            else
            {
                foreach (var err in registrationResult.Errors)
                {
                    errors.Add(err);
                }
                return "";
            }
        }

        public ActionResult RegisterPartner()
        {
            ViewBag.Error = TempData["Error"];
            return View("/Themes/Main/Views/PartnerRegistration/RegisterPartner.cshtml");
        }

        [HttpPost]
        public ActionResult RegisterPartner(FormCollection form)
        {
            string redirectUrl = "/";
            try
            {
                //Read configuration file and connect to specified CRM server.mstsc
                //ConnectToCRM();
                List<string> errors = new List<string>();
                redirectUrl = RegisterPartner(form, out errors);
                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    Task.WaitAll(Task.Run(async () => await RegisterPartnerCRM(form)));
                }
                else
                {
                    TempData["Error"] = string.Join("<br>", errors);
                    return Redirect("/RegisterPartner");
                }
            }
            catch (System.Exception ex) { DisplayException(ex); }
            finally
            {
                if (httpClient != null)
                { httpClient.Dispose(); }
            }
            return Redirect(redirectUrl);
        }
        [Authorize]
        public ActionResult CompanyInformation()
        {
            CompanyInformationModel model = new CompanyInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await CompanyInformationCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_CompanyInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult CompanyInformation(FormCollection fc, CompanyInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await CompanyInformationCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("PrincipleInformation");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult PrincipleInformation()
        {
            PrincipleInformationModel model = new PrincipleInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await PrincipleInformationCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_PrincipleInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult PrincipleInformation(FormCollection fc, PrincipleInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await PrincipleInformationCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("Authorization");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult Authorization()
        {
            AuthorizationModel model = new AuthorizationModel();
            Task.WaitAll(Task.Run(async () => { model = await AuthorizationCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_Authorization.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Authorization(FormCollection fc, AuthorizationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await AuthorizationCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("Agreement");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult Agreement()
        {
            AgreementModel model = new AgreementModel();
            Task.WaitAll(Task.Run(async () => { model = await AgreementCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_Agreement.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Agreement(FormCollection fc, AgreementModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await AgreementCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("AppendixA", new { merchantId = model.MerchantId });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult AppendixA(string merchantId)
        {
            AppendixAModel model = new AppendixAModel();
            Task.WaitAll(Task.Run(async () => { model = await AppendixACRMGet(merchantId); }));
            return View("/Themes/Main/Views/PartnerRegistration/_AppendixA.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult AppendixA(FormCollection fc, AppendixAModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await AppendixACRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("AppendixB");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult AppendixB()
        {
            AppendixBModel model = new AppendixBModel();
            Task.WaitAll(Task.Run(async () => { model = await AppendixBCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_AppendixB.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult AppendixB(FormCollection fc, AppendixBModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await AppendixBCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("PaymentInformation");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult PaymentInformation()
        {
            PaymentInformationModel model = new PaymentInformationModel();
            Task.WaitAll(Task.Run(async () => { model = await PaymentInformationCRMGet(); }));
            return View("/Themes/Main/Views/PartnerRegistration/_PaymentInformation.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult PaymentInformation(FormCollection fc, PaymentInformationModel model)
        {
            bool result = false;
            Task.WaitAll(Task.Run(async () => { result = await PaymentInformationCRMPost(fc, model); }));
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                return RedirectToAction("PartnerLogo", new { MerchantId = model.MerchantId, MerchantUri = model.MerchantUri });
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult PartnerLogo(string MerchantId, string MerchantUri)
        {
            PartnerLogo model = new PartnerLogo();
            model.MerchantId = MerchantId;
            model.MerchantUri = MerchantUri;
            model = PartnerLogoDBGet();
            return View("/Themes/Main/Views/PartnerRegistration/_PartnerLogo.cshtml", model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult PartnerLogo(FormCollection fc, PartnerLogo model)
        {
            bool result = false;
            // Add / update entry in Affiliate as a partner
            Task.WaitAll(Task.Run(async () => { model.AffiliateId = await AddUpdateAffiliate(); }));
            if (model.AffiliateId > 0)
            {
                Task.WaitAll(Task.Run(async () => { result = await PartnerAffiliateDB_CRMPost(fc, model); }));
            }
            if (result == true)
            {
                SetPartnerFormCompletedStatus();
                // If a pertner is completed his all aplication once, then set CRM_Partner_Finish to 1            
                var CRM_Partner_Finish = _workContext.CurrentCustomer.GetAttribute<int>("CRM_Partner_Finish");
                if (CRM_Partner_Finish <= 0)
                {
                    _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_Partner_Finish", 1);
                }
                return RedirectToAction("PartnerThankYou");
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult PartnerThankYou()
        {
            bool result = false;
            PartnerThankYouModel model = new PartnerThankYouModel();
            // Send email to partner at here
            result = true;
            if (result == true)
            {
                return View("/Themes/Main/Views/PartnerRegistration/_PartnerThankYou.cshtml", model);
            }
            else
            {
                return Content("");
            }
        }

        [Authorize]
        public ActionResult PartnerSteps(string _pCurrentForm)
        {
            FormSteps model = new FormSteps();
            model.FormsCompleted = GetPartnerFormCompletedStatus();
            model.CurrentForm = _pCurrentForm;
            return View("/Themes/Main/Views/PartnerRegistration/_PartnerSteps.cshtml", model);
        }
        #endregion

        #region Common Methods
        /// <summary>
        /// Load Register page of Merchant or Partnet based on logged in customer role
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult LoadRegisterPage()
        {
            var currentCustomer = _workContext.CurrentCustomer;
            if (currentCustomer.CustomerRoles != null && currentCustomer.CustomerRoles.Any())
            {
                var merchantRole = currentCustomer.CustomerRoles.FirstOrDefault(r => r.SystemName.ToLower() == "merchant");
                var partnerRole = currentCustomer.CustomerRoles.FirstOrDefault(r => r.SystemName.ToLower() == "partner");
                if (merchantRole != null)
                {
                    return View("/Themes/Main/Views/MerchantBoarding/Index.cshtml");
                }
                else if (partnerRole != null)
                {
                    return View("/Themes/Main/Views/PartnerRegistration/Index.cshtml");
                }
            }
            return Content("Registerd customer is not a Merchant or Partner. Please define the role of Merchant or Partner");
        }

        //logo
        [ChildActionOnly]
        public virtual ActionResult AffiliateLogo(string affiliate, string affiliateid)
        {
            LogoModel logoModel = new LogoModel();
            var logo = "";
            if (!string.IsNullOrEmpty(affiliate) || !string.IsNullOrEmpty(affiliateid))
            {
                var model = new AffiliateModelCustom();
                string cacheKeyword = "affiliate_";
                if (!string.IsNullOrEmpty(affiliate))
                {
                    cacheKeyword = cacheKeyword + affiliate;
                    model = _dbContext.SqlQuery<AffiliateModelCustom>(
                        "select top 1 LogoId,PartnerId from [Affiliate] where FriendlyUrlName=@p0", affiliate)
                        .FirstOrDefault() ?? new AffiliateModelCustom();
                }
                else
                {
                    cacheKeyword = cacheKeyword + affiliateid;
                    model = _dbContext.SqlQuery<AffiliateModelCustom>(
                     "select top 1 LogoId,PartnerId from [Affiliate] where Id=@p0", Convert.ToInt32(affiliateid))
                     .FirstOrDefault() ?? new AffiliateModelCustom();
                }
                if (model != null)
                {
                    TempData["PartnerId"] = model.PartnerId;
                    logoModel.StoreName = "affiliate logo";
                    var cacheKey = string.Format(ModelCacheEventConsumer.STORE_LOGO_PATH + "_{3}", _storeContext.CurrentStore.Id, EngineContext.Current.Resolve<IThemeContext>().WorkingThemeName, _webHelper.IsCurrentConnectionSecured(), cacheKeyword);
                    logoModel.LogoPath = EngineContext.Current.Resolve<ICacheManager>().Get(cacheKey, () =>
                    {
                        var logoPictureId = model.LogoId;
                        if (logoPictureId > 0)
                        {
                            // set affiliate logo
                            logo = _pictureService.GetPictureUrl(logoPictureId, showDefaultPicture: false);
                        }
                        return logo;
                    });
                }
            }
            if (String.IsNullOrEmpty(logo))
            {
                // set Site logo
                logoModel = EngineContext.Current.Resolve<ICommonModelFactory>().PrepareLogoModel();
            }
            return PartialView(string.Format("/Themes/{0}/Views/Common/Logo.cshtml", EngineContext.Current.Resolve<IThemeContext>().WorkingThemeName), logoModel);
        }

        public string GetFormCompletedStatus()
        {
            return _workContext.CurrentCustomer.GetAttribute<string>("CRM_MerchantForms_Completed");
        }

        public void SetFormCompletedStatus()
        {
            var _sCurrentForm = this.ControllerContext.RouteData.Values["action"].ToString();
            var _lValue = _workContext.CurrentCustomer.GetAttribute<string>("CRM_MerchantForms_Completed");
            if (string.IsNullOrEmpty(_lValue))
            {
                _lValue += _sCurrentForm + ",";
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_MerchantForms_Completed", _lValue);
            }
            else if (!_lValue.Contains(_sCurrentForm))
            {
                _lValue += _sCurrentForm + ",";
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_MerchantForms_Completed", _lValue);
            }
        }

        public string GetPartnerFormCompletedStatus()
        {
            return _workContext.CurrentCustomer.GetAttribute<string>("CRM_PartnerForms_Completed");
        }
        public void SetPartnerFormCompletedStatus()
        {
            var _sCurrentForm = this.ControllerContext.RouteData.Values["action"].ToString();
            var _lValue = _workContext.CurrentCustomer.GetAttribute<string>("CRM_PartnerForms_Completed");
            if (string.IsNullOrEmpty(_lValue))
            {
                _lValue += _sCurrentForm + ",";
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_PartnerForms_Completed", _lValue);
            }
            else if (!_lValue.Contains(_sCurrentForm))
            {
                _lValue += _sCurrentForm + ",";
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, "CRM_PartnerForms_Completed", _lValue);
            }
        }

        public string GeneratePartnerRepCode()
        {
            string _sRepCOde = string.Empty;
            var CRM_Partner_Finish = _workContext.CurrentCustomer.GetAttribute<int>("CRM_Partner_Finish");
            if (CRM_Partner_Finish <= 0)
            {
                int _iPartnerType = 0;
                int.TryParse(PartnerType, out _iPartnerType);
                int _iCustomerId = _workContext.CurrentCustomer.Id;
                switch (_iPartnerType)
                {
                    case 1: // ISO Partner , Sales Offices
                        int ISOCOunter = _settingService.GetSettingByKey<int>("MerchantBoarding.ISOCounter");
                        ISOCOunter = ISOCOunter + 1;
                        _sRepCOde = "ISO-" + _iCustomerId.ToString("D6") + "-" + ISOCOunter.ToString("D2");
                        _settingService.SetSetting<int>("MerchantBoarding.ISOCounter", ISOCOunter);
                        break;
                    case 2: // Bank Referral Partner
                        _sRepCOde = "BNK-" + _iCustomerId.ToString("D6") + "-00";
                        break;
                    case 3: //  ISV Partner
                        _sRepCOde = "ISV-" + _iCustomerId.ToString("D6") + "-00";
                        break;
                    case 4: //  External Sales Rep, Referral Agent
                        _sRepCOde = "ESP-" + _iCustomerId.ToString("D6") + "-00";
                        break;
                    default: // In House Rep, none of selected
                        _sRepCOde = "INH-" + _iCustomerId.ToString("D6") + "-00";
                        break;
                }
            }
            return _sRepCOde;
        }
        #endregion

        #region Merchant PDF
        private async Task<AcroFields> SetPDFCommonField(PdfStamper pdfStamper)
        {
            Configuration config = null;
            config = new FileConfiguration(null);
            //Create a helper object to authenticate the user with this connection info.
            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the 
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/");

            MerchantInformationModel model = new MerchantInformationModel();

            string CustomerEmail = _workContext.CurrentCustomer.Email;
            if (!string.IsNullOrEmpty(CustomerEmail))
            {
                // Get merchant details from customer email
                //string queryOptions = "?$select=new_merchantboardingid&$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                string queryOptions = "?$filter=new_businessemailaddress eq '" + CustomerEmail + "'";
                HttpResponseMessage merchantResponse = await httpClient.GetAsync(
               getVersionedWebAPIPath() + "new_merchantboardings" + queryOptions);
                if (merchantResponse.StatusCode == HttpStatusCode.OK) //200
                {
                    retrievedData = JsonConvert.DeserializeObject<JObject>(
                        await merchantResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new CrmHttpResponseException(merchantResponse.Content);
                }
                if (retrievedData != null)
                {
                    var jvalue = retrievedData.GetValue("value");

                    JObject inner = jvalue[0].Value<JObject>();
                    List<string> keys = inner.Properties().Select(p => p.Name).ToList();

                    if (jvalue != null && jvalue.Count() > 0)
                    {
                        string merchantId = Convert.ToString(jvalue[0].SelectToken("new_merchantboardingid"));

                        // set merchant CRM information in PDF
                        var formFields = pdfStamper.AcroFields;

                        foreach (var key in keys)
                        {
                            formFields.SetField(key, Convert.ToString(jvalue[0].SelectToken(key)));

                            if (key.Contains("_state"))
                            {
                                int SelectedState = -1;
                                int.TryParse(Convert.ToString(jvalue[0].SelectToken(key)), out SelectedState);
                                formFields.SetField(key, model.StateList.FirstOrDefault(s => s.StateIndex == SelectedState).StateName);
                            }
                        }

                        int mccccode = -1;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_mcccode")), out mccccode);
                        formFields.SetField("new_mcccode", model.MCCCodeList.FirstOrDefault(s => s.StateIndex == mccccode).StateName);

                        #region Owner Information
                        formFields.SetField("new_dateofbirth", SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth"))));
                        formFields.SetField("new_dateofbirth1", SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth1"))));
                        formFields.SetField("new_dateofbirth2", SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth2"))));
                        formFields.SetField("new_dateofbirth3", SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth3"))));
                        formFields.SetField("new_dateofbirth4", SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_dateofbirth4"))));

                        formFields.SetField("new_drivinglicensenumberexpdate1", Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber1")) + " , " +
                            SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate1"))));
                        formFields.SetField("new_drivinglicensenumberexpdate2", Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber2")) + " , " +
                            SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate2"))));
                        formFields.SetField("new_drivinglicensenumberexpdate3", Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber4")) + " , " +
                            SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate4"))));
                        formFields.SetField("new_drivinglicensenumberexpdate4", Convert.ToString(jvalue[0].SelectToken("new_drivinglicensenumber5")) + " , " +
                            SetPDFDateFormatFromField(Convert.ToString(jvalue[0].SelectToken("new_drivinglicenseexpdate5"))));
                        formFields.SetField("new_owner1title", Convert.ToString(jvalue[0].SelectToken("new_title")));
                        formFields.SetField("new_owner2title", Convert.ToString(jvalue[0].SelectToken("new_title1")));

                        var noteModel2 = new NoteEntityModel();
                        //ImpInfo_Signature2
                        noteModel2 = await GetFIleFromNote("ImpInfo_Signature2", merchantId);
                        if (noteModel2 != null)
                        {
                            FillImage("ImpInfo_Signature2", pdfStamper, noteModel2.documentbodybase64);
                            FillImage("ImpInfo_Signature4", pdfStamper, noteModel2.documentbodybase64);
                        }

                        formFields.SetField("new_owner1fullname",
                           Convert.ToString(jvalue[0].SelectToken("new_firstname1")) + " " +
                           Convert.ToString(jvalue[0].SelectToken("new_lastname1")) + " " +
                           Convert.ToString(jvalue[0].SelectToken("new_title1"))
                           );

                        var OwnershipPercent = Convert.ToString(jvalue[0].SelectToken("new_ownership"));
                        int OwnershipPercentValue;
                        int.TryParse(OwnershipPercent, out OwnershipPercentValue);
                        if (OwnershipPercentValue >= 80)
                        {
                            // owner 2
                            formFields.SetField("new_firstname2", "");
                            formFields.SetField("new_middleint1", "");
                            formFields.SetField("new_lastname2", "");
                            formFields.SetField("new_title1", "");
                            formFields.SetField("new_ssn1", "");
                            formFields.SetField("new_ownership1", "");
                            formFields.SetField("new_dateofbirth1", "");
                            formFields.SetField("new_homeaddress1", "");
                            formFields.SetField("new_city10", "");
                            formFields.SetField("new_zipcode10", "");
                            formFields.SetField("new_homephone10", "");
                            formFields.SetField("new_emailaddress10", "");
                            formFields.SetField("new_state10", "");
                            formFields.SetField("new_drivinglicensenumberexpdate2", "");


                            // owner 3
                            formFields.SetField("new_firstname4", "");
                            formFields.SetField("new_middleint3", "");
                            formFields.SetField("new_lastname4", "");
                            formFields.SetField("new_title3", "");
                            formFields.SetField("new_ssn3", "");
                            formFields.SetField("new_ownership3", "");
                            formFields.SetField("new_dateofbirth3", "");
                            formFields.SetField("new_homeaddress4", "");
                            formFields.SetField("new_city12", "");
                            formFields.SetField("new_zipcode12", "");
                            formFields.SetField("new_homephone12", "");
                            formFields.SetField("new_emailaddress12", "");
                            formFields.SetField("new_state12", "");
                            formFields.SetField("new_drivinglicensenumberexpdate3", "");

                            // Owner 4
                            formFields.SetField("new_firstname5", "");
                            formFields.SetField("new_middleint4", "");
                            formFields.SetField("new_lastname5", "");
                            formFields.SetField("new_title4", "");
                            formFields.SetField("new_ssn4", "");
                            formFields.SetField("new_ownership4", "");
                            formFields.SetField("new_dateofbirth4", "");
                            formFields.SetField("new_homeaddress5", "");
                            formFields.SetField("new_city13", "");
                            formFields.SetField("new_zipcode13", "");
                            formFields.SetField("new_homephone13", "");
                            formFields.SetField("new_emailaddress13", "");
                            formFields.SetField("new_state13", "");
                            formFields.SetField("new_drivinglicensenumberexpdate4", "");
                        }
                        else if (OwnershipPercentValue < 80 && OwnershipPercentValue >= 50)
                        {
                            // owner 3
                            formFields.SetField("new_firstname4", "");
                            formFields.SetField("new_middleint3", "");
                            formFields.SetField("new_lastname4", "");
                            formFields.SetField("new_title3", "");
                            formFields.SetField("new_ssn3", "");
                            formFields.SetField("new_ownership3", "");
                            formFields.SetField("new_dateofbirth3", "");
                            formFields.SetField("new_homeaddress4", "");
                            formFields.SetField("new_city12", "");
                            formFields.SetField("new_zipcode12", "");
                            formFields.SetField("new_homephone12", "");
                            formFields.SetField("new_emailaddress12", "");
                            formFields.SetField("new_state12", "");
                            formFields.SetField("new_drivinglicensenumberexpdate3", "");

                            // Owner 4
                            formFields.SetField("new_firstname5", "");
                            formFields.SetField("new_middleint4", "");
                            formFields.SetField("new_lastname5", "");
                            formFields.SetField("new_title4", "");
                            formFields.SetField("new_ssn4", "");
                            formFields.SetField("new_ownership4", "");
                            formFields.SetField("new_dateofbirth4", "");
                            formFields.SetField("new_homeaddress5", "");
                            formFields.SetField("new_city13", "");
                            formFields.SetField("new_zipcode13", "");
                            formFields.SetField("new_homephone13", "");
                            formFields.SetField("new_emailaddress13", "");
                            formFields.SetField("new_state13", "");
                            formFields.SetField("new_drivinglicensenumberexpdate4", "");
                        }
                        else if (OwnershipPercentValue < 50 && OwnershipPercentValue >= 25)
                        {
                            // Owner 4
                            formFields.SetField("new_firstname5", "");
                            formFields.SetField("new_middleint4", "");
                            formFields.SetField("new_lastname5", "");
                            formFields.SetField("new_title4", "");
                            formFields.SetField("new_ssn4", "");
                            formFields.SetField("new_ownership4", "");
                            formFields.SetField("new_dateofbirth4", "");
                            formFields.SetField("new_homeaddress5", "");
                            formFields.SetField("new_city13", "");
                            formFields.SetField("new_zipcode13", "");
                            formFields.SetField("new_homephone13", "");
                            formFields.SetField("new_emailaddress13", "");
                            formFields.SetField("new_state13", "");
                            formFields.SetField("new_drivinglicensenumberexpdate4", "");
                        }

                        #endregion

                        formFields.SetField("new_todaydate", DateTime.Now.ToString("MM/dd/yyyy"));

                        formFields.SetField("new_nonemvcompliance", Convert.ToString(jvalue[0].SelectToken("new_government_compliance_fee")));
                        formFields.SetField("new_chargebackretrievalperretrieval", Convert.ToString(jvalue[0].SelectToken("new_chargebackretrievalperretrievaltype")));                        

                        #region PDF Checkboxes                        
                        formFields.SetField("new_importantinformationcheck", GetCheckValue(ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_importantinformationcheck")))), true);

                        if (ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_doyoucurrentlyacceptpaymentcards"))))
                        {
                            formFields.SetField("new_doyoucurrentlyacceptpaymentcards_yes", "Yes", true);
                        }
                        else
                        {
                            formFields.SetField("new_doyoucurrentlyacceptpaymentcards_No", "Yes", true);
                        }

                        int TypeOfBusiness;
                        int.TryParse(Convert.ToString(jvalue[0].SelectToken("new_typeofbusiness")), out TypeOfBusiness);
                        formFields.SetField("new_typeofbusiness_" + TypeOfBusiness, "Yes", true);

                        if (ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_mailingaddress"))))
                        {
                            formFields.SetField("new_mailingaddress_dba", "Yes", true);
                        }
                        else
                        {
                            formFields.SetField("new_mailingaddress_legal", "Yes", true);
                        }

                        if (ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_controllinginterest"))))
                        {
                            formFields.SetField("new_controllinginterest_yes", "On", true);
                        }
                        else
                        {
                            formFields.SetField("new_controllinginterest_no", "On", true);
                        }

                        if (ConvertToBoolean(Convert.ToString(jvalue[0].SelectToken("new_hasthemerchantbeenterminatedfromaccepting"))))
                        {
                            formFields.SetField("new_hasthemerchantbeenterminatedfromaccepting_yes", "Yes", true);
                        }
                        else
                        {
                            formFields.SetField("new_hasthemerchantbeenterminatedfromaccepting_no", "Yes", true);
                            formFields.SetField("new_ifsoexplain", "");
                        }

                        string haveMerchantBankruptcy = Convert.ToString(jvalue[0].SelectToken("new_havemerchantbankruptcycsv"));
                        if (!string.IsNullOrEmpty(haveMerchantBankruptcy) && haveMerchantBankruptcy.Contains("1"))
                        {
                            formFields.SetField("new_busbankruptcy", "On", true);
                        }
                        if (!string.IsNullOrEmpty(haveMerchantBankruptcy) && haveMerchantBankruptcy.Contains("2"))
                        {
                            formFields.SetField("new_perbankruptcy", "On", true);
                        }
                        if (!string.IsNullOrEmpty(haveMerchantBankruptcy) && haveMerchantBankruptcy.Contains("3"))
                        {
                            formFields.SetField("new_neverfiled", "On", true);
                            formFields.SetField("new_pleaseexplain", "");
                        }

                        //AcroFields.FieldPosition fieldPosition = pdfStamper.AcroFields.GetFieldPositions("new_importantinformationcheck")[0];
                        //PdfContentByte cb = pdfStamper.GetOverContent(fieldPosition.page);
                        //PdfAppearance[] onOff = new PdfAppearance[2];
                        //onOff[0] = cb.CreateAppearance(20, 20);
                        //onOff[0].Rectangle(1, 1, 18, 18);
                        //onOff[0].Stroke();

                        //onOff[1] = cb.CreateAppearance(20, 20);
                        //onOff[1].SetRGBColorFill(255, 128, 128);
                        //onOff[1].Rectangle(1, 1, 18, 18);
                        //onOff[1].FillStroke();
                        //onOff[1].MoveTo(1, 1);
                        //onOff[1].LineTo(19, 19);
                        //onOff[1].MoveTo(1, 19);
                        //onOff[1].LineTo(19, 1);
                        //onOff[1].Stroke();

                        //var checkbox = new RadioCheckField(pdfStamper.Writer, new iTextSharp.text.Rectangle(20, 20), "new_importantinformationcheck", "Yes");
                        //var _chkField = checkbox.CheckField;
                        //_chkField.SetAppearance(PdfAnnotation.APPEARANCE_NORMAL, "Off", onOff[0]);
                        //_chkField.SetAppearance(PdfAnnotation.APPEARANCE_NORMAL, "Yes", onOff[1]);
                        //pdfStamper.AddAnnotation(_chkField, fieldPosition.page);
                        #endregion

                        #region PDF Images                        
                        NoteEntityModel noteModel = new NoteEntityModel();
                        //ImpInfo_Signature1
                        noteModel = await GetFIleFromNote("ImpInfo_Signature1", merchantId);
                        if (noteModel != null)
                        {
                            FillImage("ImpInfo_Signature1", pdfStamper, noteModel.documentbodybase64);
                            FillImage("ImpInfo_Signature3", pdfStamper, noteModel.documentbodybase64);
                            FillImage("ImpInfo_Signature5", pdfStamper, noteModel.documentbodybase64);
                        }
                        #endregion
                        return formFields;
                    }
                }
            }
            return null;
        }

        [Authorize] // sign
        public ActionResult MerchantPDF()
        {
            #region PDF Generation code - CRM         
            var basePath = "~/Plugins/Misc.Plugin.MerchantBoarding/PDFs/";
            // Check if template is exist
            if (System.IO.File.Exists(Server.MapPath(basePath + "Templates/Esquire_Bank.pdf")))
            {
                // create a new PDF reader based on the PDF template document
                string pdfTemplate = Server.MapPath(basePath + "Templates/Esquire_Bank.pdf");
                var pdfReader = new PdfReader(pdfTemplate);
                if (!Directory.Exists(Server.MapPath(basePath + "Esquire_Bank")))
                {
                    Directory.CreateDirectory(Server.MapPath(basePath + "Esquire_Bank"));
                }

                // create a new PDF stamper to create investor PDF from template document
                string pdfFileName = string.Format("Esquire_Bank-{0}-{1}.pdf", _workContext.CurrentCustomer.Email, _workContext.CurrentCustomer.Id);
                var merchantPdfFile = Server.MapPath(basePath + pdfFileName);
                var pdfStamper = new PdfStamper(pdfReader, new FileStream(merchantPdfFile, FileMode.OpenOrCreate));

                Task.WaitAll(Task.Run(async () => { await SetPDFCommonField(pdfStamper); }));

                pdfStamper.FormFlattening = true;
                pdfStamper.Close();
                pdfReader.Close();

                //Response.ClearHeaders();
                //Response.ContentType = "application/pdf";
                //Response.AddHeader("Content-Disposition", "attachment; filename="+ pdfFileName);
                //Response.TransmitFile(Server.MapPath(basePath+ pdfFileName));
                //Response.End();

                byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath(basePath + pdfFileName));
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, pdfFileName);
            }

            return new EmptyResult();
            #endregion
        }

        private void FillImage(string fieldName, PdfStamper pdfStamper, string base64String)
        {
            if (!string.IsNullOrEmpty(base64String))
            {
                if (pdfStamper.AcroFields.GetFieldPositions(fieldName) != null)
                {
                    byte[] signImage = System.Convert.FromBase64String(base64String);

                    AcroFields.FieldPosition fieldPosition = pdfStamper.AcroFields.GetFieldPositions(fieldName)[0];
                    PushbuttonField imageField = new PushbuttonField(pdfStamper.Writer, fieldPosition.position, fieldName);
                    imageField.Layout = PushbuttonField.LAYOUT_ICON_ONLY;
                    imageField.Image = iTextSharp.text.Image.GetInstance(signImage);
                    imageField.ScaleIcon = PushbuttonField.SCALE_ICON_ALWAYS;
                    imageField.ProportionalIcon = false;
                    imageField.Options = BaseField.READ_ONLY;
                    pdfStamper.AcroFields.RemoveField(fieldName);
                    pdfStamper.AddAnnotation(imageField.Field, fieldPosition.page);
                }
            }
        }

        public String getCheckboxValue(PdfStamper pdfStamper)
        {
            var formFields = pdfStamper.AcroFields;

            String[] values = formFields.GetAppearanceStates("new_importantinformationcheck");
            StringBuilder sb = new StringBuilder();
            foreach (String value in values)
            {
                sb.Append(value);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public string GetCheckValue(bool _value, string type = "yesno")
        {
            if (type == "onoff")
            {
                if (_value)
                    return "On";
                else
                    return "Off";
            }
            else
            {
                if (_value)
                    return "Yes";
                else
                    return "No";
            }
        }

        public string SetPDFDateFormatFromField(string _dateString)
        {
            DateTime DateOfBirth;
            DateTime.TryParse(_dateString, out DateOfBirth);
            return DateOfBirth.ToString("MM/dd/yyyy");
        }

        public void GeneratePDFDocusign()
        {            
            var basePath = "~/Plugins/Misc.Plugin.MerchantBoarding/PDFs/";
            // Check if template is exist
            if (System.IO.File.Exists(Server.MapPath(basePath + "Templates/Esquire_Bank.pdf")))
            {
                #region PDF Generation code
                // create a new PDF reader based on the PDF template document
                string pdfTemplate = Server.MapPath(basePath + "Templates/Esquire_Bank.pdf");
                var pdfReader = new PdfReader(pdfTemplate);
                if (!Directory.Exists(Server.MapPath(basePath + "Esquire_Bank")))
                {
                    Directory.CreateDirectory(Server.MapPath(basePath + "Esquire_Bank"));
                }

                // create a new PDF stamper to create investor PDF from template document
                string pdfFileName = string.Format("Esquire_Bank-{0}-{1}.pdf", _workContext.CurrentCustomer.Email, _workContext.CurrentCustomer.Id);
                var merchantPdfFile = Server.MapPath(basePath + pdfFileName);
                var pdfStamper = new PdfStamper(pdfReader, new FileStream(merchantPdfFile, FileMode.OpenOrCreate));

                Task.WaitAll(Task.Run(async () => { await SetPDFCommonField(pdfStamper); }));

                pdfStamper.FormFlattening = true;
                pdfStamper.Close();
                pdfReader.Close();
                #endregion

                #region Docusign
                // initialize client for desired environment (for production change to www)
                ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
                var baseUrl = "https://demo.docusign.net/restapi";
                Configuration.Default.ApiClient = apiClient;

                // configure 'X-DocuSign-Authentication' header
                string authHeader = string.Concat("{", string.Format("\"Username\":\"{0}\",\"Password\":\"{1}\",\"IntegratorKey\":\"{2}\"",
                    "jmartin@evanceprocessing.com", "Qw1declc3!", "0e22f21b-d194-4336-a1fa-8cc9b76eaf45"), "}");
                if (!Configuration.Default.DefaultHeader.Any())
                    Configuration.Default.AddDefaultHeader("X-DocuSign-Authentication", authHeader);

                /* ---------- Step 1: Login API ----------  */
                // login call is available in the authentication api 
                AuthenticationApi authApi = new AuthenticationApi();
                LoginInformation loginInfo = authApi.Login();

                // parse the first account ID that is returned (user might belong to multiple accounts)
                var accountId = loginInfo.LoginAccounts[0].AccountId;
                baseUrl = loginInfo.LoginAccounts[0].BaseUrl;

                // Update ApiClient with the new base url from login call
                apiClient = new ApiClient(baseUrl.Substring(0, baseUrl.IndexOf("v") - 1));
                Configuration.Default.ApiClient = apiClient;

                /* ---------- Step 2: Create Envelope API ---------- */
                // create a new envelope which we will use to send the signature request            
                EnvelopeDefinition envDef = new EnvelopeDefinition();
                envDef.EmailSubject = "demo_pdf_subject - " + model.Name1;

                // Add a document to the envelope
                var pdf = Server.MapPath("/Content/Date.pdf");
                byte[] investorPdfFileBytes = System.IO.File.ReadAllBytes(pdf);
                var investorPdfDocument = new Document();
                investorPdfDocument.DocumentBase64 = System.Convert.ToBase64String(investorPdfFileBytes);
                investorPdfDocument.Name = "Date.pdf";
                investorPdfDocument.DocumentId = "1";
                envDef.Documents = new List<Document>();
                envDef.Documents.Add(investorPdfDocument);

                // Add a recipient to sign the documeent
                Signer signer = new Signer();
                signer.Email = "bhavik@gmail.com";
                signer.Name = "Bhavik";
                signer.RecipientId = "1";
                signer.ClientUserId = "123";

                // Create a |SignHere| tab somewhere on the document for the recipient to sign
                signer.Tabs = new Tabs();
                signer.Tabs.SignHereTabs = new List<SignHere>();
                SignHere signHere = new SignHere();
                signHere.DocumentId = "1";
                signHere.PageNumber = "1";
                signHere.RecipientId = "1";
                signHere.XPosition = "120";
                signHere.YPosition = "250";
                signer.Tabs.SignHereTabs.Add(signHere);


                envDef.Recipients = new Recipients();
                envDef.Recipients.Signers = new List<Signer>();
                envDef.Recipients.Signers.Add(signer);

                // set envelope status to "sent" to immediately send the signature request
                envDef.Status = "sent";
                // |EnvelopesApi| contains methods related to creating and sending Envelopes (aka signature requests)
                EnvelopesApi envelopesApi = new EnvelopesApi();
                EnvelopeSummary envelopeSummary = envelopesApi.CreateEnvelope(accountId, envDef);
                RecipientViewRequest viewOptions = new RecipientViewRequest()
                {
                    //ReturnUrl = String.Format("{0}/crowdpay/SigningResult?documentType={1}&productId={2}", "https://crowdignition.com/", "SubscriptionAgreementTemplateId", 0),
                    ReturnUrl = "http://localhost:63652/",
                    ClientUserId = "123",  // must match clientUserId set in step #2!
                    AuthenticationMethod = "email",
                    UserName = "Bhavik",
                    Email = "bhavik@gmail.com",
                };

                // create the recipient view(aka signing URL)
                //ViewUrl recipientView = envelopesApi.CreateRecipientView(accountId, envelopeSummary.EnvelopeId, viewOptions);
                ViewUrl recipientView = envelopesApi.CreateRecipientView(accountId, envelopeSummary.EnvelopeId, viewOptions);

                model.RecipientViewUrl = recipientView.Url;
                #endregion

            }                        
        }
        #endregion
    }
}