﻿using System.Reflection;

namespace BudgetAnalyser.Engine;

/// <summary>
///     An initialisation class to discover all default IoC registrations required in the specified assembly.
/// </summary>
public static class DefaultIoCRegistrations
{
    /// <summary>
    ///     Gets a named instance from all the given instances. This is used to find an instance of an interface with a specific name.
    /// </summary>
    /// <typeparam name="T">Any interface or abstract class</typeparam>
    /// <returns>The found named instance, or null if no instance matches the name.</returns>
    public static T GetNamedInstance<T>(IEnumerable<T> instances, string name) where T : class
    {
        if (instances is null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        foreach (var instance in instances)
        {
            var attributes = instance.GetType().GetTypeInfo().GetCustomAttributes<AutoRegisterWithIoCAttribute>();
            var attribute = attributes.FirstOrDefault();
            if (attribute is not null)
            {
                if (attribute.Named == name)
                {
                    return instance;
                }
            }
        }

        throw new NotSupportedException($"No instance found with the specified name '{name}'.");
    }

    /// <summary>
    ///     Enumerates through all static types in the given assembly and finds static properties decorated with <see cref="PropertyInjectionAttribute" />.
    ///     DO NOT USE THIS. Except as a last resort, property injection is a bad pattern, but is sometimes required for UI data bindings. Do not use it for any other reason.
    /// </summary>
    /// <param name="assembly">The assembly in which to search for automatic registrations.</param>
    public static IEnumerable<PropertyInjectionDependencyRequirement> ProcessPropertyInjection(Assembly assembly)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var allTypes = assembly.GetTypes()
            .Where(t =>
            {
                var typeInfo = t.GetTypeInfo();
                return typeInfo is { IsClass: true, IsAbstract: true, IsSealed: true } && typeInfo.GetCustomAttribute<AutoRegisterWithIoCAttribute>() is not null;
            })
            .ToArray();
        foreach (var type in allTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var injectionAttribute = property.GetCustomAttribute<PropertyInjectionAttribute>();
                if (injectionAttribute is not null)
                {
                    yield return new PropertyInjectionDependencyRequirement { DependencyRequired = property.PropertyType, PropertyInjectionAssignment = instance => property.SetValue(null, instance) };
                }
            }
        }
    }

    /// <summary>
    ///     Finds all types that need to be registered with the hosting application's IoC container.  The host application
    ///     needs to register these with the container of its choosing.
    ///     See the attribute for registration options. No dependent assemblies are searched.
    /// </summary>
    /// <param name="assembly">The assembly in which to search for automatic registrations.</param>
    public static IEnumerable<DependencyRegistrationRequirement> RegisterAutoMappingsFromAssembly(Assembly assembly)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var allTypes = assembly.GetTypes()
            .Where(t =>
            {
                var typeInfo = t.GetTypeInfo();
                return typeInfo is { IsAbstract: false, IsClass: true } && typeInfo.GetCustomAttribute<AutoRegisterWithIoCAttribute>() is not null;
            })
            .ToArray();

        return from type in allTypes
               let autoRegisterAttribute = type.GetTypeInfo().GetCustomAttribute<AutoRegisterWithIoCAttribute>()
               select new DependencyRegistrationRequirement { DependencyRequired = type, IsSingleInstance = autoRegisterAttribute.SingleInstance, NamedInstanceName = autoRegisterAttribute.Named };
    }
}
