using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogAPI.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CatalogAPI.CustomFormatters;

namespace CatalogAPI
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
            services.AddMvc(options=> 
            {
                options.OutputFormatters.Add(new CsvOutputFormatter());
            })
                .AddXmlDataContractSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddCors(c =>
            {
                //c.AddDefaultPolicy(x => x.AllowAnyOrigin()
                //.AllowAnyMethod()
                //.AllowAnyHeader());

                //these below are example of named policy
                c.AddPolicy("AllowPartners", x =>
                {
                     x.WithOrigins("http://microsoft.com", "http://ivp.in")
                     .WithMethods("GET", "POST")
                     .AllowAnyHeader();
                });

                c.AddPolicy("AllowAll", y =>
                 {
                     y.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                 });
            });
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Catalog API",
                    Description = "Catalog management API methods",
                    Version = "1.0",
                    Contact = new Contact
                    {
                        Name = "Rohit Omar",
                        Email = "Ror@mail.com",
                        Url = "https://github.com/notMadeYet"
                    }
                });
            });

            services.AddAuthentication(c=>
            {
                c.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                c.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(c =>
                {
                    c.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration.GetValue<string>("Jwt:issuer"),
                        ValidAudience = Configuration.GetValue<string>("Jwt:audience"),
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes
                        (Configuration.GetValue<string>("Jwt:secret")))
                    };
                });

            services.AddScoped<CatalogContract>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUI(config =>
                {
                    config.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API");
                    config.RoutePrefix="";
                });
            //}
            //else
            //{
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            //}

            app.UseHttpsRedirection();


            app.UseCors();
            app.UseSwagger();

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
