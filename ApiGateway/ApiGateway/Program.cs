using ApiGateway.LoadBalancerPolicy;
using ApiGateway.RateLimiter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("YARP"));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]))
        };
    });
//builder.Services.AddRateLimiter(options =>
//{
//    options.AddPolicy("fixed_windowRLPolicy", httpContext =>
//    {
//        // Use the custom policy
//        var policy = new FixedWindowRL();
//        return policy.GetPartition(httpContext);
//    });
//});
// 👇 Authorization policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});
// Adding LoadBalancer policy
builder.Services.AddSingleton<ILoadBalancingPolicy, RoundRobinPolicy>();
builder.Services.AddSingleton<ILoadBalancingPolicy, IpHashPolicy>();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

// code to Add condition to MiddleWare based on Tag...
app.MapWhen(context =>
context.GetEndpoint()?.
Metadata?.GetMetadata<RouteModel>()?.Config?.Metadata?.TryGetValue("FixedWindowRL", out var value) == true &&
        bool.TryParse(value, out var result) && result,
        branch =>
        {
            branch.UseMiddleware<FixedWindowRL>();
        }
);
app.Run();

