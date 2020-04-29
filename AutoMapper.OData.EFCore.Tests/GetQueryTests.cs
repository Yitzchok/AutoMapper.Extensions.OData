﻿using AutoMapper.AspNet.OData;
using DAL.EFCore;
using Domain.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AutoMapper.OData.EFCore.Tests
{
    public class GetQueryTests
    {
        public GetQueryTests()
        {
            Initialize();
        }

        #region Fields
        private IServiceProvider serviceProvider;
        #endregion Fields

        private void Initialize()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddOData();
            services.AddDbContext<MyDbContext>
                (
                    options =>
                    {
                        options.UseInMemoryDatabase("MyDbContext");
                        options.UseInternalServiceProvider(new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider());
                    },
                    ServiceLifetime.Transient
                )
                .AddSingleton<AutoMapper.IConfigurationProvider>(new MapperConfiguration(cfg => cfg.AddMaps(typeof(GetTests).Assembly)))
                .AddTransient<IMapper>(sp => new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService))
                .AddTransient<IApplicationBuilder>(sp => new Microsoft.AspNetCore.Builder.Internal.ApplicationBuilder(sp))
                .AddTransient<IRouteBuilder>(sp => new RouteBuilder(sp.GetRequiredService<IApplicationBuilder>()));

            serviceProvider = services.BuildServiceProvider();

            MyDbContext context = serviceProvider.GetRequiredService<MyDbContext>();
            context.Database.EnsureCreated();
            Seed_Database(context);
        }

        [Fact]
        public void IsConfigurationValid()
        {
            serviceProvider.GetRequiredService<IConfigurationProvider>().AssertConfigurationIsValid();
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_filter_eq_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$filter=Name eq 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_filter_ne_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$filter=Name ne 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_filter_eq_no_expand()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$filter=Name eq 'One'"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings == null);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_no_filter_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_no_expand_no_filter_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Buildings == null);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_no_expand_filter_eq_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$filter=Name eq 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings == null);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]//Similar to test below but works if $select=Buildings is added to the query
        public async void OpsTenant_expand_Buildings_SelectNameAndBuilder_expand_Builder_expand_City_filter_ne_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$select=Buildings,Name&$expand=Buildings($select=Name,Builder;$expand=Builder($select=Name,City;$expand=City))&$filter=Name ne 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Buildings.First().Builder != null);
                Assert.True(collection.First().Buildings.First().Builder.City != null);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact(Skip = "ProjectTo does not load expanded child collections. #3379")]
        public async void OpsTenant_expand_Buildings_expand_Builder_expand_City_filter_ne_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings($expand=Builder($expand=City))&$filter=Name ne 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Buildings.First().Builder != null);
                Assert.True(collection.First().Buildings.First().Builder.City != null);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_filter_eq_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder,Tenant&$filter=name eq 'One L1'"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.Name == "Sam");
                Assert.True(collection.First().Tenant.Name == "One");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_select_Name_expand_Tenant_filter_eq_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($select=Name),Tenant&$filter=name eq 'One L1'"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.Name == "Sam");
                Assert.True(collection.First().Tenant.Name == "One");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_filter_on_nested_property_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder,Tenant&$filter=Builder/Name eq 'Sam'&$orderby=Name asc"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Builder.Name == "Sam");
                Assert.True(collection.First().Tenant.Name == "One");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_filter_on_property_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$filter=Name ne 'One L2'&$orderby=Name desc"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 3);
                Assert.True(collection.First().Builder.City != null);
                Assert.True(collection.First().Name != "One L2");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_filter_on_nested_nested_property_with_count()
        {
            string query = "/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$filter=Builder/City/Name eq 'Leeds'&$count=true";
            ODataQueryOptions<CoreBuilding> options = ODataHelpers.GetODataQueryOptions<CoreBuilding>
            (
                query,
                serviceProvider,
                serviceProvider.GetRequiredService<IRouteBuilder>()
            );
            Test
            (
                await Get<CoreBuilding, TBuilding>
                (
                    query,
                    options
                )
            );

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.Equal(1, options.Request.ODataFeature().TotalCount);
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.City.Name == "Leeds");
                Assert.True(collection.First().Name == "Two L2");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_order_by_name()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$orderby=Name desc"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 4);
                Assert.True(collection.First().Builder.City.Name == "Leeds");
                Assert.True(collection.First().Name == "Two L2");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_order_by_name_then_by_identity()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$orderby=Name desc,Identity"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 4);
                Assert.True(collection.First().Builder.City.Name == "Leeds");
                Assert.True(collection.First().Name == "Two L2");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_order_by_builderName()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$orderby=Builder/Name"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 4);
                Assert.True(collection.First().Builder.City.Name == "London");
                Assert.True(collection.First().Name == "Two L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_order_by_builderName_skip_3_take_1_with_count()
        {
            string query = "/corebuilding?$skip=3&$top=1&$expand=Builder($expand=City),Tenant&$orderby=Name desc,Identity&$count=true";
            ODataQueryOptions<CoreBuilding> options = ODataHelpers.GetODataQueryOptions<CoreBuilding>
            (
                query,
                serviceProvider,
                serviceProvider.GetRequiredService<IRouteBuilder>()
            );
            Test
            (
                await Get<CoreBuilding, TBuilding>
                (
                    query,
                    options
                )
            );

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.Equal(4, options.Request.ODataFeature().TotalCount);
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.City.Name == "London");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_order_by_builderName_skip_3_take_1_no_count()
        {
            string query = "/corebuilding?$skip=3&$top=1&$expand=Builder($expand=City),Tenant&$orderby=Name desc,Identity";
            ODataQueryOptions<CoreBuilding> options = ODataHelpers.GetODataQueryOptions<CoreBuilding>
            (
                query,
                serviceProvider,
                serviceProvider.GetRequiredService<IRouteBuilder>()
            );

            Test
            (
                await Get<CoreBuilding, TBuilding>
                (
                    query,
                    options
                )
            );

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.Null(options.Request.ODataFeature().TotalCount);
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.City.Name == "London");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        private async Task<ICollection<TModel>> Get<TModel, TData>(string query, ODataQueryOptions<TModel> options = null) where TModel : class where TData : class
        {
            return 
            (
                await DoGet
                (
                    serviceProvider.GetRequiredService<IMapper>(),
                    serviceProvider.GetRequiredService<MyDbContext>()
                )
            ).ToList();

            async Task<IQueryable<TModel>> DoGet(IMapper mapper, MyDbContext context)
            {
                return await context.Set<TData>().GetQueryAsync
                (
                    mapper,
                    options ?? ODataHelpers.GetODataQueryOptions<TModel>
                    (
                        query,
                        serviceProvider,
                        serviceProvider.GetRequiredService<IRouteBuilder>()
                    ),
                    HandleNullPropagationOption.False
                );
            }
        }

        static void Seed_Database(MyDbContext context)
        {
            context.City.Add(new TCity { Name = "London" });
            context.City.Add(new TCity { Name = "Leeds" });
            context.SaveChanges();

            List<TCity> cities = context.City.ToList();
            context.Builder.Add(new TBuilder { Name = "Sam", CityId = cities.First(b => b.Name == "London").Id });
            context.Builder.Add(new TBuilder { Name = "John", CityId = cities.First(b => b.Name == "London").Id });
            context.Builder.Add(new TBuilder { Name = "Mark", CityId = cities.First(b => b.Name == "Leeds").Id });
            context.SaveChanges();

            List<TBuilder> builders = context.Builder.ToList();
            context.MandatorSet.Add(new TMandator
            {
                Identity = Guid.NewGuid(),
                Name = "One",
                Buildings = new List<TBuilding>
                {
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "One L1", BuilderId = builders.First(b => b.Name == "Sam").Id },
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "One L2", BuilderId = builders.First(b => b.Name == "Sam").Id  }
                }
            });
            context.MandatorSet.Add(new TMandator
            {
                Identity = Guid.NewGuid(),
                Name = "Two",
                Buildings = new List<TBuilding>
                {
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "Two L1", BuilderId = builders.First(b => b.Name == "John").Id  },
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "Two L2", BuilderId = builders.First(b => b.Name == "Mark").Id  }
                }
            });
            context.SaveChanges();
        }
    }
}