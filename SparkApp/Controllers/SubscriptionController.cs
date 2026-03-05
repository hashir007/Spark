using Amazon.SecurityToken.Model;
using Asp.Versioning;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;
using SparkApp.APIModel.Organizer;
using SparkApp.APIModel.Subscribe;
using SparkApp.APIModel.SubscriptionPlans;
using SparkApp.APIModel.SubscriptionServices;
using SparkApp.Security.Policy;
using SparkService;
using SparkService.Models;
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Core.DependencyInjection.Services.Interfaces;
using System.Numerics;
using System.Security.Claims;
using System.Text;


namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/subscription")]
    public class SubscriptionController : Controller
    {
        private readonly ILogger<SubscriptionController> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly UsersService _usersService;
        private readonly SubscriptionService _subscriptionService;
        private readonly PaypalOptions _paypalOptions;
        private readonly AuthorizeNetOptions _authorizeNetOptions;

        public SubscriptionController(ILogger<SubscriptionController> logger, IOptions<PaypalOptions> paypalOptions, IOptions<AuthorizeNetOptions> authorizeNetOptions, IAuthorizationService authorizationService, UsersService usersService, SubscriptionService subscriptionService)
          => (_logger, _paypalOptions, _authorizeNetOptions, _authorizationService, _usersService, _subscriptionService) = (logger, paypalOptions.Value, authorizeNetOptions.Value, authorizationService, usersService, subscriptionService);


        [MapToApiVersion(1)]
        [HttpPost("services")]
        public async Task<ActionResult<ResponseModel<SubscriptionServicesViewModel>>> CreateSubscriptionServices([FromBody] SubscriptionServicesCreateRequest model)
        {
            ResponseModel<SubscriptionServicesViewModel> responseModel = new ResponseModel<SubscriptionServicesViewModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.CreateSubscriptionServices Error = userId = {userId} not found.");
                throw new Exception("username not found");
            }

            if (_subscriptionService.GetSubscriptionService().Any(x => x.name.ToLower() == model.name!.ToLower()))
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.CreateSubscriptionServices Error = Service with same name already exists");
                throw new Exception("Service name must be unique.");
            }

            SubscriptionServices newSubscriptionServices = new SubscriptionServices();
            newSubscriptionServices.name = model.name!;

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, newSubscriptionServices, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            await _subscriptionService.AddSubscriptionServiceAsync(newSubscriptionServices);

            var services = _subscriptionService.GetSubscriptionService().Where(x => x.Id == newSubscriptionServices.Id);

            if (services is null)
            {
                return StatusCode(500, $"Something went wrong");
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new SubscriptionServicesViewModel().ToSubscriptionServicesViewModel(services.FirstOrDefault()!);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("services/{id}")]
        public async Task<ActionResult<ResponseModel<SubscriptionServicesViewModel>>> UpdateSubscriptionServices([FromRoute][BindRequired] string id, [FromBody] SubscriptionServicesUpdateRequest model)
        {
            ResponseModel<SubscriptionServicesViewModel> responseModel = new ResponseModel<SubscriptionServicesViewModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.UpdateSubscriptionServices Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            if (_subscriptionService.GetSubscriptionService().Any(x => x.name.ToLower() == model.name!.ToLower() && x.Id != id))
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.UpdateSubscriptionServices Error = Service with same name already exists");
                throw new Exception("Service name must be unique.");
            }

            SubscriptionServices? subscriptionServices = await _subscriptionService.GetSubscriptionService().FirstOrDefaultAsync(x => x.Id == id);
            if (subscriptionServices is null)
            {
                return NotFound();
            }

            subscriptionServices.name = model.name!;

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, subscriptionServices, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            await _subscriptionService.UpdateSubscriptionServiceAsync(id, subscriptionServices);

            var services = await _subscriptionService.GetSubscriptionService().FirstOrDefaultAsync(x => x.Id == id);

            if (services is null)
            {
                return StatusCode(500, $"Something went wrong");
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new SubscriptionServicesViewModel().ToSubscriptionServicesViewModel(services);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [AllowAnonymous]
        [HttpGet("services")]
        public ActionResult<ResponseModel<object>> GetSubscriptionServicesAll()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var services = _subscriptionService.GetSubscriptionService().ToList();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = services;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("plan")]
        public async Task<ActionResult<ResponseModel<SubscriptionPlansV2ViewModel>>> CreateSubscriptionPlan([FromBody] SubscriptionPlanCreateRequest model)
        {
            ResponseModel<SubscriptionPlansV2ViewModel> responseModel = new ResponseModel<SubscriptionPlansV2ViewModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.CreateSubscriptionPlanViewModel Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            if (_subscriptionService.GetSubscriptionPlans().Any(x => x.name.ToLower() == model.name!.ToLower()))
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.CreateSubscriptionPlanViewModel Error = plan with same name already exists");
                throw new Exception("Plan name must be unique.");
            }

            SubscriptionPlans subscriptionPlans = new SubscriptionPlans();
            subscriptionPlans.price = model.price;
            subscriptionPlans.name = model.name;
            subscriptionPlans.description = model.description;
            subscriptionPlans.descriptionHTML = model.descriptionHTML;
            subscriptionPlans.type = model.type;
            subscriptionPlans.status = SubscriptionPlanStatus.active.ToString();
            subscriptionPlans.created_at = DateTime.UtcNow;
            subscriptionPlans.order = model.order;
            subscriptionPlans.colour = model.colour;
            subscriptionPlans.storage = model.storage;

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, subscriptionPlans, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            #region Paypal Plan

            if (subscriptionPlans.type != "free")
            {
                var paypalPlan = await _subscriptionService.PayPalSetupPlan(subscriptionPlans);
                subscriptionPlans.paypal_plan_id = paypalPlan.Id;
            }

            #endregion

            await _subscriptionService.AddSubscriptionPlansAsync(subscriptionPlans);

            if (subscriptionPlans is null)
            {
                return StatusCode(500, $"Something went wrong");
            }

            var plan = new SubscriptionPlansV2ViewModel().ToSubscriptionPlansViewModel(subscriptionPlans);
            plan.SubscriptionServices = new List<SubscriptionServices>();

            foreach (var service in model.services)
            {
                SubscriptionServices? subscriptionService = _subscriptionService.GetSubscriptionService().Where(x => x.Id == service).FirstOrDefault();

                if (subscriptionService is null)
                {
                    return NotFound("Subscription service not found");
                }

                SubscriptionPlanServices subscriptionPlanServices = new SubscriptionPlanServices();
                subscriptionPlanServices.subscriptionPlansId = subscriptionPlans.Id!;
                subscriptionPlanServices.subscriptionServicesId = subscriptionService.Id!;

                await _subscriptionService.AddSubscriptionPlanServicesAsync(subscriptionPlanServices);

                plan.SubscriptionServices.Add(subscriptionService);
            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = plan;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("plan/{id}/disable")]
        public async Task<ActionResult<ResponseModel<SubscriptionPlansV2ViewModel>>> DisableSubscriptionPlan([FromRoute][BindRequired] string id)
        {
            ResponseModel<SubscriptionPlansV2ViewModel> responseModel = new ResponseModel<SubscriptionPlansV2ViewModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.UpdateSubscriptionServices Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            if (!_subscriptionService.GetSubscriptionPlans().Any(x => x.Id == id))
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.DisableSubscriptionPlan Error = No plan found");
                throw new Exception("No subscription plan found");
            }

            SubscriptionPlans? subscriptionPlans = await _subscriptionService.GetSubscriptionPlans().FirstOrDefaultAsync(x => x.Id == id);
            subscriptionPlans!.status = SubscriptionPlanStatus.disable.ToString();

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, subscriptionPlans, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            await _subscriptionService.UpdateSubscriptionPlansAsync(id, subscriptionPlans);

            var plan = new SubscriptionPlansV2ViewModel().ToSubscriptionPlansViewModel(subscriptionPlans);

            var subscriptionPlanServices = _subscriptionService.GetSubscriptionPlanServices().Where(x => x.subscriptionPlansId == plan.id).ToList();

            foreach (SubscriptionPlanServices item in subscriptionPlanServices)
            {
                SubscriptionServices? subscriptionService = await _subscriptionService.GetSubscriptionService().FirstOrDefaultAsync(x => x.Id == item.Id);

                plan.SubscriptionServices.Add(subscriptionService!);
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = plan;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("plan/{id}")]
        public async Task<ActionResult<ResponseModel<SubscriptionPlansV2ViewModel>>> GetSubscriptionPlan([FromRoute][BindRequired] string id)
        {
            ResponseModel<SubscriptionPlansV2ViewModel> responseModel = new ResponseModel<SubscriptionPlansV2ViewModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.UpdateSubscriptionServices Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            if (!_subscriptionService.GetSubscriptionPlans().Any(x => x.Id == id))
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.DisableSubscriptionPlan Error = No plan found");
                throw new Exception("No subscription plan found");
            }


            SubscriptionPlans? subscriptionPlans = await _subscriptionService.GetSubscriptionPlans().FirstOrDefaultAsync(x => x.Id == id);

            #region Authorize Handler

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, subscriptionPlans, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #endregion

            var plan = new SubscriptionPlansV2ViewModel().ToSubscriptionPlansViewModel(subscriptionPlans!);
            plan.SubscriptionServices = new List<SubscriptionServices>();

            var subscriptionPlanServices = _subscriptionService.GetSubscriptionPlanServices().Where(x => x.subscriptionPlansId == plan.id).ToList();

            foreach (SubscriptionPlanServices item in subscriptionPlanServices)
            {
                SubscriptionServices? subscriptionService = await _subscriptionService.GetSubscriptionService().FirstOrDefaultAsync(x => x.Id == item.Id);

                plan.SubscriptionServices.Add(subscriptionService!);
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = plan;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [AllowAnonymous]
        [HttpGet("plan")]
        public ActionResult<ResponseModel<List<SubscriptionPlansV2ViewModel>>> GetSubscriptionPlanAll()
        {
            ResponseModel<List<SubscriptionPlansV2ViewModel>> responseModel = new ResponseModel<List<SubscriptionPlansV2ViewModel>>();

            List<SubscriptionPlansV2ViewModel> subscriptionPlans = _subscriptionService.GetSubscriptionPlans().
                Select(p => new SubscriptionPlansV2ViewModel()
                {
                    id = p.Id,
                    created_at = p.created_at,
                    updated_at = p.updated_at,
                    description = p.description,
                    descriptionHTML = p.descriptionHTML,
                    name = p.name,
                    price = p.price,
                    type = p.type,
                    colour = p.colour,
                    status = p.status,
                    storage = p.storage,
                    order = p.order,
                    paypal_plan_id = p.paypal_plan_id
                }).ToList();

            foreach (var item in subscriptionPlans)
            {
                var subscriptionPlanServices = _subscriptionService.GetSubscriptionPlanServices().Where(x => x.subscriptionPlansId == item.id).ToList();
                item.SubscriptionServices = new List<SubscriptionServices>();

                foreach (var service in subscriptionPlanServices)
                {
                    SubscriptionServices? subscriptionService = _subscriptionService.GetSubscriptionService().Where(x => x.Id == service.subscriptionServicesId).FirstOrDefault();

                    item.SubscriptionServices.Add(subscriptionService!);
                }

            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = subscriptionPlans;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("subscribe")]
        public async Task<ActionResult<ResponseModel<object>>> CreateSubscribe([FromBody] SubscribeCreateRequest model)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            var selectedPlans = _subscriptionService.GetSubscriptionPlans().Where(x => x.Id == model.subscriptionPlansId).FirstOrDefault();


            // CHECK IF SELECTED PLAN IS NOT FREE 

            if (selectedPlans!.type.ToLower() != SubscriptionPlanTypes.free.ToString().ToLower())
            {
                // IF SELECTED PLAN IS NOT FREE

                if (model.source == "AUTHORIZE_NET")
                {
                    AuthorizeNetSubscriptionRequest? request = JsonConvert.DeserializeObject<AuthorizeNetSubscriptionRequest>(model.data);
                    if (request is not null)
                    {
                        if (request.messages.message.FirstOrDefault()?.code == "I_WC_01")
                        {
                            await _subscriptionService.UpdateAuthorizeNetSubscription(request, userId!, model.subscriptionPlansId, model.source);
                        }
                    }
                }
                else if (model.source == "PAYPAL")
                {
                    PayPalSubscriptionRequest? request = JsonConvert.DeserializeObject<PayPalSubscriptionRequest>(model.data);

                    if (request is not null)
                    {
                        await _subscriptionService.UpdatePayPalSubscription(request, userId!, model.subscriptionPlansId, model.source);
                    }
                }
            }

            var updatedSubscription = _subscriptionService.GetSubscription(userId!);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedSubscription;
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("cancel")]
        public async Task<ActionResult<ResponseModel<object>>> CancelSubscription()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;


            var currentSubscription = _subscriptionService.GetSubscriptions().Where(x => x.userId == userId && x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower()).FirstOrDefault();

            var currentSubscribedPlan = _subscriptionService.GetSubscriptionPlans().Where(x => x.Id == currentSubscription!.subscriptionPlansId).FirstOrDefault();

            if (currentSubscribedPlan!.type.ToLower() == SubscriptionPlanTypes.free.ToString().ToLower())
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.CancelSubscription Error = user ({userId}) subscription cancel request failed");
                throw new Exception("Default free subscription cannot be canceled.It can be upgraded only.");
            }


            if (currentSubscription!.source == "AUTHORIZE_NET")
            {
                await _subscriptionService.CancelAuthorizeNetSubscription(userId!);
            }
            else if (currentSubscription!.source == "PAYPAL")
            {
                await _subscriptionService.CancelPayPalSubscription(userId!);
            }

            var updatedSubscription = _subscriptionService.GetSubscription(userId!);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = updatedSubscription;
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [AllowAnonymous]
        [HttpPost("paypal/webhook-receiver")]
        public async Task<ActionResult<ResponseModel<object>>> PayPalWebhookReceiver()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            HttpRequest request = HttpContext.Request;

            bool webhookValidationStatus = await _subscriptionService.ValidatePayPalWebhook(request);
            if (!webhookValidationStatus)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.PayPalWebhookReceiver Error = Invalid webhook ");
                throw new Exception(" Verification for webhook failed for PAYPAL");
            }

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var data = JObject.Parse(body);
            var eventData = data["event_type"]?.ToString();

            switch (eventData)
            {
                case "BILLING.SUBSCRIPTION.ACTIVATED":
                    {
                        PayPalBillingSubscriptionActivated? payPalBillingSubscriptionActivated = JsonConvert.DeserializeObject<PayPalBillingSubscriptionActivated>(body);
                        if (payPalBillingSubscriptionActivated is not null)
                        {
                            await _subscriptionService.PayPalOnSubscriptionCreate(payPalBillingSubscriptionActivated);
                        }
                    }
                    break;
                case "BILLING.SUBSCRIPTION.CANCELLED":
                    {
                        PayPalBillingSubscriptionCancelled? payPalBillingSubscriptionCancelled = JsonConvert.DeserializeObject<PayPalBillingSubscriptionCancelled>(body);
                        if (payPalBillingSubscriptionCancelled is not null)
                        {
                            await _subscriptionService.PayPalOnSubscriptionCancel(payPalBillingSubscriptionCancelled);
                        }
                    }
                    break;
                case "PAYMENT.SALE.COMPLETED":
                    {
                        PayPalPaymentSaleCompleted? payPalPaymentSaleCompleted = JsonConvert.DeserializeObject<PayPalPaymentSaleCompleted>(body);
                        if (payPalPaymentSaleCompleted is not null)
                        {
                            await _subscriptionService.PayPalOnPaymentCompleted(payPalPaymentSaleCompleted);
                        }
                    }
                    break;
                case "BILLING.SUBSCRIPTION.PAYMENT.FAILED":
                    {
                        PayPalSubscriptionPaymentFailed? payPalSubscriptionPaymentFailed = JsonConvert.DeserializeObject<PayPalSubscriptionPaymentFailed>(body);
                        if (payPalSubscriptionPaymentFailed is not null)
                        {
                            await _subscriptionService.PayPalOnPaymentFailed(payPalSubscriptionPaymentFailed);
                        }
                    }
                    break;
                case "BILLING.SUBSCRIPTION.SUSPENDED":
                    {
                        PayPalBillingSubscriptionSuspended? payPalBillingSubscriptionSuspended = JsonConvert.DeserializeObject<PayPalBillingSubscriptionSuspended>(body);
                        if (payPalBillingSubscriptionSuspended is not null)
                        {
                            await _subscriptionService.PayPalOnPaymentSuspended(payPalBillingSubscriptionSuspended);
                        }
                    }
                    break;

            }


            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = "";
            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [AllowAnonymous]
        [HttpPost("authorizenet/webhook-receiver")]
        public async Task<ActionResult<ResponseModel<object>>> AuthorizeNetWebhookReceiver()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            HttpRequest request = HttpContext.Request;

            string body = string.Empty;

            using (var reader = new StreamReader(request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            {
                body = await reader.ReadToEndAsync();
            }

            bool webhookValidationStatus = _subscriptionService.ValidateAuthorizeNetWebhook(request, body);
            if (!webhookValidationStatus)
            {
                _logger.LogError($"SparkApp.Controllers.SubscriptionController.AuthorizeNetWebhookReceiver Error = Invalid webhook ");
                throw new Exception(" Verification for webhook failed for AuthorizeNet");
            }

            var data = JObject.Parse(body);
            var eventData = data["eventType"]?.ToString();

            switch (eventData)
            {
                case "net.authorize.payment.authcapture.created":
                    {
                        AuthorizeNetPaymentAuthCaptureCreated? authorizeNetPaymentAuthCaptureCreated = JsonConvert.DeserializeObject<AuthorizeNetPaymentAuthCaptureCreated>(body);
                        if (authorizeNetPaymentAuthCaptureCreated is not null)
                        {
                            await _subscriptionService.AuthorizeNetOnPaymentAuthCaptureCreated(authorizeNetPaymentAuthCaptureCreated);
                        }
                    }
                    break;
                case "net.authorize.customer.subscription.created":
                    {
                        AuthorizeNetCustomerSubscriptionCreated? authorizeNetCustomerSubscriptionCreated = JsonConvert.DeserializeObject<AuthorizeNetCustomerSubscriptionCreated>(body);
                        if (authorizeNetCustomerSubscriptionCreated is not null)
                        {
                            await _subscriptionService.AuthorizeNetOnCustomerSubscriptionCreated(authorizeNetCustomerSubscriptionCreated);
                        }
                    }
                    break;
                case "net.authorize.customer.subscription.cancelled":
                    {
                        AuthorizeNetCustomerSubscriptionCancelled? authorizeNetCustomerSubscriptionCancelled = JsonConvert.DeserializeObject<AuthorizeNetCustomerSubscriptionCancelled>(body);
                        if (authorizeNetCustomerSubscriptionCancelled is not null)
                        {
                            await _subscriptionService.AuthorizeNetOnCustomerSubscriptionCancelled(authorizeNetCustomerSubscriptionCancelled);
                        }
                    }
                    break;
                case "net.authorize.customer.subscription.suspended":
                    {
                        AuthorizeNetCustomerSubscriptionSuspended? authorizeNetCustomerSubscriptionSuspended = JsonConvert.DeserializeObject<AuthorizeNetCustomerSubscriptionSuspended>(body);
                        if (authorizeNetCustomerSubscriptionSuspended is not null)
                        {
                            await _subscriptionService.AuthorizeNetOnCustomerSubscriptionSuspended(authorizeNetCustomerSubscriptionSuspended);
                        }
                    }
                    break;
                case "net.authorize.customer.subscription.failed":
                    {
                        AuthorizeNetCustomerSubscriptionFailed? authorizeNetCustomerSubscriptionFailed = JsonConvert.DeserializeObject<AuthorizeNetCustomerSubscriptionFailed>(body);
                        if (authorizeNetCustomerSubscriptionFailed is not null)
                        {
                            await _subscriptionService.AuthorizeNetOnCustomerSubscriptionFailed(authorizeNetCustomerSubscriptionFailed);
                        }
                    }
                    break;
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = "";
            return Ok(responseModel);
        }

    }
}
