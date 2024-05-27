namespace Scripts

open System.Runtime.InteropServices
open Fake.Core

module TestEnvironment =    
    let isCI = Environment.hasEnvironVar "BUILD_ID" 
    let isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    let isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
    
    // C# style enum for System.CommandLine
    type TestSuite =
        | Unit = 0 //only runs Elastic.Apm.Tests & Elastic.Apm.OpenTelemetry.Tests
        | CI = 1  //runs Unit and all instrumentation/integration tests
        | Profiler = 2
        | Azure = 3
        | StartupHooks = 4
        | IIS = 5
