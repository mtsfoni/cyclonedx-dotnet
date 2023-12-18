using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CycloneDX.Models;
using Xunit;

namespace CycloneDX.Tests.FunctionalTests
{
    public class FunctionalTestHelper
    {
        /// <summary>
        /// Trying to build SBOM from provided parameters and validated the result file
        /// </summary>
        /// <param name="assetsJson"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<Bom> Test(string assetsJson, RunOptions options)
        {
            options.disableGithubLicenses = true;
            options.outputDirectory ??= "/bom/";
            options.outputFilename ??= options.json ? "bom.json" : "bom.xml";
            options.SolutionOrProjectFile ??= MockUnixSupport.Path("c:/ProjectPath/Project.csproj");
            options.disablePackageRestore = true;


            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { MockUnixSupport.Path(options.SolutionOrProjectFile), new MockFileData(CsprojContents) },
                { MockUnixSupport.Path("c:/ProjectPath/obj/project.assets.json"), new MockFileData(assetsJson) }
            });


            Runner runner = new Runner(mockFileSystem, null, null, null, null, null, null);
            int exitCode = await runner.HandleCommandAsync(options);

            Assert.Equal((int)ExitCode.OK, exitCode);

            var expectedFileName = mockFileSystem.Path.Combine(options.outputDirectory, options.outputFilename);

    

            Assert.True(mockFileSystem.FileExists(MockUnixSupport.Path(expectedFileName)), "Bom file not generated");

            var mockBomFile = mockFileSystem.GetFile(MockUnixSupport.Path(expectedFileName));
            var mockBomFileStream = new MemoryStream(mockBomFile.Contents);
            ValidationResult validationResult;
            if (options.json)
            {
                validationResult = await Json.Validator.ValidateAsync(mockBomFileStream, SpecificationVersion.v1_5).ConfigureAwait(false);
            }
            else
            {
                validationResult = Xml.Validator.Validate(mockBomFileStream, SpecificationVersion.v1_5);
            }
            Assert.True(validationResult.Valid);

            return runner.LastGeneratedBom;
        }


        private const string CsprojContents =
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n\n  " +
            "<PropertyGroup>\n    " +
                "<OutputType>Exe</OutputType>\n    " +
                "<PackageId>SampleProject</PackageId>\n    " +
            "</PropertyGroup>\n\n  " +
            "<ItemGroup>\n    " +          
            "</ItemGroup>\n" +
        "</Project>\n";

    }
}
