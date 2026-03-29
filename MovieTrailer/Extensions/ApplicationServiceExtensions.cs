using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using MovieTrailer.Options;
using MovieTrailer.Services;
using Serilog;

namespace MovieTrailer.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddResponseCompression(opts => opts.EnableForHttps = true);
        services.AddScoped<MovieDiscoveryService>();
        return services;
    }

    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
    {
        var redisConn = config["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConn;
                opts.InstanceName = "movietrailer:";
            });
        else
            services.AddDistributedMemoryCache();

        return services;
    }

    public static IServiceCollection AddExternalClients(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<TmdbOptions>()
            .Bind(config.GetSection(TmdbOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<YouTubeOptions>()
            .Bind(config.GetSection(YouTubeOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var tmdbBase = (config["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3").TrimEnd('/') + '/';
        services.AddHttpClient<TmdbMovieClient>(c =>
        {
            c.BaseAddress = new Uri(tmdbBase);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.Timeout = TimeSpan.FromSeconds(config.GetValue("Tmdb:TimeoutSeconds", 6));
        });

        var ytBase = (config["YouTube:BaseUrl"] ?? "https://www.googleapis.com/youtube/v3").TrimEnd('/') + '/';
        services.AddHttpClient<YouTubeTrailerClient>(c =>
        {
            c.BaseAddress = new Uri(ytBase);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.Timeout = TimeSpan.FromSeconds(config.GetValue("YouTube:TimeoutSeconds", 5));
        });

        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        services.AddCors(opts =>
        {
            opts.AddPolicy("Frontend", policy =>
            {
                if (origins.Length > 0)
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                else
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });
        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        return services;
    }

    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}
