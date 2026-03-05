using SparkService.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayPal;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using PayPal.Core;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System.Net;
using System.Data;
using System.Net.Http;
using MimeKit;
using SparkService.ViewModels;
using System.Net.Mime;
using PayPal.v1.Sync;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Xml.Linq;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;
using AuthorizeNet.Api.Controllers;
using Amazon.Auth.AccessControlPolicy;
using MongoDB.Bson;
using System.Security.Cryptography;
using Amazon.Runtime.Internal;
using MongoDB.Driver.Linq;

namespace SparkService.Services
{
    public class SubscriptionService
    {

        private readonly IMongoCollection<SubscriptionPlanServices> _subscriptionPlanServicesCollection;
        private readonly IMongoCollection<SubscriptionPlans> _subscriptionPlansCollection;
        private readonly IMongoCollection<SubscriptionServices> _subscriptionServicesCollection;
        private readonly IMongoCollection<Subscriptions> _subscriptionsCollection;
        private readonly IMongoCollection<SubscriptionsHistories> _subscriptionsHistoriesCollection;
        private readonly IMongoCollection<SubscriptionPayments> _subscriptionPaymentsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Profile> _profileCollection;
        private readonly MailService _mailService;
        private readonly PaypalOptions _paypalOptions;
        private readonly AuthorizeNetOptions _authorizeNetOptions;

        private const string PAYPAL_PRODUCT_NAME = "Happy SugarDaddy Subscription";

        public SubscriptionService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, IOptions<PaypalOptions> paypalOptions, IOptions<AuthorizeNetOptions> authorizeNetOptions, MailService mailService)
        {
            var mongoClient = new MongoClient(
            happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _subscriptionPlanServicesCollection = mongoDatabase.GetCollection<SubscriptionPlanServices>(
                happySugarDaddyDatabaseSettings.Value.SubscriptionPlanServicesCollectionName);

            _subscriptionPlansCollection = mongoDatabase.GetCollection<SubscriptionPlans>(
               happySugarDaddyDatabaseSettings.Value.SubscriptionPlansCollectionName);

            _subscriptionServicesCollection = mongoDatabase.GetCollection<SubscriptionServices>(
               happySugarDaddyDatabaseSettings.Value.SubscriptionServicesCollectionName);

            _subscriptionsCollection = mongoDatabase.GetCollection<Subscriptions>(
              happySugarDaddyDatabaseSettings.Value.SubscriptionsCollectionName);

            _subscriptionsHistoriesCollection = mongoDatabase.GetCollection<SubscriptionsHistories>(
              happySugarDaddyDatabaseSettings.Value.SubscriptionsHistoriesCollectionName);

            _subscriptionPaymentsCollection = mongoDatabase.GetCollection<SubscriptionPayments>(
              happySugarDaddyDatabaseSettings.Value.SubscriptionPaymentsCollectionName);

            _usersCollection = mongoDatabase.GetCollection<User>(
               happySugarDaddyDatabaseSettings.Value.UsersCollectionName);

            _profileCollection = mongoDatabase.GetCollection<Profile>(
             happySugarDaddyDatabaseSettings.Value.ProfileCollectionName);

            _paypalOptions = paypalOptions.Value;

            _authorizeNetOptions = authorizeNetOptions.Value;

            _mailService = mailService;
        }

        public async Task AddSubscriptionPlanServicesAsync(SubscriptionPlanServices model) => await _subscriptionPlanServicesCollection.InsertOneAsync(model);
        public IQueryable<SubscriptionPlanServices> GetSubscriptionPlanServices() => _subscriptionPlanServicesCollection.AsQueryable();
        public async Task UpdateSubscriptionPlanServicesAsync(string id, SubscriptionPlanServices updatePlanServices) => await _subscriptionPlanServicesCollection.ReplaceOneAsync(x => x.Id == id, updatePlanServices);


        public async Task AddSubscriptionPlansAsync(SubscriptionPlans model) => await _subscriptionPlansCollection.InsertOneAsync(model);
        public IQueryable<SubscriptionPlans> GetSubscriptionPlans() => _subscriptionPlansCollection.AsQueryable();
        public async Task UpdateSubscriptionPlansAsync(string id, SubscriptionPlans updateSubscriptionPlans) => await _subscriptionPlansCollection.ReplaceOneAsync(x => x.Id == id, updateSubscriptionPlans);


        public async Task AddSubscriptionServiceAsync(SubscriptionServices model) => await _subscriptionServicesCollection.InsertOneAsync(model);
        public IQueryable<SubscriptionServices> GetSubscriptionService() => _subscriptionServicesCollection.AsQueryable();
        public async Task UpdateSubscriptionServiceAsync(string id, SubscriptionServices updateSubscriptionService) => await _subscriptionServicesCollection.ReplaceOneAsync(x => x.Id == id, updateSubscriptionService);


        public async Task AddSubscriptionsAsync(Subscriptions model) => await _subscriptionsCollection.InsertOneAsync(model);
        public IQueryable<Subscriptions> GetSubscriptions() => _subscriptionsCollection.AsQueryable();
        public async Task UpdateSubscriptionsAsync(string id, Subscriptions updateSubscriptions) => await _subscriptionsCollection.ReplaceOneAsync(x => x.Id == id, updateSubscriptions);


        public async Task AddSubscriptionsHistoriesAsync(SubscriptionsHistories model) => await _subscriptionsHistoriesCollection.InsertOneAsync(model);
        public IQueryable<SubscriptionsHistories> GetSubscriptionsHistories() => _subscriptionsHistoriesCollection.AsQueryable();
        public async Task UpdateSubscriptionsHistoriesAsync(string id, SubscriptionsHistories updateSubscriptionsHistories) => await _subscriptionsHistoriesCollection.ReplaceOneAsync(x => x.Id == id, updateSubscriptionsHistories);


        public async Task AddSubscriptionPaymentsAsync(SubscriptionPayments model) => await _subscriptionPaymentsCollection.InsertOneAsync(model);
        public IQueryable<SubscriptionPayments> GetSubscriptionPayments() => _subscriptionPaymentsCollection.AsQueryable();
        public async Task UpdateSubscriptionPaymentsAsync(string id, SubscriptionPayments updateSubscriptionPayments) => await _subscriptionPaymentsCollection.ReplaceOneAsync(x => x.Id == id, updateSubscriptionPayments);

        public SubscriptionV2ViewModel? GetSubscription(string id)
        {
            SubscriptionV2ViewModel? subscriptionV2View = null;

            Subscriptions? subscription = GetSubscriptions().
                Where(x => x.userId == id && x.status == SubscriptionsStatus.active.ToString().ToLower()).FirstOrDefault();

            if (subscription is not null)
            {
                SubscriptionPlans subscriptionPlans = GetSubscriptionPlans().Where(x => x.Id == subscription!.subscriptionPlansId).FirstOrDefault()!;

                List<SubscriptionServices> subscriptionServices = (from sps in GetSubscriptionPlanServices()
                                                                   join ss in GetSubscriptionService()
                                                                   on sps.subscriptionServicesId equals ss.Id
                                                                   where sps.subscriptionPlansId == subscription.subscriptionPlansId
                                                                   select ss
                                                                  ).ToList();


                subscriptionV2View = new SubscriptionV2ViewModel()
                {
                    Plan = new SubscriptionPlansViewModel().ToSubscriptionPlansViewModel(subscriptionPlans),
                    Services = subscriptionServices,
                    Id = subscription!.Id,
                    source = subscription!.source,
                    paypal_subscriptionId = subscription!.paypal_subscriptionId!,
                    authorizenet_subscriptionId = subscription!.authorizenet_subscriptionId!,
                    end_date = subscription!.end_date,
                    start_date = subscription!.start_date,
                    status = subscription!.status
                };
            }

            return subscriptionV2View;
        }

        public SubscriptionV3ViewModel? ConvertToSubscriptionV3ViewModel(SubscriptionV2ViewModel subscriptionV2ViewModel)
        {
            return new SubscriptionV3ViewModel
            {
                Plan = subscriptionV2ViewModel.Plan,
                status = subscriptionV2ViewModel.status
            };
        }


        #region AuthorizeNet Subscription

        public async Task UpdateAuthorizeNetSubscription(AuthorizeNetSubscriptionRequest request, string userId, string subscriptionPlansId, string source)
        {

            var user = await _usersCollection.Find(x => x.Id == userId!).FirstOrDefaultAsync();

            var selectedPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptionPlansId);

            var currentSubscription = GetSubscriptions().FirstOrDefault(x => x.userId == userId && x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower());

            var currentSubscriptionPlan = GetSubscriptionPlans().FirstOrDefault(x => x.Id == currentSubscription!.subscriptionPlansId);


            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            var opaqueData = new opaqueDataType
            {
                dataDescriptor = request.opaqueData.dataDescriptor,
                dataValue = request.opaqueData.dataValue
            };

            nameAndAddressType addressInfo = new nameAndAddressType()
            {
                firstName = request.customerInformation.firstName,
                lastName = request.customerInformation.lastName,
                address = request.customerInformation.address,
                city = request.customerInformation.city,
                zip = request.customerInformation.zip
            };

            var paymentType = new paymentType { Item = opaqueData };


            #region CASE WHEN UPGRADING FROM FREE TO PAID

            if (currentSubscriptionPlan!.type.ToLower() == SubscriptionPlanTypes.free.ToString().ToLower() && selectedPlans!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower())
            {
                paymentScheduleTypeInterval interval = new paymentScheduleTypeInterval();

                Func<ARBSubscriptionUnitEnum> getARBSubscriptionUnitEnum = () => ARBSubscriptionUnitEnum.months;
                Func<short> getpaymentScheduleTypeIntervalLength = () => { return (selectedPlans.type.ToLower() == SubscriptionPlanTypes.month.ToString().ToLower() ? (short)1 : (short)12); };

                interval.length = getpaymentScheduleTypeIntervalLength();       // months can be indicated between 1 and 12
                interval.unit = getARBSubscriptionUnitEnum();

                paymentScheduleType schedule = new paymentScheduleType
                {
                    interval = interval,
                    startDate = DateTime.Now.AddDays(1),      // start date should be tomorrow
                    totalOccurrences = 9999,                          // 999 indicates no end date                                           
                };


                ARBSubscriptionType subscriptionType = new ARBSubscriptionType()
                {
                    amount = selectedPlans!.price,
                    paymentSchedule = schedule,
                    payment = paymentType,
                    billTo = addressInfo
                };

                var arbCreateSubscriptionRequest = new ARBCreateSubscriptionRequest
                {
                    subscription = subscriptionType
                };

                var arbCreateSubscriptionController = new ARBCreateSubscriptionController(arbCreateSubscriptionRequest);          // instantiate the controller that will call the service
                arbCreateSubscriptionController.Execute();


                ARBCreateSubscriptionResponse arbCreateSubscriptionResponse = arbCreateSubscriptionController.GetApiResponse();

                // validate response
                if (arbCreateSubscriptionResponse != null && arbCreateSubscriptionResponse.messages.resultCode == messageTypeEnum.Ok)
                {
                    if (arbCreateSubscriptionResponse != null && arbCreateSubscriptionResponse.messages.message != null)
                    {
                        Console.WriteLine("Success, Subscription ID : " + arbCreateSubscriptionResponse.subscriptionId.ToString());




                        #region Update User Subscription 

                        currentSubscription!.end_date = DateTime.Now.ToUniversalTime();
                        currentSubscription.status = SubscriptionsStatus.terminated.ToString();
                        await UpdateSubscriptionsAsync(currentSubscription.Id!, currentSubscription);


                        Subscriptions subscriptions = new Subscriptions();
                        subscriptions.start_date = DateTime.Now.ToUniversalTime();
                        subscriptions.end_date = DateTime.Now.ToUniversalTime();
                        subscriptions.status = SubscriptionsStatus.active.ToString();
                        subscriptions.created_at = DateTime.Now.ToUniversalTime();
                        subscriptions.source = source;
                        subscriptions.subscriptionPlansId = subscriptionPlansId;
                        subscriptions.authorizenet_subscriptionId = arbCreateSubscriptionResponse.subscriptionId;
                        subscriptions.userId = userId;
                        await AddSubscriptionsAsync(subscriptions);


                        #endregion

                    }
                }
                else if (arbCreateSubscriptionResponse != null)
                {
                    Console.WriteLine("Error: " + arbCreateSubscriptionResponse.messages.message[0].code + "  " + arbCreateSubscriptionResponse.messages.message[0].text);
                }
            }

            #endregion

            #region CASE WHEN UPDATING FROM ONE PAID PLAN TO ANOTHER

            else if (currentSubscriptionPlan!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower() && selectedPlans!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower())
            {

                paymentScheduleTypeInterval interval = new paymentScheduleTypeInterval();

                Func<ARBSubscriptionUnitEnum> getARBSubscriptionUnitEnum = () => ARBSubscriptionUnitEnum.months;
                Func<short> getpaymentScheduleTypeIntervalLength = () => { return (selectedPlans.type.ToLower() == SubscriptionPlanTypes.month.ToString().ToLower() ? (short)1 : (short)12); };

                interval.length = getpaymentScheduleTypeIntervalLength();       // months can be indicated between 1 and 12
                interval.unit = getARBSubscriptionUnitEnum();

                paymentScheduleType schedule = new paymentScheduleType
                {
                    interval = interval,
                    startDate = DateTime.Now.AddDays(1),      // start date should be tomorrow
                    totalOccurrences = 9999                          // 999 indicates no end date
                };


                ARBSubscriptionType subscriptionType = new ARBSubscriptionType()
                {
                    amount = selectedPlans!.price,
                    paymentSchedule = schedule,
                    payment = paymentType,
                    billTo = addressInfo
                };

                var arbUpdateSubscriptionRequest = new ARBUpdateSubscriptionRequest
                {
                    subscription = subscriptionType,
                    subscriptionId = currentSubscription!.authorizenet_subscriptionId
                };
                var arbUpdateSubscriptionController = new ARBUpdateSubscriptionController(arbUpdateSubscriptionRequest);
                arbUpdateSubscriptionController.Execute();

                ARBUpdateSubscriptionResponse arbUpdateSubscriptionResponse = arbUpdateSubscriptionController.GetApiResponse();

                if (arbUpdateSubscriptionResponse != null && arbUpdateSubscriptionResponse.messages.resultCode == messageTypeEnum.Ok)
                {
                    if (arbUpdateSubscriptionResponse != null && arbUpdateSubscriptionResponse.messages.message != null)
                    {
                        Console.WriteLine("Success, RefID Code : " + arbUpdateSubscriptionResponse.refId);

                        #region Update User Subscription 

                        currentSubscription!.end_date = DateTime.Now.ToUniversalTime();
                        currentSubscription.status = SubscriptionsStatus.terminated.ToString();
                        await UpdateSubscriptionsAsync(currentSubscription.Id!, currentSubscription);


                        Subscriptions subscriptions = new Subscriptions();
                        subscriptions.start_date = DateTime.Now.ToUniversalTime();
                        subscriptions.end_date = DateTime.Now.ToUniversalTime();
                        subscriptions.status = SubscriptionsStatus.active.ToString();
                        subscriptions.created_at = DateTime.Now.ToUniversalTime();
                        subscriptions.source = source;
                        subscriptions.subscriptionPlansId = subscriptionPlansId;
                        subscriptions.authorizenet_subscriptionId = currentSubscription!.authorizenet_subscriptionId;
                        subscriptions.userId = userId;
                        await AddSubscriptionsAsync(subscriptions);


                        #endregion
                    }
                }
                else if (arbUpdateSubscriptionResponse != null)
                {
                    Console.WriteLine("Error: " + arbUpdateSubscriptionResponse.messages.message[0].code + "  " + arbUpdateSubscriptionResponse.messages.message[0].text);
                }


            }

            #endregion



        }

        public async Task CancelAuthorizeNetSubscription(string userId)
        {
            var user = await _usersCollection.Find(x => x.Id == userId!).FirstOrDefaultAsync();

            var currentSubscription = GetSubscriptions().FirstOrDefault(x => x.userId == userId && x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower());

            var currentSubscribedPlan = GetSubscriptionPlans().FirstOrDefault(x => x.Id == currentSubscription!.subscriptionPlansId);

            if (currentSubscribedPlan!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower())
            {

                ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

                ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                {
                    name = _authorizeNetOptions.ApiLoginID,
                    ItemElementName = ItemChoiceType.transactionKey,
                    Item = _authorizeNetOptions.ApiTransactionKey,
                };

                var request = new ARBCancelSubscriptionRequest { subscriptionId = currentSubscription!.authorizenet_subscriptionId };
                var controller = new ARBCancelSubscriptionController(request);                          // instantiate the controller that will call the service
                controller.Execute();

                ARBCancelSubscriptionResponse response = controller.GetApiResponse();                   // get the response from the service (errors contained if any)

                // validate response
                if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
                {
                    if (response != null && response.messages.message != null)
                    {
                        Console.WriteLine("Success, Subscription Cancelled With RefID : " + response.refId);


                        #region Update User Subscription 

                        currentSubscription!.end_date = DateTime.Now.ToUniversalTime();
                        currentSubscription.status = SubscriptionsStatus.terminated.ToString();
                        await UpdateSubscriptionsAsync(currentSubscription.Id!, currentSubscription);

                        SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.type == "free");

                        if (subscriptionPlans is not null)
                        {
                            Subscriptions subscriptions = new Subscriptions();
                            subscriptions.start_date = DateTime.UtcNow;
                            subscriptions.status = SubscriptionsStatus.active.ToString();
                            subscriptions.created_at = DateTime.UtcNow;
                            subscriptions.subscriptionPlansId = subscriptionPlans.Id!;
                            subscriptions.userId = userId;

                            await AddSubscriptionsAsync(subscriptions);
                        }

                        #endregion
                    }
                }
                else if (response != null)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " + response.messages.message[0].text);
                }
            }
        }

        public bool ValidateAuthorizeNetWebhook(Microsoft.AspNetCore.Http.HttpRequest webhookRequest, string body)
        {
            bool result = false;

            if (webhookRequest.Headers.ContainsKey("X-Anet-Signature"))
            {
                string hash = GetSHAToken(body, _authorizeNetOptions.X_ANET_Signature);

                string X_ANET_Signature = webhookRequest.Headers["X-Anet-Signature"].ToString().Split('=').Last();

                if (hash.Equals(X_ANET_Signature, StringComparison.InvariantCultureIgnoreCase))
                {
                    result = true;
                }
            }

            return result;
        }

        private string GetSHAToken(string data, string key)
        {
            // use Encoding.ASCII.GetBytes or Encoding.UTF8.GetBytes

            byte[] _key = Encoding.ASCII.GetBytes(key);
            using (var myhmacsha1 = new HMACSHA1(_key))
            {
                var hashArray = new HMACSHA512(_key).ComputeHash(Encoding.ASCII.GetBytes(data));

                return hashArray.Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
            }

        }

        public async Task AuthorizeNetOnPaymentAuthCaptureCreated(AuthorizeNetPaymentAuthCaptureCreated authorizeNetPaymentAuthCaptureCreated)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            string transactionId = authorizeNetPaymentAuthCaptureCreated.payload["id"]!.ToString();

            var request = new getTransactionDetailsRequest();
            request.transId = transactionId;

            // instantiate the controller that will call the service
            var controller = new getTransactionDetailsController(request);
            controller.Execute();

            // get the response from the service (errors contained if any)
            var response = controller.GetApiResponse();

            if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
            {
                if (response.transaction is not null)
                {
                    Console.WriteLine("Transaction Id: {0}", response.transaction.transId);
                    Console.WriteLine("Transaction type: {0}", response.transaction.transactionType);
                    Console.WriteLine("Transaction status: {0}", response.transaction.transactionStatus);
                    Console.WriteLine("Transaction auth amount: {0}", response.transaction.authAmount);
                    Console.WriteLine("Transaction settle amount: {0}", response.transaction.settleAmount);

                    #region Update Subscription

                    if (response.transaction.subscription is not null && response.transaction.transactionStatus == "settledSuccessfully")
                    {
                        Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.authorizenet_subscriptionId == response.transaction.subscription.id.ToString());
                        if (subscriptions is not null)
                        {
                            SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                            Func<int> GetDaysForSubscription = () =>
                            {
                                return (subscriptionPlans!.type.ToLower() == SubscriptionPlanTypes.month.ToString().ToLower() ? 30 : (365));
                            };

                            subscriptions.end_date = subscriptions.end_date.GetValueOrDefault().AddDays(GetDaysForSubscription());
                            subscriptions.status = SubscriptionsStatus.active.ToString();
                            await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                            SubscriptionPayments subscriptionPayments = new SubscriptionPayments();
                            subscriptionPayments.source = subscriptions.source;
                            subscriptionPayments.subscriptionId = subscriptions.Id;
                            subscriptionPayments.amount = response.transaction.settleAmount;
                            subscriptionPayments.api_response = JsonConvert.SerializeObject(authorizeNetPaymentAuthCaptureCreated);
                            subscriptionPayments.userId = subscriptions.userId;
                            subscriptionPayments.created_at = DateTime.UtcNow;
                            subscriptionPayments.event_type = authorizeNetPaymentAuthCaptureCreated.eventType;
                            subscriptionPayments.status = response.transaction.transactionStatus;
                            await AddSubscriptionPaymentsAsync(subscriptionPayments);


                            // SEND EMAIL NOTIFICATION

                            await _mailService.SendSubscriptionPaymentSuccess(
                                      _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                                      _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                                       subscriptionPayments,
                                       subscriptions,
                                       subscriptionPlans!,
                                       response.transaction.transId
                                );


                            await _mailService.SendSubscriptionRenewalSuccess(
                                       _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                                       _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                                       subscriptionPlans!
                                );
                        }
                    }

                    #endregion
                }
            }
            else if (response != null)
            {
                Console.WriteLine("Error: " + response.messages.message[0].code + "  " +
                                  response.messages.message[0].text);
            }

        }

        public async Task AuthorizeNetOnCustomerSubscriptionCreated(AuthorizeNetCustomerSubscriptionCreated authorizeNetCustomerSubscriptionCreated)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            string subscriptionId = authorizeNetCustomerSubscriptionCreated.payload["id"]!.ToString();

            var request = new ARBGetSubscriptionRequest { subscriptionId = subscriptionId };

            var controller = new ARBGetSubscriptionController(request);          // instantiate the contoller that will call the service
            controller.Execute();

            ARBGetSubscriptionResponse response = controller.GetApiResponse();   // get the response from the service (errors contained if any)

            //validate
            if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
            {
                if (response.subscription != null)
                {
                    Console.WriteLine("Subscription returned : " + response.subscription.name);

                    #region Update Subscription

                    Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.authorizenet_subscriptionId == subscriptionId);
                    if (subscriptions is not null)
                    {
                        subscriptions.start_date = response.subscription.paymentSchedule.startDate;
                        subscriptions.end_date = response.subscription.paymentSchedule.startDate.AddMonths(response.subscription.paymentSchedule.interval.length);
                        subscriptions.status = SubscriptionsStatus.active.ToString();
                        await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                        SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                        subscriptionsHistories.api_response = JsonConvert.SerializeObject(authorizeNetCustomerSubscriptionCreated);
                        subscriptionsHistories.created_at = DateTime.UtcNow;
                        subscriptionsHistories.end_date = subscriptions.end_date;
                        subscriptionsHistories.event_type = authorizeNetCustomerSubscriptionCreated.eventType;
                        subscriptionsHistories.source = subscriptions.source;
                        subscriptionsHistories.subscriptionId = subscriptions.Id!;
                        subscriptionsHistories.start_date = subscriptions.start_date;
                        subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                        subscriptionsHistories.summary = authorizeNetCustomerSubscriptionCreated.eventType;
                        subscriptionsHistories.userId = subscriptions.userId;
                        await AddSubscriptionsHistoriesAsync(subscriptionsHistories);

                        //SEND EMAIL NOTIFICATION

                        SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                        List<SubscriptionServices> subscriptionServices = (from sps in GetSubscriptionPlanServices()
                                                                           join ss in GetSubscriptionService()
                                                                           on sps.subscriptionServicesId equals ss.Id
                                                                           where sps.subscriptionPlansId == subscriptions.subscriptionPlansId
                                                                           select ss
                                                                ).ToList();

                        await _mailService.SendSubscriptionCreatedSuccess(
                               _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                               _profileCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                               subscriptionPlans!,
                               subscriptionServices!
                               );
                    }


                    #endregion

                }
            }
            else if (response != null)
            {
                if (response.messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " +
                                      response.messages.message[0].text);
                }
            }
            else
            {
                if (controller.GetErrorResponse().messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " + response.messages.message[0].text);
                }
            }
        }

        public async Task AuthorizeNetOnCustomerSubscriptionCancelled(AuthorizeNetCustomerSubscriptionCancelled authorizeNetCustomerSubscriptionCancelled)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            string subscriptionId = authorizeNetCustomerSubscriptionCancelled.payload["id"]!.ToString();

            var request = new ARBGetSubscriptionRequest { subscriptionId = subscriptionId };

            var controller = new ARBGetSubscriptionController(request);          // instantiate the contoller that will call the service
            controller.Execute();

            ARBGetSubscriptionResponse response = controller.GetApiResponse();   // get the response from the service (errors contained if any)

            //validate
            if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
            {
                if (response.subscription != null)
                {
                    Console.WriteLine("Subscription returned : " + response.subscription.name);

                    #region Update Subscription

                    Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.authorizenet_subscriptionId == subscriptionId);
                    if (subscriptions is not null)
                    {
                        subscriptions.status = SubscriptionsStatus.canceled.ToString();
                        await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);


                        SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                        subscriptionsHistories.api_response = JsonConvert.SerializeObject(authorizeNetCustomerSubscriptionCancelled);
                        subscriptionsHistories.created_at = DateTime.UtcNow;
                        subscriptionsHistories.end_date = subscriptions.end_date;
                        subscriptionsHistories.event_type = authorizeNetCustomerSubscriptionCancelled.eventType;
                        subscriptionsHistories.source = subscriptions.source;
                        subscriptionsHistories.subscriptionId = subscriptions.Id!;
                        subscriptionsHistories.start_date = subscriptions.start_date;
                        subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                        subscriptionsHistories.summary = authorizeNetCustomerSubscriptionCancelled.eventType;
                        subscriptionsHistories.userId = subscriptions.userId;
                        await AddSubscriptionsHistoriesAsync(subscriptionsHistories);


                        await _mailService.SendSubscriptionCanceledSuccess(
                            _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                            _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                            GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId)!
                            );
                    }

                    #endregion

                }
            }
            else if (response != null)
            {
                if (response.messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " +
                                      response.messages.message[0].text);
                }
            }
            else
            {
                if (controller.GetErrorResponse().messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " + response.messages.message[0].text);
                }
            }

        }

        public async Task AuthorizeNetOnCustomerSubscriptionSuspended(AuthorizeNetCustomerSubscriptionSuspended authorizeNetCustomerSubscriptionSuspended)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            string subscriptionId = authorizeNetCustomerSubscriptionSuspended.payload["id"]!.ToString();

            var request = new ARBGetSubscriptionRequest { subscriptionId = subscriptionId };

            var controller = new ARBGetSubscriptionController(request);          // instantiate the contoller that will call the service
            controller.Execute();

            ARBGetSubscriptionResponse response = controller.GetApiResponse();   // get the response from the service (errors contained if any)

            //validate
            if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
            {
                if (response.subscription != null)
                {
                    Console.WriteLine("Subscription returned : " + response.subscription.name);

                    #region Update Subscription

                    Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.authorizenet_subscriptionId == subscriptionId);
                    if (subscriptions is not null)
                    {
                        subscriptions.status = SubscriptionsStatus.suspended.ToString();
                        await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);


                        SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                        subscriptionsHistories.api_response = JsonConvert.SerializeObject(authorizeNetCustomerSubscriptionSuspended);
                        subscriptionsHistories.created_at = DateTime.UtcNow;
                        subscriptionsHistories.end_date = subscriptions.end_date;
                        subscriptionsHistories.event_type = authorizeNetCustomerSubscriptionSuspended.eventType;
                        subscriptionsHistories.source = subscriptions.source;
                        subscriptionsHistories.subscriptionId = subscriptions.Id!;
                        subscriptionsHistories.start_date = subscriptions.start_date;
                        subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                        subscriptionsHistories.summary = authorizeNetCustomerSubscriptionSuspended.eventType;
                        subscriptionsHistories.userId = subscriptions.userId;
                        await AddSubscriptionsHistoriesAsync(subscriptionsHistories);


                        await _mailService.SendSubscriptionSuspendededSuccess(
                          _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                          _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                          GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId)!
                          );
                    }

                    #endregion
                }
            }
            else if (response != null)
            {
                if (response.messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " +
                                      response.messages.message[0].text);
                }
            }
            else
            {
                if (controller.GetErrorResponse().messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " + response.messages.message[0].text);
                }
            }
        }

        public async Task AuthorizeNetOnCustomerSubscriptionFailed(AuthorizeNetCustomerSubscriptionFailed authorizeNetCustomerSubscriptionFailed)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = (_authorizeNetOptions.Mode == "SANDBOX") ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;

            ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
            {
                name = _authorizeNetOptions.ApiLoginID,
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _authorizeNetOptions.ApiTransactionKey,
            };

            string subscriptionId = authorizeNetCustomerSubscriptionFailed.payload["id"]!.ToString();

            var request = new ARBGetSubscriptionRequest { subscriptionId = subscriptionId };

            var controller = new ARBGetSubscriptionController(request);          // instantiate the contoller that will call the service
            controller.Execute();

            ARBGetSubscriptionResponse response = controller.GetApiResponse();   // get the response from the service (errors contained if any)

            //validate
            if (response != null && response.messages.resultCode == messageTypeEnum.Ok)
            {
                if (response.subscription != null)
                {
                    Console.WriteLine("Subscription returned : " + response.subscription.name);

                    #region Update Subscription

                    Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.authorizenet_subscriptionId == subscriptionId);
                    if (subscriptions is not null)
                    {

                        SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                        subscriptionsHistories.api_response = JsonConvert.SerializeObject(authorizeNetCustomerSubscriptionFailed);
                        subscriptionsHistories.created_at = DateTime.UtcNow;
                        subscriptionsHistories.end_date = subscriptions.end_date;
                        subscriptionsHistories.event_type = authorizeNetCustomerSubscriptionFailed.eventType;
                        subscriptionsHistories.source = subscriptions.source;
                        subscriptionsHistories.subscriptionId = subscriptions.Id!;
                        subscriptionsHistories.start_date = subscriptions.start_date;
                        subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                        subscriptionsHistories.summary = authorizeNetCustomerSubscriptionFailed.eventType;
                        subscriptionsHistories.userId = subscriptions.userId;
                        await AddSubscriptionsHistoriesAsync(subscriptionsHistories);



                    }

                    #endregion
                }
            }
            else if (response != null)
            {
                if (response.messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " +
                                      response.messages.message[0].text);
                }
            }
            else
            {
                if (controller.GetErrorResponse().messages.message.Length > 0)
                {
                    Console.WriteLine("Error: " + response.messages.message[0].code + "  " + response.messages.message[0].text);
                }
            }
        }

        #endregion

        #region PayPal Subscription

        #region PAYPAL PLAN 

        public async Task<PayPal.v1.BillingPlans.Plan> PayPalSetupPlan(SubscriptionPlans plan)
        {

            PayPal.Core.AccessToken accessToken = await GetPayPalAccessToken();

            #region PayPal Catalog Product

            var products = await ListPayPalProducts(accessToken);

            PayPalProductCreateResponse product = new PayPalProductCreateResponse();

            if (products.products.Count > 0)
            {
                if (!products.products.Any(x => x.name.ToLower() == PAYPAL_PRODUCT_NAME.ToLower()))
                {
                    product = await CreatePayPalProducts(accessToken);
                }
                else
                {
                    var item = products.products.Where(x => x.name.ToLower() == PAYPAL_PRODUCT_NAME.ToLower()).FirstOrDefault();

                    product = new PayPalProductCreateResponse()
                    {
                        id = item!.id,
                        name = item.name,
                        description = item.description
                    };
                }
            }
            else
            {
                product = await CreatePayPalProducts(accessToken);
            }

            #endregion

            PayPal.v1.BillingPlans.Plan paypalPlan = await CreatePayPalPlan(accessToken, product.id!, plan);

            return paypalPlan;
        }

        private async Task<AccessToken> GetPayPalAccessToken()
        {
            var env = new PayPal.Core.PayPalEnvironment(
                clientId: _paypalOptions.ClientId,
                 clientSecret: _paypalOptions.ClientSecret,
                 baseUrl: _paypalOptions.BaseUrl,
                 webUrl: "/v1/oauth2/token"
                );

            var paypalHttp = new PayPal.Core.PayPalHttpClient(env);

            PayPal.Core.AccessTokenRequest accessTokenRequest = new AccessTokenRequest(env);
            accessTokenRequest.Headers.Add("Accept", "application/json");
            accessTokenRequest.Headers.Add("Accept-Language", "en_US");
            accessTokenRequest.Method = HttpMethod.Post;
            accessTokenRequest.RequestUri = new Uri("https://api.sandbox.paypal.com/v1/oauth2/token");
            accessTokenRequest.Body = new Dictionary<string, string>() { { "grant_type", "client_credentials" } };

            var response = await paypalHttp.Execute<AccessTokenRequest>(accessTokenRequest);

            return response.Result<AccessToken>();

        }

        private async Task<PayPalProductListResponse> ListPayPalProducts(AccessToken accessToken)
        {
            var env = new PayPal.Core.PayPalEnvironment(
                clientId: _paypalOptions.ClientId,
                 clientSecret: _paypalOptions.ClientSecret,
                 baseUrl: _paypalOptions.BaseUrl,
                 webUrl: "/v1/oauth2/token"
                );

            var paypalHttp = new PayPal.Core.PayPalHttpClient(env);

            BraintreeHttp.HttpRequest request = new BraintreeHttp.HttpRequest("/v1/catalogs/products", HttpMethod.Get, typeof(PayPalProductListResponse));
            request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
            request.Headers.Add("Accept", $"application/json");
            request.Headers.Add("Accept-Language", $"en_US");
            request.Headers.Add("Prefer", $"return=representation");

            var listResponse = await paypalHttp.Execute(request);

            PayPalProductListResponse productListResponse = listResponse.Result<PayPalProductListResponse>();

            return productListResponse;
        }

        private async Task<PayPalProductCreateResponse> CreatePayPalProducts(AccessToken accessToken)
        {
            var env = new PayPal.Core.PayPalEnvironment(
                clientId: _paypalOptions.ClientId,
                 clientSecret: _paypalOptions.ClientSecret,
                 baseUrl: _paypalOptions.BaseUrl,
                 webUrl: "/v1/oauth2/token"
                );

            var paypalHttp = new PayPal.Core.PayPalHttpClient(env);



            BraintreeHttp.HttpRequest request = new BraintreeHttp.HttpRequest("/v1/catalogs/products", HttpMethod.Post, typeof(PayPalProductCreateResponse));
            request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
            request.Headers.Add("Accept", $"application/json");
            request.Headers.Add("Accept-Language", $"en_US");
            request.Headers.Add("Prefer", $"return=representation");

            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                name = PAYPAL_PRODUCT_NAME,
                description = $"{PAYPAL_PRODUCT_NAME} Service",
                type = "SERVICE",
                category = "SOFTWARE",
                image_url = "https://chip.chipinpool.com/assets/img/logo-2.png",
                home_url = "http://chipinpool.com/"
            }),
                                               Encoding.UTF8,
                                               "application/json");

            var createResponse = await paypalHttp.Execute(request);

            PayPalProductCreateResponse productCreateResponse = createResponse.Result<PayPalProductCreateResponse>();

            return productCreateResponse;
        }

        private async Task<PayPal.v1.BillingPlans.Plan> CreatePayPalPlan(AccessToken accessToken, string productId, SubscriptionPlans plan)
        {
            var env = new PayPal.Core.PayPalEnvironment(
                clientId: _paypalOptions.ClientId,
                 clientSecret: _paypalOptions.ClientSecret,
                 baseUrl: _paypalOptions.BaseUrl,
                 webUrl: "/v1/oauth2/token"
                );

            var paypalHttp = new PayPal.Core.PayPalHttpClient(env);

            BraintreeHttp.HttpRequest request = new BraintreeHttp.HttpRequest("/v1/billing/plans", HttpMethod.Post, typeof(PayPal.v1.BillingPlans.Plan));
            request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
            request.Headers.Add("Accept", $"application/json");
            request.Headers.Add("Accept-Language", $"en_US");
            request.Headers.Add("Prefer", $"return=representation");

            string body = "{\"product_id\":\"" + productId + "\",\"name\":\"" + plan.name + "\",\"description\":\"" + plan.description + "\",\"status\":\"ACTIVE\",\"billing_cycles\":[{\"frequency\":{\"interval_unit\":\"" + plan.type.ToUpper() + "\",\"interval_count\":1},\"tenure_type\":\"REGULAR\",\"sequence\":1,\"total_cycles\":0,\"pricing_scheme\":{\"fixed_price\":{\"value\":\"" + plan.price + "\",\"currency_code\":\"USD\"}}}],\"payment_preferences\":{\"auto_bill_outstanding\":true,\"payment_failure_threshold\":3}}";

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var createResponse = await paypalHttp.Execute(request);

            PayPal.v1.BillingPlans.Plan productCreateResponse = createResponse.Result<PayPal.v1.BillingPlans.Plan>();

            return productCreateResponse;
        }

        #endregion

        public async Task UpdatePayPalSubscription(PayPalSubscriptionRequest request, string userId, string subscriptionPlansId, string source)
        {
            var user = await _usersCollection.Find(x => x.Id == userId!).FirstOrDefaultAsync();

            var selectedPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptionPlansId);

            var currentSubscription = GetSubscriptions().FirstOrDefault(x => x.userId == userId && x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower());




            #region Update User Subscription 

            currentSubscription!.end_date = DateTime.Now.ToUniversalTime();
            currentSubscription.status = SubscriptionsStatus.terminated.ToString();
            await UpdateSubscriptionsAsync(currentSubscription.Id!, currentSubscription);


            Subscriptions subscriptions = new Subscriptions();
            subscriptions.start_date = DateTime.Now.ToUniversalTime();
            subscriptions.end_date = DateTime.Now.ToUniversalTime();
            subscriptions.status = SubscriptionsStatus.active.ToString();
            subscriptions.created_at = DateTime.Now.ToUniversalTime();
            subscriptions.source = source;
            subscriptions.subscriptionPlansId = subscriptionPlansId;
            subscriptions.paypal_subscriptionId = request!.subscriptionID;
            subscriptions.userId = userId;
            await AddSubscriptionsAsync(subscriptions);

            #endregion
        }

        public async Task CancelPayPalSubscription(string userId)
        {
            var user = await _usersCollection.Find(x => x.Id == userId!).FirstOrDefaultAsync();

            var currentSubscription = GetSubscriptions().FirstOrDefault(x => x.userId == userId && x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower());

            var currentSubscribedPlan = GetSubscriptionPlans().FirstOrDefault(x => x.Id == currentSubscription!.subscriptionPlansId);

            if (currentSubscribedPlan!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower())
            {

                AccessToken accessToken = await GetPayPalAccessToken();

                var env = new PayPal.Core.PayPalEnvironment(
                  clientId: _paypalOptions.ClientId,
                  clientSecret: _paypalOptions.ClientSecret,
                  baseUrl: _paypalOptions.BaseUrl,
                  webUrl: "/v1/oauth2/token"
                );

                var paypalHttp = new PayPal.Core.PayPalHttpClient(env);

                BraintreeHttp.HttpRequest request = new BraintreeHttp.HttpRequest(string.Format("/v1/billing/subscriptions/{0}/cancel", currentSubscription!.paypal_subscriptionId), HttpMethod.Post);
                request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
                request.Headers.Add("Accept", $"application/json");
                request.Headers.Add("Accept-Language", $"en_US");
                request.Headers.Add("Prefer", $"return=representation");

                request.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    reason = "NA"
                }),
                Encoding.UTF8,
                "application/json");

                var createResponse = await paypalHttp.Execute(request);

                if (createResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    Console.WriteLine($"Success");
                }
                else
                {
                    Console.WriteLine($"Error");
                }
            }

        }

        public async Task<bool> ValidatePayPalWebhook(Microsoft.AspNetCore.Http.HttpRequest webhookRequest)
        {
            bool result = false;

            AccessToken accessToken = await GetPayPalAccessToken();

            var env = new PayPal.Core.PayPalEnvironment(
                     clientId: _paypalOptions.ClientId,
                     clientSecret: _paypalOptions.ClientSecret,
                     baseUrl: _paypalOptions.BaseUrl,
                     webUrl: "/v1/oauth2/token"
                   );

            var paypalHttp = new PayPal.Core.PayPalHttpClient(env);

            BraintreeHttp.HttpRequest request = new BraintreeHttp.HttpRequest("/v1/notifications/verify-webhook-signature", HttpMethod.Post);
            request.Headers.Add("Authorization", $"Bearer {accessToken.Token}");
            request.Headers.Add("Accept", $"application/json");
            request.Headers.Add("Accept-Language", $"en_US");
            request.Headers.Add("Prefer", $"return=representation");

            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                cert_url = webhookRequest.Headers["paypal-cert-url"].ToString(),
                transmission_time = webhookRequest.Headers["paypal-transmission-time"].ToString(),
                transmission_id = webhookRequest.Headers["paypal-transmission-id"].ToString(),
                transmission_sig = webhookRequest.Headers["paypal-transmission-sig"].ToString(),
                auth_algo = webhookRequest.Headers["paypal-auth-algo"].ToString(),
                webhook_id = _paypalOptions.WebhookSecret,
                webhook_event = webhookRequest.Body

            }), Encoding.UTF8, "application/json");

            var createResponse = await paypalHttp.Execute(request);

            var response = createResponse.Result<PayPalWebhookVerificationResponse>();

            if (response.verification_status == "SUCCESS")
            {
                result = true;
            }

            return result;
        }

        public async Task PayPalOnSubscriptionCreate(PayPalBillingSubscriptionActivated payPalBillingSubscriptionActivated)
        {

            Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.paypal_subscriptionId == payPalBillingSubscriptionActivated.resource.id);
            if (subscriptions is not null)
            {
                SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                subscriptions.start_date = payPalBillingSubscriptionActivated.resource.create_time;
                subscriptions.end_date = payPalBillingSubscriptionActivated.resource.billing_info.next_billing_time;
                subscriptions.status = SubscriptionsStatus.active.ToString();
                await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                subscriptionsHistories.api_response = JsonConvert.SerializeObject(payPalBillingSubscriptionActivated);
                subscriptionsHistories.created_at = DateTime.UtcNow;
                subscriptionsHistories.end_date = subscriptions.end_date;
                subscriptionsHistories.event_type = payPalBillingSubscriptionActivated.event_type;
                subscriptionsHistories.source = subscriptions.source;
                subscriptionsHistories.subscriptionId = subscriptions.Id!;
                subscriptionsHistories.start_date = subscriptions.start_date;
                subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                subscriptionsHistories.summary = payPalBillingSubscriptionActivated.summary;
                subscriptionsHistories.userId = subscriptions.userId;
                await AddSubscriptionsHistoriesAsync(subscriptionsHistories);



                List<SubscriptionServices> subscriptionServices = (from sps in GetSubscriptionPlanServices()
                                                                   join ss in GetSubscriptionService()
                                                                   on sps.subscriptionServicesId equals ss.Id
                                                                   where sps.subscriptionPlansId == subscriptions.subscriptionPlansId
                                                                   select ss
                                                        ).ToList();

                await _mailService.SendSubscriptionCreatedSuccess(
                       _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                       _profileCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                       subscriptionPlans!,
                       subscriptionServices!
                       );

            }

        }

        public async Task PayPalOnSubscriptionCancel(PayPalBillingSubscriptionCancelled payPalBillingSubscriptionCancelled)
        {
            Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.paypal_subscriptionId == payPalBillingSubscriptionCancelled.resource.id);
            if (subscriptions is not null)
            {
                SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                subscriptions.status = SubscriptionsStatus.canceled.ToString();
                await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                subscriptionsHistories.api_response = JsonConvert.SerializeObject(payPalBillingSubscriptionCancelled);
                subscriptionsHistories.created_at = DateTime.UtcNow;
                subscriptionsHistories.end_date = subscriptions.end_date;
                subscriptionsHistories.event_type = payPalBillingSubscriptionCancelled.event_type;
                subscriptionsHistories.source = subscriptions.source;
                subscriptionsHistories.subscriptionId = subscriptions.Id!;
                subscriptionsHistories.start_date = subscriptions.start_date;
                subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                subscriptionsHistories.summary = payPalBillingSubscriptionCancelled.summary;
                subscriptionsHistories.userId = subscriptions.userId;
                await AddSubscriptionsHistoriesAsync(subscriptionsHistories);


                await _mailService.SendSubscriptionCanceledSuccess(
                          _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                          _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                          GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId)!
                          );

            }

        }

        public async Task PayPalOnPaymentCompleted(PayPalPaymentSaleCompleted payPalPaymentSaleCompleted)
        {
            Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.paypal_subscriptionId == payPalPaymentSaleCompleted.resource.billing_agreement_id);
            if (subscriptions is not null)
            {
                SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);


                Func<int> GetDaysForSubscription = () =>
                {
                    return (subscriptionPlans!.type.ToLower() == SubscriptionPlanTypes.month.ToString().ToLower() ? 30 : (365));
                };

                subscriptions.status = SubscriptionsStatus.active.ToString();
                subscriptions.end_date = subscriptions.end_date.GetValueOrDefault().AddDays(GetDaysForSubscription());
                await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                SubscriptionPayments subscriptionPayments = new SubscriptionPayments();
                subscriptionPayments.source = subscriptions.source;
                subscriptionPayments.subscriptionId = subscriptions.Id!;
                subscriptionPayments.amount = Convert.ToDecimal(payPalPaymentSaleCompleted.resource.amount.total);
                subscriptionPayments.api_response = JsonConvert.SerializeObject(payPalPaymentSaleCompleted);
                subscriptionPayments.userId = subscriptions.userId;
                subscriptionPayments.created_at = DateTime.UtcNow;
                subscriptionPayments.event_type = payPalPaymentSaleCompleted.event_type;
                subscriptionPayments.status = payPalPaymentSaleCompleted.resource.state;
                await AddSubscriptionPaymentsAsync(subscriptionPayments);

                await _mailService.SendSubscriptionPaymentSuccess(
                                     _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                                     _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                                      subscriptionPayments,
                                      subscriptions,
                                      subscriptionPlans!,
                                      payPalPaymentSaleCompleted.resource.id
                               );


                await _mailService.SendSubscriptionRenewalSuccess(
                           _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                           _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                           subscriptionPlans!
                    );

            }
        }

        public async Task PayPalOnPaymentFailed(PayPalSubscriptionPaymentFailed payPalSubscriptionPaymentFailed)
        {
            Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.paypal_subscriptionId == payPalSubscriptionPaymentFailed.resource.id);
            if (subscriptions is not null)
            {

                SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                subscriptionsHistories.api_response = JsonConvert.SerializeObject(payPalSubscriptionPaymentFailed);
                subscriptionsHistories.created_at = DateTime.UtcNow;
                subscriptionsHistories.end_date = subscriptions.end_date;
                subscriptionsHistories.event_type = payPalSubscriptionPaymentFailed.event_type;
                subscriptionsHistories.source = subscriptions.source;
                subscriptionsHistories.subscriptionId = subscriptions.Id!;
                subscriptionsHistories.start_date = subscriptions.start_date;
                subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                subscriptionsHistories.summary = payPalSubscriptionPaymentFailed.summary;
                subscriptionsHistories.userId = subscriptions.userId;
                await AddSubscriptionsHistoriesAsync(subscriptionsHistories);

            }
        }

        public async Task PayPalOnPaymentSuspended(PayPalBillingSubscriptionSuspended payPalBillingSubscriptionSuspended)
        {
            Subscriptions? subscriptions = GetSubscriptions().FirstOrDefault(x => x.paypal_subscriptionId == payPalBillingSubscriptionSuspended.resource.id);
            if (subscriptions is not null)
            {
                SubscriptionPlans? subscriptionPlans = GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                subscriptions.status = SubscriptionsStatus.canceled.ToString();
                await UpdateSubscriptionsAsync(subscriptions.Id!, subscriptions);

                SubscriptionsHistories subscriptionsHistories = new SubscriptionsHistories();
                subscriptionsHistories.api_response = JsonConvert.SerializeObject(payPalBillingSubscriptionSuspended);
                subscriptionsHistories.created_at = DateTime.UtcNow;
                subscriptionsHistories.end_date = subscriptions.end_date;
                subscriptionsHistories.event_type = payPalBillingSubscriptionSuspended.event_type;
                subscriptionsHistories.source = subscriptions.source;
                subscriptionsHistories.subscriptionId = subscriptions.Id!;
                subscriptionsHistories.start_date = subscriptions.start_date;
                subscriptionsHistories.subscriptionPlansId = subscriptions.subscriptionPlansId;
                subscriptionsHistories.summary = payPalBillingSubscriptionSuspended.summary;
                subscriptionsHistories.userId = subscriptions.userId;
                await AddSubscriptionsHistoriesAsync(subscriptionsHistories);


                await _mailService.SendSubscriptionSuspendededSuccess(
                       _usersCollection.AsQueryable().FirstOrDefault(x => x.Id == subscriptions.userId)!,
                       _profileCollection.AsQueryable().FirstOrDefault(x => x.UserId == subscriptions.userId)!,
                       GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId)!
                       );

            }
        }

        #endregion    



    }
}
