use rust_embed::RustEmbed;

/// Embedded assets of the managed loader
#[derive(RustEmbed)]
#[folder = "../Elastic.Apm.Profiler.Managed.Loader/bin/Release"]
#[prefix = ""]
pub(crate) struct ManagedLoader;
