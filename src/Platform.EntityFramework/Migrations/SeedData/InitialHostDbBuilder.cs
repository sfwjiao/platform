using Platform.EntityFramework;
using EntityFramework.DynamicFilters;

namespace Platform.Migrations.SeedData
{
    public class InitialHostDbBuilder
    {
        private readonly PlatformDbContext _context;

        public InitialHostDbBuilder(PlatformDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            _context.DisableAllFilters();

            new DefaultEditionsCreator(_context).Create();
            new DefaultLanguagesCreator(_context).Create();
            new HostRoleAndUserCreator(_context).Create();
            new DefaultSettingsCreator(_context).Create();
        }
    }
}
