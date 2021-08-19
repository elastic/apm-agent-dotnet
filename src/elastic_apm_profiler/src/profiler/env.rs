// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    ffi::{COR_PRF_CLAUSE_TYPE::COR_PRF_CLAUSE_FILTER, E_FAIL},
    profiler::types::Integration,
};
use com::sys::HRESULT;
use log::LevelFilter;
use once_cell::sync::Lazy;
use std::{borrow::Borrow, ffi::OsStr, fs::File, io::BufReader, path::PathBuf, str::FromStr};

const ELASTIC_APM_PROFILER_INTEGRATIONS: &str = "ELASTIC_APM_PROFILER_INTEGRATIONS";
const ELASTIC_APM_PROFILER_LOG_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG";
const ELASTIC_APM_PROFILER_LOG_IL_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG_IL";
const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";
const ELASTIC_APM_PROFILER_ENABLE_INLINING: &str = "ELASTIC_APM_PROFILER_ENABLE_INLINING";
const ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS: &str =
    "ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS";

pub static ELASTIC_APM_PROFILER_LOG_IL: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_LOG_IL_ENV_VAR, false));

pub static ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR, true));

/// Gets the environment variables of interest
pub fn get_env_vars() -> String {
    std::env::vars()
        .filter_map(|(k, v)| {
            if k.starts_with("ELASTIC_") || k.starts_with("CORECLR_") || k.starts_with("COR_") {
                Some(format!("  {}=\"{}\"", k, v))
            } else {
                None
            }
        })
        .collect::<Vec<_>>()
        .join("\n")
}

/// Gets the path to the profiler file on windows
#[cfg(target_os = "windows")]
pub fn get_native_profiler_file() -> Result<String, HRESULT> {
    Ok("elastic_apm_profiler.dll".into())
}

/// Gets the path to the profiler file on non windows
#[cfg(not(target_os = "windows"))]
pub fn get_native_profiler_file() -> Result<String, HRESULT> {
    let env_var = if cfg!(target_pointer_width = "64") {
        "CORECLR_PROFILER_PATH_64"
    } else {
        "CORECLR_PROFILER_PATH_32"
    };
    match std::env::var(env_var) {
        Ok(v) => Ok(v),
        Err(_) => std::env::var("CORECLR_PROFILER_PATH").map_err(|e| {
            log::warn!(
                "problem getting env var CORECLR_PROFILER_PATH: {}",
                e.to_string()
            );
            E_FAIL
        }),
    }
}

pub fn disable_optimizations() -> bool {
    read_bool_env_var(ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS, false)
}

pub fn enable_inlining(default: bool) -> bool {
    read_bool_env_var(ELASTIC_APM_PROFILER_ENABLE_INLINING, default)
}

pub fn read_log_level_from_env_var(default: LevelFilter) -> LevelFilter {
    match std::env::var(ELASTIC_APM_PROFILER_LOG_ENV_VAR) {
        Ok(level) => match level.to_lowercase().as_str() {
            "trace" => log::LevelFilter::Trace,
            "debug" => log::LevelFilter::Debug,
            "info" => log::LevelFilter::Info,
            "warn" => log::LevelFilter::Warn,
            _ => log::LevelFilter::Error,
        },
        _ => default,
    }
}

fn read_bool_env_var(key: &str, default: bool) -> bool {
    match std::env::var(key) {
        Ok(enabled) => match enabled.as_str() {
            "true" => true,
            "True" => true,
            "TRUE" => true,
            "1" => true,
            "false" => false,
            "False" => false,
            "FALSE" => false,
            "0" => false,
            v => {
                log::info!("Unknown value for {}: {}. Setting to {}", key, v, default);
                default
            }
        },
        Err(e) => {
            log::info!(
                "Problem reading {}: {}. Setting to {}",
                key,
                e.to_string(),
                default
            );
            default
        }
    }
}

/// Loads the integrations by reading the file pointed to by [ELASTIC_APM_PROFILER_INTEGRATIONS]
/// environment variable
pub fn load_integrations() -> Result<Vec<Integration>, HRESULT> {
    let path = std::env::var(ELASTIC_APM_PROFILER_INTEGRATIONS).map_err(|e| {
        log::warn!(
            "Problem reading {} environment variable: {}. profiler is disabled.",
            ELASTIC_APM_PROFILER_INTEGRATIONS,
            e.to_string()
        );
        E_FAIL
    })?;

    let file = File::open(&path).map_err(|e| {
        log::warn!(
            "Problem reading integrations file {}: {}. profiler is disabled.",
            &path,
            e.to_string()
        );
        E_FAIL
    })?;

    let path_buf = PathBuf::from_str(&path).map_err(|e| {
        log::warn!(
            "Problem reading path buf from integrations file path: {}",
            e.to_string()
        );
        E_FAIL
    })?;
    let reader = BufReader::new(file);
    let extension = path_buf
        .extension()
        .unwrap_or_else(|| OsStr::new("json"))
        .to_string_lossy()
        .to_string()
        .to_lowercase();

    let integrations = match extension.as_str() {
        "yml" | "yaml" => serde_yaml::from_reader(reader).map_err(|e| {
            log::warn!(
                "Problem reading integrations file {}: {}. profiler is disabled.",
                &path,
                e.to_string()
            );
            E_FAIL
        })?,
        "json" => serde_json::from_reader(reader).map_err(|e| {
            log::warn!(
                "Problem reading integrations file {}: {}. profiler is disabled.",
                &path,
                e.to_string()
            );
            E_FAIL
        })?,
        p => {
            log::warn!("Problem reading integrations file {}: Unknown file extension {}. profiler is disabled.", &path, p);
            return Err(E_FAIL);
        }
    };

    Ok(integrations)
}
