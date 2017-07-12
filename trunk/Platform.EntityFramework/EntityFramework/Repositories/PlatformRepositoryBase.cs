using Abp.Domain.Entities;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;

namespace Platform.EntityFramework.Repositories
{
    public abstract class PlatformRepositoryBase<TEntity, TPrimaryKey> : EfRepositoryBase<PlatformDbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected PlatformRepositoryBase(IDbContextProvider<PlatformDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //add common methods for all repositories
    }

    public abstract class PlatformRepositoryBase<TEntity> : PlatformRepositoryBase<TEntity, int>
        where TEntity : class, IEntity<int>
    {
        protected PlatformRepositoryBase(IDbContextProvider<PlatformDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //do not add any method here, add to the class above (since this inherits it)
    }
}
