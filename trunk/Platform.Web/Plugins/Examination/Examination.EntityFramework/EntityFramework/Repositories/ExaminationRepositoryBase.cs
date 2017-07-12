using Abp.Domain.Entities;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;

namespace Examination.EntityFramework.Repositories
{
    public abstract class ExaminationRepositoryBase<TEntity, TPrimaryKey> : EfRepositoryBase<ExaminationDbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected ExaminationRepositoryBase(IDbContextProvider<ExaminationDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //add common methods for all repositories
    }

    public abstract class ExaminationRepositoryBase<TEntity> : ExaminationRepositoryBase<TEntity, int>
        where TEntity : class, IEntity<int>
    {
        protected ExaminationRepositoryBase(IDbContextProvider<ExaminationDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        //do not add any method here, add to the class above (since this inherits it)
    }
}
