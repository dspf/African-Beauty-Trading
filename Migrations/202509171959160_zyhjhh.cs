namespace African_Beauty_Trading.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class zyhjhh : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AdminNotifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AdminId = c.String(maxLength: 128),
                        Title = c.String(),
                        Message = c.String(),
                        Type = c.String(),
                        RelatedEntity = c.String(),
                        RelatedEntityId = c.Int(),
                        IsRead = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ReadDate = c.DateTime(precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.AdminId)
                .Index(t => t.AdminId);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        DateCreated = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        IsActive = c.Boolean(nullable: false),
                        Address = c.String(),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        LastName = c.String(nullable: false, maxLength: 50),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(precision: 7, storeType: "datetime2"),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CustomerId = c.String(maxLength: 128),
                        DriverId = c.String(maxLength: 128),
                        DeliveryStatus = c.String(),
                        DeliveryOtp = c.String(),
                        DeliveryAddress = c.String(),
                        Latitude = c.Double(),
                        Longitude = c.Double(),
                        OtpGeneratedAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        DeliveryDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        DriverAccepted = c.Boolean(),
                        DeclineReason = c.String(),
                        DriverRating = c.Int(),
                        DriverFeedback = c.String(),
                        OrderDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        TotalPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaymentStatus = c.String(),
                        OrderType = c.String(),
                        AssignedDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        DriverAssignedDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        DriverArrivedDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        RentalStatus = c.String(),
                        RentStartDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        RentEndDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        ReturnDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        Priority = c.String(),
                        Products_Id = c.Int(),
                        ApplicationUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CustomerId)
                .ForeignKey("dbo.AspNetUsers", t => t.DriverId)
                .ForeignKey("dbo.Products", t => t.Products_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .Index(t => t.CustomerId)
                .Index(t => t.DriverId)
                .Index(t => t.Products_Id)
                .Index(t => t.ApplicationUser_Id);
            
            CreateTable(
                "dbo.OrderItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsRental = c.Boolean(nullable: false),
                        RentalStartDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        RentalEndDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        RentalFeePerDay = c.Decimal(precision: 18, scale: 2),
                        Deposit = c.Decimal(precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.OrderId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Size = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RentalFee = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProductType = c.String(),
                        Stock = c.Int(nullable: false),
                        ImagePath = c.String(),
                        AgeGroup = c.String(),
                        Description = c.String(maxLength: 500),
                        EthnicGroup = c.String(),
                        Occasion = c.String(),
                        CategoryId = c.Int(nullable: false),
                        DepartmentId = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastUpdated = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categories", t => t.CategoryId, cascadeDelete: true)
                .ForeignKey("dbo.Departments", t => t.DepartmentId, cascadeDelete: true)
                .Index(t => t.CategoryId)
                .Index(t => t.DepartmentId);
            
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Departments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.Deliveries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        DriverId = c.String(maxLength: 128),
                        Address = c.String(),
                        DeliveryType = c.String(),
                        Status = c.String(),
                        DeliveryDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.DriverId)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .Index(t => t.OrderId)
                .Index(t => t.DriverId);
            
            CreateTable(
                "dbo.DriverAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        DriverId = c.String(maxLength: 128),
                        AssignedBy = c.String(),
                        AssignedDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ResponseDate = c.DateTime(precision: 7, storeType: "datetime2"),
                        ExpiryTime = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Status = c.String(),
                        DeclineReason = c.String(),
                        CreatedDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        AssignedByUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.AssignedByUser_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.DriverId)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .Index(t => t.OrderId)
                .Index(t => t.DriverId)
                .Index(t => t.AssignedByUser_Id);
            
            CreateTable(
                "dbo.DriverNotifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DriverId = c.String(maxLength: 128),
                        Title = c.String(),
                        Message = c.String(),
                        Type = c.String(),
                        IsRead = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ReadDate = c.DateTime(precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.DriverId)
                .Index(t => t.DriverId);
            
            CreateTable(
                "dbo.Rentals",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CustomerId = c.String(maxLength: 128),
                        ProductId = c.Int(nullable: false),
                        EventDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ReturnDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Deposit = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CustomerId)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.CustomerId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Reviews",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        CustomerId = c.String(maxLength: 128),
                        Rating = c.Int(nullable: false),
                        Comment = c.String(),
                        ReviewDate = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CustomerId)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Reviews", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Reviews", "CustomerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Rentals", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Rentals", "CustomerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.DriverNotifications", "DriverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.DriverAssignments", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.DriverAssignments", "DriverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.DriverAssignments", "AssignedByUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Deliveries", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Deliveries", "DriverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AdminNotifications", "AdminId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Orders", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Orders", "Products_Id", "dbo.Products");
            DropForeignKey("dbo.OrderItems", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Products", "DepartmentId", "dbo.Departments");
            DropForeignKey("dbo.Products", "CategoryId", "dbo.Categories");
            DropForeignKey("dbo.OrderItems", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "DriverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Orders", "CustomerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Reviews", new[] { "CustomerId" });
            DropIndex("dbo.Reviews", new[] { "ProductId" });
            DropIndex("dbo.Rentals", new[] { "ProductId" });
            DropIndex("dbo.Rentals", new[] { "CustomerId" });
            DropIndex("dbo.DriverNotifications", new[] { "DriverId" });
            DropIndex("dbo.DriverAssignments", new[] { "AssignedByUser_Id" });
            DropIndex("dbo.DriverAssignments", new[] { "DriverId" });
            DropIndex("dbo.DriverAssignments", new[] { "OrderId" });
            DropIndex("dbo.Deliveries", new[] { "DriverId" });
            DropIndex("dbo.Deliveries", new[] { "OrderId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.Products", new[] { "DepartmentId" });
            DropIndex("dbo.Products", new[] { "CategoryId" });
            DropIndex("dbo.OrderItems", new[] { "ProductId" });
            DropIndex("dbo.OrderItems", new[] { "OrderId" });
            DropIndex("dbo.Orders", new[] { "ApplicationUser_Id" });
            DropIndex("dbo.Orders", new[] { "Products_Id" });
            DropIndex("dbo.Orders", new[] { "DriverId" });
            DropIndex("dbo.Orders", new[] { "CustomerId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AdminNotifications", new[] { "AdminId" });
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Reviews");
            DropTable("dbo.Rentals");
            DropTable("dbo.DriverNotifications");
            DropTable("dbo.DriverAssignments");
            DropTable("dbo.Deliveries");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.Departments");
            DropTable("dbo.Categories");
            DropTable("dbo.Products");
            DropTable("dbo.OrderItems");
            DropTable("dbo.Orders");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AdminNotifications");
        }
    }
}
