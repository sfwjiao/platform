using Abp.Application.Services;
using Abp.Domain.Repositories;
using SingleProjectTemplate.CustomService.Customer.Dto;

namespace SingleProjectTemplate.CustomService.Customer
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
