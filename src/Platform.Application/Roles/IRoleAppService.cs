using System.Threading.Tasks;
using Abp.Application.Services;
using Platform.Roles.Dto;

namespace Platform.Roles
{
    public interface IRoleAppService : IApplicationService
    {
        Task UpdateRolePermissions(UpdateRolePermissionsInput input);
    }
}
