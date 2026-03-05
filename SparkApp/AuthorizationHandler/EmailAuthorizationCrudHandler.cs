using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using SparkApp.Security.Policy;
using System.Security.Claims;
using SparkService.Services;
using SparkService.Models;

namespace SparkApp.AuthorizationHandler
{
    public class EmailAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, SparkService.Models.EmailMessage>
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly string serviceName = "messages";

        public EmailAuthorizationCrudHandler(SubscriptionService subscriptionService) => (_subscriptionService) = (subscriptionService);
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SparkService.Models.EmailMessage resource)
        {
            var allowedOperations = GetAllowedOperations(context.User, resource);

            if (allowedOperations.Contains(requirement))
            {
                var allowedSubscriptionService = DoesSubscriptionAllowForThisService(context.User);
                if (allowedSubscriptionService)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }

        private List<OperationAuthorizationRequirement> GetAllowedOperations(ClaimsPrincipal user, SparkService.Models.EmailMessage resource)
        {
            if (user.IsInRole("Administrator"))
            {
                return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Create };
            }
            else if (user.IsInRole("User"))
            {

                if (resource.created_by == user.Claims.FirstOrDefault(x => x.Type == "id")!.Value)
                {
                    return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Delete, Operations.Create };
                }

            }

            return new List<OperationAuthorizationRequirement>();
        }

        private bool DoesSubscriptionAllowForThisService(ClaimsPrincipal user)
        {
            bool result = false;
            try
            {
                string userId = user.Claims.FirstOrDefault(x => x.Type == "id")!.Value;

                Subscriptions? subscriptions = _subscriptionService.GetSubscriptions().FirstOrDefault(x => x.userId == userId &&
                x.status.ToLower() == SubscriptionsStatus.active.ToString().ToLower());

                if (subscriptions is not null)
                {
                    SubscriptionPlans? subscriptionPlans = _subscriptionService.GetSubscriptionPlans().FirstOrDefault(x => x.Id == subscriptions.subscriptionPlansId);

                    if (subscriptionPlans is not null)
                    {
                        List<SubscriptionServices> subscriptionServices = (from ps in _subscriptionService.GetSubscriptionPlanServices()
                                                                           join ss in _subscriptionService.GetSubscriptionService() on ps.subscriptionServicesId equals ss.Id
                                                                           where ps.subscriptionPlansId == subscriptionPlans!.Id
                                                                           select ss
                                           ).ToList();

                        if (subscriptionServices.Any(x => x.name == serviceName))
                        {
                            result = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return result;
        }
    }
}
