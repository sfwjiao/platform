namespace Platform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Sharp2 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PlatSyslogs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        CreationTime = c.DateTime(nullable: false),
                        Thread = c.String(maxLength: 256),
                        Level = c.String(maxLength: 256),
                        Logger = c.String(maxLength: 256),
                        Message = c.String(maxLength: 2048),
                        Exception = c.String(maxLength: 2048),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PlatSyslogs");
        }
    }
}
