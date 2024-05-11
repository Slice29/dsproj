using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthAPI.Models;
using AutoMapper;
namespace AuthAPI.Profile
{
    public class UserProfiles : AutoMapper.Profile
    {
          public UserProfiles()
        {
            CreateMap<UserRegister,User>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));
        }
    }
}