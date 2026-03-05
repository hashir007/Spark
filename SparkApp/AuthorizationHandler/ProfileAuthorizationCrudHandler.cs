using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using SparkApp.Security.Policy;
using System.Security.Claims;
using SparkService.Services;

namespace SparkApp.AuthorizationHandler
{
    public class ProfileAuthorizationCrudHandler : AuthorizationHandler<OperationAuthorizationRequirement, SparkService.Models.Profile>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, SparkService.Models.Profile resource)
        {
            var allowedOperations = GetAllowedOperations(context.User, resource);

            if (allowedOperations.Contains(requirement))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private List<OperationAuthorizationRequirement> GetAllowedOperations(ClaimsPrincipal user, SparkService.Models.Profile resource)
        {
            if (user.IsInRole("Administrator"))
            {
                return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update, Operations.Create };
            }
            else if (user.IsInRole("User"))
            {
                if (resource.UserId == user.Claims.FirstOrDefault(x => x.Type == "id")!.Value)
                {
                    return new List<OperationAuthorizationRequirement>() { Operations.Read, Operations.Update };
                }
            }

            return new List<OperationAuthorizationRequirement>();
        }
    }
}
