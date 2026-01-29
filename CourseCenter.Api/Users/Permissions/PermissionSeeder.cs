using CourseCenter.Api;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace CourseCenter.Api.Users.Permissions
{
    public static class PermissionSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context == null) return;

            var permissionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Collect permissions from static "Permissions" classes (public static string fields/properties)
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }

                foreach (var type in types)
                {
                    // static class named Permissions
                    if (type.IsClass && type.IsAbstract && type.IsSealed && type.Name.Equals("Permissions", StringComparison.OrdinalIgnoreCase))
                    {
                        // fields
                        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (field.FieldType == typeof(string))
                            {
                                var val = field.GetValue(null) as string;
                                if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                            }
                        }

                        // properties
                        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (prop.PropertyType == typeof(string) && prop.GetMethod != null)
                            {
                                try
                                {
                                    var val = prop.GetValue(null) as string;
                                    if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                                }
                                catch { }
                            }
                        }

                        // nested types (e.g., Permissions.Students)
                        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
                            {
                                if (field.FieldType == typeof(string))
                                {
                                    var val = field.GetValue(null) as string;
                                    if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                                }
                            }
                            foreach (var prop in nested.GetProperties(BindingFlags.Public | BindingFlags.Static))
                            {
                                if (prop.PropertyType == typeof(string) && prop.GetMethod != null)
                                {
                                    try
                                    {
                                        var val = prop.GetValue(null) as string;
                                        if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }

            // 2) Collect permissions from Authorize attributes used as policies on controllers/actions
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }

                foreach (var type in types)
                {
                    if (!typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type))
                        continue;

                    // class-level
                    var classAuths = type.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                    foreach (var a in classAuths)
                    {
                        if (!string.IsNullOrWhiteSpace(a.Policy)) permissionNames.Add(a.Policy);
                    }

                    // method-level
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        var methAuths = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                        foreach (var a in methAuths)
                        {
                            if (!string.IsNullOrWhiteSpace(a.Policy)) permissionNames.Add(a.Policy);
                        }

                        // custom HasPermission attribute (if present)
                        var hasPermAttrs = method.GetCustomAttributes(inherit: true).Where(x => x.GetType().Name == "HasPermissionAttribute");
                        foreach (var hp in hasPermAttrs)
                        {
                            // try to read Policy property or first constructor arg
                            var t = hp.GetType();
                            var prop = t.GetProperty("Policy");
                            if (prop != null)
                            {
                                var val = prop.GetValue(hp) as string;
                                if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                            }
                            else
                            {
                                var f = t.GetField("_permission", BindingFlags.NonPublic | BindingFlags.Instance) ?? t.GetField("Permission", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (f != null)
                                {
                                    var val = f.GetValue(hp) as string;
                                    if (!string.IsNullOrWhiteSpace(val)) permissionNames.Add(val);
                                }
                            }
                        }
                    }
                }
            }

            // 3) Persist missing permissions
            var codes = permissionNames.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!codes.Any()) return;

            var existingCodes = new HashSet<string>(context.Permissions.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Permission>();
            foreach (var code in codes)
            {
                if (existingCodes.Contains(code)) continue;

                var module = code.Contains('.') ? code.Split('.')[0] : (code.Contains('_') ? code.Split('_')[0] : "General");
                toAdd.Add(new Permission
                {
                    Code = code,
                    Name = code,
                    Module = module
                });
            }

            if (toAdd.Any())
            {
                context.Permissions.AddRange(toAdd);
                context.SaveChanges();
            }
        }
    }
}
