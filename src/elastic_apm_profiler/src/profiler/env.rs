// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{ffi::E_FAIL, profiler::types::Integration};
use com::sys::HRESULT;
use log::LevelFilter;
use log4rs::{
    append::{
        console::ConsoleAppender,
        rolling_file::{
            policy::compound::{
                roll::fixed_window::FixedWindowRoller, trigger::size::SizeTrigger, CompoundPolicy,
            },
            RollingFileAppender,
        },
    },
    config::{Appender, Root},
    encode::pattern::PatternEncoder,
    Config, Handle,
};
use once_cell::sync::Lazy;
use std::time::SystemTime;
use std::{collections::HashSet, fs::File, io::BufReader, path::PathBuf, str::FromStr};

const APP_POOL_ID_ENV_VAR: &str = "APP_POOL_ID";
const DOTNET_CLI_TELEMETRY_PROFILE_ENV_VAR: &str = "DOTNET_CLI_TELEMETRY_PROFILE";
const COMPLUS_LOADEROPTIMIZATION: &str = "COMPLUS_LOADEROPTIMIZATION";

const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";
const ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS";
const ELASTIC_APM_PROFILER_ENABLE_INLINING_ENV_VAR: &str = "ELASTIC_APM_PROFILER_ENABLE_INLINING";
const ELASTIC_APM_PROFILER_EXCLUDE_INTEGRATIONS_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_EXCLUDE_INTEGRATIONS";
const ELASTIC_APM_PROFILER_EXCLUDE_PROCESSES_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_EXCLUDE_PROCESSES";
const ELASTIC_APM_PROFILER_EXCLUDE_SERVICE_NAMES_ENV_VAR: &str =
    "ELASTIC_APM_PROFILER_EXCLUDE_SERVICE_NAMES";
const ELASTIC_APM_PROFILER_HOME_ENV_VAR: &str = "ELASTIC_APM_PROFILER_HOME";
const ELASTIC_APM_PROFILER_INTEGRATIONS_ENV_VAR: &str = "ELASTIC_APM_PROFILER_INTEGRATIONS";
const ELASTIC_APM_PROFILER_LOG_DIR_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG_DIR";
const ELASTIC_APM_PROFILER_LOG_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG";
const ELASTIC_APM_PROFILER_LOG_TARGETS_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG_TARGETS";
const ELASTIC_APM_PROFILER_LOG_IL_ENV_VAR: &str = "ELASTIC_APM_PROFILER_LOG_IL";

const ELASTIC_APM_SERVICE_NAME_ENV_VAR: &str = "ELASTIC_APM_SERVICE_NAME";

pub static ELASTIC_APM_PROFILER_LOG_IL: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_LOG_IL_ENV_VAR, false));

pub static ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: Lazy<bool> =
    Lazy::new(|| read_bool_env_var(ELASTIC_APM_PROFILER_CALLTARGET_ENABLED_ENV_VAR, true));

pub static IS_AZURE_APP_SERVICE: Lazy<bool> = Lazy::new(|| {
    std::env::var("WEBSITE_SITE_NAME").is_ok()
        && std::env::var("WEBSITE_OWNER_NAME").is_ok()
        && std::env::var("WEBSITE_RESOURCE_GROUP").is_ok()
        && std::env::var("WEBSITE_INSTANCE_ID").is_ok()
});

/// Checks if the profiler is running in Azure App Service, in an infrastructure or
/// reserved process, and returns an error if so
pub fn check_if_running_in_azure_app_service() -> Result<(), HRESULT> {
    if *IS_AZURE_APP_SERVICE {
        log::info!("Initialize: detected Azure App Service context");
        if let Ok(app_pool_id) = std::env::var(APP_POOL_ID_ENV_VAR) {
            if app_pool_id.starts_with('~') {
                log::info!(
                    "Initialize: {} environment variable value {} suggests \
                    this is an Azure App Service infrastructure process. Profiler disabled",
                    APP_POOL_ID_ENV_VAR,
                    app_pool_id
                );
                return Err(E_FAIL);
            }
        }

        if let Ok(cli_telemetry) = std::env::var(DOTNET_CLI_TELEMETRY_PROFILE_ENV_VAR) {
            if &cli_telemetry == "AzureKudu" {
                log::info!(
                    "Initialize: {} environment variable value {} suggests \
                    this is an Azure App Service reserved process. Profiler disabled",
                    DOTNET_CLI_TELEMETRY_PROFILE_ENV_VAR,
                    cli_telemetry
                );
                return Err(E_FAIL);
            }
        }
    }

    Ok(())
}

/// Gets the environment variables of interest
pub fn get_env_vars() -> String {
    std::env::vars()
        .filter_map(|(k, v)| {
            let key = k.to_uppercase();
            if key.starts_with("ELASTIC_")
                || key.starts_with("CORECLR_")
                || key.starts_with("COR_")
                || key == APP_POOL_ID_ENV_VAR
                || key == DOTNET_CLI_TELEMETRY_PROFILE_ENV_VAR
                || key == COMPLUS_LOADEROPTIMIZATION
            {
                let value = if key.contains("SECRET") || key.contains("API_KEY") {
                    "[REDACTED]"
                } else {
                    &v
                };
                Some(format!("  {}=\"{}\"", k, value))
            } else {
                None
            }
        })
        .collect::<Vec<_>>()
        .join("\n")
}

fn read_semicolon_separated_env_var(key: &str) -> Option<Vec<String>> {
    match std::env::var(key) {
        Ok(val) => Some(val.split(';').map(|s| s.to_string()).collect()),
        Err(_) => None,
    }
}

pub fn get_exclude_processes() -> Option<Vec<String>> {
    read_semicolon_separated_env_var(ELASTIC_APM_PROFILER_EXCLUDE_PROCESSES_ENV_VAR)
}

pub fn get_exclude_service_names() -> Option<Vec<String>> {
    read_semicolon_separated_env_var(ELASTIC_APM_PROFILER_EXCLUDE_SERVICE_NAMES_ENV_VAR)
}

pub fn get_service_name() -> Option<String> {
    std::env::var(ELASTIC_APM_SERVICE_NAME_ENV_VAR).ok()
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
    read_bool_env_var(ELASTIC_APM_PROFILER_DISABLE_OPTIMIZATIONS_ENV_VAR, false)
}

pub fn enable_inlining(default: bool) -> bool {
    read_bool_env_var(ELASTIC_APM_PROFILER_ENABLE_INLINING_ENV_VAR, default)
}

fn read_log_targets_from_env_var() -> HashSet<String> {
    let mut set = match std::env::var(ELASTIC_APM_PROFILER_LOG_TARGETS_ENV_VAR) {
        Ok(value) => value
            .split(';')
            .into_iter()
            .filter_map(|s| match s.to_lowercase().as_str() {
                out if out == "file" || out == "stdout" => Some(out.into()),
                _ => None,
            })
            .collect(),
        _ => HashSet::with_capacity(1),
    };

    if set.is_empty() {
        set.insert("file".into());
    }
    set
}

pub fn read_log_level_from_env_var(default: LevelFilter) -> LevelFilter {
    match std::env::var(ELASTIC_APM_PROFILER_LOG_ENV_VAR) {
        Ok(value) => LevelFilter::from_str(value.as_str()).unwrap_or(default),
        _ => default,
    }
}

fn read_bool_env_var(key: &str, default: bool) -> bool {
    match std::env::var(key) {
        Ok(enabled) => match enabled.to_lowercase().as_str() {
            "true" | "1" => true,
            "false" | "0" => false,
            _ => {
                log::warn!(
                    "Unknown value for {}: {}. Setting to {}",
                    key,
                    enabled,
                    default
                );
                default
            }
        },
        Err(e) => {
            log::debug!(
                "Problem reading {}: {}. Setting to {}",
                key,
                e.to_string(),
                default
            );
            default
        }
    }
}

/// get the profiler directory
fn get_profiler_dir() -> String {
    let env_var = if cfg!(target_pointer_width = "64") {
        "CORECLR_PROFILER_PATH_64"
    } else {
        "CORECLR_PROFILER_PATH_32"
    };

    match std::env::var(env_var) {
        Ok(v) => v,
        Err(_) => match std::env::var("CORECLR_PROFILER_PATH") {
            Ok(v) => v,
            Err(_) => {
                // try .NET Framework env vars
                let env_var = if cfg!(target_pointer_width = "64") {
                    "COR_PROFILER_PATH_64"
                } else {
                    "COR_PROFILER_PATH_32"
                };

                match std::env::var(env_var) {
                    Ok(v) => v,
                    Err(_) => std::env::var("COR_PROFILER_PATH").unwrap_or_else(|_| String::new()),
                }
            }
        },
    }
}

/// Gets the default log directory on Windows
#[cfg(target_os = "windows")]
fn get_default_log_dir() -> PathBuf {
    // ideally we would use the windows function SHGetKnownFolderPath to get
    // the CommonApplicationData special folder. However, this requires a few package dependencies
    // like winapi that would increase the size of the profiler binary. Instead,
    // use the %PROGRAMDATA% environment variable if it exists
    match std::env::var("PROGRAMDATA") {
        Ok(path) => {
            let mut path_buf = PathBuf::from(path);
            path_buf.push("elastic");
            path_buf.push("apm-agent-dotnet");
            path_buf.push("logs");
            path_buf
        }
        Err(_) => get_home_log_dir(),
    }
}

/// Gets the path to the profiler file on non windows
#[cfg(not(target_os = "windows"))]
fn get_default_log_dir() -> PathBuf {
    PathBuf::from_str("/var/log/elastic/apm-agent-dotnet").unwrap()
}

fn get_home_log_dir() -> PathBuf {
    let mut path_buf = match std::env::var(ELASTIC_APM_PROFILER_HOME_ENV_VAR) {
        Ok(val) => PathBuf::from(val),
        Err(_) => PathBuf::from(get_profiler_dir()),
    };

    path_buf.push("logs");
    path_buf
}

fn get_log_dir() -> PathBuf {
    match std::env::var(ELASTIC_APM_PROFILER_LOG_DIR_ENV_VAR) {
        Ok(path) => PathBuf::from(path),
        Err(_) => get_default_log_dir(),
    }
}

fn get_sys_time_in_seconds() -> u64 {
    match SystemTime::now().duration_since(SystemTime::UNIX_EPOCH) {
        Ok(n) => n.as_secs(),
        Err(_) => 0,
    }
}

pub fn initialize_logging(process_name: &str) -> Option<Handle> {
    let targets = read_log_targets_from_env_var();
    let level = read_log_level_from_env_var(LevelFilter::Warn);
    let mut root_builder = Root::builder();
    let mut config_builder = Config::builder();
    let log_pattern = "[{d(%Y-%m-%dT%H:%M:%S.%f%:z)}] [{l:<5}] {m}{n}";

    if targets.contains("stdout") {
        let pattern = PatternEncoder::new(log_pattern);
        let stdout = ConsoleAppender::builder()
            .encoder(Box::new(pattern))
            .build();
        config_builder =
            config_builder.appender(Appender::builder().build("stdout", Box::new(stdout)));
        root_builder = root_builder.appender("stdout");
    }

    if targets.contains("file") {
        let pid = std::process::id();
        let timestamp = get_sys_time_in_seconds();
        let mut log_dir = get_log_dir();
        let mut valid_log_dir = true;

        // try to create the log directory ahead of time so that we can determine if it's a valid
        // directory. if the directory can't be created, try the default log directory or home directory
        // before bailing and not setting up the file logger.
        if std::fs::create_dir_all(&log_dir).is_err() {
            let default_log_dir = get_default_log_dir();
            if log_dir != default_log_dir {
                log_dir = default_log_dir;
                if std::fs::create_dir_all(&log_dir).is_err() {
                    let home_log_dir = get_home_log_dir();
                    if log_dir != home_log_dir {
                        log_dir = home_log_dir;
                        if std::fs::create_dir_all(&log_dir).is_err() {
                            valid_log_dir = false;
                        }
                    } else {
                        valid_log_dir = false;
                    }
                }
            } else {
                log_dir = get_home_log_dir();
                if std::fs::create_dir_all(&log_dir).is_err() {
                    valid_log_dir = false;
                }
            }
        }

        if valid_log_dir {
            let log_file_name = log_dir
                .join(format!(
                    "elastic_apm_profiler_{}_{}_{}.log",
                    process_name, pid, timestamp
                ))
                .to_string_lossy()
                .to_string();
            let rolling_log_file_name = log_dir
                .join(format!(
                    "elastic_apm_profiler_{}_{}_{}_{{}}.log",
                    process_name, pid, timestamp
                ))
                .to_string_lossy()
                .to_string();

            let trigger = SizeTrigger::new(5 * 1024 * 1024);
            let roller_result = FixedWindowRoller::builder().build(&rolling_log_file_name, 10);
            if let Ok(roller) = roller_result {
                let policy = CompoundPolicy::new(Box::new(trigger), Box::new(roller));
                let pattern = PatternEncoder::new(log_pattern);
                let file_result = RollingFileAppender::builder()
                    .append(true)
                    .encoder(Box::new(pattern))
                    .build(&log_file_name, Box::new(policy));
                if let Ok(file) = file_result {
                    config_builder =
                        config_builder.appender(Appender::builder().build("file", Box::new(file)));
                    root_builder = root_builder.appender("file");
                }
            }
        }
    }

    let root = root_builder.build(level);
    let config = config_builder.build(root);
    return match config {
        Ok(c) => log4rs::init_config(c).ok(),
        Err(_) => None,
    };
}

/// Loads the integrations by reading the yml file pointed to
/// by [ELASTIC_APM_PROFILER_INTEGRATIONS] environment variable, filtering
/// integrations by [ELASTIC_APM_PROFILER_EXCLUDE_INTEGRATIONS_ENV_VAR] environment variable,
/// if present
pub fn load_integrations() -> Result<Vec<Integration>, HRESULT> {
    let path = match std::env::var(ELASTIC_APM_PROFILER_INTEGRATIONS_ENV_VAR) {
        Ok(val) => val,
        Err(e) => {
            log::debug!(
                "problem reading {} environment variable: {}. trying integrations.yml in directory of {} environment variable value",
                ELASTIC_APM_PROFILER_INTEGRATIONS_ENV_VAR,
                e.to_string(),
                ELASTIC_APM_PROFILER_HOME_ENV_VAR
            );

            match std::env::var(ELASTIC_APM_PROFILER_HOME_ENV_VAR) {
                Ok(val) => {
                    let mut path_buf = PathBuf::from(val);
                    path_buf.push("integrations.yml");
                    path_buf.to_string_lossy().to_string()
                }
                Err(e) => {
                    log::warn!(
                        "problem reading {} environment variable: {}. profiler disabled",
                        ELASTIC_APM_PROFILER_HOME_ENV_VAR,
                        e.to_string(),
                    );
                    return Err(E_FAIL);
                }
            }
        }
    };

    let file = File::open(&path).map_err(|e| {
        log::warn!(
            "problem reading integrations file {}: {}. profiler is disabled.",
            &path,
            e.to_string()
        );
        E_FAIL
    })?;

    let reader = BufReader::new(file);
    let mut integrations: Vec<Integration> = serde_yaml::from_reader(reader).map_err(|e| {
        log::warn!(
            "problem reading integrations file {}: {}. profiler is disabled.",
            &path,
            e.to_string()
        );
        E_FAIL
    })?;

    log::trace!(
        "loaded {} integration(s) from {}",
        integrations.len(),
        &path
    );

    // Now filter integrations
    if let Ok(val) = std::env::var(ELASTIC_APM_PROFILER_EXCLUDE_INTEGRATIONS_ENV_VAR) {
        let exclude_integrations = val.split(';');
        for exclude_integration in exclude_integrations {
            log::trace!("exclude integrations that match {}", exclude_integration);
            integrations.retain(|i| i.name.to_lowercase() != exclude_integration.to_lowercase());
        }
    };

    Ok(integrations)
}
