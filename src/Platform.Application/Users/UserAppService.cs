using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Domain.Repositories;
using Platform.Authorization;
using Platform.Users.Dto;
using Microsoft.AspNet.Identity;
using Abp.UI;
using Abp.Threading;

namespace Platform.Users
{
    /* THIS IS JUST A SAMPLE. */
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class UserAppService : PlatformAppServiceBase, IUserAppService
    {
        private readonly IRepository<User, long> _userRepository;
        private readonly IPermissionManager _permissionManager;
        private readonly LogInManager _loginManager;

        public UserAppService(IRepository<User, long> userRepository, IPermissionManager permissionManager,
            LogInManager loginManager)
        {
            _userRepository = userRepository;
            _permissionManager = permissionManager;
            _loginManager = loginManager;
        }

        public async Task ProhibitPermission(ProhibitPermissionInput input)
        {
            var user = await UserManager.GetUserByIdAsync(input.UserId);
            var permission = _permissionManager.GetPermission(input.PermissionName);

            await UserManager.ProhibitPermissionAsync(user, permission);
        }

        //Example for primitive method parameters.
        public async Task RemoveFromRole(long userId, string roleName)
        {
            CheckErrors(await UserManager.RemoveFromRoleAsync(userId, roleName));
        }

        public async Task<ListResultDto<UserListDto>> GetUsers()
        {
            var users = await _userRepository.GetAllListAsync();

            return new ListResultDto<UserListDto>(
                users.MapTo<List<UserListDto>>()
                );
        }

        public async Task CreateUser(CreateUserInput input)
        {
            var user = input.MapTo<User>();

            user.TenantId = AbpSession.TenantId;
            user.Password = new PasswordHasher().HashPassword(input.Password);
            user.IsEmailConfirmed = true;

            CheckErrors(await UserManager.CreateAsync(user));
        }

        public async Task UpdatePwd(UpdatePwdInput input)
        {
            //检查传入参数
            if (!AbpSession.TenantId.HasValue) throw new UserFriendlyException("您无权访问该系统！");
            if (!input.Id.HasValue) throw new UserFriendlyException("传入Id参数不正确！");
            if (string.IsNullOrEmpty(input.Password)) throw new UserFriendlyException("传入Password参数不正确！");
            if (string.IsNullOrEmpty(input.OldPassword)) throw new UserFriendlyException("传入OldPassword参数不正确！");

            //获取需要修改的对象
            var customer = await _userRepository.FirstOrDefaultAsync(x => x.Id == input.Id.Value);
            if (customer == null) throw new UserFriendlyException("当前记录不存在！");

            //修改密码
            if (!string.IsNullOrEmpty(input.Password))
            {
                var user = AsyncHelper.RunSync(() => UserManager.GetUserByIdAsync(AbpSession.UserId ?? 0));
                if (user == null) throw new UserFriendlyException("请重新登录！");

                var tenant = await TenantManager.GetByIdAsync(AbpSession.TenantId.Value);
                var loginResult = await _loginManager.LoginAsync(user.UserName, input.OldPassword, tenant?.TenancyName);
                if (loginResult.Result != AbpLoginResultType.Success)
                {
                    throw new UserFriendlyException("原密码错误！");
                }

                customer.Password = new PasswordHasher().HashPassword(input.Password);
            }

            //执行修改数据方法
            await _userRepository.UpdateAsync(customer);
        }
    }
}