using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Typedown.XamlUI
{
    public class ReflectionXamlMetadataProvider : IXamlMetadataProvider
    {
        private static readonly Dictionary<string, List<IXamlMetadataProvider>> _typeNameProviders = new();

        private static readonly Dictionary<Type, List<IXamlMetadataProvider>> _typeProviders = new();

        private static readonly List<IXamlMetadataProvider> _otherProviders = new();

        private static readonly Dictionary<Type, IXamlType> _typeCache = new();

        private static readonly Dictionary<string, IXamlType> _typeNameCache = new();

        private static Task _initializeTask;

        private static bool _cycle = false;

        public ReflectionXamlMetadataProvider()
        {
            _initializeTask ??= Task.Run(() => TryInitializeXamlMetadataProvider());
        }

        public IXamlType GetXamlType(Type type)
        {
            if (_typeCache.TryGetValue(type, out var cachedValue))
                return cachedValue;
            if (_cycle)
                return null;
            try
            {
                _cycle = true;
                var xamlType = GetXamlTypeNoCache(type);
                _typeCache[type] = xamlType;
                return xamlType;
            }
            finally
            {
                _cycle = false;
            }
        }

        public IXamlType GetXamlType(string fullName)
        {
            if (_typeNameCache.TryGetValue(fullName, out var cachedValue))
                return cachedValue;
            if (_cycle)
                return null;
            try
            {
                _cycle = true;
                var xamlType = GetXamlTypeNoCache(fullName);
                _typeNameCache[fullName] = xamlType;
                return xamlType;
            }
            finally
            {
                _cycle = false;
            }
        }

        private IXamlType GetXamlTypeNoCache(Type type)
        {
            _initializeTask.Wait();
            if (_typeProviders.TryGetValue(type, out var providers))
            {
                foreach (var provider in providers)
                {
                    var xamlType = provider.GetXamlType(type);
                    if (xamlType != null)
                        return xamlType;
                }
            }
            foreach (var provider in _otherProviders)
            {
                var xamlType = provider.GetXamlType(type);
                if (xamlType != null)
                    return xamlType;
            }
            return null;
        }

        private IXamlType GetXamlTypeNoCache(string fullName)
        {
            _initializeTask.Wait();
            if (_typeNameProviders.TryGetValue(fullName, out var providers))
            {
                foreach (var provider in providers)
                {
                    var xamlType = provider.GetXamlType(fullName);
                    if (xamlType != null)
                        return xamlType;
                }
            }
            foreach (var provider in _otherProviders)
            {
                var xamlType = provider.GetXamlType(fullName);
                if (xamlType != null)
                    return xamlType;
            }
            return null;
        }

        public XmlnsDefinition[] GetXmlnsDefinitions()
        {
            return new XmlnsDefinition[0];
        }

        private bool TryInitializeXamlMetadataProvider()
        {
            var searchedAssemblies = new HashSet<string>();
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var extensions = new HashSet<string> { ".exe", ".dll", ".winmd" };
            var searchFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(x => extensions.Contains(Path.GetExtension(x).ToLower()))
                .ToHashSet();
            Parallel.ForEach(searchFiles, file =>
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    lock (searchedAssemblies)
                    {
                        if (!searchedAssemblies.Add(assembly.FullName))
                            return;
                    }
                    var providerTypes = assembly.DefinedTypes.Where(IXamlMetadataProviderFilter).ToList();
                    foreach (var providerType in providerTypes)
                    {
                        try
                        {
                            var provider = Activator.CreateInstance(providerType) as IXamlMetadataProvider;
                            var (typeNameTable, typeTable) = TryGetTypeTable(provider);

                            if (typeNameTable is null || typeTable is null)
                            {
                                lock (_otherProviders)
                                {
                                    _otherProviders.Add(provider);
                                }
                            }

                            if (typeNameTable != null)
                            {
                                lock (_typeNameProviders)
                                {
                                    foreach (var typeName in typeNameTable)
                                    {
                                        if (_typeNameProviders.TryGetValue(typeName, out var list))
                                        {
                                            list.Add(provider);
                                        }
                                        else
                                        {
                                            _typeNameProviders.Add(typeName, new() { provider });
                                        }
                                    }
                                }
                            }

                            if (typeTable != null)
                            {
                                lock (_typeProviders)
                                {
                                    foreach (var type in typeTable)
                                    {
                                        if (_typeProviders.TryGetValue(type, out var list))
                                        {
                                            list.Add(provider);
                                        }
                                        else
                                        {
                                            _typeProviders.Add(type, new() { provider });
                                        }
                                    }
                                }
                            }

                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }
                catch
                {
                    // Ignore
                }
            });
            return true;
        }

        public static bool IXamlMetadataProviderFilter(Type type)
        {
            return typeof(IXamlMetadataProvider).IsAssignableFrom(type) &&
                !typeof(Application).IsAssignableFrom(type) &&
                !typeof(ReflectionXamlMetadataProvider).IsAssignableFrom(type) &&
                !type.IsInterface &&
                !type.IsAbstract;
        }

        public static (string[], Type[]) TryGetTypeTable(IXamlMetadataProvider provider)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var internalProvider = provider?.GetType().GetProperty("Provider", flags)?.GetValue(provider);
            internalProvider?.GetType().GetMethod("InitTypeTables", flags)?.Invoke(internalProvider, null);
            var typeNameTable = internalProvider?.GetType().GetField("_typeNameTable", flags)?.GetValue(internalProvider) as string[];
            var typeTable = internalProvider?.GetType().GetField("_typeTable", flags)?.GetValue(internalProvider) as Type[];
            return (typeNameTable, typeTable);
        }
    }
}
