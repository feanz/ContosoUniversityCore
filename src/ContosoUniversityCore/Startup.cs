﻿namespace ContosoUniversityCore
{
    using System;
    using AutoMapper;
    using FluentValidation;
    using FluentValidation.AspNetCore;
    using HtmlTags;
    using Infrastructure;
    using Infrastructure.CrossCutting;
    using Infrastructure.Tags;
    using MediatR;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using StructureMap;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("C:\\logs\\contoso\\contoso-{Date}.txt")
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc(opt =>
                {
                    opt.Conventions.Add(new FeatureConvention());
                    opt.Filters.Add(typeof(DbContextTransactionFilter));
                    opt.Filters.Add(typeof(ValidatorExceptionFilter));
                    opt.ModelBinderProviders.Insert(0, new EntityModelBinderProvider());
                })
                .AddRazorOptions(options =>
                {
                    // {0} - Action Name
                    // {1} - Controller Name
                    // {2} - Area Name
                    // {3} - Feature Name
                    // Replace normal view location entirely
                    options.ViewLocationFormats.Clear();
                    options.ViewLocationFormats.Add("/Features/{3}/{1}/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Features/{3}/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Features/Shared/{0}.cshtml");
                    options.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
                })
                .AddControllersAsServices();                

            services.AddHtmlTags(new TagConventions());
            services.AddAutoMapper(typeof(Startup));

            Mapper.AssertConfigurationIsValid();
            
            return ConfigureIoC(services);
        }

        public IServiceProvider ConfigureIoC(IServiceCollection services)
        {
            var container = new Container();

            container.Configure(config =>
            {
                config.Scan(_ =>
                {
                    _.AssemblyContainingType<Startup>();
                    _.WithDefaultConventions();
                    _.ConnectImplementationsToTypesClosing(typeof(IValidator<>));
                    _.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
                    _.ConnectImplementationsToTypesClosing(typeof(IAsyncRequestHandler<,>));
                    _.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    _.ConnectImplementationsToTypesClosing(typeof(IAsyncNotificationHandler<>)); 
                });

                config.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                config.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                config.For<IMediator>().Use<Mediator>();
                config.For<SchoolContext>().Use(_ => new SchoolContext(Configuration["Data:DefaultConnection:ConnectionString"]));

                var asyncHandlerType = config.For(typeof(IAsyncRequestHandler<,>));

                asyncHandlerType.DecorateAllWith(typeof(AsyncTransactionHandlerDecorator<,>));
                asyncHandlerType.DecorateAllWith(typeof(AsyncValidationHandlerDecorator<,>));
                asyncHandlerType.DecorateAllWith(typeof(AsyncMetricsHandlerDecorator<,>));
                asyncHandlerType.DecorateAllWith(typeof(AsyncLoggingHandlerDecorator<,>));
                

                var handlerType = config.For(typeof(IRequestHandler<,>));

                asyncHandlerType.DecorateAllWith(typeof(TransactionHandlerDecorator<,>));
                handlerType.DecorateAllWith(typeof(ValidationHandlerDecorator<,>));
                handlerType.DecorateAllWith(typeof(MetricsHandlerDecorator<,>));
                handlerType.DecorateAllWith(typeof(LoggingHandlerDecorator<,>));

                //Populate the container using the service collection
                config.Populate(services);
            });

            return container.GetInstance<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.AddSerilog();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}