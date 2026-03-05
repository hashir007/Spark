using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using SparkApp.Security.Policy;
using System.Security.Claims;

namespace SparkApp.AuthorizationHandler
{
    public class SubscriptionsAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, SparkService.Models.Subscriptions>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SparkService.Models.Subscriptions resource)
        {
            var allowedOperations = GetAllowedOperations(context.User, resource);

            if (allowedOperations.Contains(requirement))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private List<OperationAuthorizationRequirement> GetAllowedOperations(ClaimsPrincipal user, SparkService.Models.Subscriptions resource)
        {
            if (user.IsInRole("Administrator"))
            {
                return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Create };
            }
            else if (user.IsInRole("User"))
            {

                if (resource.userId == user.Claims.FirstOrDefault(x => x.Type == "id")!.Value)
                {
                    return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Delete };
                }

            }

            return new List<OperationAuthorizationRequirement>();
        }
    }
}
