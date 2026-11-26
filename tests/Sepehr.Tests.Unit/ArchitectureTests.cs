using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Sepehr.Domain;
using Xunit;

namespace Sepehr.Tests.Unit
{
    public class ArchitectureTests
    {
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Sepehr.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Sepehr.sln not found.");
        }

        [Theory]
        [InlineData("Sepehr.Domain", "")]
        [InlineData("Sepehr.Shared", "")]
        [InlineData("Sepehr.Contracts", "")]
        [InlineData("Sepehr.Application", "Sepehr.Domain")]
        [InlineData("Sepehr.Infrastructure", "Sepehr.Application,Sepehr.Domain")]
        [InlineData("Sepehr.Api", "Sepehr.Application,Sepehr.Contracts,Sepehr.Infrastructure")]
        public void Project_references_follow_dependency_rule(string project, string allowedList)
        {
            var path = Path.Combine(RepositoryRoot(), "src", project, project + ".csproj");
            var allowed = allowedList.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var references = XDocument.Load(path).Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")!.Value.Split('\\', '/').Last())
                .Select(file => file.Replace(".csproj", string.Empty));

            Assert.All(references, reference => Assert.Contains(reference, allowed));
        }

        [Fact]
        public void Sql_seed_permissions_match_domain_codes()
        {
            var sql = File.ReadAllText(Path.Combine(
                RepositoryRoot(), "database", "migrations", "0001_initial.sql"));
            var block = Regex.Match(sql, "-- BEGIN PERMISSIONS(.*?)-- END PERMISSIONS", RegexOptions.Singleline);
            var seeded = new HashSet<string>(
                Regex.Matches(block.Groups[1].Value, "N'([A-Za-z]+\\.[A-Za-z]+)'")
                    .Select(match => match.Groups[1].Value));

            Assert.True(seeded.SetEquals(Permissions.All));
        }
    }
}
