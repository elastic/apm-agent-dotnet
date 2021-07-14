use crate::{ffi::E_FAIL, profiler::types::Integration};
use com::sys::HRESULT;
use once_cell::sync::Lazy;
use std::{fs::File, io::BufReader};

const ELASTIC_APM_PROFILER_INTEGRATIONS: &str = "ELASTIC_APM_PROFILER_INTEGRATIONS";

const ELASTIC_APM_PROFILER_DISPLAY_IL_ENV_VAR: &str = "ELASTIC_APM_PROFILER_DISPLAY_IL";
const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";

pub static ELASTIC_APM_PROFILER_DISPLAY_IL: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_DISPLAY_IL_ENV_VAR, false));

pub static ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR, true));

/// Reads the [ELASTIC_APM_PROFILER_CALLTARGET_ENABLED] environment variable value to
/// determine if calltarget is enabled\
fn read_calltarget_env_var() -> bool {
    read_bool_env_var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR, true)
}

fn read_display_il() -> bool {
    read_bool_env_var(ELASTIC_APM_PROFILER_DISPLAY_IL_ENV_VAR, false)
}

/// Gets the path to the profiler file
pub fn get_native_profiler_file() -> Result<String, HRESULT> {
    if cfg!(target_os = "linux") {
        let env_var = if cfg!(target_pointer_width = "64") {
            "CORECLR_PROFILER_PATH_64"
        } else {
            "CORECLR_PROFILER_PATH_32"
        };
        match std::env::var(env_var) {
            Ok(v) => {
                log::debug!("env var {}: {}", env_var, &v);
                Ok(v)
            }
            Err(_) => std::env::var("CORECLR_PROFILER_PATH").map_err(|e| {
                log::warn!(
                    "problem getting env var CORECLR_PROFILER_PATH: {}",
                    e.to_string()
                );
                E_FAIL
            }),
        }
    } else {
        Ok("elastic_apm_profiler.dll".into())
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

    let reader = BufReader::new(file);
    let integrations = serde_json::from_reader(reader).map_err(|e| {
        log::warn!(
            "Problem reading integrations file {}: {}. profiler is disabled.",
            &path,
            e.to_string()
        );
        E_FAIL
    })?;

    Ok(integrations)
}
