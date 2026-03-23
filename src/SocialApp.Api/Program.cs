using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using SocialApp.Api.Models;
using SocialApp.Api.Options;
using SocialApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOpts.SigningKey) || jwtOpts.SigningKey.Length < 32)
    jwtOpts.SigningKey = "DEV_ONLY_DEV_ONLY_DEV_ONLY_DEV_ONLY_CHANGE_IN_PRODUCTION_32";
if (string.IsNullOrWhiteSpace(jwtOpts.Issuer))
    jwtOpts.Issuer = "SocialApp.Api";
if (string.IsNullOrWhiteSpace(jwtOpts.Audience))
    jwtOpts.Audience = "SocialApp.Spa";
builder.Services.AddSingleton(Options.Create(jwtOpts));

builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
builder.Services.AddSingleton<ISocialSessionStore, MemorySocialSessionStore>();
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<OAuthLoginCompletionHandler>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISocialProfileService, SocialProfileService>();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
});

authBuilder.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOpts.Issuer,
        ValidAudience = jwtOpts.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

authBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookie =>
{
    cookie.Cookie.Name = "SocialApp.OAuth";
    cookie.Cookie.HttpOnly = true;
    cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    cookie.ExpireTimeSpan = TimeSpan.FromMinutes(15);
});

var fbId = builder.Configuration["Authentication:Facebook:AppId"];
var fbSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(fbId) && !string.IsNullOrWhiteSpace(fbSecret))
{
    authBuilder.AddFacebook(facebook =>
    {
        facebook.AppId = fbId;
        facebook.AppSecret = fbSecret;
        facebook.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        facebook.SaveTokens = true;
        facebook.Scope.Add("email");
        facebook.Scope.Add("public_profile");
        facebook.Events = new OAuthEvents
        {
            OnTicketReceived = context =>
            {
                var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
                return handler.CompleteFromTicketReceivedAsync(context, SocialProvider.Facebook);
            },
            OnRemoteFailure = context =>
            {
                var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
                handler.RedirectWithRemoteFailure(context, context.Failure?.Message);
                return Task.CompletedTask;
            }
        };
    });
}

var twKey = builder.Configuration["Authentication:Twitter:ConsumerKey"];
var twSecret = builder.Configuration["Authentication:Twitter:ConsumerSecret"];
if (!string.IsNullOrWhiteSpace(twKey) && !string.IsNullOrWhiteSpace(twSecret))
{
    authBuilder.AddTwitter(twitter =>
    {
        twitter.ConsumerKey = twKey;
        twitter.ConsumerSecret = twSecret;
        twitter.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        twitter.SaveTokens = true;
        twitter.RetrieveUserDetails = true;
        twitter.Events.OnTicketReceived = context =>
        {
            var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
            return handler.CompleteFromTicketReceivedAsync(context, SocialProvider.Twitter);
        };
        twitter.Events.OnRemoteFailure = context =>
        {
            var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
            handler.RedirectWithRemoteFailure(context, context.Failure?.Message);
            return Task.CompletedTask;
        };
    });
}

var liId = builder.Configuration["Authentication:LinkedIn:ClientId"];
var liSecret = builder.Configuration["Authentication:LinkedIn:ClientSecret"];
if (!string.IsNullOrWhiteSpace(liId) && !string.IsNullOrWhiteSpace(liSecret))
{
    authBuilder.AddLinkedIn(linkedin =>
    {
        linkedin.ClientId = liId;
        linkedin.ClientSecret = liSecret;
        linkedin.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        linkedin.SaveTokens = true;
        // OpenID userinfo (requires "Sign In with LinkedIn using OpenID Connect" on the LinkedIn app).
        // Older package versions used /v2/me and fail with ACCESS_DENIED / me.GET.NO_VERSION.
        linkedin.UserInformationEndpoint = "https://api.linkedin.com/v2/userinfo";
        linkedin.Scope.Clear();
        linkedin.Scope.Add("openid");
        linkedin.Scope.Add("profile");
        linkedin.Scope.Add("email");
        linkedin.Events = new OAuthEvents
        {
            OnTicketReceived = context =>
            {
                var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
                return handler.CompleteFromTicketReceivedAsync(context, SocialProvider.LinkedIn);
            },
            OnRemoteFailure = context =>
            {
                var handler = context.HttpContext.RequestServices.GetRequiredService<OAuthLoginCompletionHandler>();
                handler.RedirectWithRemoteFailure(context, context.Failure?.Message);
                return Task.CompletedTask;
            }
        };
    });
}

var fe = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(fe.TrimEnd('/'))
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
