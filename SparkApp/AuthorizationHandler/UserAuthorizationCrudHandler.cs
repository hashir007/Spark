using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SparkApp.Security.Policy;

namespace SparkApp.AuthorizationHandler
{
    public class UserAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, SparkService.Models.User>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SparkService.Models.User resource)
        {
            var allowedOperations = GetAllowedOperations(context.User, resource);

            if (allowedOperations.Contains(requirement))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private List<OperationAuthorizationRequirement> GetAllowedOperations(ClaimsPrincipal user, SparkService.Models.User resource)
        {
            if (user.IsInRole("Administrator"))
            {
                return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Create };
            }
            else if (user.IsInRole("User"))
            {
                if (resource.Id == user.Claims.FirstOrDefault(x => x.Type == "id")!.Value)
                {
                    return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update };
                }
                else
                {
                    return new List<OperationAuthorizationRequirement>() { Operations.Read };
                }

            }

            return new List<OperationAuthorizationRequirement>();
        }
    }
}
