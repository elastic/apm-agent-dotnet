use serde::{Serialize, Deserialize, Deserializer, de};
use crate::ffi::{AppDomainID, AssemblyID, ModuleID, COR_PRF_MODULE_FLAGS, BYTE};
use crate::types::{Version, PublicKey};
use std::str::FromStr;
use std::collections::BTreeMap;
use crate::error::Error;
use std::marker::PhantomData;
use serde::de::{Visitor, MapAccess, DeserializeOwned};
use core::fmt;

pub(crate) struct ModuleInfo {
    pub id: ModuleID,
    pub path: String,
    pub assembly: AssemblyInfo,
    pub flags: COR_PRF_MODULE_FLAGS,
}
impl ModuleInfo {
    pub fn is_windows_runtime(&self) -> bool {
        self.flags
            .contains(COR_PRF_MODULE_FLAGS::COR_PRF_MODULE_WINDOWS_RUNTIME)
    }
}

pub(crate) struct AssemblyInfo {
    pub id: AssemblyID,
    pub name: String,
    pub manifest_module_id: ModuleID,
    pub app_domain_id: AppDomainID,
    pub app_domain_name: String,
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct MethodSignature {
    data: Vec<BYTE>
}

struct MethodSignatureVisitor;
impl<'de> Visitor<'de> for MethodSignatureVisitor {
    type Value = MethodSignature;

    fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
        formatter.write_str("a string")
    }

    fn visit_str<E>(self, v: &str) -> Result<Self::Value, E>
        where
            E: de::Error,
    {
        let parse_bytes: Result<Vec<_>, _> = v.split(' ')
            .map(|p| hex::decode(p))
            .collect();
        match parse_bytes {
            Ok(b) => Ok(MethodSignature { data: b.into_iter().flatten().collect() }),
            Err(e) => Err(de::Error::custom(format!("Could not parse MethodSignature: {:?}", e.to_string()))),
        }
    }
}

impl<'de> Deserialize<'de> for MethodSignature {
    fn deserialize<D>(deserializer: D) -> Result<MethodSignature, D::Error>
        where
            D: Deserializer<'de>,
    {
        deserializer.deserialize_str(MethodSignatureVisitor)
    }
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct AssemblyReference {
    name: String,
    version: Version,
    locale: String,
    public_key: PublicKeyToken,
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct PublicKeyToken(String);

impl FromStr for AssemblyReference {
    type Err = Error;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let mut parts: Vec<&str> = s
            .split(',')
            .map(|p| p.trim())
            .collect();

        if parts.len() != 4 {
            return Err(Error::InvalidAssemblyReference);
        }

        let name = parts.remove(0).to_string();
        let map: BTreeMap<&str, &str> = parts.iter()
            .map(|p| {
                let pp: Vec<&str> = p
                    .split('=')
                    .map(|pp| pp.trim())
                    .collect();
                (pp[0], pp[1])
            })
            .collect();

        let version = Version::parse(map["Version"])?;
        let locale = map["Culture"].to_string();
        let public_key = PublicKeyToken(map["PublicKeyToken"].to_string());
        Ok(AssemblyReference {
            name,
            version,
            locale,
            public_key
        })
    }
}

struct AssemblyReferenceVisitor;
impl<'de> Visitor<'de> for AssemblyReferenceVisitor {
    type Value = AssemblyReference;

    fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
        formatter.write_str("a string")
    }

    fn visit_str<E>(self, v: &str) -> Result<Self::Value, E>
        where
            E: de::Error,
    {
        AssemblyReference::from_str(v).map_err(|_| {
            de::Error::custom("Could not deserialize AssemblyReference")
        })
    }
}

impl<'de> Deserialize<'de> for AssemblyReference {
    fn deserialize<D>(deserializer: D) -> Result<AssemblyReference, D::Error>
        where
            D: Deserializer<'de>,
    {
        deserializer.deserialize_str(AssemblyReferenceVisitor)
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct CallerMethodReference {
    assembly: String,
    #[serde(rename = "type")]
    type_name: String,
    #[serde(rename = "method")]
    method_name: String
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct WrapperMethodReference {
    pub (crate) assembly: AssemblyReference,
    #[serde(rename = "type")]
    pub (crate) type_name: String,
    #[serde(rename = "method")]
    pub (crate) method_name: Option<String>,
    pub (crate) action: String,
    #[serde(rename = "signature")]
    pub (crate) method_signature: Option<MethodSignature>,
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct TargetMethodReference {
    assembly: String,
    #[serde(rename = "type")]
    type_name: String,
    #[serde(rename = "method")]
    method_name: String,
    minimum_major: u16,
    minimum_minor: u16,
    minimum_patch: u16,
    #[serde(default = "u16_max")]
    maximum_major: u16,
    #[serde(default = "u16_max")]
    maximum_minor: u16,
    #[serde(default = "u16_max")]
    maximum_patch: u16,
    signature_types: Option<Vec<String>>,
}

fn u16_max() -> u16 { u16::MAX }

impl TargetMethodReference {
    pub fn minimum_version(&self) -> Version {
        Version::new(self.minimum_major, self.minimum_minor, self.minimum_patch, 0)
    }

    pub fn maximum_version(&self) -> Version {
        Version::new(self.maximum_major, self.maximum_minor, self.maximum_patch, 0)
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct MethodReplacement {
    #[serde(deserialize_with = "empty_struct_is_none")]
    pub (crate) caller: Option<CallerMethodReference>,
    pub (crate) target: Option<TargetMethodReference>,
    pub (crate) wrapper: Option<WrapperMethodReference>,
}

fn empty_struct_is_none<'de, T, D>(deserializer: D) -> Result<Option<T>, D::Error>
    where
        T: DeserializeOwned,
        D: Deserializer<'de>,
{
    #[derive(Deserialize)]
    #[serde(untagged)]
    enum DataOrEmpty<T> {
        Data(T),
        Empty {},
    }

    match DataOrEmpty::deserialize(deserializer)? {
        DataOrEmpty::Data(data) => Ok(Some(data)),
        DataOrEmpty::Empty {} => Ok(None)
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize)]
pub struct IntegrationMethod {
    pub(crate) name: String,
    pub(crate) method_replacement: MethodReplacement
}

#[derive(Debug, Eq, PartialEq, Deserialize)]
pub struct Integration {
    pub(crate) name: String,
    pub(crate) method_replacements: Vec<MethodReplacement>
}

#[cfg(test)]
pub mod tests {
    use crate::profiler::types::{Integration, MethodSignature, AssemblyReference, PublicKeyToken};
    use std::fs::File;
    use std::error::Error;
    use crate::types::Version;

    #[test]
    fn deserialize_method_signature() -> Result<(), Box<dyn Error>> {
        let json = "\"00 08 1C 1C 1C 1C 1C 1C 08 08 0A\"";
        let method_signature: MethodSignature = serde_json::from_str(json)?;
        assert_eq!(MethodSignature{ data: vec![0,8,28,28,28,28,28,28,8,8,10] }, method_signature);
        Ok(())
    }

    #[test]
    fn deserialize_assembly_reference() -> Result<(), Box<dyn Error>> {
        let json = "\"Elastic.Apm, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22\"";
        let assembly_reference: AssemblyReference = serde_json::from_str(json)?;

        let expected_assembly_reference = AssemblyReference {
            name: "Elastic.Apm".into(),
            version: Version::new(1,9,0,0),
            locale: "neutral".into(),
            public_key: PublicKeyToken("ae7400d2c189cf22".into())
        };

        assert_eq!(expected_assembly_reference, assembly_reference);
        Ok(())
    }

    #[test]
    fn deserialize_integrations() -> Result<(), Box<dyn Error>> {
        let json = r#"[
              {
                "name": "AdoNet",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "System.Data",
                      "type": "System.Data.Common.DbCommand",
                      "method": "ExecuteNonQueryAsync",
                      "signature_types": [
                        "System.Threading.Tasks.Task`1<System.Int32>",
                        "System.Threading.CancellationToken"
                      ],
                      "minimum_major": 4,
                      "minimum_minor": 0,
                      "minimum_patch": 0,
                      "maximum_major": 4,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.AdoNet.CommandExecuteNonQueryAsyncIntegration",
                      "action": "CallTargetModification"
                    }
                  }
                ]
              },
              {
                "name": "AspNet",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "System.Web",
                      "type": "System.Web.Compilation.BuildManager",
                      "method": "InvokePreStartInitMethodsCore",
                      "signature_types": [
                        "System.Void",
                        "System.Collections.Generic.ICollection`1[System.Reflection.MethodInfo]",
                        "System.Func`1[System.IDisposable]"
                      ],
                      "minimum_major": 4,
                      "minimum_minor": 0,
                      "minimum_patch": 0,
                      "maximum_major": 4,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.AspNet.HttpModule_Integration",
                      "action": "CallTargetModification"
                    }
                  }
                ]
              },
              {
                "name": "XUnit",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "xunit.execution.desktop",
                      "type": "Xunit.Sdk.TestInvoker`1",
                      "method": "RunAsync",
                      "signature_types": [
                        "System.Threading.Tasks.Task`1<System.Decimal>"
                      ],
                      "minimum_major": 2,
                      "minimum_minor": 2,
                      "minimum_patch": 0,
                      "maximum_major": 2,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.Testing.XUnitIntegration",
                      "method": "TestInvoker_RunAsync",
                      "signature": "00 04 1C 1C 08 08 0A",
                      "action": "ReplaceTargetMethod"
                    }
                  }
                ]
              }
            ]"#;
        let integrations: Vec<Integration> = serde_json::from_str(json)?;

        assert_eq!(3, integrations.len());
        Ok(())
    }
}
