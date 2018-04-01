using Abp.Application.Services;
using SingleProjectTemplate.CustomService.Customer.Dto;

namespace SingleProjectTemplate.CustomService.Customer
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
