﻿using DAL.EFCore;
using Domain.OData;

namespace AutoMapper.OData.EFCore.Tests.Mappings
{
    public class OpsTenantMappings : Profile
    {
        public OpsTenantMappings()
        {
            CreateMap<TMandator, OpsTenant>()
                .ForAllMembers(o => o.ExplicitExpansion());
        }
    }
}
