namespace SPSS.App.Common;

public sealed class AuthorizationBehavior<TMessage, TResponse>(IIdentityService identity) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IIdentityService _identity = identity;

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = message.GetType().GetCustomAttributes<AuthorizeAttribute>().ToArray();
        if (message.GetType().IsDefined(typeof(SPSS.Domain.Security.PublicRequestAttribute), false))
            return await next(message, cancellationToken);
        if (!authorizeAttributes.Any())
            throw new ForbiddenAccessException("This operation has no explicit authorization policy.");
        // Participant identities must never inherit supervisor-wide permissions by role assignment.
        if (!string.IsNullOrWhiteSpace(_identity.TenantId) &&
            authorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles)) &&
            !authorizeAttributes.Any(a => a.Roles == QueryParticipantNSI && _identity.IsInRole(QueryParticipantNSI)))
            throw new ForbiddenAccessException("This operation is restricted to SPS staff.");

        if (authorizeAttributes.Any())
        {
            // Must be authenticated user
            if (!_identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("UnAuthorized Access!");
            }

            // Role-based authorization
            var authorizeAttributesWithRoles = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            if (authorizeAttributesWithRoles.Any())
            {
                var authorized = false;
                var ls = authorizeAttributesWithRoles.Select(a => a.Roles).ToArray();

                foreach (var role in authorizeAttributesWithRoles.Select(a => a.Roles))
                {
                    if (_identity.IsInRole(role.Trim()))
                    {
                        authorized = true;
                        break;
                    }
                }
                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException("Access is denied");
                }
            }

            // claim-based authorization
            var authorizeAttributesWithClaims = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Claims));

            if (authorizeAttributesWithClaims.Any())
            {
                var authorized = true;

                foreach (var claims in authorizeAttributesWithClaims.Select(a => a.Claims.Split(',')))
                {
                    foreach (var claim in claims.Select(a => a.Split(':')))
                    {
                        var hasClaim = _identity.HasClaim(claim[0].Trim());
                        if (!hasClaim)
                        {
                            authorized = false;
                            break;
                        }
                    }
                }

                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException("Access is denied");
                }
            }

        }

        return await next(message, cancellationToken);
    }

}