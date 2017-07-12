using Abp.Application.Services;
using PluginTemplate.CustomService.Customer.Dto;

namespace PluginTemplate.CustomService.Customer
{
    public interface ICustomerAppService : IApplicationService, IDefaultActionApplicationService<long,
        CustomerDto,
        CustomerSimpleDto,
        CustomerInput,
        CustomerQueryInput
        >
    {
    }
}
