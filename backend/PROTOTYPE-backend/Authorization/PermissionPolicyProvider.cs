using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PROTOTYPE_backend.Authorization
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Ha van előre regisztrált beépített policy, azt adja vissza
            var policy = await base.GetPolicyAsync(policyName);
            if (policy != null)
            {
                return policy;
            }

            // Minden más esetben dinamikusan létrehozza a PermissionRequirement-tel
            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
        }
    }
}