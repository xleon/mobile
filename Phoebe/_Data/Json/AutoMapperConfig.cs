using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data.Json
{
    public static class AutoMapperConfig
    {
        public static void RegisterMappings()
        {
            AutoMapper.Mapper.CreateMap<CommonJson, CommonData>()
            .ForMember (dest => dest.RemoteId, opt => opt.MapFrom (src => src.Id))
            .ForMember (dest => dest.DeletedAt, opt => opt.UseValue (null));

            AutoMapper.Mapper.CreateMap<CommonJson, CommonData>().ReverseMap();

            // Project mapping
            AutoMapper.Mapper.CreateMap<ProjectJson, ProjectData>();
            AutoMapper.Mapper.CreateMap<ProjectJson, ProjectData>().ReverseMap();

            // Client data
            AutoMapper.Mapper.CreateMap<ClientJson, ClientData>();
            AutoMapper.Mapper.CreateMap<ClientJson, ClientData>().ReverseMap();

            // Tag data
            AutoMapper.Mapper.CreateMap<TagJson, TagData>();
            AutoMapper.Mapper.CreateMap<TagJson, TagData>().ReverseMap();

            // Task data
            AutoMapper.Mapper.CreateMap<TaskJson, TaskData>();
            AutoMapper.Mapper.CreateMap<TaskJson, TaskData>().ReverseMap();

            // User data
            AutoMapper.Mapper.CreateMap<UserJson, UserData>();
            AutoMapper.Mapper.CreateMap<UserJson, UserData>().ReverseMap();

            // Workspace data
            AutoMapper.Mapper.CreateMap<WorkspaceJson, WorkspaceData>();
            AutoMapper.Mapper.CreateMap<WorkspaceJson, WorkspaceData>().ReverseMap();

            // Workspace user data
            AutoMapper.Mapper.CreateMap<WorkspaceJson, WorkspaceData>();
            AutoMapper.Mapper.CreateMap<WorkspaceJson, WorkspaceData>().ReverseMap();

            // TimeEntry data
            AutoMapper.Mapper.CreateMap<UserJson, UserData>();
            AutoMapper.Mapper.CreateMap<UserData, UserJson>()
            .ForMember (dest => dest.CreatedWith, opt => opt.UseValue (null));
        }
    }
}
