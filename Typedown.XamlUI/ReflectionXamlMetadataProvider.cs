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
    internal class ReflectionXamlMetadataProvider : IXamlMetadataProvider
    {
        private HashSet<string> _candidateFiles;

        private readonly HashSet<string> _searchedAssemblies = new() { typeof(ReflectionXamlMetadataProvider).Assembly.FullName };

        private readonly List<IXamlMetadataProvider> _providers = new();

        private readonly Dictionary<Type, IXamlType> _typeCache = new();

        private readonly Dictionary<string, IXamlType> _nameCache = new();

        public IXamlType GetXamlType(Type type)
        {
            if (_typeCache.TryGetValue(type, out var cachedValue))
                return cachedValue;
            var xamlType = GetXamlTypeNoCache(type);
            _typeCache[type] = xamlType;
            return xamlType;
        }

        public IXamlType GetXamlType(string fullName)
        {
            if (_nameCache.TryGetValue(fullName, out var cachedValue))
                return cachedValue;
            var xamlType = GetXamlTypeNoCache(fullName);
            _nameCache[fullName] = xamlType;
            return xamlType;
        }

        private IXamlType GetXamlTypeNoCache(Type type)
        {
            var searched = new HashSet<IXamlMetadataProvider>();
            var prefix = type.FullName.Split('.').ToList();
            while (true)
            {
                foreach (var provider in _providers.Where(x => !searched.Contains(x)))
                {
                    searched.Add(provider);
                    var xamlType = provider.GetXamlType(type);
                    if (xamlType != null)
                        return xamlType;
                }
                SearchXamlMetadataProvider(string.Join(".", prefix));
                if (!prefix.Any())
                    break;
                prefix.RemoveAt(prefix.Count - 1);
            }
            return null;
        }

        private IXamlType GetXamlTypeNoCache(string fullName)
        {
            var searched = new HashSet<IXamlMetadataProvider>();
            var prefix = fullName.Split('.').ToList();
            while (true)
            {
                foreach (var provider in _providers.Where(x => !searched.Contains(x)))
                {
                    searched.Add(provider);
                    var xamlType = provider.GetXamlType(fullName);
                    if (xamlType != null)
                        return xamlType;
                }
                SearchXamlMetadataProvider(string.Join(".", prefix));
                if (!prefix.Any())
                    break;
                prefix.RemoveAt(prefix.Count - 1);
            }
            return null;
        }

        public XmlnsDefinition[] GetXmlnsDefinitions()
        {
            return new XmlnsDefinition[0];
        }

        private bool SearchXamlMetadataProvider(string prefix)
        {
            if (_candidateFiles == null)
            {
                var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var extensions = new HashSet<string> { ".exe", ".dll", ".winmd" };
                _candidateFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(x => extensions.Contains(Path.GetExtension(x).ToLower()))
                    .ToHashSet();
            }
            var searchFiles = _candidateFiles.Where(x => Path.GetFileName(x).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!searchFiles.Any())
            {
                return false;
            }
            foreach (var file in searchFiles)
            {
                _candidateFiles.Remove(file);
            }
            Parallel.ForEach(searchFiles, file =>
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    lock (_searchedAssemblies)
                    {
                        if (_searchedAssemblies.Contains(assembly.FullName))
                            return;
                        _searchedAssemblies.Add(assembly.FullName);
                    }
                    var types = assembly.DefinedTypes
                        .Where(x => typeof(IXamlMetadataProvider).IsAssignableFrom(x) && !typeof(Application).IsAssignableFrom(x))
                        .Where(x => !x.IsInterface && !x.IsAbstract)
                        .ToList();
                    foreach (var type in types)
                    {
                        try
                        {
                            var provider = Activator.CreateInstance(type) as IXamlMetadataProvider;
                            lock (_providers)
                            {
                                _providers.Add(provider);
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
    }
}
