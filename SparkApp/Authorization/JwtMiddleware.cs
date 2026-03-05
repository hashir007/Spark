namespace SparkApp.Authorization;

using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebASparkApppi.Authorization;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, UsersService userService, JwtUtils jwtUtils)
    {
        string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var userId = jwtUtils.ValidateJwtToken(token!);
                if (userId != null)
                {
                    // attach user to context on successful jwt validation
                    context.Items["User"] = await userService.GetAsync(userId);
                }
            }
            catch (Exception)
            {
                // If the token is invalid, throw an exception
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = 401; //UnAuthorized
                await context.Response.WriteAsync("Invalid token");
            }
        }

        await _next(context);
    }
}