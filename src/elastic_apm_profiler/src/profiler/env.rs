use crate::{ffi::E_FAIL, profiler::types::Integration};
use com::sys::HRESULT;
use std::{fs::File, io::BufReader};

const ELASTIC_APM_PROFILER_INTEGRATIONS: &'static str = "ELASTIC_APM_PROFILER_INTEGRATIONS";
const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: &'static str =
    "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";

/// Reads the [ELASTIC_APM_PROFILER_CALLTARGET_ENABLED] environment variable value to
/// determine if calltarget is enabled
pub fn read_calltarget_env_var() -> bool {
    match std::env::var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED) {
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
                log::info!(
                    "Unknown value for {}: {}. Setting to true",
                    ELASTIC_APM_PROFILER_CALLTARGET_ENABLED,
                    v
                );
                true
            }
        },
        Err(e) => {
            log::info!(
                "Problem reading {}: {}. Setting to true",
                ELASTIC_APM_PROFILER_CALLTARGET_ENABLED,
                e.to_string()
            );
            true
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
