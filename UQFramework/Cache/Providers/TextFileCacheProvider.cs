using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UQFramework.Attributes;

namespace UQFramework.Cache.Providers
{
	public class TextFileCacheProvider<T> : PersistentCacheProviderBase<T>
    {
        private readonly string _folder;
        private readonly string _pathToFile;

        private const string ProviderCode = "TN";

        private readonly Version _cacheFormatVersion = new Version(1, 0, 6, 0);
        private readonly Version _daoVersion;
        private readonly Type _dataAccessObjectType;
        private readonly IContractResolver _jsonContractResolver;

        private static readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

		public TextFileCacheProvider(string dataStoreSetId, IDictionary<string, string> parameters, object dataSourceReader, IEnumerable<PropertyInfo> cachedProperties)
            : base(dataStoreSetId, dataSourceReader, cachedProperties)
        {
            if (string.IsNullOrEmpty(dataStoreSetId))
                throw new ArgumentNullException(nameof(dataStoreSetId));

            if (!parameters.TryGetValue("folder", out _folder))
                throw new ArgumentException("Required key 'folder' missing in parameters", nameof(parameters));

            ProcessMacroses(ref _folder);

            _dataAccessObjectType = dataSourceReader.GetType();

            var fileName = $"{ProviderCode}.{_dataStoreSetId}.{typeof(T).FullName}.{dataSourceReader.GetType()}.txt";
            _pathToFile = Path.Combine(_folder, fileName);

            _daoVersion = _dataAccessObjectType.GetCustomAttribute<DaoVersionAttribute>()?.Version ?? new Version(1, 0, 0, 0);

            _jsonContractResolver = new CachedPropertyContractResolver<T>(_cachedProperties);
        }

		public override long LastChanged => File.GetLastWriteTimeUtc(_pathToFile).ToFileTimeUtc();

		protected override bool RebuildRequired()
        {
            _cacheLock.EnterReadLock();
            // YSV: think about storing header separately to avoid the concurency problem
            try
            {
                if (!File.Exists(_pathToFile))
                    return true;

                var headerString = File.ReadLines(_pathToFile).FirstOrDefault();

                if (string.IsNullOrEmpty(headerString))
                    return true;

                var header = JsonConvert.DeserializeObject<CacheHeader>(headerString);

                if (header == null)
                    return true;

                if (header.CacheFormatVersion != _cacheFormatVersion)
                    return true;

                if (header.CachedEntityType != typeof(T).FullName)
                    return true;

                if (header.CachedPropertiesString != CachedPropertiesString)
                    return true;

                if (header.DataAccessObjectType != _dataAccessObjectType.FullName)
                    return true;

                if (header.DaoVersion != _daoVersion)
                    return true;

                return false;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        protected override IDictionary<string, T> ReadAllDataFromCache()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return File.ReadLines(_pathToFile) // start reading file
                    .Skip(1) // skip header
                    .Select(DeserializeItem)
                    .ToDictionary(d => d.key, d => d.data);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        protected override void ReplaceAll(IDictionary<string, T> newEntities)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                var header = new CacheHeader
                {
                    CacheFormatVersion = _cacheFormatVersion,
                    CachedPropertiesString = CachedPropertiesString,
                    CachedEntityType = typeof(T).FullName,
                    DaoVersion = _daoVersion,
                    DataAccessObjectType = _dataAccessObjectType.FullName
                };

                var headerString = JsonConvert.SerializeObject(header);

                var lines = newEntities.Select(e => SerializeItem(e.Key, e.Value)).Prepend(headerString);

                File.WriteAllLines(_pathToFile, lines);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        protected override void ReplaceItems(IEnumerable<string> identifiers, IDictionary<string, T> newEntities)
        {
            //YSV: run on thread pool to update in parallel with memory updates
            //would be great to implement a sort of transaction log to exclude possibility of failures
            Task.Run(() =>
            {
                _cacheLock.EnterWriteLock();
                try
                {
                    var newContent = GetNewFileLines(identifiers, newEntities).ToArray();
                    File.WriteAllLines(_pathToFile, newContent);
                }
                catch (Exception ex)
                {
                    var errFileName = Path.Combine(_folder, "TextFileCacheUpdateErrors.txt");
                    File.WriteAllText(errFileName, $"{ex.Message}\n{ex.StackTrace}");
                    throw;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            });
        }

        private IEnumerable<string> GetNewFileLines(IEnumerable<string> identifiers, IDictionary<string, T> newEntities)
        {
            // YSV: all these contains are thereotically not good for performance, so might be optimized using presorting and the stuff

            var idArr = identifiers.ToList();
            var keyArr = newEntities.Keys.ToArray();

            var linesArr = File.ReadAllLines(_pathToFile);


            var isFirst = true;
            foreach (var line in linesArr)
            {
                if (isFirst) // return header as is
                {
                    yield return line;
                    isFirst = false;
                    continue;
                }
                var key = line.Split('|')[0];

                if (!idArr.Contains(key)) // if existing item id is not among modified items identifiers
                    yield return line; // return this line as is 
                else  // else (i.e. existing item id IS among modified items identifiers
                {
                    if (keyArr.Contains(key)) // if existing item id is among replacing items identifiers (it means it was updated, not deleted)
                        yield return SerializeItem(key, newEntities[key]);

                    idArr.Remove(key); // remove the key from processing as already processed 
                }
            }

			foreach (var key in idArr)
			{// add new
				if (!newEntities.Keys.Contains(key)) // still not sure why
					continue;
				yield return SerializeItem(key, newEntities[key]);
			}
        }

        #region Item JSon Serialization

        private string SerializeItem(string key, T data)
        {
            if (key.Contains('|'))
                throw new InvalidOperationException($"Item identifier must not contain '|' (item {key} of {typeof(T)})");

            return $"{key}|{JsonConvert.SerializeObject(data, Formatting.None, GetSerializerSettings())}";
        }

        private (string key, T data) DeserializeItem(string line)
        {
            var i = line.IndexOf('|');
            var key = line.Substring(0, i);
            var json = line.Substring(i + 1);
            return (key, JsonConvert.DeserializeObject<T>(json, GetSerializerSettings()));
        }

        private JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = _jsonContractResolver,
            };
        }

        #endregion

        #region Helpers

        private static void ProcessMacroses(ref string folderPath)
        {
            var matches = Regex.Matches(folderPath, @"\$\(.*?\)");

            foreach (Match m in matches)
            {
                folderPath = folderPath.Replace(m.Value, GetMacrosReplacement(m.Value));
            }
        }

        private static string GetMacrosReplacement(string macro)
        {
            switch (macro.ToLower())
            {
                case "$(appdata)":
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                case "$(appdatacommon)":
                    return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                default:
                    throw new InvalidOperationException($"Unknown macro: {macro}");
            }
        }

        #endregion
    }
}
