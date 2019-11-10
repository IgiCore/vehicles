// <auto-generated />
// ReSharper disable all

using System;
using System.Data.Entity.Migrations;
using System.CodeDom.Compiler;
using System.Data.Entity.Migrations.Infrastructure;

namespace IgiCore.Vehicles.Server.Migrations
{
    [GeneratedCode("NFive.Migration", "0.3 Alpha Build 175")]
    public class AddRotationAndSteeringAngle : DbMigration, IMigrationMetadata
    {
        string IMigrationMetadata.Id => "201906020114463_AddRotationAndSteeringAngle";
        
        string IMigrationMetadata.Source => null;
        
        string IMigrationMetadata.Target => "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";
        
        public override void Up()
        {
            AddColumn("dbo.Vehicles", "Rotation_Z", c => c.Single(nullable: false));
            AddColumn("dbo.Vehicles", "Rotation_X", c => c.Single(nullable: false));
            AddColumn("dbo.Vehicles", "Rotation_Y", c => c.Single(nullable: false));
            AddColumn("dbo.Vehicles", "SteeringAngle", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Vehicles", "SteeringAngle");
            DropColumn("dbo.Vehicles", "Rotation_Y");
            DropColumn("dbo.Vehicles", "Rotation_X");
            DropColumn("dbo.Vehicles", "Rotation_Z");
        }
    }
}
