use crate::{
    cli::{Method, Operand},
    ffi::*,
    interfaces::{
        icor_profiler_assembly_reference_provider::ICorProfilerAssemblyReferenceProvider,
        icor_profiler_callback::{
            ICorProfilerCallback, ICorProfilerCallback2, ICorProfilerCallback3,
            ICorProfilerCallback4, ICorProfilerCallback5, ICorProfilerCallback6,
            ICorProfilerCallback7, ICorProfilerCallback8, ICorProfilerCallback9,
        },
        icor_profiler_function_control::ICorProfilerFunctionControl,
        icor_profiler_info::{
            ICorProfilerInfo, ICorProfilerInfo2, ICorProfilerInfo3, ICorProfilerInfo4,
            ICorProfilerInfo5, ICorProfilerInfo7, IID_ICOR_PROFILER_INFO, IID_ICOR_PROFILER_INFO4,
        },
        imetadata_assembly_emit::IMetaDataAssemblyEmit,
        imetadata_assembly_import::IMetaDataAssemblyImport,
        imetadata_emit::{IMetaDataEmit, IMetaDataEmit2},
        imetadata_import::{IMetaDataImport, IMetaDataImport2},
    },
    profiler::managed::ManagedLoader,
    profiler::types::{
        Integration, IntegrationMethod, MethodReplacement, ModuleMetadata, TargetMethodReference,
    },
    types::{AssemblyMetaData, HashAlgorithmType, ModuleInfo, RuntimeInfo, Version},
};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, GUID, HRESULT},
    Interface,
};
use rust_embed::RustEmbed;
use simple_logger::SimpleLogger;
use std::fs::File;
use std::io::BufReader;
use std::ops::Deref;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::{
    cell::RefCell,
    collections::HashMap,
    ffi::c_void,
    sync::{atomic::AtomicBool, Mutex},
};

const MANAGED_PROFILER_ASSEMBLY_LOADER: &'static str = "Elastic.Apm.Profiler.Managed.Loader";
const MANAGED_PROFILER_ASSEMBLY: &'static str = "Elastic.Apm.Profiler.Managed";
const ELASTIC_APM_PROFILER_INTEGRATIONS: &'static str = "ELASTIC_APM_PROFILER_INTEGRATIONS";
const ELASTIC_APM_PROFILER_CALLTARGET_ENABLED: &'static str =
    "ELASTIC_APM_PROFILER_CALLTARGET_ENABLED";

// TODO: Look at moving static refs that can be moved onto CorProfiler as fields.
lazy_static! {
    static ref LOCK: Mutex<i32> = Mutex::new(0);
    static ref FIRST_JIT_COMPILATION_APP_DOMAINS: Mutex<Vec<AppDomainID>> = Mutex::new(Vec::new());
    static ref MANAGED_PROFILER_LOADED_APP_DOMAINS: Mutex<Vec<AppDomainID>> =
        Mutex::new(Vec::new());
    static ref SKIP_ASSEMBLY_PREFIXES: Vec<&'static str> = vec![
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
    static ref SKIP_ASSEMBLIES: Vec<&'static str> = vec![
        "mscorlib",
        "netstandard",
        "System.Configuration",
        "Microsoft.AspNetCore.Razor.Language",
        "Microsoft.AspNetCore.Mvc.RazorPages",
        "Anonymously Hosted DynamicMethods Assembly",
        "ISymWrapper",
    ];
    static ref PROFILER_VERSION: Version = Version::new(1, 9, 0, 0);
    static ref INTEGRATION_METHODS: Mutex<Vec<IntegrationMethod>> = Mutex::new(Vec::new());
}

static COR_LIB_MODULE_LOADED: AtomicBool = AtomicBool::new(false);
static COR_APP_DOMAIN_ID: AtomicUsize = AtomicUsize::new(0);
static MANAGED_PROFILER_LOADED_DOMAIN_NEUTRAL: AtomicBool = AtomicBool::new(false);
static CALLTARGET_ENABLED: AtomicBool = AtomicBool::new(true);

pub(crate) static IS_ATTACHED: AtomicBool = AtomicBool::new(false);

class! {
    /// The profiler implementation
    pub class CorProfiler:
        ICorProfilerCallback9(ICorProfilerCallback8(ICorProfilerCallback7(
            ICorProfilerCallback6(ICorProfilerCallback5(ICorProfilerCallback4(
                ICorProfilerCallback3(ICorProfilerCallback2(ICorProfilerCallback)))))))) {
        profiler_info: RefCell<Option<ICorProfilerInfo4>>,
        runtime_info: RefCell<Option<RuntimeInfo>>,
        modules: RefCell<HashMap<ModuleID, ModuleMetadata>>,
        cor_assembly_property: RefCell<Option<AssemblyMetaData>>,
        is_desktop_iis: AtomicBool,
    }

    impl ICorProfilerCallback for CorProfiler {
        pub fn Initialize(
             &self,
            pICorProfilerInfoUnk: IUnknown,
        ) -> HRESULT {
            match self.initialize(pICorProfilerInfoUnk) {
                Ok(_) => S_OK,
                Err(_) => S_OK
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
        ) -> HRESULT { S_OK }
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

    impl ICorProfilerCallback2 for CorProfiler {
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

    impl ICorProfilerCallback3 for CorProfiler {
        pub fn InitializeForAttach(&self,
            pCorProfilerInfoUnk: *const IUnknown,
            pvClientData: *const c_void,
            cbClientData: UINT,
        ) -> HRESULT { S_OK }
        pub fn ProfilerAttachComplete(&self) -> HRESULT { S_OK }
        pub fn ProfilerDetachSucceeded(&self) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback4 for CorProfiler {
        pub fn ReJITCompilationStarted(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn GetReJITParameters(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            pFunctionControl: *const ICorProfilerFunctionControl,
        ) -> HRESULT { S_OK }
        pub fn ReJITCompilationFinished(&self,
            functionId: FunctionID,
            rejitId: ReJITID,
            hrStatus: HRESULT,
            fIsSafeToBlock: BOOL,
        ) -> HRESULT { S_OK }
        pub fn ReJITError(&self,
            moduleId: ModuleID,
            methodId: mdMethodDef,
            functionId: FunctionID,
            hrStatus: HRESULT,
        ) -> HRESULT { S_OK }
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

    impl ICorProfilerCallback5 for CorProfiler {
        pub fn ConditionalWeakTableElementReferences(&self,
            cRootRefs: ULONG,
            keyRefIds: *const ObjectID,
            valueRefIds: *const ObjectID,
            rootIds: *const GCHandleID,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback6 for CorProfiler {
        pub fn GetAssemblyReferences(&self,
            wszAssemblyPath: *const WCHAR,
            pAsmRefProvider: *const ICorProfilerAssemblyReferenceProvider,
        ) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback7 for CorProfiler {
        pub fn ModuleInMemorySymbolsUpdated(&self, moduleId: ModuleID) -> HRESULT { S_OK }
    }

    impl ICorProfilerCallback8 for CorProfiler {
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

    impl ICorProfilerCallback9 for CorProfiler {
        pub fn DynamicMethodUnloaded(&self, functionId: FunctionID) -> HRESULT { S_OK }
    }
}

impl CorProfiler {
    fn initialize(&self, unknown: IUnknown) -> Result<(), HRESULT> {
        let _lock = LOCK.lock().unwrap();

        SimpleLogger::from_env().init().unwrap();
        log::trace!("Initialize: started");

        let integrations = self.load_integrations()?;

        // get the ICorProfilerInfo4 interface, which will be available for all CLR versions targeted
        let profiler_info = unknown.query_interface::<ICorProfilerInfo4>();
        if profiler_info.is_none() {
            log::error!("Initialize: could not get ICorProfilerInfo4 from IUnknown");
            return Err(E_FAIL);
        }

        let profiler_info = profiler_info.unwrap();

        // get the integrations from file
        let integrations: Vec<Integration> = self.load_integrations()?;
        let calltarget_enabled = self.read_calltarget_env_var();
        CALLTARGET_ENABLED.store(calltarget_enabled, Ordering::SeqCst);

        if calltarget_enabled {
            // TODO: initialize rejit handler
        }

        let mut integration_methods: Vec<IntegrationMethod> = integrations
            .iter()
            .flat_map(|i| {
                i.method_replacements.iter().filter_map(move |m| {
                    if let Some(wrapper_method) = &m.wrapper {
                        let is_calltarget = &wrapper_method.action == "CallTargetModification";
                        if calltarget_enabled && is_calltarget {
                            Some(IntegrationMethod {
                                name: i.name.clone(),
                                method_replacement: m.clone(),
                            })
                        } else if !calltarget_enabled && !is_calltarget {
                            Some(IntegrationMethod {
                                name: i.name.clone(),
                                method_replacement: m.clone(),
                            })
                        } else {
                            None
                        }
                    } else {
                        None
                    }
                })
            })
            .collect();

        if integration_methods.is_empty() {
            log::warn!("Initialize: No integrations loaded. Profiler disabled.");
            return Err(E_FAIL);
        } else {
            log::debug!(
                "Initialize: loaded {} integration(s)",
                integration_methods.len()
            );
        }

        INTEGRATION_METHODS
            .lock()
            .unwrap()
            .append(&mut integration_methods);

        // Set the event mask for CLR events we're interested in
        let mut event_mask = COR_PRF_MONITOR::COR_PRF_MONITOR_JIT_COMPILATION
            | COR_PRF_MONITOR::COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST
            | COR_PRF_MONITOR::COR_PRF_MONITOR_MODULE_LOADS
            | COR_PRF_MONITOR::COR_PRF_MONITOR_ASSEMBLY_LOADS
            | COR_PRF_MONITOR::COR_PRF_DISABLE_ALL_NGEN_IMAGES;

        if calltarget_enabled {
            log::info!("Initialize: CallTarget instrumentation is enabled");
            event_mask |= COR_PRF_MONITOR::COR_PRF_ENABLE_REJIT;
        } else {
            log::info!("Initialize: CallTarget instrumentation is disabled");
        }

        // TODO: enable/disable inlining and optimizations

        log::trace!("Initialize: set event mask to {:?}", &event_mask);
        profiler_info.set_event_mask(event_mask)?;

        // if the runtime also supports ICorProfilerInfo5, set eventmask2
        if let Some(profiler_info5) = unknown.query_interface::<ICorProfilerInfo5>() {
            let event_mask2 = COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES;
            log::trace!("Initialize: set event mask2 to {:?}", &event_mask2);
            profiler_info5.set_event_mask2(event_mask, event_mask2)?;
        }

        // get the details for the runtime
        let runtime_info = profiler_info.get_runtime_information()?;
        let process_path = std::env::current_exe().unwrap();
        let process_name = process_path.file_name().unwrap();
        if process_name == "w3wp.exe" || process_name == "iisexpress.exe" {
            self.is_desktop_iis.store(runtime_info.is_desktop_clr(), Ordering::SeqCst);
        }

        // Store the profiler and runtime info for later use
        self.profiler_info.replace(Some(profiler_info));
        self.runtime_info.replace(Some(runtime_info));

        IS_ATTACHED.store(true, Ordering::SeqCst);

        Ok(())
    }

    fn shutdown(&self) -> Result<(), HRESULT> {
        log::trace!("Shutdown: started");
        let _ = LOCK.lock().unwrap();

        self.profiler_info.replace(None);
        IS_ATTACHED.store(false, Ordering::SeqCst);

        Ok(())
    }

    fn assembly_load_finished(
        &self,
        assembly_id: AssemblyID,
        hr_status: HRESULT,
    ) -> Result<(), HRESULT> {
        if FAILED(hr_status) {
            log::error!("AssemblyLoadFinished: hrStatus is {:X}", hr_status);
            return Ok(());
        }

        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("AssemblyLoadFinished: profiler not attached");
            return Ok(());
        }

        let _lock = LOCK.lock().unwrap();
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
            .expect("AssemblyLoadFinished: unable to get metadata assembly import");

        let assembly_metadata = metadata_assembly_import.get_assembly_metadata()?;

        let is_managed_profiler_assembly = &assembly_info.name == MANAGED_PROFILER_ASSEMBLY;

        log::debug!(
            "AssemblyLoadFinished: name={}, version={}",
            &assembly_metadata.name,
            &assembly_metadata.version
        );

        if is_managed_profiler_assembly {
            if &assembly_metadata.version == PROFILER_VERSION.deref() {
                log::info!(
                    "AssemblyLoadFinished: {} {} matched profiler version {}",
                    MANAGED_PROFILER_ASSEMBLY,
                    &assembly_metadata.version,
                    PROFILER_VERSION.deref()
                );

                MANAGED_PROFILER_LOADED_APP_DOMAINS
                    .lock()
                    .unwrap()
                    .push(assembly_info.app_domain_id);

                let runtime_borrow = self.runtime_info.borrow();
                let runtime_info = runtime_borrow.as_ref().unwrap();

                if runtime_info.is_desktop_clr() && COR_LIB_MODULE_LOADED.load(Ordering::SeqCst) {
                    if assembly_info.app_domain_id == COR_APP_DOMAIN_ID.load(Ordering::SeqCst) {
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
                    "AssemblyLoadFinished: {} {} did not match profiler version {}",
                    MANAGED_PROFILER_ASSEMBLY,
                    &assembly_metadata.version,
                    PROFILER_VERSION.deref()
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

        let _lock = LOCK.lock().unwrap();
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            log::trace!("ModuleLoadFinished: profiler not attached");
            return Ok(());
        }

        if let Some(module_info) = self.get_module_info(module_id) {
            let appdomain_id = module_info.assembly.app_domain_id;
            let assembly_name = &module_info.assembly.name;

            log::debug!(
                "ModuleLoadFinished: {} {} app domain {} {}",
                module_id,
                assembly_name,
                appdomain_id,
                &module_info.assembly.app_domain_name
            );

            if !COR_LIB_MODULE_LOADED.load(Ordering::SeqCst) && assembly_name == "mscorlib"
                || assembly_name == "System.Private.CoreLib"
            {
                COR_LIB_MODULE_LOADED.store(true, Ordering::SeqCst);
                COR_APP_DOMAIN_ID.store(appdomain_id, Ordering::SeqCst);

                let profiler_borrow = self.profiler_info.borrow();
                let profiler_info = profiler_borrow.as_ref().unwrap();
                let metadata_assembly_import = profiler_info
                    .get_module_metadata::<IMetaDataAssemblyImport>(
                        module_id,
                        CorOpenFlags::ofRead | CorOpenFlags::ofWrite,
                    )?;

                let mut assembly_metadata = metadata_assembly_import.get_assembly_metadata()?;
                log::trace!(
                    "assembly metadata name {}, module info assembly name {}",
                    &assembly_metadata.name,
                    assembly_name
                );
                assembly_metadata.name = assembly_name.to_string();

                log::info!(
                    "ModuleLoadFinished: Cor library {} {}",
                    &assembly_metadata.name,
                    &assembly_metadata.version
                );
                self.cor_assembly_property.replace(Some(assembly_metadata));

                return Ok(());
            }

            if assembly_name == MANAGED_PROFILER_ASSEMBLY_LOADER {
                log::info!(
                    "ModuleLoadFinished: {} loaded into AppDomain {} {}",
                    MANAGED_PROFILER_ASSEMBLY_LOADER,
                    appdomain_id,
                    &module_info.assembly.app_domain_name
                );

                FIRST_JIT_COMPILATION_APP_DOMAINS
                    .lock()
                    .unwrap()
                    .push(appdomain_id);

                return Ok(());
            }

            if module_info.is_windows_runtime() {
                log::debug!(
                    "ModuleLoadFinished: skipping windows metadata module {} {}",
                    module_id,
                    &assembly_name
                );
                return Ok(());
            }

            for pattern in SKIP_ASSEMBLY_PREFIXES.iter() {
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

            for skip in SKIP_ASSEMBLIES.iter() {
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

            // TODO: Avoid cloning integration methods. Should be possible to make all filtered_integrations a collection of references
            let is_call_target_enabled = CALLTARGET_ENABLED.load(Ordering::SeqCst);

            let mut filtered_integrations = if is_call_target_enabled {
                INTEGRATION_METHODS.lock().unwrap().to_vec()
            } else {
                INTEGRATION_METHODS
                    .lock()
                    .unwrap()
                    .iter()
                    .filter(|m| {
                        if let Some(caller) = &m.method_replacement.caller {
                            if caller.assembly.is_empty() || &caller.assembly == assembly_name {
                                return true;
                            }
                        }

                        false
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
            }

            // get the metadata interfaces for the module
            let profiler_borrow = self.profiler_info.borrow();
            let profiler_info = profiler_borrow.as_ref().unwrap();
            let metadata_import = profiler_info.get_module_metadata::<IMetaDataImport2>(
                module_id,
                CorOpenFlags::ofRead | CorOpenFlags::ofWrite,
            )?;
            let metadata_emit = metadata_import.query_interface::<IMetaDataEmit2>().unwrap();
            let assembly_import = metadata_import
                .query_interface::<IMetaDataAssemblyImport>()
                .unwrap();
            let assembly_emit = metadata_import
                .query_interface::<IMetaDataAssemblyEmit>()
                .unwrap();

            // don't skip Microsoft.AspNetCore.Hosting so we can run the startup hook and
            // subscribe to DiagnosticSource events.
            // don't skip Dapper: it makes ADO.NET calls even though it doesn't reference
            // System.Data or System.Data.Common
            if assembly_name != "Microsoft.AspNetCore.Hosting"
                && assembly_name != "Dapper"
                && !is_call_target_enabled
            {
                fn meets_requirements(
                    assembly_metadata: &AssemblyMetaData,
                    method_replacement: &MethodReplacement,
                ) -> bool {
                    match &method_replacement.target {
                        None => false,
                        Some(target) => {
                            if &target.assembly != &assembly_metadata.name {
                                return false;
                            }
                            if &target.minimum_version() > &assembly_metadata.version {
                                return false;
                            }
                            if &target.maximum_version() < &assembly_metadata.version {
                                return false;
                            }

                            true
                        }
                    }
                }

                let assembly_metadata = assembly_import.get_assembly_metadata()?;
                let assembly_refs = assembly_import.enum_assembly_refs()?;
                let assembly_ref_metadata: Result<Vec<AssemblyMetaData>, HRESULT> = assembly_refs
                    .into_iter()
                    .map(|r| assembly_import.get_referenced_assembly_metadata(r))
                    .collect();
                if assembly_ref_metadata.is_err() {
                    return Err(assembly_ref_metadata.err().unwrap());
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

            let module_version_id = metadata_import.get_module_version_id()?;
            let module_metadata = ModuleMetadata::new(
                metadata_import,
                metadata_emit,
                assembly_import,
                assembly_emit,
                assembly_name.to_string(),
                appdomain_id,
                module_version_id,
                filtered_integrations,
            );

            let mut modules = self.modules.borrow_mut();
            modules.insert(module_id, module_metadata);

            log::debug!(
                "ModuleLoadFinished: stored metadata for {} {} app domain {} {}",
                module_id,
                assembly_name,
                appdomain_id,
                &module_info.assembly.app_domain_name
            );

            log::trace!("ModuleLoadFinished: tracking {} module(s)", modules.len());

            if is_call_target_enabled {
                // TODO: request rejit of module
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

        let _lock = LOCK.lock().unwrap();
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        let mut modules = self.modules.borrow_mut();
        if let Some(module_metadata) = modules.remove(&module_id) {
            MANAGED_PROFILER_LOADED_APP_DOMAINS
                .lock()
                .unwrap()
                .retain(|appdomain_id| appdomain_id != &module_metadata.appdomain_id);
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

        let _lock = LOCK.lock().unwrap();
        if !IS_ATTACHED.load(Ordering::SeqCst) {
            return Ok(());
        }

        let profiler_borrow = self.profiler_info.borrow();
        let profiler_info = profiler_borrow.as_ref().unwrap();
        let function_info = profiler_info.get_function_info(function_id)
            .map_err(|e| {
                log::warn!("JITCompilationStarted: get function info failed for {}", function_id);
                e
            })?;

        let modules = self.modules.borrow();
        let module_metadata = modules.get(&function_info.module_id);
        if module_metadata.is_none() {
            return Ok(());
        }

        let module_metadata = module_metadata.unwrap();
        let call_target_enabled = CALLTARGET_ENABLED.load(Ordering::SeqCst);
        let loader_injected_in_appdomain = {
            let app_domains = FIRST_JIT_COMPILATION_APP_DOMAINS.lock().unwrap();
            app_domains.contains(&module_metadata.appdomain_id)
        };

        if call_target_enabled && loader_injected_in_appdomain {
            return Ok(());
        }

        let caller = module_metadata.metadata_import.get_function_info(function_info.token)?;
        log::trace!(
            "JITCompilationStarted: function_id={} token={} name={}()",
            function_id,
            &function_info.token,
            &caller.full_name());

        let is_desktop_iis = self.is_desktop_iis.load(Ordering::SeqCst);
        let valid_startup_hook_callsite = if is_desktop_iis {
            match &caller.type_info {
                Some(t) => &module_metadata.assembly_name == "System.Web" &&
                    t.name == "System.Web.Compilation.BuildManager" &&
                    &caller.name== "InvokerPreInitMethods",
                None => false
            }
        } else if &module_metadata.assembly_name == "System" || &module_metadata.assembly_name == "System.Net.Http" {
            false
        } else {
            true
        };

        if valid_startup_hook_callsite && !loader_injected_in_appdomain {
            let runtime_info_borrow = self.runtime_info.borrow();
            let runtime_info = runtime_info_borrow.as_ref().unwrap();

            let domain_neutral_assembly = runtime_info.is_desktop_clr() &&
                COR_LIB_MODULE_LOADED.load(Ordering::SeqCst) &&
                COR_APP_DOMAIN_ID.load(Ordering::SeqCst) == module_metadata.appdomain_id;

            log::info!(
                "JITCompilationStarted: Startup hook registered in function_id={} token={} name={}() assembly_name={} app_domain_id={} domain_neutral={}",
                function_id,
                &function_info.token,
                &caller.full_name(),
                &module_metadata.assembly_name,
                &module_metadata.appdomain_id,
                domain_neutral_assembly
            );

            FIRST_JIT_COMPILATION_APP_DOMAINS.lock().unwrap().push(module_metadata.appdomain_id);

            self.run_il_startup_hook(
                &module_metadata,
                function_info.module_id,
                function_info.token)?;

            if is_desktop_iis {
                // TODO: hookup IIS module

            }
        }

        if !call_target_enabled {

            // TODO: process calls
        }

        Ok(())
    }

    fn get_module_info(&self, module_id: ModuleID) -> Option<super::types::ModuleInfo> {
        let borrow = self.profiler_info.borrow();
        let profiler_info = borrow.as_ref().unwrap();
        if let Some(module_info) = profiler_info.get_module_info_2(module_id).ok() {
            if let Some(assembly_info) = profiler_info
                .get_assembly_info(module_info.assembly_id)
                .ok()
            {
                if let Some(app_domain_info) = profiler_info
                    .get_app_domain_info(assembly_info.app_domain_id)
                    .ok()
                {
                    return Some(super::types::ModuleInfo {
                        id: module_id,
                        assembly: super::types::AssemblyInfo {
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

    fn load_integrations(&self) -> Result<Vec<Integration>, HRESULT> {
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

    fn read_calltarget_env_var(&self) -> bool {
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
                _ => true,
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

    fn run_il_startup_hook(
        &self,
        module_metadata: &ModuleMetadata,
        module_id: ModuleID,
        function_token: mdToken) -> Result<(), HRESULT> {

        let startup_method_def = self.generate_void_il_startup_method(module_id, module_metadata)?;

        // TODO: finish

        Ok(())
    }

    fn generate_void_il_startup_method(&self, module_id: ModuleID, module_metadata: &ModuleMetadata) -> Result<mdMethodDef, HRESULT> {
        let mscorlib_ref = self.create_assembly_ref_to_mscorlib(&module_metadata.assembly_emit)?;

        log::trace!("generate_void_il_startup_method: created mscorlib ref");

        // TODO: implement
        Ok(mdMethodDefNil)
    }

    fn create_assembly_ref_to_mscorlib(&self, assembly_emit: &IMetaDataAssemblyEmit) -> Result<mdAssemblyRef, HRESULT> {
        let assembly_metadata = ASSEMBLYMETADATA {
            usMajorVersion: 4,
            usMinorVersion: 0,
            usBuildNumber: 0,
            usRevisionNumber: 0,
            szLocale: std::ptr::null_mut(),
            cbLocale: 0,
            rProcessor: std::ptr::null_mut(),
            ulProcessor: 0,
            rOS: std::ptr::null_mut(),
            ulOS: 0
        };

        let public_key: &[u8; 8] = &[0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89];
        assembly_emit.define_assembly_ref(public_key, "mscorlib", assembly_metadata, &[], CorAssemblyFlags::afPA_None)
    }
}
