using Abp.Domain.Entities;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;

namespace SingleProjectTemplate.EntityFramework.Repositories
{
    public abstract class SingleProjectTemplateRepositoryBase<TEntity, TPrimaryKey> : EfRepositoryBase<SingleProjectTemplateDbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected SingleProjectTemplateRepositoryBase(IDbContextProvider<SingleProjectTemplateDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //add common methods for all repositories
    }

    public abstract class SingleProjectTemplateRepositoryBase<TEntity> : SingleProjectTemplateRepositoryBase<TEntity, int>
        where TEntity : class, IEntity<int>
    {
        protected SingleProjectTemplateRepositoryBase(IDbContextProvider<SingleProjectTemplateDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //do not add any method here, add to the class above (since this inherits it)
    }
}
