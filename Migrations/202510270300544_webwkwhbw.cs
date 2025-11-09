namespace African_Beauty_Trading.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class webwkwhbw : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "AvailableSizes", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "AvailableSizes");
        }
    }
}
