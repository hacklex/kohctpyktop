using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using JsonSubTypes;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kohctpyktop.Serialization
{
    public static class LayerSerializer
    {
        private class UpperSnakeCaseNamingStrategy : SnakeCaseNamingStrategy
        {
            protected override string ResolvePropertyName(string name) => base.ResolvePropertyName(name).ToUpper();
        }
        
        // todo: async methods

        private static readonly JsonSerializerSettings Settings = BuildSerializerSettings();
        
        private static JsonSerializerSettings BuildSerializerSettings()
        {
            var settings = new JsonSerializerSettings();

            settings.Converters.Add(new StringEnumConverter {NamingStrategy = new UpperSnakeCaseNamingStrategy()});
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(ValuesFunction), nameof(ValuesFunction.Type))
                .RegisterSubtype(typeof(StaticValuesFunction), ValuesFunctionType.Static)
                .RegisterSubtype(typeof(PeriodicValuesFunction), ValuesFunctionType.Periodic)
                .RegisterSubtype(typeof(AggregateValuesFunction), ValuesFunctionType.Aggregate)
                .Build());

            return settings;
        }

        public static ILayer ReadLayer(Stream stream)
        {
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                var json = reader.ReadToEnd();
                var layerData = JsonConvert.DeserializeObject<LayerData>(json, Settings);
                
                return new Layer(layerData);
            }
        } 
        
        public static void WriteLayer(Stream stream, ILayer layer)
        {
            var layerData = layer.ExportLayerData();
            var json = JsonConvert.SerializeObject(layerData, Settings);

            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzipStream))
                writer.Write(json);
        }

        public static IEnumerable<LayerTemplate> ReadResourceTemplates()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            foreach (var resName in assembly
                .GetManifestResourceNames()
                .Where(x => x.StartsWith("Kohctpyktop.Resources.LevelTemplates.")))
            {
                using (var stream = assembly.GetManifestResourceStream(resName))
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    yield return JsonConvert.DeserializeObject<LayerTemplate>(json, Settings);
                }
            }
        }
    }
}