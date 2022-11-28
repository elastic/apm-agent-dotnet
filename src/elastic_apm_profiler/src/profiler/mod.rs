// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    ffi::{types::RuntimeInfo, *},
    interfaces::{
        ICorProfilerAssemblyReferenceProvider, ICorProfilerCallback, ICorProfilerCallback2,
        ICorProfilerCallback3, ICorProfilerCallback4, ICorProfilerCallback5, ICorProfilerCallback6,
        ICorProfilerCallback7, ICorProfilerCallback8, ICorProfilerCallback9,
        ICorProfilerFunctionControl, ICorProfilerInfo4, ICorProfilerInfo5, IMetaDataAssemblyEmit,
        IMetaDataAssemblyImport, IMetaDataEmit2, IMetaDataImport2,
    },
    profiler::{
        calltarget_tokens::CallTargetTokens,
        helpers::flatten_integrations,
        managed::{
            IGNORE, MANAGED_PROFILER_ASSEMBLY, MANAGED_PROFILER_ASSEMBLY_LOADER,
            MANAGED_PROFILER_FULL_ASSEMBLY_VERSION,
        },
        rejit::RejitHandler,
        sig::get_sig_type_token_name,
        types::{
            IntegrationMethod, MethodReplacement, ModuleMetadata, ModuleWrapperTokens,
            WrapperMethodAction,
        },
    },
};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, GUID, HRESULT, S_OK},
};
use log::Level;
use log4rs::Handle;
use once_cell::sync::Lazy;
use std::{
    cell::RefCell,
    collections::{HashMap, HashSet},
    ffi::c_void,
    ops::Deref,
    path::PathBuf,
    sync::{
        atomic::{AtomicBool, AtomicUsize, Ordering},
        Mutex, RwLock,
    },
};
use types::{AssemblyMetaData, FunctionInfo, Version};
use widestring::{U16CStr, U16CString};

mod calltarget_tokens;
pub mod env;
mod helpers;
pub mod managed;
mod process;
mod rejit;
pub mod sig;
mod startup_hook;
pub mod types;

const SKIP_ASSEMBLY_PREFIXES: [&str; 22] = [
    "Elastic.Apm",
    "MessagePack",
    "Microsoft.AI",
    "Microsoft.ApplicationInsights",
    "Microsoft.Build",
    "Microsoft.CSharp",
    "Microsoft.Extensions",
    "Microsoft.Web.Compilation.Snapshots",
    "Sigil",
    "System.Core",
    "System.Console",
    "System.Collections",
    "System.ComponentModel",
    "System.Diagnostics",
    "System.Drawing",
    "System.EnterpriseServices",
    "System.IO",
    "System.Runtime",
    "System.Text",
    "System.Threading",
    "System.Xml",
    "Newtonsoft",
];
const SKIP_ASSEMBLIES: [&str; 7] = [
    "mscorlib",
    "netstandard",
    "System.Configuration",
    "Microsoft.AspNetCore.Razor.Language",
    "Microsoft.AspNetCore.Mvc.RazorPages",
    "Anonymously Hosted DynamicMethods Assembly",
    "ISymWrapper",
];

/// The git hash defined on build
static GIT_HASH: &str = env!("GIT_HASH");

/// The profiler package version
static PROFILER_PACKAGE_VERSION: &str = env!("CARGO_PKG_VERSION");

/// The profiler version. Must match the managed assembly version
pub static PROFILER_VERSION: Lazy<Version> = Lazy::new(|| {
    let major = env!("CARGO_PKG_VERSION_MAJOR").parse::<u16>().unwrap();
    let minor = env!("CARGO_PKG_VERSION_MINOR").parse::<u16>().unwrap();
    let patch = env!("CARGO_PKG_VERSION_PATCH").parse::<u16>().unwrap();
    Version::new(major, minor, patch, 0)
});

/// Whether the managed profiler has been loaded as domain-neutral i.e.
/// into the shared domain, which can be shared code among other app domains
static MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL: AtomicBool = AtomicBool::new(false);

/// Tracks the app domain ids into which the managed profiler assembly has been loaded
static MANAGED_PROFILER_LOADED_APP_DOMAINS: Lazy<Mutex<HashSet<AppDomainID>>> =
    Lazy::new(|| Mutex::new(HashSet::new()));

/// Indicates whether the profiler is attached
pub(crate) static IS_ATTACHED: AtomicBool = AtomicBool::new(false);
/// Indicates whether the profiler is running in a Desktop CLR
pub(crate) static IS_DESKTOP_CLR: AtomicBool = AtomicBool::new(false);

class! {
    /// The profiler implementation
    pub class Profiler:
        ICorProfilerCallback9(ICorProfilerCallback8(ICorProfilerCallback7(
            ICorProfilerCallback6(ICorProfilerCallback5(ICorProfilerCallback4(
                ICorProfilerCallback3(ICorProfilerCallback2(ICorProfilerCallback)))))))) {
        logger: RefCell<Option<Handle>>,
        profiler_info: RefCell<Option<ICorProfilerInfo4>>,
        rejit_handler: RefCell<Option<RejitHandler>>,
        runtime_info: RefCell<Option<RuntimeInfo>>,
        modules: Mutex<HashMap<ModuleID, ModuleMetadata>>,
        module_wrapper_tokens: Mutex<HashMap<ModuleID, ModuleWrapperTokens>>,
        call_target_tokens: RefCell<HashMap<ModuleID, CallTargetTokens>>,
        cor_assembly_property: RefCell<Option<AssemblyMetaData>>,
        cor_lib_module_loaded: AtomicBool,
        cor_app_domain_id: AtomicUsize,
        is_desktop_iis: AtomicBool,
        integration_methods: RwLock<Vec<IntegrationMethod>>,
        first_jit_compilation_app_domains: RwLock<HashSet<AppDomainID>>,
    }

    impl ICorProfilerCallback for Profiler {
        pub fn Initialize(
             &self,
            pICorProfilerInfoUnk: IUnknown,
        ) -> HRESULT {
            match self.initialize(pICorProfilerInfoUnk) {
                Ok(_) => S_OK,
                Err(hr) => hr
            }
        }
        pub fn Shutdown(&self) -> HRESULT {
            match self.shutdown() {
                Ok(_) => S_OK,
                Err(_) => S_OK
            }
        }
        pub fn AppDomainCreationStarted(&self, appDomainId: AppDomainID) -> HRESULT { S_OK }
        pub fn AppDomainCreationFinished(
             &self,
            appDomainId: AppDomainID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn AppDomainShutdownStarted(&self, appDomainId: AppDomainID) -> HRESULT { S_OK }
        pub fn AppDomainShutdownFinished(
             &self,
            appDomainId: AppDomainID,
            hrStatus: HRESULT,
        ) -> HRESULT {
            self.app_domain_shutdown_finished(appDomainId, hrStatus);
            S_OK
        }
        pub fn AssemblyLoadStarted(&self, assemblyId: AssemblyID) -> HRESULT { S_OK }
        pub fn AssemblyLoadFinished(
            &self,
            assemblyId: AssemblyID,
            hrStatus: HRESULT,
        ) -> HRESULT {
            match self.assembly_load_finished(assemblyId, hrStatus) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn AssemblyUnloadStarted(&self, assemblyId: AssemblyID) -> HRESULT { S_OK }
        pub fn AssemblyUnloadFinished(
             &self,
            assemblyId: AssemblyID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
        pub fn ModuleLoadStarted(&self, moduleId: ModuleID) -> HRESULT { S_OK }
        pub fn ModuleLoadFinished(&self, moduleId: ModuleID, hrStatus: HRESULT) -> HRESULT {
            match self.module_load_finished(moduleId, hrStatus) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn ModuleUnloadStarted(&self, moduleId: ModuleID) -> HRESULT {
            match self.module_unload_started(moduleId) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn ModuleUnloadFinished(&self, moduleId: ModuleID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn ModuleAttachedToAssembly(
             &self,
            moduleId: ModuleID,
            AssemblyId: AssemblyID,
        ) -> HRESULT { S_OK }
        pub fn ClassLoadStarted(&self, classId: ClassID) -> HRESULT { S_OK }
        pub fn ClassLoadFinished(&self, classId: ClassID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn ClassUnloadStarted(&self, classId: ClassID) -> HRESULT { S_OK }
        pub fn ClassUnloadFinished(&self, classId: ClassID, hrStatus: HRESULT) -> HRESULT { S_OK }
        pub fn FunctionUnloadStarted(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn JITCompilationStarted(
             &self,
            functionId: FunctionID,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT {
            match self.jit_compilation_started(functionId, fIsSafeToBlock) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn JITCompilationFinished(
             &self,
            functionId: FunctionID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn JITCachedFunctionSearchStarted(
             &self,
            functionId: FunctionID,
            pbUseCachedFunction: *mut BOOL,
        ) -> HRESULT { S_OK }
        pub fn JITCachedFunctionSearchFinished(
             &self,
            functionId: FunctionID,
            result: COR_PRF_JIT_CACHE,
        ) -> HRESULT { S_OK }
        pub fn JITFunctionPitched(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn JITInlining(
             &self,
            callerId: FunctionID,
            calleeId: FunctionID,
            pfShouldInline: *mut BOOL,
        ) -> HRESULT { S_OK }
        pub fn ThreadCreated(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn ThreadDestroyed(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn ThreadAssignedToOSThread(
             &self,
            managedThreadId: ThreadID,
            osThreadId: DWORD,
        ) -> HRESULT { S_OK }
        pub fn RemotingClientInvocationStarted(&self) -> HRESULT { S_OK }
        pub fn RemotingClientSendingMessage(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingClientReceivingReply(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingClientInvocationFinished(&self) -> HRESULT { S_OK }
        pub fn RemotingServerReceivingMessage(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn RemotingServerInvocationStarted(&self) -> HRESULT { S_OK }
        pub fn RemotingServerInvocationReturned(&self) -> HRESULT { S_OK }
        pub fn RemotingServerSendingReply(&self, pCookie: *const GUID, fIsAsync: BOOL) -> HRESULT { S_OK }
        pub fn UnmanagedToManagedTransition(
             &self,
            functionId: FunctionID,
            reason: COR_PRF_TRANSITION_REASON,
        ) -> HRESULT { S_OK }
        pub fn ManagedToUnmanagedTransition(
             &self,
            functionId: FunctionID,
            reason: COR_PRF_TRANSITION_REASON,
        ) -> HRESULT { S_OK }
        pub fn RuntimeSuspendStarted(&self, suspendReason: COR_PRF_SUSPEND_REASON) -> HRESULT { S_OK }
        pub fn RuntimeSuspendFinished(&self) -> HRESULT { S_OK }
        pub fn RuntimeSuspendAborted(&self) -> HRESULT { S_OK }
        pub fn RuntimeResumeStarted(&self) -> HRESULT { S_OK }
        pub fn RuntimeResumeFinished(&self) -> HRESULT { S_OK }
        pub fn RuntimeThreadSuspended(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn RuntimeThreadResumed(&self, threadId: ThreadID) -> HRESULT { S_OK }
        pub fn MovedReferences(
             &self,
            cMovedObjectIDRanges: ULONG,
            oldObjectIDRangeStart: *const ObjectID,
            newObjectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn ObjectAllocated(&self, objectId: ObjectID, classId: ClassID) -> HRESULT { S_OK }
        pub fn ObjectsAllocatedByClass(
             &self,
            cClassCount: ULONG,
            classIds: *const ClassID,
            cObjects: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn ObjectReferences(
             &self,
            objectId: ObjectID,
            classId: ClassID,
            cObjectRefs: ULONG,
            objectRefIds: *const ObjectID,
        ) -> HRESULT { S_OK }
        pub fn RootReferences(
             &self,
            cRootRefs: ULONG,
            rootRefIds: *const ObjectID,
        ) -> HRESULT { S_OK }
        pub fn ExceptionThrown(&self, thrownObjectId: ObjectID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFunctionEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFunctionLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionSearchFilterEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionSearchFilterLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionSearchCatcherFound(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionOSHandlerEnter(&self, __unused: UINT_PTR) -> HRESULT { S_OK }
        pub fn ExceptionOSHandlerLeave(&self, __unused: UINT_PTR) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFunctionEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFunctionLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFinallyEnter(&self, functionId: FunctionID) -> HRESULT { S_OK }
        pub fn ExceptionUnwindFinallyLeave(&self) -> HRESULT { S_OK }
        pub fn ExceptionCatcherEnter(
             &self,
            functionId: FunctionID,
            objectId: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn ExceptionCatcherLeave(&self) -> HRESULT { S_OK }
        pub fn COMClassicVTableCreated(
             &self,
            wrappedClassId: ClassID,
            implementedIID: REFGUID,
            pVTable: *const c_void,
            cSlots: ULONG,
        ) -> HRESULT { S_OK }
        pub fn COMClassicVTableDestroyed(
             &self,
            wrappedClassId: ClassID,
            implementedIID: REFGUID,
            pVTable: *const c_void,
        ) -> HRESULT { S_OK }
        pub fn ExceptionCLRCatcherFound(&self) -> HRESULT { S_OK }
        pub fn ExceptionCLRCatcherExecute(&self) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback2 for Profiler {
        pub fn ThreadNameChanged(&self,
            threadId: ThreadID,
            cchName: ULONG,
            name: *const WCHAR,
        ) -> HRESULT { S_OK }
        pub fn GarbageCollectionStarted(&self,
            cGenerations: int,
            generationCollected: *const BOOL,
            reason: COR_PRF_GC_REASON,
        ) -> HRESULT { S_OK }
        pub fn SurvivingReferences(&self,
            cSurvivingObjectIDRanges: ULONG,
            objectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const ULONG,
        ) -> HRESULT { S_OK }
        pub fn GarbageCollectionFinished(&self) -> HRESULT { S_OK }
        pub fn FinalizeableObjectQueued(&self,
            finalizerFlags: DWORD,
            objectID: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn RootReferences2(&self,
            cRootRefs: ULONG,
            rootRefIds: *const ObjectID,
            rootKinds: *const COR_PRF_GC_ROOT_KIND,
            rootFlags: *const COR_PRF_GC_ROOT_FLAGS,
            rootIds: *const UINT_PTR,
        ) -> HRESULT { S_OK }
        pub fn HandleCreated(&self,
            handleId: GCHandleID,
            initialObjectId: ObjectID,
        ) -> HRESULT { S_OK }
        pub fn HandleDestroyed(&self, handleId: GCHandleID) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback3 for Profiler {
        pub fn InitializeForAttach(&self,
            pCorProfilerInfoUnk: IUnknown,
            pvClientData: *const c_void,
            cbClientData: UINT,
        ) -> HRESULT { S_OK }
        pub fn ProfilerAttachComplete(&self) -> HRESULT { S_OK }
        pub fn ProfilerDetachSucceeded(&self) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback4 for Profiler {
        pub fn ReJITCompilationStarted(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT {
            match self.rejit_compilation_started(functionId, rejitId, fIsSafeToBlock) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
        pub fn GetReJITParameters(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            pFunctionControl: ICorProfilerFunctionControl,
        ) -> HRESULT {
            match self.get_rejit_parameters(moduleId, methodId, pFunctionControl) {
                Ok(_) => S_OK,
                Err(hr) => hr,
            }
        }
        pub fn ReJITCompilationFinished(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT {
            self.rejit_compilation_finished(functionId, rejitId, hrStatus, fIsSafeToBlock);
            S_OK
        }
        pub fn ReJITError(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            functionId: FunctionID,
            hrStatus: HRESULT,
        ) -> HRESULT {
            self.rejit_error(moduleId, methodId, functionId, hrStatus);
            S_OK
        }
        pub fn MovedReferences2(&self,
            cMovedObjectIDRanges: ULONG,
            oldObjectIDRangeStart: *const ObjectID,
            newObjectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const SIZE_T,
        ) -> HRESULT { S_OK }
        pub fn SurvivingReferences2(&self,
            cSurvivingObjectIDRanges: ULONG,
            objectIDRangeStart: *const ObjectID,
            cObjectIDRangeLength: *const SIZE_T,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback5 for Profiler {
        pub fn ConditionalWeakTableElementReferences(&self,
            cRootRefs: ULONG,
            keyRefIds: *const ObjectID,
            valueRefIds: *const ObjectID,
            rootIds: *const GCHandleID,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback6 for Profiler {
        pub fn GetAssemblyReferences(&self,
            wszAssemblyPath: *const WCHAR,
            pAsmRefProvider: ICorProfilerAssemblyReferenceProvider,
        ) -> HRESULT {
            match self.get_assembly_references(wszAssemblyPath, pAsmRefProvider) {
                Ok(_) => S_OK,
                Err(_) => S_OK,
            }
        }
    }

    impl ICorProfilerCallback7 for Profiler {
        pub fn ModuleInMemorySymbolsUpdated(&self, moduleId: ModuleID) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback8 for Profiler {
        pub fn DynamicMethodJITCompilationStarted(&self,
            functionId: FunctionID,
            fIsSafeToBlock: BOOL,
            pILHeader: LPCBYTE,
            cbILHeader: ULONG,
        ) -> HRESULT { S_OK }
        pub fn DynamicMethodJITCompilationFinished(&self,
            functionId: FunctionID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback9 for Profiler {
        pub fn DynamicMethodUnloaded(&self, functionId: FunctionID) -> HRESULT { S_OK }
    }
}

impl Profiler {
    fn initialize(&self, unknown: IUnknown) -> Result<(), HRESULT> {
        unsafe {
            unknown.AddRef();
        }

        let process_path = std::env::current_exe().map_err(|e| {
            // logging hasn't yet been initialized so unable to log
            E_FAIL
        })?;

        let process_file_name = process_path
            .file_name()
            .unwrap()
            .to_string_lossy()
            .to_string();

        let process_name = process_path
            .file_stem()
            .unwrap()
            .to_string_lossy()
            .to_string();
        let logger = env::initialize_logging(&process_name);

        log::trace!(
            "Initialize: started. profiler package version {} (commit: {})",
            PROFILER_PACKAGE_VERSION,
            GIT_HASH
        );

        if log::log_enabled!(Level::Debug) {
            log::debug!("Environment variables\n{}", env::get_env_vars());
        }

        if let Some(exclude_process_names) = env::get_exclude_processes() {
            for exclude_process_name in exclude_process_names {
                if process_file_name == exclude_process_name {
                    log::info!(
                        "Initialize: process name {} matches excluded name {}. Profiler disabled",
                        &process_file_name,
                        &exclude_process_name
                    );
                    return Err(E_FAIL);
                }
            }
        }

        if let Some(exclude_service_names) = env::get_exclude_service_names() {
            if let Some(service_name) = env::get_service_name() {
                for exclude_service_name in exclude_service_names {
                    if service_name == exclude_service_name {
                        log::info!(
                            "Initialize: service name {} matches excluded name {}. Profiler disabled",
                            &service_name,
                            &exclude_service_name);
                        return Err(E_FAIL);
                    }
                }
            }
        }

        env::check_if_running_in_azure_app_service()?;

        // get the ICorProfilerInfo4 interface, which will be available for all CLR versions targeted
        let profiler_info = unknown
            .query_interface::<ICorProfilerInfo4>()
            .ok_or_else(|| {
                log::error!("Initialize: could not get ICorProfilerInfo4 from IUnknown");
                E_FAIL
            })?;

        // get the integrations from file
        let integrations = env::load_integrations()?;
        let calltarget_enabled = *env::ELASTIC_APM_PROFILER_CALLTARGET_ENABLED;
        if calltarget_enabled {
            let rejit_handler = RejitHandler::new(profiler_info.clone());
            self.rejit_handler.replace(Some(rejit_handler));
        }

        let mut integration_methods = flatten_integrations(integrations, calltarget_enabled);

        if integration_methods.is_empty() {
            log::warn!("Initialize: no integrations. Profiler disabled.");
            return Err(E_FAIL);
        } else {
            log::debug!(
                "Initialize: loaded {} integration(s)",
                integration_methods.len()
            );
        }

        self.integration_methods
            .write()
            .unwrap()
            .append(&mut integration_methods);

        // Set the event mask for CLR events we're interested in
        let mut event_mask = COR_PRF_MONITOR::COR_PRF_MONITOR_JIT_COMPILATION
            | COR_PRF_MONITOR::COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST
            | COR_PRF_MONITOR::COR_PRF_MONITOR_MODULE_LOADS
            | COR_PRF_MONITOR::COR_PRF_MONITOR_ASSEMBLY_LOADS
            | COR_PRF_MONITOR::COR_PRF_MONITOR_APPDOMAIN_LOADS
            | COR_PRF_MONITOR::COR_PRF_DISABLE_ALL_NGEN_IMAGES;

        if calltarget_enabled {
            log::info!("Initialize: CallTarget instrumentation is enabled");
            event_mask |= COR_PRF_MONITOR::COR_PRF_ENABLE_REJIT;
        } else {
            log::info!("Initialize: CallTarget instrumentation is disabled");
        }

        if !env::enable_inlining(calltarget_enabled) {
            log::info!("Initialize: JIT Inlining is disabled");
            event_mask |= COR_PRF_MONITOR::COR_PRF_DISABLE_INLINING;
        } else {
            log::info!("Initialize: JIT Inlining is enabled");
        }

        if env::disable_optimizations() {
            log::info!("Initialize: optimizations are disabled");
            event_mask |= COR_PRF_MONITOR::COR_PRF_DISABLE_OPTIMIZATIONS;
        }

        // if the runtime also supports ICorProfilerInfo5, set eventmask2
        if let Some(profiler_info5) = unknown.query_interface::<ICorProfilerInfo5>() {
            let event_mask2 = COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES;
            log::trace!(
                "Initialize: set event mask2 to {:?}, {:?}",
                &event_mask,
                &event_mask2
            );
            profiler_info5.set_event_mask2(event_mask, event_mask2)?;
        } else {
            log::trace!("Initialize: set event mask to {:?}", &event_mask);
            profiler_info.set_event_mask(event_mask)?;
        }

        // get the details for the runtime
        let runtime_info = profiler_info.get_runtime_information()?;
        let is_desktop_clr = runtime_info.is_desktop_clr();
        let process_name = process_path.file_name().unwrap();
        if process_name == "w3wp.exe" || process_name == "iisexpress.exe" {
            self.is_desktop_iis.store(is_desktop_clr, Ordering::SeqCst);
        }

        // Store the profiler and runtime info for later use
        self.profiler_info.replace(Some(profiler_info));
        self.runtime_info.replace(Some(runtime_info));
        self.logger.replace(logger);

        IS_ATTACHED.store(true, Ordering::SeqCst);
        IS_DESKTOP_CLR.store(is_desktop_clr, Ordering::SeqCst);

        Ok(())
    }

    fn shutdown(&self) -> Result<(), HRESULT> {
        log::trace!("Shutdown: started");
        let _lock = self.modules.lock();

        // shutdown the rejit handler, if it's running
        if let Some(rejit_handler) = self.rejit_handler.replace(None) {
            rejit_handler.shutdown();
        }

        // Cannot safely call methods on profiler_info after shutdown is called,
        // so replace it on the profiler
        self.profiler_info.replace(None);

        IS_ATTACHED.store(false, Ordering::SeqCst);

        Ok(())
    }

    fn app_domain_shutdown_finished(&self, app_domain_id: AppDomainID, hr_status: HRESULT) {
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return;
        }
        let _lock = self.modules.lock();
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return;
        }

        self.first_jit_compilation_app_domains
            .write()
            .unwrap()
            .remove(&app_domain_id);

        log::debug!(
            "AppDomainShutdownFinished: app_domain={} removed ",
            app_domain_id
        );
    }

    fn assembly_load_finished(
        &self,
        assembly_id: AssemblyID,
        hr_status: HRESULT,
    ) -> Result<(), HRESULT> {
        if FAILED(hr_status) {
            log::error!("AssemblyLoadFinished: hr_status is {:X}", hr_status);
            return Ok(());
        }

        let _lock = self.modules.lock();

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("AssemblyLoadFinished: profiler not attached");
            return Ok(());
        }

        let profiler_info_borrow = self.profiler_info.borrow();
        let profiler_info = profiler_info_borrow.as_ref().unwrap();

        let assembly_info = profiler_info.get_assembly_info(assembly_id)?;
        let metadata_import = profiler_info.get_module_metadata::<IMetaDataImport2>(
            assembly_info.module_id,
            CorOpenFlags::ofRead,
        )?;

        let metadata_assembly_import = metadata_import
            .query_interface::<IMetaDataAssemblyImport>()
            .ok_or_else(|| {
                log::warn!("AssemblyLoadFinished: unable to get metadata assembly import");
                E_FAIL
            })?;

        let assembly_metadata = metadata_assembly_import.get_assembly_metadata()?;
        let is_managed_profiler_assembly = assembly_info.name == MANAGED_PROFILER_ASSEMBLY;

        log::debug!(
            "AssemblyLoadFinished: name={}, version={}, culture={}",
            &assembly_metadata.name,
            &assembly_metadata.version,
            &assembly_metadata.locale.as_deref().unwrap_or("neutral")
        );

        if is_managed_profiler_assembly {
            if assembly_metadata.version == *PROFILER_VERSION {
                log::info!(
                    "AssemblyLoadFinished: {} {} matched profiler version {}",
                    MANAGED_PROFILER_ASSEMBLY,
                    &assembly_metadata.version,
                    *PROFILER_VERSION
                );

                MANAGED_PROFILER_LOADED_APP_DOMAINS
                    .lock()
                    .unwrap()
                    .insert(assembly_info.app_domain_id);

                let runtime_borrow = self.runtime_info.borrow();
                let runtime_info = runtime_borrow.as_ref().unwrap();

                if runtime_info.is_desktop_clr()
                    && self.cor_lib_module_loaded.load(Ordering::SeqCst)
                {
                    if assembly_info.app_domain_id == self.cor_app_domain_id.load(Ordering::SeqCst)
                    {
                        log::info!(
                            "AssemblyLoadFinished: {} was loaded domain-neutral",
                            MANAGED_PROFILER_ASSEMBLY
                        );
                        MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL.store(true, Ordering::SeqCst);
                    } else {
                        log::info!(
                            "AssemblyLoadFinished: {} was not loaded domain-neutral",
                            MANAGED_PROFILER_ASSEMBLY
                        );
                    }
                }
            } else {
                log::warn!(
                    "AssemblyLoadFinished: {} {} did not match profiler version {}. Will not be marked as loaded",
                    MANAGED_PROFILER_ASSEMBLY,
                    &assembly_metadata.version,
                    *PROFILER_VERSION
                );
            }
        }

        Ok(())
    }

    fn module_load_finished(&self, module_id: ModuleID, hr_status: HRESULT) -> Result<(), HRESULT> {
        if FAILED(hr_status) {
            log::error!(
                "ModuleLoadFinished: hr status is {} for module id {}. skipping",
                hr_status,
                module_id
            );
            return Ok(());
        }

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("ModuleLoadFinished: profiler not attached");
            return Ok(());
        }

        let mut modules = self.modules.lock().unwrap();

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("ModuleLoadFinished: profiler not attached");
            return Ok(());
        }

        if let Some(module_info) = self.get_module_info(module_id) {
            let app_domain_id = module_info.assembly.app_domain_id;
            let assembly_name = &module_info.assembly.name;

            log::debug!(
                "ModuleLoadFinished: {} {} app domain {} {}",
                module_id,
                assembly_name,
                app_domain_id,
                &module_info.assembly.app_domain_name
            );

            if !self.cor_lib_module_loaded.load(Ordering::SeqCst)
                && (assembly_name == "mscorlib" || assembly_name == "System.Private.CoreLib")
            {
                self.cor_lib_module_loaded.store(true, Ordering::SeqCst);
                self.cor_app_domain_id
                    .store(app_domain_id, Ordering::SeqCst);

                let profiler_borrow = self.profiler_info.borrow();
                let profiler_info = profiler_borrow.as_ref().unwrap();
                let metadata_assembly_import = profiler_info
                    .get_module_metadata::<IMetaDataAssemblyImport>(
                        module_id,
                        CorOpenFlags::ofRead | CorOpenFlags::ofWrite,
                    )?;

                let mut assembly_metadata = metadata_assembly_import.get_assembly_metadata()?;
                assembly_metadata.name = assembly_name.to_string();

                log::info!(
                    "ModuleLoadFinished: Cor library {} {}",
                    &assembly_metadata.name,
                    &assembly_metadata.version
                );
                self.cor_assembly_property.replace(Some(assembly_metadata));

                return Ok(());
            }

            // if this is the loader module, track the app domain it's been loaded into
            if assembly_name == MANAGED_PROFILER_ASSEMBLY_LOADER {
                log::info!(
                    "ModuleLoadFinished: {} loaded into AppDomain {} {}",
                    MANAGED_PROFILER_ASSEMBLY_LOADER,
                    app_domain_id,
                    &module_info.assembly.app_domain_name
                );

                self.first_jit_compilation_app_domains
                    .write()
                    .unwrap()
                    .insert(app_domain_id);

                return Ok(());
            }

            // if this is a windows runtime module, skip it
            if module_info.is_windows_runtime() {
                log::debug!(
                    "ModuleLoadFinished: skipping windows metadata module {} {}",
                    module_id,
                    &assembly_name
                );
                return Ok(());
            }

            for pattern in SKIP_ASSEMBLY_PREFIXES {
                if assembly_name.starts_with(pattern) {
                    log::debug!(
                        "ModuleLoadFinished: skipping module {} {} because it matches skip pattern {}",
                        module_id,
                        assembly_name,
                        pattern
                    );
                    return Ok(());
                }
            }

            for skip in SKIP_ASSEMBLIES {
                if assembly_name == skip {
                    log::debug!(
                        "ModuleLoadFinished: skipping module {} {} because it matches skip {}",
                        module_id,
                        assembly_name,
                        skip
                    );
                    return Ok(());
                }
            }

            let call_target_enabled = *env::ELASTIC_APM_PROFILER_CALLTARGET_ENABLED;

            // TODO: Avoid cloning integration methods. Should be possible to make all filtered_integrations a collection of references
            let mut filtered_integrations = if call_target_enabled {
                self.integration_methods.read().unwrap().to_vec()
            } else {
                self.integration_methods
                    .read()
                    .unwrap()
                    .iter()
                    .filter(|m| {
                        if let Some(caller) = m.method_replacement.caller() {
                            caller.assembly.is_empty() || &caller.assembly == assembly_name
                        } else {
                            true
                        }
                    })
                    .cloned()
                    .collect()
            };

            if filtered_integrations.is_empty() {
                log::debug!(
                    "ModuleLoadFinished: skipping module {} {} because filtered by caller",
                    module_id,
                    assembly_name
                );
                return Ok(());
            }

            // get the metadata interfaces for the module
            let profiler_borrow = self.profiler_info.borrow();
            let profiler_info = profiler_borrow.as_ref().unwrap();
            let metadata_import = profiler_info
                .get_module_metadata::<IMetaDataImport2>(
                    module_id,
                    CorOpenFlags::ofRead | CorOpenFlags::ofWrite,
                )
                .map_err(|e| {
                    log::warn!(
                        "ModuleLoadFinished: unable to get IMetaDataEmit2 for module id {}",
                        module_id
                    );
                    e
                })?;

            let metadata_emit = metadata_import
                .query_interface::<IMetaDataEmit2>()
                .ok_or_else(|| {
                    log::warn!(
                        "ModuleLoadFinished: unable to get IMetaDataEmit2 for module id {}",
                        module_id
                    );
                    E_FAIL
                })?;
            let assembly_import = metadata_import
                .query_interface::<IMetaDataAssemblyImport>().ok_or_else(|| {
                log::warn!("ModuleLoadFinished: unable to get IMetaDataAssemblyImport for module id {}", module_id);
                E_FAIL
            })?;
            let assembly_emit = metadata_import
                .query_interface::<IMetaDataAssemblyEmit>()
                .ok_or_else(|| {
                    log::warn!(
                        "ModuleLoadFinished: unable to get IMetaDataAssemblyEmit for module id {}",
                        module_id
                    );
                    E_FAIL
                })?;

            // don't skip Microsoft.AspNetCore.Hosting so we can run the startup hook and
            // subscribe to DiagnosticSource events.
            // don't skip Dapper: it makes ADO.NET calls even though it doesn't reference
            // System.Data or System.Data.Common
            if assembly_name != "Microsoft.AspNetCore.Hosting"
                && assembly_name != "Dapper"
                && !call_target_enabled
            {
                let assembly_metadata = assembly_import.get_assembly_metadata().map_err(|e| {
                    log::warn!(
                        "ModuleLoadFinished: unable to get assembly metadata for {}",
                        assembly_name
                    );
                    e
                })?;

                let assembly_refs = assembly_import.enum_assembly_refs()?;
                let assembly_ref_metadata: Result<Vec<AssemblyMetaData>, HRESULT> = assembly_refs
                    .into_iter()
                    .map(|r| assembly_import.get_referenced_assembly_metadata(r))
                    .collect();
                if assembly_ref_metadata.is_err() {
                    log::warn!("ModuleLoadFinished: unable to get referenced assembly metadata");
                    return Err(assembly_ref_metadata.err().unwrap());
                }

                fn meets_requirements(
                    assembly_metadata: &AssemblyMetaData,
                    method_replacement: &MethodReplacement,
                ) -> bool {
                    match method_replacement.target() {
                        Some(target)
                            if target.is_valid_for_assembly(
                                &assembly_metadata.name,
                                &assembly_metadata.version,
                            ) =>
                        {
                            true
                        }
                        _ => false,
                    }
                }

                let assembly_ref_metadata = assembly_ref_metadata.unwrap();
                filtered_integrations.retain(|i| {
                    meets_requirements(&assembly_metadata, &i.method_replacement)
                        || assembly_ref_metadata
                            .iter()
                            .any(|m| meets_requirements(m, &i.method_replacement))
                });

                if filtered_integrations.is_empty() {
                    log::debug!(
                        "ModuleLoadFinished: skipping module {} {} because filtered by target",
                        module_id,
                        assembly_name
                    );
                    return Ok(());
                }
            }

            let module_version_id = metadata_import.get_module_version_id().map_err(|e| {
                log::warn!("ModuleLoadFinished: unable to get module version id for {} {} app domain {} {}",
                module_id,
                assembly_name,
                app_domain_id,
                &module_info.assembly.app_domain_name);
                e
            })?;

            let cor_assembly_property = {
                let cor_assembly_property_borrow = self.cor_assembly_property.borrow();
                cor_assembly_property_borrow.as_ref().unwrap().clone()
            };

            let module_metadata = ModuleMetadata::new(
                metadata_import,
                metadata_emit,
                assembly_import,
                assembly_emit,
                assembly_name.to_string(),
                app_domain_id,
                module_version_id,
                filtered_integrations,
                cor_assembly_property,
            );

            modules.insert(module_id, module_metadata);
            let module_metadata = modules.get(&module_id).unwrap();

            {
                let module_wrapper_tokens = self
                    .module_wrapper_tokens
                    .lock()
                    .unwrap()
                    .insert(module_id, ModuleWrapperTokens::new());
            }

            log::debug!(
                "ModuleLoadFinished: stored metadata for {} {} app domain {} {}",
                module_id,
                assembly_name,
                app_domain_id,
                &module_info.assembly.app_domain_name
            );

            log::trace!("ModuleLoadFinished: tracking {} module(s)", modules.len());

            if call_target_enabled {
                let rejit_count =
                    self.calltarget_request_rejit_for_module(module_id, module_metadata)?;
                if rejit_count > 0 {
                    log::trace!(
                        "ModuleLoadFinished: requested rejit of {} methods",
                        rejit_count
                    );
                }
            }
        }

        Ok(())
    }

    fn module_unload_started(&self, module_id: ModuleID) -> Result<(), HRESULT> {
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        if log::log_enabled!(log::Level::Debug) {
            if let Some(module_info) = self.get_module_info(module_id) {
                log::debug!(
                    "ModuleUnloadStarted: {} {} app domain {} {}",
                    module_id,
                    &module_info.assembly.name,
                    &module_info.assembly.app_domain_id,
                    &module_info.assembly.app_domain_name
                );
            }
        }

        let mut modules = self.modules.lock().unwrap();

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        if let Some(module_metadata) = modules.remove(&module_id) {
            MANAGED_PROFILER_LOADED_APP_DOMAINS
                .lock()
                .unwrap()
                .retain(|app_domain_id| app_domain_id != &module_metadata.app_domain_id);
        }

        Ok(())
    }

    fn jit_compilation_started(
        &self,
        function_id: FunctionID,
        is_safe_to_block: BOOL,
    ) -> Result<(), HRESULT> {
        if !IS_ATTACHED.load(Ordering::SeqCst) || is_safe_to_block == 0 {
            return Ok(());
        }

        let modules = self.modules.lock().unwrap();

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        let profiler_borrow = self.profiler_info.borrow();
        let profiler_info = profiler_borrow.as_ref().unwrap();
        let function_info = profiler_info.get_function_info(function_id).map_err(|e| {
            log::warn!(
                "JITCompilationStarted: get function info failed for {}",
                function_id
            );
            e
        })?;

        let module_metadata = modules.get(&function_info.module_id);
        if module_metadata.is_none() {
            return Ok(());
        }

        let module_metadata = module_metadata.unwrap();
        let call_target_enabled = *env::ELASTIC_APM_PROFILER_CALLTARGET_ENABLED;
        let loader_injected_in_app_domain = {
            // scope reading to this block
            self.first_jit_compilation_app_domains
                .read()
                .unwrap()
                .contains(&module_metadata.app_domain_id)
        };

        if call_target_enabled && loader_injected_in_app_domain {
            return Ok(());
        }

        let caller = module_metadata
            .import
            .get_function_info(function_info.token)?;

        log::trace!(
            "JITCompilationStarted: function_id={}, name={}()",
            function_id,
            caller.full_name()
        );

        let is_desktop_iis = self.is_desktop_iis.load(Ordering::SeqCst);
        let valid_startup_hook_callsite = if is_desktop_iis {
            match &caller.type_info {
                Some(t) => {
                    &module_metadata.assembly_name == "System.Web"
                        && t.name == "System.Web.Compilation.BuildManager"
                        && &caller.name == "InvokePreStartInitMethods"
                }
                None => false,
            }
        } else {
            !(&module_metadata.assembly_name == "System"
                || &module_metadata.assembly_name == "System.Net.Http")
        };

        if valid_startup_hook_callsite && !loader_injected_in_app_domain {
            let runtime_info_borrow = self.runtime_info.borrow();
            let runtime_info = runtime_info_borrow.as_ref().unwrap();

            let domain_neutral_assembly = runtime_info.is_desktop_clr()
                && self.cor_lib_module_loaded.load(Ordering::SeqCst)
                && self.cor_app_domain_id.load(Ordering::SeqCst) == module_metadata.app_domain_id;

            log::info!(
                "JITCompilationStarted: Startup hook registered in function_id={} token={} \
                name={}() assembly_name={} app_domain_id={} domain_neutral={}",
                function_id,
                &function_info.token,
                &caller.full_name(),
                &module_metadata.assembly_name,
                &module_metadata.app_domain_id,
                domain_neutral_assembly
            );

            self.first_jit_compilation_app_domains
                .write()
                .unwrap()
                .insert(module_metadata.app_domain_id);

            startup_hook::run_il_startup_hook(
                profiler_info,
                &module_metadata,
                function_info.module_id,
                function_info.token,
            )?;

            if is_desktop_iis {
                // TODO: hookup IIS module
            }
        }

        if !call_target_enabled {
            if &module_metadata.assembly_name == "Microsoft.AspNetCore.Hosting" {
                return Ok(());
            }

            let method_replacements = module_metadata.get_method_replacements_for_caller(&caller);
            if method_replacements.is_empty() {
                return Ok(());
            }

            let mut module_wrapper_tokens = self.module_wrapper_tokens.lock().unwrap();
            let mut module_wrapper_token = module_wrapper_tokens
                .get_mut(&function_info.module_id)
                .unwrap();

            process::process_insertion_calls(
                profiler_info,
                &module_metadata,
                &mut module_wrapper_token,
                function_id,
                function_info.module_id,
                function_info.token,
                &caller,
                &method_replacements,
            )?;

            process::process_replacement_calls(
                profiler_info,
                &module_metadata,
                &mut module_wrapper_token,
                function_id,
                function_info.module_id,
                function_info.token,
                &caller,
                &method_replacements,
            )?;
        }

        Ok(())
    }

    fn get_module_info(&self, module_id: ModuleID) -> Option<types::ModuleInfo> {
        let borrow = self.profiler_info.borrow();
        let profiler_info = borrow.as_ref().unwrap();
        if let Ok(module_info) = profiler_info.get_module_info_2(module_id) {
            if let Ok(assembly_info) = profiler_info.get_assembly_info(module_info.assembly_id) {
                if let Ok(app_domain_info) =
                    profiler_info.get_app_domain_info(assembly_info.app_domain_id)
                {
                    return Some(types::ModuleInfo {
                        id: module_id,
                        assembly: types::AssemblyInfo {
                            id: module_info.assembly_id,
                            name: assembly_info.name,
                            app_domain_id: assembly_info.app_domain_id,
                            app_domain_name: app_domain_info.name,
                            manifest_module_id: assembly_info.module_id,
                        },
                        path: module_info.file_name,
                        flags: module_info.module_flags,
                    });
                }
            }
        }

        None
    }

    fn get_assembly_references(
        &self,
        assembly_path: *const WCHAR,
        assembly_reference_provider: ICorProfilerAssemblyReferenceProvider,
    ) -> Result<(), HRESULT> {
        unsafe {
            assembly_reference_provider.AddRef();
        }

        let path = {
            let p = unsafe { U16CStr::from_ptr_str(assembly_path) };
            p.to_string_lossy()
        };

        if *env::IS_AZURE_APP_SERVICE {
            log::debug!(
                "GetAssemblyReferences: skipping because profiler is running in \
                Azure App Services, which is not yet supported. path={}",
                &path
            );
            return Ok(());
        }

        log::trace!("GetAssemblyReferences: called for path={}", &path);

        let path_buf = PathBuf::from(&path);
        let mut assembly_name = path_buf.file_name().unwrap().to_str().unwrap();
        if assembly_name.ends_with(".dll") {
            assembly_name = assembly_name.strip_suffix(".dll").unwrap();
        } else if assembly_name.ends_with(".ni.dll") {
            assembly_name = assembly_name.strip_suffix(".ni.dll").unwrap();
        }

        for pattern in SKIP_ASSEMBLY_PREFIXES.iter() {
            if assembly_name.starts_with(pattern) {
                log::debug!(
                        "GetAssemblyReferences: skipping module {} {} because it matches skip pattern {}",
                        assembly_name,
                        &path,
                        pattern
                    );
                return Ok(());
            }
        }

        for skip in SKIP_ASSEMBLIES.iter() {
            if &assembly_name == skip {
                log::debug!(
                    "GetAssemblyReferences: skipping assembly {} {} because it matches skip {}",
                    assembly_name,
                    &path,
                    skip
                );
                return Ok(());
            }
        }

        let assembly_reference = MANAGED_PROFILER_FULL_ASSEMBLY_VERSION.deref();
        let locale = if &assembly_reference.locale == "neutral" {
            U16CString::default()
        } else {
            U16CString::from_str(&assembly_reference.locale).unwrap()
        };

        let cb_locale = locale.len() as ULONG;
        let sz_locale = locale.into_vec_with_nul().as_mut_ptr();
        let assembly_metadata = ASSEMBLYMETADATA {
            usMajorVersion: assembly_reference.version.major,
            usMinorVersion: assembly_reference.version.minor,
            usBuildNumber: assembly_reference.version.build,
            usRevisionNumber: assembly_reference.version.revision,
            szLocale: sz_locale,
            cbLocale: cb_locale,
            rProcessor: std::ptr::null_mut(),
            ulProcessor: 0,
            rOS: std::ptr::null_mut(),
            ulOS: 0,
        };

        let public_key = assembly_reference.public_key.into_bytes();
        let name = U16CString::from_str(&assembly_reference.name).unwrap();
        let len = name.len() as ULONG;

        let assembly_reference_info = COR_PRF_ASSEMBLY_REFERENCE_INFO {
            pbPublicKeyOrToken: public_key.as_ptr() as *const _ as *const c_void,
            cbPublicKeyOrToken: public_key.len() as ULONG,
            szName: name.as_ptr(),
            pMetaData: &assembly_metadata as *const ASSEMBLYMETADATA,
            pbHashValue: std::ptr::null() as *const c_void,
            cbHashValue: 0,
            dwAssemblyRefFlags: 0,
        };

        match assembly_reference_provider.add_assembly_reference(&assembly_reference_info) {
            Ok(()) => {
                log::trace!(
                    "GetAssemblyReferences succeeded for {}, path={}",
                    assembly_name,
                    &path
                )
            }
            Err(e) => log::warn!(
                "GetAssemblyReferences failed for {}, path={}. 0x{:X}",
                assembly_name,
                &path,
                e
            ),
        }

        Ok(())
    }

    fn rejit_compilation_started(
        &self,
        function_id: FunctionID,
        rejit_id: ReJITID,
        is_safe_to_block: BOOL,
    ) -> Result<(), HRESULT> {
        if !IS_ATTACHED.load(Ordering::SeqCst) || is_safe_to_block == 0 {
            return Ok(());
        }

        log::debug!(
            "ReJITCompilationStarted: function_id={} rejit_id={} is_safe_to_block={}",
            function_id,
            rejit_id,
            is_safe_to_block
        );

        let borrow = self.rejit_handler.borrow();
        let rejit_handler: &RejitHandler = borrow.as_ref().unwrap();
        rejit_handler.notify_rejit_compilation_started(function_id, rejit_id)
    }

    fn rejit_compilation_finished(
        &self,
        function_id: FunctionID,
        rejit_id: ReJITID,
        hr_status: HRESULT,
        is_safe_to_block: BOOL,
    ) {
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return;
        }

        log::debug!(
            "ReJITCompilationFinished: function_id={} rejit_id={} hr_status={} is_safe_to_block={}",
            function_id,
            rejit_id,
            hr_status,
            is_safe_to_block
        );
    }

    fn get_rejit_parameters(
        &self,
        module_id: ModuleID,
        method_id: mdMethodDef,
        function_control: ICorProfilerFunctionControl,
    ) -> Result<(), HRESULT> {
        unsafe {
            function_control.AddRef();
        }

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        log::debug!(
            "GetReJITParameters: module_id={} method_id={}",
            module_id,
            method_id
        );

        let modules = self.modules.lock().unwrap();

        if let Some(module_metadata) = modules.get(&module_id) {
            let mut module_wrapper_tokens = self.module_wrapper_tokens.lock().unwrap();

            let module_wrapper_token = module_wrapper_tokens.get_mut(&module_id).unwrap();

            let borrow = self.profiler_info.borrow();
            let profiler_info = borrow.as_ref().unwrap();

            let mut tokens = self.call_target_tokens.borrow_mut();
            let call_target_tokens = tokens
                .entry(module_id)
                .or_insert_with(CallTargetTokens::new);

            let mut rejit_borrow = self.rejit_handler.borrow_mut();
            let rejit_handler: &mut RejitHandler = rejit_borrow.as_mut().unwrap();
            rejit_handler.notify_rejit_parameters(
                module_id,
                method_id,
                &function_control,
                module_metadata,
                module_wrapper_token,
                profiler_info,
                call_target_tokens,
            )
        } else {
            Ok(())
        }
    }

    fn rejit_error(
        &self,
        module_id: ModuleID,
        method_id: mdMethodDef,
        function_id: FunctionID,
        hr_status: HRESULT,
    ) {
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return;
        }

        log::warn!(
            "ReJITError: function_id={} module_id={} method_id={} hr_status={}",
            function_id,
            module_id,
            method_id,
            hr_status
        );
    }

    fn calltarget_request_rejit_for_module(
        &self,
        module_id: ModuleID,
        module_metadata: &ModuleMetadata,
    ) -> Result<usize, HRESULT> {
        let metadata_import = &module_metadata.import;
        let assembly_metadata: AssemblyMetaData =
            module_metadata.assembly_import.get_assembly_metadata()?;

        let mut method_ids = vec![];

        for integration in &module_metadata.integrations {
            let target = match integration.method_replacement.target() {
                Some(t)
                    if t.is_valid_for_assembly(
                        &module_metadata.assembly_name,
                        &assembly_metadata.version,
                    ) =>
                {
                    t
                }
                _ => continue,
            };

            let wrapper = match integration.method_replacement.wrapper() {
                Some(w) if w.action == WrapperMethodAction::CallTargetModification => w,
                _ => continue,
            };

            let type_def = match helpers::find_type_def_by_name(
                target.type_name(),
                &module_metadata.assembly_name,
                &metadata_import,
            ) {
                Some(t) => t,
                None => continue,
            };

            let method_defs =
                metadata_import.enum_methods_with_name(type_def, target.method_name())?;
            for method_def in method_defs {
                let caller: FunctionInfo = match metadata_import.get_function_info(method_def) {
                    Ok(c) => c,
                    Err(e) => {
                        log::warn!(
                            "Could not get function_info for method_def={}, {}",
                            method_def,
                            e
                        );
                        continue;
                    }
                };

                let parsed_signature = match caller.method_signature.try_parse() {
                    Some(p) => p,
                    None => {
                        log::warn!(
                            "The method {} with signature={:?} cannot be parsed",
                            &caller.full_name(),
                            &caller.method_signature.data
                        );
                        continue;
                    }
                };

                let signature_types = match target.signature_types() {
                    Some(s) => s,
                    None => {
                        log::debug!("target does not have arguments defined");
                        continue;
                    }
                };

                if parsed_signature.arg_len as usize != signature_types.len() - 1 {
                    log::debug!(
                        "The caller for method_def {} does not have expected number of arguments",
                        target.method_name()
                    );
                    continue;
                }

                log::trace!(
                    "comparing signature for method {}.{}",
                    target.type_name(),
                    target.method_name()
                );
                let mut mismatch = false;
                for arg_idx in 0..parsed_signature.arg_len {
                    let (start_idx, _) = parsed_signature.args[arg_idx as usize];
                    let (argument_type_name, _) = get_sig_type_token_name(
                        &parsed_signature.data[start_idx..],
                        &metadata_import,
                    );

                    let integration_argument_type_name = &signature_types[arg_idx as usize + 1];
                    log::trace!(
                        "-> {} = {}",
                        &argument_type_name,
                        integration_argument_type_name
                    );
                    if &argument_type_name != integration_argument_type_name
                        && integration_argument_type_name != IGNORE
                    {
                        mismatch = true;
                        break;
                    }
                }

                if mismatch {
                    log::debug!(
                        "The caller for method_def {} does not have the right type of arguments",
                        target.method_name()
                    );
                    continue;
                }

                let mut borrow = self.rejit_handler.borrow_mut();
                let rejit_handler: &mut RejitHandler = borrow.as_mut().unwrap();
                let rejit_module = rejit_handler.get_or_add_module(module_id);
                let rejit_method = rejit_module.get_or_add_method(method_def);
                rejit_method.set_function_info(caller);
                rejit_method.set_method_replacement(integration.method_replacement.clone());

                method_ids.push(method_def);

                if log::log_enabled!(Level::Info) {
                    let caller_assembly_is_domain_neutral = IS_DESKTOP_CLR.load(Ordering::SeqCst)
                        && self.cor_lib_module_loaded.load(Ordering::SeqCst)
                        && module_metadata.app_domain_id
                            == self.cor_app_domain_id.load(Ordering::SeqCst);
                    let caller = rejit_method.function_info().unwrap();

                    log::info!(
                        "enqueue for ReJIT module_id={}, method_def={}, app_domain_id={}, \
                        domain_neutral={}, assembly={}, type={}, method={}, signature={:?}",
                        module_id,
                        method_def,
                        module_metadata.app_domain_id,
                        caller_assembly_is_domain_neutral,
                        &module_metadata.assembly_name,
                        caller.type_info.as_ref().map_or("", |t| t.name.as_str()),
                        &caller.name,
                        &caller.signature.bytes()
                    );
                }
            }
        }

        let len = method_ids.len();
        if !method_ids.is_empty() {
            let borrow = self.rejit_handler.borrow();
            let rejit_handler = borrow.as_ref().unwrap();
            rejit_handler.enqueue_for_rejit(vec![module_id; method_ids.len()], method_ids);
        }

        Ok(len)
    }
}

pub fn profiler_assembly_loaded_in_app_domain(app_domain_id: AppDomainID) -> bool {
    MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL.load(Ordering::SeqCst)
        || MANAGED_PROFILER_LOADED_APP_DOMAINS
            .lock()
            .unwrap()
            .contains(&app_domain_id)
}
