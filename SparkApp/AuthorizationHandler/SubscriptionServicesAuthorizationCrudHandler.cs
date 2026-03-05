using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using SparkApp.Security.Policy;
using System.Security.Claims;

namespace SparkApp.AuthorizationHandler
{
    public class SubscriptionServicesAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, SparkService.Models.SubscriptionServices>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SparkService.Models.SubscriptionServices resource)
        {
            var allowedOperations = GetAllowedOperations(context.User, resource);

            if (allowedOperations.Contains(requirement))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private List<OperationAuthorizationRequirement> GetAllowedOperations(ClaimsPrincipal user, SparkService.Models.SubscriptionServices resource)
        {
            if (user.IsInRole("Administrator"))
            {
                return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Create, Operations.Delete };
            }
            return new List<OperationAuthorizationRequirement>();
        }
    }
}
