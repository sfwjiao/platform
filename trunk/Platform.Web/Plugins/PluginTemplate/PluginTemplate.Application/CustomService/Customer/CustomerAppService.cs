using Abp.Application.Services;
using Abp.Domain.Repositories;
using PluginTemplate.CustomService.Customer.Dto;

namespace PluginTemplate.CustomService.Customer
{
    public class CustomerAppService :
        DefaultActionApplicationService<
            long,
            Custom.Customer,
            CustomerDto,
            CustomerSimpleDto,
            CustomerInput,
            CustomerQueryInput
            >, ICustomerAppService
    {
        public CustomerAppService(IRepository<Custom.Customer, long> entityRepository) : base(entityRepository)
        {
        }
    }
}
