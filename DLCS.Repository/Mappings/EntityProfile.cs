using System.Linq;
using AutoMapper;
using DLCS.Core;
using DLCS.Core.Enum;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Repository.Entities;

namespace DLCS.Repository.Mappings
{
    /// <summary>
    /// AutoMapper profile for mapping Entities to models
    /// </summary>
    public class EntityProfile : Profile
    {
        public EntityProfile()
        {
            CreateMap<AssetEntity, Asset>()
                .ForMember(src => src.Number1, opt => opt.MapFrom(src => src.NumberReference1))
                .ForMember(src => src.Number2, opt => opt.MapFrom(src => src.NumberReference2))
                .ForMember(src => src.Number3, opt => opt.MapFrom(src => src.NumberReference3))
                .ForMember(src => src.String1, opt => opt.MapFrom(src => src.Reference1))
                .ForMember(src => src.String2, opt => opt.MapFrom(src => src.Reference2))
                .ForMember(src => src.String3, opt => opt.MapFrom(src => src.Reference3))
                .ForMember(src => src.Roles, opt => opt.MapFrom(src => src.Roles.SplitCsvString().ToList()))
                .ForMember(src => src.Tags, opt => opt.MapFrom(src => src.Tags.SplitCsvString().ToList()));

            CreateMap<ThumbnailPolicyEntity, ThumbnailPolicy>()
                .ForMember(src => src.Sizes, opt => opt.MapFrom(src => src.Sizes.SplitCsvString(int.Parse).ToList()));
            
            CreateMap<ImageOptimisationPolicyEntity, ImageOptimisationPolicy>()
                .ForMember(src => src.TechnicalDetails, opt => opt.MapFrom(src => src.TechnicalDetails.SplitCsvString().ToList()));

            CreateMap<CustomerOriginStrategyEntity, CustomerOriginStrategy>()
                .ForMember(src => src.Strategy,
                    opt => opt.MapFrom(src => src.Strategy.GetEnumFromString<OriginStrategy>(true)));
        }
    }
}