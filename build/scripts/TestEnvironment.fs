namespace Scripts

open System.Runtime.InteropServices
open Fake.Core

module TestEnvironment =    
    let isCI = Environment.hasEnvironVar "BUILD_ID" 
    let isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    let isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)