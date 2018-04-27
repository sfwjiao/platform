using Abp.Domain.Entities;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;

namespace $projectname$.EntityFramework.Repositories
{
    public abstract class $projectname$RepositoryBase<TEntity, TPrimaryKey> : EfRepositoryBase<$projectname$DbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected $projectname$RepositoryBase(IDbContextProvider<$projectname$DbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //add common methods for all repositories
    }

    public abstract class $projectname$RepositoryBase<TEntity> : $projectname$RepositoryBase<TEntity, int>
        where TEntity : class, IEntity<int>
    {
        protected $projectname$RepositoryBase(IDbContextProvider<$projectname$DbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //do not add any method here, add to the class above (since this inherits it)
    }
}
