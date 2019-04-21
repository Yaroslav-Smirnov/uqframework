using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UQFrameWork.Demo
{
    /// <summary>
    /// Generates data files
    /// </summary>
    internal static class Generator
    {
        public static void GenerateFiles(string folder, int number, bool overwrite = false)
        {
            var random = new Random();

            var data = Enumerable.Range(0, number).Select(i => RandomString(random, 20)).ToArray();

            Parallel.For(0, number, i =>
            {
                var file = Path.Combine(folder, $"{i.ToString()}.json");

                if (File.Exists(file) && !overwrite)
                    return;

                var item = new Entity
                {
                    Identifier = i.ToString(),
                    Name = data[i],
                    Property1 = $"Value1-{i}",
                    Collection = new List<string>(Enumerable.Range(1, 100).Select(x => x.ToString())),
                };

                File.WriteAllText(file, JsonConvert.SerializeObject(item));
            });
        }

        private static string RandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_&@";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
