namespace African_Beauty_Trading.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class jeqkbuuqb : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "Priority", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "Priority", c => c.String());
        }
    }
}
