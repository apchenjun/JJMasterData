using System.Globalization;
using System.Text.RegularExpressions;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Extensions;
using JJMasterData.Core.DataDictionary.Services;
using JJMasterData.Core.DataDictionary.Services.Abstractions;
using JJMasterData.Web.Authorization;
using JJMasterData.Web.Hosting;
using JJMasterData.Web.Models;
using JJMasterData.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ResponseEndFilter = JJMasterData.Web.Filters.ResponseEndFilter;

namespace JJMasterData.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static JJServiceBuilder AddJJMasterDataWeb(this IServiceCollection services)
    {
        services.ConfigureOptions(typeof(JJMasterDataConfigureOptions));

        services.AddHttpContextAccessor();
        services.AddSession();

        AddSystemWebAdapters(services);

        services.AddDistributedMemoryCache();
        services.AddJJMasterDataServices();
        services.AddUrlRequestCultureProvider();
        services.AddAnonymousAuthorization();
        return services.AddJJMasterData();
    }

    private static void AddSystemWebAdapters(IServiceCollection services)
    {
        services.AddSystemWebAdapters();
        services.AddTransient<ResponseEndFilter>();
        services.AddOptions<MvcOptions>()
            .Configure(options =>
            {
                // We want the check for HttpResponse.End() to be done as soon as possible after the action is run.
                // This will minimize any chance that output will be written which will fail since the response has completed.
                options.Filters.Add<ResponseEndFilter>(int.MaxValue);
            });
    }

    private static void AddAnonymousAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("MasterData", policy => policy.AddRequirements(new AllowAnonymousAuthorizationRequirement()));
            options.AddPolicy("DataDictionary", policy => policy.AddRequirements(new AllowAnonymousAuthorizationRequirement()));
            options.AddPolicy("Log", policy => policy.AddRequirements(new AllowAnonymousAuthorizationRequirement()));
        });
    }

    public static void AddUrlRequestCultureProvider(this IServiceCollection services)
    {
        CultureInfo[] supportedCultures = new[]
        {
            new CultureInfo("pt-BR"),
            new CultureInfo("en-US")
        };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(supportedCultures.First());
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
            {
                var currentCulture = "en-US";

                var segments = context.Request.Path.Value?.Split(new[] { '/' },
                    StringSplitOptions.RemoveEmptyEntries);

                var culturePattern = new Regex(@"^[a-z]{2}(-[a-z]{2,4})?$",
                    RegexOptions.IgnoreCase);

                if (segments?.Length > 0 && culturePattern.IsMatch(segments![0]))
                {
                    currentCulture = segments[0];
                }

                var requestCulture = new ProviderCultureResult(currentCulture);

                return await Task.FromResult(requestCulture);
            }));
        });
    }

    private static void AddJJMasterDataServices(this IServiceCollection services)
    {
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddTransient<IValidationDictionary, ModelStateWrapper>();

        services.AddTransient<ActionsService>();
        services.AddTransient<ApiService>();
        services.AddTransient<ElementService>();
        services.AddTransient<EntityService>();
        services.AddTransient<FieldService>();
        services.AddTransient<IndexesService>();
        services.AddTransient<OptionsService>();
        services.AddTransient<PanelService>();
        services.AddTransient<RelationsService>();
        services.AddTransient<ResourcesService>();
        services.AddTransient<RazorPartialRendererService>();
        services.AddTransient<ThemeService>();
    }
}