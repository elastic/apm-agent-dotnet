namespace Scripts

open Fake.Core
open System.Xml.Linq

module Versioning = 
    let private buildProps = Paths.Src "Directory.Build.props"    
    let private xName name = XName.op_Implicit name
    
    type AssemblyInfo = {
        AssemblyVersion: SemVerInfo
        InformationalVersion: SemVerInfo
        FileVersion: SemVerInfo
        VersionPrefix: SemVerInfo
    }
    
    /// Gets the current AssemblyInfo version from the Directory.Build.Props in /src
    let CurrentVersion =
        let project = XElement.Load(buildProps)
        let propertyGroup = project.Element(xName "PropertyGroup")
        
        { AssemblyVersion = propertyGroup.Element(xName "AssemblyVersion").Value |> SemVer.parse
          InformationalVersion = propertyGroup.Element(xName "InformationalVersion").Value |> SemVer.parse
          FileVersion = propertyGroup.Element(xName "FileVersion").Value |> SemVer.parse
          VersionPrefix = propertyGroup.Element(xName "VersionPrefix").Value |> SemVer.parse }
