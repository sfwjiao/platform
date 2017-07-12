using Abp.Domain.Entities;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;

namespace PluginTemplate.EntityFramework.Repositories
{
    public abstract class PluginTemplateRepositoryBase<TEntity, TPrimaryKey> : EfRepositoryBase<PluginTemplateDbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected PluginTemplateRepositoryBase(IDbContextProvider<PluginTemplateDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //add common methods for all repositories
    }

    public abstract class PluginTemplateRepositoryBase<TEntity> : PluginTemplateRepositoryBase<TEntity, int>
        where TEntity : class, IEntity<int>
    {
        protected PluginTemplateRepositoryBase(IDbContextProvider<PluginTemplateDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //do not add any method here, add to the class above (since this inherits it)
    }
}
