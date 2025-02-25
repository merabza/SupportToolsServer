using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SystemToolsShared;

namespace SupportToolsServerDb.Conventions;

public class DatabaseEntitiesDefaultConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var tableNameAnnotation = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableNameAnnotation))
                entityType.SetTableName(entityType.ClrType.Name.Pluralize().UnCapitalize());
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(property.Name);
                if (property.ClrType == typeof(DateTime)) property.SetColumnType("datetime");
                if (property.ClrType == typeof(decimal)) property.SetColumnType("money");
            }
        }
    }
}