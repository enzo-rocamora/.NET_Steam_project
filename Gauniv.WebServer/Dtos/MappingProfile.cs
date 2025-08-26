using AutoMapper;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos.Games;
using Gauniv.WebServer.Dtos.Categories;
using Gauniv.WebServer.Dtos.Users;

namespace Gauniv.WebServer.Dtos
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Game, GameDto>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Game, GameDto>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Game, UserGameDto>();
            CreateMap<User, FriendDto>()
                   .ForMember(d => d.FullName, opt =>
                        opt.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()));
            }
        }
    }
