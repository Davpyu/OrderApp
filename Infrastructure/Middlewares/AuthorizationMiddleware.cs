using Microsoft.AspNetCore.Authorization;
using DotNetService.Domain.Auth.Util;
using DotNetService.Infrastructure.Attributes;
using DotNetService.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication;
using DotNetService.Domain.Auth.Token;
using DotNetService.Infrastructure.Databases;

namespace DotNetService.Infrastructure.Middlewares
{
    public class AuthorizationMiddleware(
        RequestDelegate next,
        LocalStorageDatabase localStorage,
        IConfiguration config,
        AuthUtil authUtil
        )
    {
        private readonly RequestDelegate _next = next;
        private readonly IConfiguration _config = config;
        private readonly LocalStorageDatabase _localStorage = localStorage;
        private readonly AuthUtil _authUtil = authUtil;

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is object || context.Request.Path.ToString().Contains("swagger"))
            {
                await _next(context);
                return;
            }

            var token = string.Empty;
            var headers = context.Request.Headers;
            if (headers.ContainsKey("Authorization") && headers.Authorization.ToString().StartsWith("Bearer "))
            {
                token = headers.Authorization.ToString().Replace("Bearer ", string.Empty);
            }

            AuthUtil.ValidateJwtToken(_config["JWTSetting:Secret"], token);

            var user = AuthUtil.GetUserLogged(token);

            context.User = AuthUtil.ClaimPrincipalWithJson(user);
            var userId = context.User.Claims.FirstOrDefault(claim => claim.Type == "Id")?.Value;

            var localStorageKey = _authUtil.GenerateKeyLocalStorage(userId);

            var userAuthInfo = await _localStorage.Get<UserAuthInfo>(localStorageKey);

            if (userAuthInfo == null)
            {
                context.Response.StatusCode = 401;
                await context.SignOutAsync();
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            if (endpoint?.Metadata?.GetMetadata<PermissionsAttribute>() is PermissionsAttribute permissionAttr)
            {
                // Extract required permissions from the attribute
                var requiredPermissions = permissionAttr.Permissions;
                var userPermissions = userAuthInfo.Permissions;

                // Validate that the user has the required permissions
                PermissionUtil.ValidatePermission([.. userPermissions], requiredPermissions);
            }

            await _next(context);
        }
    }
}

