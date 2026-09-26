using EnterpriseKnowledgeHub.Application.Services;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Application
{
    public static class EnterpriseKnowledgeHubApplicationModule
    {
        public static IServiceCollection AddEnterpriseKnowledgeHubApplicationModule(this IServiceCollection services)
        {
            services.AddScoped<IUserInfoService, UserInfoService>();
            return services;
        }
    }
}
