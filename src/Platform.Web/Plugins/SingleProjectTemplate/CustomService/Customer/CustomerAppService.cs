using Abp.Application.Services;
using Abp.Domain.Repositories;
using SingleProjectTemplate.CustomService.Customer.Dto;

namespace SingleProjectTemplate.CustomService.Customer
{
    public class CustomerAppService :
        AsyncCrudAppService<Core.Customer, CustomerDto, long,
            CustomerQueryInput,
            CustomerInput,
            CustomerSimpleDto,
            CustomerSimpleDto,
            CustomerSimpleDto
            >, ICustomerAppService
    {
        public CustomerAppService(IRepository<Core.Customer, long> entityRepository) : base(entityRepository)
        {
        }
    }
}
