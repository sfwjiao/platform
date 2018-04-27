using Abp.Application.Services;
using SingleProjectTemplate.CustomService.Customer.Dto;

namespace SingleProjectTemplate.CustomService.Customer
{
    public interface ICustomerAppService : IAsyncCrudAppService<CustomerDto, long,
        CustomerQueryInput,
        CustomerInput,
        CustomerSimpleDto,
        CustomerSimpleDto,
        CustomerSimpleDto
        >
    {
    }
}
