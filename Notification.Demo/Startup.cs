using Dapper.CX.SqlServer.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Demo.Services;
using Notification.Shared;
using System;

namespace Notification.Demo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            var connectionString = Configuration.GetConnectionString("Database");
            services.AddSingleton(sp => new JobTrackerRepository(connectionString));

            var storageConnection = Configuration.GetConnectionString("Storage");
            services.AddScoped(sp => new QueueManager(storageConnection));

            services.AddDapperCX(connectionString, (id) => Convert.ToInt32(id));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapPost("/JobUpdated/{id:int}", async (context) =>
                {
                    var repo = context.RequestServices.GetRequiredService<JobTrackerRepository>();
                    var id = int.Parse(context.Request.RouteValues["id"].ToString());
                    await repo.OnUpdatedAsync(id);
                });
            });
        }
    }
}
