using BBP.CORE.API.Service;
using BBP.CORE.API.Utilities;
using BBPAPI;
using BMSCommon;
using BMSCommon.Model;
using Microsoft.OpenApi.Models;
using System.Reflection;
using static BMSCommon.Common;

namespace BiblePay.BMS
{
	public class StartupConfig
    {

        public static string WebRootPath = String.Empty;
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder.WithOrigins(new string[] {
                    "https://social.biblepay.org",
                    "https://localhost:8443", "https://*", "http://localhost", "https://localhost" })
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
            */
            WebRootPath = Path.GetFullPath("wwwroot");
            //
            //_env.WebRootPath here.

			services.AddControllers();
			services.AddResponseCaching();
			services.AddMvcCore().AddApiExplorer();

			services.AddSwaggerGen(options =>
			{

				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1.94a",
					Title = "BBP.CORE.API",
					Description = "Back End CryptoCurrency Services",

				});
			});

            // Get bms config
            string sMyConfValueForTemple = BMSCommon.Common.GetConfigKeyValue("abc");
		}


        public static IHostBuilder CreateHostBuilder(string[] args, string sBindURL, IEnumerable<ServiceDescriptor> services) =>
            Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((hostingContext, config) =>
			{
				//IWebHostEnvironment? env = (IWebHostEnvironment?)hostingContext.HostingEnvironment;

				config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true); // obbponal extra provider

				config.AddEnvironmentVariables(); // overwrites previous values
                
				if (args != null)
				{
					config.AddCommandLine(args);
				}
			})
				.ConfigureWebHostDefaults(webBuilder =>
                {
                    Log("*** USING BIND URL " + sBindURL);

                    if (!IsWindows())
                    {
                        webBuilder.UseKestrel(serverOptions =>
                        {
                            // Video sizes
                            serverOptions.Limits.MaxRequestHeadersTotalSize = 19500000;
                            serverOptions.Limits.MaxRequestBufferSize = 19500000;
                            serverOptions.Limits.RequestHeadersTimeout = new TimeSpan(35000);
                            serverOptions.Limits.MaxRequestBodySize = 70001001;
                        })
                       .ConfigureServices(collection =>
                       {
                           if (services == null)
                           {
                               return;
                           }
                       })
                       
                    .UseIISIntegration()
                    .UseUrls(sBindURL)
                    .UseStartup<BiblePay.BMS.StartupConfig>();
                    }
                    else
                    {
                        webBuilder.UseIIS().UseIISIntegration().UseUrls(sBindURL)
                        .UseStartup<BiblePay.BMS.StartupConfig>();
					}
                    // *****  BACKGROUND THREADS *****
                    bool f1 = false;
                    System.Threading.Thread t = new Thread(QuantBilling.Looper);
                    
                    t.Start();



                    System.Threading.Thread tSyncer = new Thread(QuorumSyncer.Looper);
                    tSyncer.Start();



                });
        public IConfigurationRoot Configuration { get; }

        [Obsolete]
        public StartupConfig(Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            Global.msContentRootPath = env.ContentRootPath;

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        object GetFirstCP(dynamic oCollection)
        {
            foreach (object o in oCollection)
            {
                return o;
            }
            return null;
        }
        public static int GetPort(string URL)
        {
            string[] pieces = URL.Split(':');
            if (pieces.Length > 1)
            {
                return (int)GetDouble(pieces[2]);
            }
            return 0;
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
				app.UseDeveloperExceptionPage();
			}
			else
            {
            }

			app.UseSwagger();
            app.UseStaticFiles();

    	app.UseSwaggerUI(c =>
		{
			string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
			c.SwaggerEndpoint($"/swagger/v1/swagger.json", "Web API 1.27");
    	});

			app.UseRouting();
            //app.UseSession();
            app.UseResponseCaching();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
        }
    }
}
