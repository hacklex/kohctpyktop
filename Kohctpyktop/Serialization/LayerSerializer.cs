using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using JsonSubTypes;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Field.ValuesFunctions;
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
                .RegisterSubtype(typeof(RepeatingSequenceValuesFunction), ValuesFunctionType.RepeatingSequence)
                .RegisterSubtype(typeof(ReferenceValuesFunction), ValuesFunctionType.Reference)
                .Build());

            return settings;
        }

        public static ILayer ReadLayer(Stream stream)
        {
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                var json = reader.ReadToEnd();
                var save = JsonConvert.DeserializeObject<SavedLayer>(json, Settings);
                
                return new Layer(save);
            }
        } 
        
        public static void WriteLayer(Stream stream, ILayer layer)
        {
            var layerData = layer.Save();
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
                {
                    Debug.Assert(stream != null, nameof(stream) + " != null");
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        yield return JsonConvert.DeserializeObject<LayerTemplate>(json, Settings);
                    }
                }
            }
        }

        public static LayerTemplate ReadTemplate(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                var json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<LayerTemplate>(json, Settings);
            }
        }

        public static void WriteTemplate(Stream stream, LayerTemplate template)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                var json = JsonConvert.SerializeObject(template, Settings);
                writer.Write(json);
            }
        }
    }
}