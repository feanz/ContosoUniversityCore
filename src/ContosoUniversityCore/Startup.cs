namespace ContosoUniversityCore
{
    using System;
    using System.Linq;
    using System.Reflection;
    using AutoMapper;
    using FluentValidation.AspNetCore;
    using HtmlTags;
    using Infrastructure;
    using Infrastructure.Tags;
    using MediatR;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
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
                    opt.Filters.Add(typeof(ValidatorActionFilter));
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
                .AddControllersAsServices()
                .AddFluentValidation(cfg => { cfg.RegisterValidatorsFromAssemblyContaining<Startup>(); });

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
                    _.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
                    _.ConnectImplementationsToTypesClosing(typeof(IAsyncRequestHandler<,>));
                    _.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    _.ConnectImplementationsToTypesClosing(typeof(IAsyncNotificationHandler<>)); 
                });

                config.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                config.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                config.For<IMediator>().Use<Mediator>();
                config.For<SchoolContext>().Use(_ => new SchoolContext(Configuration["Data:DefaultConnection:ConnectionString"]));

                var handlerType = config.For(typeof(IAsyncRequestHandler<,>));

                handlerType.DecorateAllWith(typeof(MediatorPipeline<,>));

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