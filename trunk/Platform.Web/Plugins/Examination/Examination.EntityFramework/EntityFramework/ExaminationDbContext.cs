using System.Data.Common;
using System.Data.Entity;
using Abp.EntityFramework;
using Examination.TrialBalloon;

namespace Examination.EntityFramework
{
    public class ExaminationDbContext : AbpDbContext
    {
        public virtual IDbSet<SubjectUnit> SubjectUnits { get; set; }

        public ExaminationDbContext()
            : base("Default")
        {

        }

        public ExaminationDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }
        
        public ExaminationDbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public ExaminationDbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
