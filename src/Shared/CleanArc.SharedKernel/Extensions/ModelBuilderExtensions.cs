using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Pluralize.NET;

namespace CleanArc.SharedKernel.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Singularizin table name like Posts to Post or People to Person
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void AddSingularizingTableNameConvention(this ModelBuilder modelBuilder)
    {
        Pluralizer pluralizer = new Pluralizer();
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            string tableName = entityType.GetTableName();

            if (!string.IsNullOrEmpty(tableName))
                entityType.SetTableName(pluralizer.Pluralize(tableName));
        }
    }

    /// <summary>
    /// Pluralizing table name like Post to Posts or Person to People
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void AddPluralizingTableNameConvention(this ModelBuilder modelBuilder)
    {
        Pluralizer pluralizer = new Pluralizer();
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            string tableName = entityType.GetTableName();
            string schema = entityType.GetSchema();

            if (!string.IsNullOrEmpty(tableName))
            {
                entityType.SetTableName(pluralizer.Pluralize(tableName));
            }
        }
    }

    /// <summary>
    /// Set NEWSEQUENTIALID() sql function for all columns named "Id"
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <param name="mustBeIdentity">Set to true if you want only "Identity" guid fields that named "Id"</param>
    public static void AddSequentialGuidForIdConvention(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddDefaultValueSqlConvention("Id", typeof(Guid), "NEWSEQUENTIALID()");
    }

    /// <summary>
    /// Set DefaultValueSql for sepecific property name and type
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <param name="propertyName">Name of property wants to set DefaultValueSql for</param>
    /// <param name="propertyType">Type of property wants to set DefaultValueSql for </param>
    /// <param name="defaultValueSql">DefaultValueSql like "NEWSEQUENTIALID()"</param>
    public static void AddDefaultValueSqlConvention(this ModelBuilder modelBuilder, string propertyName, Type propertyType, string defaultValueSql)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            IMutableProperty property = entityType.GetProperties().SingleOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null && property.ClrType == propertyType)
                property.SetDefaultValueSql(defaultValueSql);
        }
    }

    /// <summary>
    /// Set DeleteBehavior.Restrict by default for relations, while preserving explicit cascade delete behaviors and user-owned data cascades.
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void AddRestrictDeleteBehaviorConvention(this ModelBuilder modelBuilder)
    {
        var allFKs = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => !fk.IsOwnership)
            .ToList();

        var cascadeAllowedPrincipalNames = new HashSet<string>
        {
            "User",
            "Classroom",
            "VocabularyItem",
            "StudentChallengeAttempt",
            "SpellingTest",
            "Badge",
            "Role"
        };

        foreach (var fk in allFKs)
        {
            var principalName = fk.PrincipalEntityType.ClrType.Name;

            // Check if cascade delete is allowed for this principal entity type
            if (cascadeAllowedPrincipalNames.Contains(principalName))
            {
                if (principalName == "User")
                {
                    // For User, do not cascade delete if it represents a teacher/creator association
                    bool isTeacherOrCreator = false;
                    foreach (var property in fk.Properties)
                    {
                        var name = property.Name.ToLowerInvariant();
                        if (name.Contains("teacher") || name.Contains("createdby") || name.Contains("approvedby"))
                        {
                            isTeacherOrCreator = true;
                            break;
                        }
                    }

                    if (!isTeacherOrCreator)
                    {
                        fk.DeleteBehavior = DeleteBehavior.Cascade;
                        continue;
                    }
                }
                else
                {
                    // Allow cascade delete for other designated aggregate roots
                    fk.DeleteBehavior = DeleteBehavior.Cascade;
                    continue;
                }
            }

            // Default all other cascade relationships to Restrict
            if (fk.DeleteBehavior == DeleteBehavior.Cascade)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    /// <summary>
    /// Dynamicaly load all IEntityTypeConfiguration with Reflection
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <param name="assemblies">Assemblies contains Entities</param>
    [Obsolete("Use the ApplyConfigurationsFromAssembly instead")]
    public static void RegisterEntityTypeConfiguration(this ModelBuilder modelBuilder, params Assembly[] assemblies)
    {
        MethodInfo applyGenericMethod = typeof(ModelBuilder).GetMethods().First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration));

        IEnumerable<Type> types = assemblies.SelectMany(a => a.GetExportedTypes())
            .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic);

        foreach (Type type in types)
        {
            foreach (Type iface in type.GetInterfaces())
            {
                if (iface.IsConstructedGenericType && iface.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                {
                    MethodInfo applyConcreteMethod = applyGenericMethod.MakeGenericMethod(iface.GenericTypeArguments[0]);
                    applyConcreteMethod.Invoke(modelBuilder, new object[] { Activator.CreateInstance(type) });
                }
            }
        }
    }

    /// <summary>
    /// Dynamicaly register all Entities that inherit from specific BaseType
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <param name="baseType">Base type that Entities inherit from this</param>
    /// <param name="assemblies">Assemblies contains Entities</param>
    public static void RegisterAllEntities<BaseType>(this ModelBuilder modelBuilder, params Assembly[] assemblies)
    {
        IEnumerable<Type> types = assemblies.SelectMany(a => a.GetExportedTypes())
            .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic && typeof(BaseType).IsAssignableFrom(c));

        foreach (Type type in types)
            modelBuilder.Entity(type);
    }
}