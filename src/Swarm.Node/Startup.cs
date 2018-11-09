using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swarm.Core;

namespace Swarm.Node
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddDbContext<SwarmDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetSection("Swarm").GetValue<string>("ConnectionString")));

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddSwarm(Configuration.GetSection("Swarm"), configure =>
                {
                    configure.UseSqlServer();
                    configure.UseSqlServerLogStore();
                    configure.UseSqlServerClientStore();
                    configure.UseDefaultSharding();
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSwarm();

            app.UseHttpsRedirection();
            var staticFileOptions = new StaticFileOptions();
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (Directory.Exists(path))
            {
                staticFileOptions.FileProvider =
                    new PhysicalFileProvider(path);
            }
            else
            {
                path= Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (Directory.Exists(path))
                {
                    staticFileOptions.FileProvider =
                        new PhysicalFileProvider(path);
                }
            }
            app.UseStaticFiles(staticFileOptions);

            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Dashboard}/{action=Index}/{id?}");
            });
        }
    }
}